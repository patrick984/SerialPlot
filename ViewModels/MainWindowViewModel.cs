using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SerialPlot.Models;
using SerialPlot.Services;

namespace SerialPlot.ViewModels;

public partial class MainWindowViewModel : ViewModelBase, IAsyncDisposable
{
    private readonly AppConfig _config;
    private readonly ICsvLineSource _source;
    private readonly CsvStreamParser _parser;
    private readonly CancellationTokenSource _cancellation = new();
    private readonly object _gate = new();
    private readonly Stopwatch _plotUpdateClock = Stopwatch.StartNew();
    private CsvSchema? _schema;
    private ColumnState[] _columnStates = [];
    private Task? _readerTask;
    private bool _updatingChannelSelection;

    private static readonly TimeSpan MinimumPlotUpdateInterval = TimeSpan.FromMilliseconds(33);

    public event EventHandler? PlotDataChanged;

    public ObservableCollection<ChannelViewModel> Channels { get; } = [];

    [ObservableProperty]
    private ChannelViewModel? _selectedXChannel;

    [ObservableProperty]
    private bool _isPaused;

    [ObservableProperty]
    private string _status = "Waiting for CSV header...";

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _hasError;

    [ObservableProperty]
    private bool _followNewest = true;

    public PlotBuffer Buffer { get; }
    public RawCsvBuffer RawCsv { get; }
    public int BufferCapacity => _config.BufferSize;

    public MainWindowViewModel()
        : this(AppConfig.Defaults(), new TextReaderLineSource(TextReader.Null))
    {
    }

    public MainWindowViewModel(AppConfig config)
        : this(config, CsvLineSourceFactory.Create(config))
    {
    }

    public MainWindowViewModel(AppConfig config, ICsvLineSource source)
    {
        _config = config;
        _source = source;
        _parser = new CsvStreamParser(config.TimestampUnit);
        Buffer = new PlotBuffer(config.BufferSize);
        RawCsv = new RawCsvBuffer(config.BufferSize + 1);
    }

    public void Start()
    {
        Status = $"Waiting for CSV header from {DescribeSource(_config.Source)}...";
        _readerTask ??= Task.Run(ReadLoopAsync);
    }

    public (double[] Xs, double[] Ys) GetSeries(ChannelViewModel yChannel)
    {
        var x = SelectedXChannel;
        if (x is null)
        {
            return ([], []);
        }

        lock (_gate)
        {
            return Buffer.GetSeries(x.Index, yChannel.Index);
        }
    }

    public int CopySeries(ChannelViewModel yChannel, double[] xs, double[] ys)
    {
        var x = SelectedXChannel;
        if (x is null)
        {
            return 0;
        }

        lock (_gate)
        {
            return Buffer.CopySeries(x.Index, yChannel.Index, xs, ys);
        }
    }

    public IReadOnlyList<ChannelViewModel> SelectedLeftChannels => Channels.Where(x => x.IsSelectedLeft && x.CanBeY).ToArray();
    public IReadOnlyList<ChannelViewModel> SelectedRightChannels => Channels.Where(x => x.IsSelectedRight && x.CanBeY).ToArray();

    [RelayCommand]
    private void TogglePause()
    {
        IsPaused = !IsPaused;
        Status = IsPaused ? "Plot paused; acquisition continues." : "Streaming.";
    }

    [RelayCommand]
    public void Clear()
    {
        lock (_gate)
        {
            Buffer.Clear();
        }

        PlotDataChanged?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    public void Autoscale()
    {
        FollowNewest = true;
        PlotDataChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task SaveRawCsvAsync(string path)
    {
        string text;
        lock (_gate)
        {
            text = RawCsv.ToCsvText();
        }

        await File.WriteAllTextAsync(path, text, _cancellation.Token).ConfigureAwait(false);
    }

    partial void OnSelectedXChannelChanged(ChannelViewModel? value)
    {
        PlotDataChanged?.Invoke(this, EventArgs.Empty);
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
                    if (!IsPaused && ShouldUpdatePlot())
                    {
                        PlotDataChanged?.Invoke(this, EventArgs.Empty);
                    }
                });
            }

            if (_schema is null && !_cancellation.IsCancellationRequested)
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    Status = "Stream ended before CSV header.";
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
                Status = "Stream stopped.";
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
                    PlotDataChanged?.Invoke(this, EventArgs.Empty);
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
    }

    private bool ShouldUpdatePlot()
    {
        if (_plotUpdateClock.Elapsed < MinimumPlotUpdateInterval)
        {
            return false;
        }

        _plotUpdateClock.Restart();
        return true;
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
