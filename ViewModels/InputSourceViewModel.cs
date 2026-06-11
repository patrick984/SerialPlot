using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using SerialPlot.Models;
using SerialPlot.Services;

namespace SerialPlot.ViewModels;

public partial class InputSourceViewModel : ViewModelBase, IAsyncDisposable
{
    private readonly InputSourceConfig _config;
    private readonly ICsvLineSource _source;
    private readonly CsvStreamParser _parser;
    private readonly CancellationTokenSource _cancellation = new();
    private readonly object _gate = new();
    private readonly object _uiNotificationGate = new();
    private CsvSchema? _schema;
    private ColumnState[] _columnStates = [];
    private Task? _readerTask;
    private bool _appendNotificationQueued;
    private bool _appendNotificationPending;
    private bool _eligibilityUpdatePending;
    private long _pendingAppendBufferVersion;
    private bool _updatingChannelSelection;

    private static readonly TimeSpan MinimumAppendNotificationInterval = TimeSpan.FromMilliseconds(33);

    public event EventHandler<SourceDataChangedEventArgs>? DataChanged;

    public InputSourceViewModel(InputSourceConfig config)
        : this(config, CsvLineSourceFactory.Create(config))
    {
    }

    public InputSourceViewModel(InputSourceConfig config, ICsvLineSource source)
    {
        _config = config;
        _source = source;
        _parser = new CsvStreamParser(config.TimestampUnit);
        Buffer = new PlotBuffer(config.BufferSize);
        RawCsv = new RawCsvBuffer(config.BufferSize + 1);
        DisplayName = string.IsNullOrWhiteSpace(config.Name) ? "Source" : config.Name;
        Status = $"Waiting for CSV header from {DescribeSource(config.Source)}...";
    }

    public ObservableCollection<ChannelViewModel> Channels { get; } = [];
    public PlotBuffer Buffer { get; }
    public RawCsvBuffer RawCsv { get; }
    public int BufferCapacity => _config.BufferSize;

    [ObservableProperty]
    private string _displayName;

    [ObservableProperty]
    private ChannelViewModel? _selectedXChannel;

    [ObservableProperty]
    private string _status;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _hasError;

    [ObservableProperty]
    private bool _isStopped;

    public IReadOnlyList<ChannelViewModel> SelectedLeftChannels => Channels.Where(x => x.IsSelectedLeft && x.CanBeY).ToArray();
    public IReadOnlyList<ChannelViewModel> SelectedRightChannels => Channels.Where(x => x.IsSelectedRight && x.CanBeY).ToArray();

    public void Start()
    {
        _readerTask ??= Task.Factory.StartNew(
            ReadLoopAsync,
            _cancellation.Token,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default).Unwrap();
    }

    public void Clear()
    {
        lock (_gate)
        {
            Buffer.Clear();
            RawCsv.Clear();
        }

        RaiseDataChanged(PlotDataChangeKind.Clear);
    }

    public int CopyValidPairs(ChannelViewModel yChannel, double[] xs, double[] ys)
    {
        var x = SelectedXChannel;
        if (x is null)
        {
            return 0;
        }

        lock (_gate)
        {
            return Buffer.CopyValidPairs(x.Index, yChannel.Index, xs, ys);
        }
    }

    public int CopyValidPairsSince(long afterVersion, ChannelViewModel yChannel, double[] xs, double[] ys)
    {
        var x = SelectedXChannel;
        if (x is null)
        {
            return 0;
        }

        lock (_gate)
        {
            return Buffer.CopyValidPairsSince(afterVersion, x.Index, yChannel.Index, xs, ys);
        }
    }

    public bool IsBufferVersionAvailable(long version)
    {
        lock (_gate)
        {
            return Buffer.Count == 0 || version >= Buffer.OldestVersion;
        }
    }

    public string GetRawCsvText()
    {
        lock (_gate)
        {
            return RawCsv.ToCsvText();
        }
    }

    partial void OnSelectedXChannelChanged(ChannelViewModel? value)
    {
        RaiseDataChanged(PlotDataChangeKind.XChannelChanged);
    }

    private async Task ReadLoopAsync()
    {
        try
        {
            await foreach (var line in _source.ReadLinesAsync(_cancellation.Token).ConfigureAwait(false))
            {
                if (_schema is null)
                {
                    var schema = _parser.ParseHeader(line);
                    _schema = schema;
                    lock (_gate)
                    {
                        RawCsv.Add(line);
                        _columnStates = schema.Headers.Select(x => new ColumnState(x)).ToArray();
                    }

                    QueueImmediateUiNotification(PlotDataChangeKind.Rebuild);
                    continue;
                }

                var parsed = _parser.ParseRow(_schema, line);
                var eligibilityChanged = false;
                lock (_gate)
                {
                    RawCsv.Add(parsed.RawLine);
                    Buffer.Add(parsed.Cells);
                    for (var i = 0; i < parsed.Cells.Count; i++)
                    {
                        var canBeX = _columnStates[i].CanBeX;
                        var canBeY = _columnStates[i].CanBeY;
                        _columnStates[i].Observe(parsed.Cells[i]);
                        eligibilityChanged |= canBeX != _columnStates[i].CanBeX || canBeY != _columnStates[i].CanBeY;
                    }
                }

                QueueAppendNotification(eligibilityChanged);
            }

            if (!_cancellation.IsCancellationRequested)
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    IsStopped = true;
                    Status = _schema is null ? "Stream ended before CSV header." : "Stream ended.";
                    RaiseDataChanged(PlotDataChangeKind.Rebuild);
                });
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                ErrorMessage = ex.Message;
                HasError = true;
                IsStopped = true;
                Status = "Stream stopped.";
                RaiseDataChanged(PlotDataChangeKind.Rebuild);
            });
        }
    }

    private void InitializeChannels(CsvSchema schema)
    {
        Channels.Clear();
        for (var i = 0; i < schema.Headers.Count; i++)
        {
            var channel = new ChannelViewModel(schema.Headers[i], i);
            channel.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName is nameof(ChannelViewModel.IsSelectedLeft) or nameof(ChannelViewModel.IsSelectedRight))
                {
                    ApplyExclusiveAxisSelection(channel, args.PropertyName);
                    OnPropertyChanged(nameof(SelectedLeftChannels));
                    OnPropertyChanged(nameof(SelectedRightChannels));
                    RaiseDataChanged(PlotDataChangeKind.SelectionChanged);
                }
            };
            Channels.Add(channel);
        }

        SelectedXChannel = Channels.FirstOrDefault(x => string.Equals(x.Name, _config.InitialX, StringComparison.Ordinal));
        foreach (var channel in Channels)
        {
            channel.IsSelectedLeft = _config.InitialYLeft.Contains(channel.Name, StringComparer.Ordinal);
            channel.IsSelectedRight = _config.InitialYRight.Contains(channel.Name, StringComparer.Ordinal);
        }

        Status = "Header read; waiting for selectable data.";
        UpdateEligibility();
    }

    private void ApplyExclusiveAxisSelection(ChannelViewModel channel, string? propertyName)
    {
        if (_updatingChannelSelection)
        {
            return;
        }

        try
        {
            _updatingChannelSelection = true;
            if (propertyName == nameof(ChannelViewModel.IsSelectedLeft) && channel.IsSelectedLeft)
            {
                channel.IsSelectedRight = false;
            }
            else if (propertyName == nameof(ChannelViewModel.IsSelectedRight) && channel.IsSelectedRight)
            {
                channel.IsSelectedLeft = false;
            }
        }
        finally
        {
            _updatingChannelSelection = false;
        }
    }

    private void UpdateEligibility()
    {
        lock (_gate)
        {
            for (var i = 0; i < Channels.Count; i++)
            {
                Channels[i].Apply(_columnStates[i]);
            }
        }

        if (SelectedXChannel is null)
        {
            SelectedXChannel = Channels.FirstOrDefault(x => x.CanBeX);
        }

        OnPropertyChanged(nameof(SelectedLeftChannels));
        OnPropertyChanged(nameof(SelectedRightChannels));
    }

    private void QueueImmediateUiNotification(PlotDataChangeKind kind)
    {
        long version;
        lock (_gate)
        {
            version = Buffer.Version;
        }

        Dispatcher.UIThread.Post(() =>
        {
            if (kind == PlotDataChangeKind.Rebuild && _schema is { } schema && Channels.Count == 0)
            {
                InitializeChannels(schema);
            }

            RaiseDataChanged(kind, version);
        });
    }

    private void QueueAppendNotification(bool eligibilityChanged)
    {
        long version;
        lock (_gate)
        {
            version = Buffer.Version;
        }

        lock (_uiNotificationGate)
        {
            _appendNotificationPending = true;
            _eligibilityUpdatePending |= eligibilityChanged;
            _pendingAppendBufferVersion = version;
            if (_appendNotificationQueued)
            {
                return;
            }

            _appendNotificationQueued = true;
        }

        _ = ProcessAppendNotificationAfterDelayAsync();
    }

    private async Task ProcessAppendNotificationAfterDelayAsync()
    {
        try
        {
            await Task.Delay(MinimumAppendNotificationInterval, _cancellation.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        Dispatcher.UIThread.Post(ProcessQueuedAppendNotification);
    }

    private void ProcessQueuedAppendNotification()
    {
        long version;
        bool updateEligibility;
        lock (_uiNotificationGate)
        {
            version = _pendingAppendBufferVersion;
            updateEligibility = _eligibilityUpdatePending;
            _appendNotificationPending = false;
            _eligibilityUpdatePending = false;
            _appendNotificationQueued = false;
        }

        if (_schema is { } schema && Channels.Count == 0)
        {
            InitializeChannels(schema);
            RaiseDataChanged(PlotDataChangeKind.Rebuild, version);
            return;
        }

        if (updateEligibility)
        {
            UpdateEligibility();
        }

        if (_appendNotificationPending)
        {
            QueueAppendNotification(eligibilityChanged: false);
        }

        RaiseDataChanged(PlotDataChangeKind.Append, version);
    }

    private void RaiseDataChanged(PlotDataChangeKind kind)
        => DataChanged?.Invoke(this, new SourceDataChangedEventArgs(this, kind, Buffer.Version));

    private void RaiseDataChanged(PlotDataChangeKind kind, long bufferVersion)
        => DataChanged?.Invoke(this, new SourceDataChangedEventArgs(this, kind, bufferVersion));

    public async ValueTask DisposeAsync()
    {
        _cancellation.Cancel();
        if (_readerTask is not null)
        {
            try { await _readerTask.ConfigureAwait(false); }
            catch (OperationCanceledException) { }
        }

        await _source.DisposeAsync().ConfigureAwait(false);
        _cancellation.Dispose();
    }

    private static string DescribeSource(SourceType source) => source switch
    {
        SourceType.Stdin => "standard input",
        SourceType.Serial => "serial port",
        SourceType.Tcp => "TCP socket",
        SourceType.Udp => "UDP socket",
        SourceType.Test => "test generator",
        _ => "stream",
    };
}

public sealed class SourceDataChangedEventArgs(InputSourceViewModel source, PlotDataChangeKind kind, long bufferVersion) : EventArgs
{
    public InputSourceViewModel Source { get; } = source;
    public PlotDataChangeKind Kind { get; } = kind;
    public long BufferVersion { get; } = bufferVersion;
}

public sealed record TraceSelection(InputSourceViewModel Source, ChannelViewModel Channel, TraceAxisSide Side);
