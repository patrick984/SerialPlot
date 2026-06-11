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
    private CsvSchema? _schema;
    private ColumnState[] _columnStates = [];
    private Task? _readerTask;
    private bool _updatingChannelSelection;

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
        _readerTask ??= Task.Run(ReadLoopAsync);
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
                    }

                    await Dispatcher.UIThread.InvokeAsync(() => InitializeChannels(schema));
                    continue;
                }

                var parsed = _parser.ParseRow(_schema, line);
                lock (_gate)
                {
                    RawCsv.Add(parsed.RawLine);
                    Buffer.Add(parsed.Cells);
                    for (var i = 0; i < parsed.Cells.Count; i++)
                    {
                        _columnStates[i].Observe(parsed.Cells[i]);
                    }
                }

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    UpdateEligibility();
                    RaiseDataChanged(PlotDataChangeKind.Append);
                });
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
        _columnStates = schema.Headers.Select(x => new ColumnState(x)).ToArray();
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
        RaiseDataChanged(PlotDataChangeKind.Rebuild);
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
        for (var i = 0; i < Channels.Count; i++)
        {
            Channels[i].Apply(_columnStates[i]);
        }

        if (SelectedXChannel is null)
        {
            SelectedXChannel = Channels.FirstOrDefault(x => x.CanBeX);
        }

        OnPropertyChanged(nameof(SelectedLeftChannels));
        OnPropertyChanged(nameof(SelectedRightChannels));
    }

    private void RaiseDataChanged(PlotDataChangeKind kind)
        => DataChanged?.Invoke(this, new SourceDataChangedEventArgs(this, kind, Buffer.Version));

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
