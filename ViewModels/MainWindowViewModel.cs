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
    private static readonly XAutoscaleModeOption[] XAutoscaleModeOptionValues =
    [
        new(XAutoscaleMode.ContinuousFollowNewest, "Continuous Follow"),
        new(XAutoscaleMode.SteppedExpansion, "Stepped Expand"),
        new(XAutoscaleMode.SteppedPan, "Stepped Pan"),
    ];

    private readonly AppConfig _config;
    private readonly CancellationTokenSource _cancellation = new();
    private readonly Stopwatch _plotUpdateClock = Stopwatch.StartNew();
    private readonly UserPreferencesService _preferencesService;
    private readonly Dictionary<InputSourceViewModel, long> _dirtySourceVersions = [];
    private Task? _preferencesLoadTask;
    private bool _loadingPreferences;
    private bool _updatingSelectedSource;
    private bool _started;

    private static readonly TimeSpan MinimumPlotUpdateInterval = TimeSpan.FromMilliseconds(33);

    public event EventHandler<PlotDataChangedEventArgs>? PlotDataChanged;

    public ObservableCollection<InputSourceViewModel> Sources { get; } = [];

    [ObservableProperty]
    private ChannelViewModel? _selectedXChannel;

    [ObservableProperty]
    private InputSourceViewModel? _selectedSource;

    [ObservableProperty]
    private bool _isPaused;

    [ObservableProperty]
    private string _status = "Waiting for CSV header...";

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _hasError;

    [ObservableProperty]
    private bool _autoScaleX = true;

    [ObservableProperty]
    private bool _autoScaleLeftY = true;

    [ObservableProperty]
    private bool _autoScaleRightY = true;

    [ObservableProperty]
    private XAutoscaleMode _xAutoscaleMode = XAutoscaleMode.ContinuousFollowNewest;

    [ObservableProperty]
    private XAutoscaleModeOption _selectedXAutoscaleModeOption = XAutoscaleModeOptionValues[0];

    [ObservableProperty]
    private int _steppedFutureSpaceSeconds = UserPreferences.DefaultSteppedFutureSpaceSeconds;

    public ObservableCollection<ChannelViewModel> Channels => SelectedSource?.Channels ?? [];
    public int BufferCapacity => Sources.Count == 0 ? _config.BufferSize : Sources.Max(x => x.BufferCapacity);
    public IReadOnlyList<XAutoscaleModeOption> XAutoscaleModeOptions { get; } = XAutoscaleModeOptionValues;
    public string PauseButtonText => IsPaused ? "Resume" : "Pause";
    public bool IsSteppedExpansionSelected => XAutoscaleMode is XAutoscaleMode.SteppedExpansion;

    public MainWindowViewModel()
        : this(AppConfig.Defaults(), new TextReaderLineSource(TextReader.Null), new UserPreferencesService())
    {
    }

    public MainWindowViewModel(AppConfig config)
        : this(config, CreateSources(config), new UserPreferencesService())
    {
    }

    public MainWindowViewModel(AppConfig config, ICsvLineSource source)
        : this(config, [new InputSourceViewModel(config.Sources[0], source)], new UserPreferencesService())
    {
    }

    public MainWindowViewModel(AppConfig config, ICsvLineSource source, UserPreferencesService preferencesService)
        : this(config, [new InputSourceViewModel(config.Sources[0], source)], preferencesService)
    {
    }

    public MainWindowViewModel(AppConfig config, IEnumerable<InputSourceViewModel> sources, UserPreferencesService preferencesService)
    {
        _config = config;
        _preferencesService = preferencesService;
        foreach (var source in sources)
        {
            AddSource(source);
        }

        SelectedSource = Sources.FirstOrDefault();
    }

    public void Start()
    {
        Status = Sources.Count == 0 ? "No sources configured." : "Waiting for CSV headers...";
        _preferencesLoadTask ??= LoadPreferencesAsync();
        _ = StartSourcesAfterPreferencesAsync();
    }

    private async Task StartSourcesAfterPreferencesAsync()
    {
        if (_preferencesLoadTask is not null)
        {
            await _preferencesLoadTask.ConfigureAwait(false);
        }

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            foreach (var source in Sources)
            {
                source.Start();
            }

            _started = true;
        });
    }

    public (double[] Xs, double[] Ys) GetSeries(ChannelViewModel yChannel)
    {
        return SelectedSource?.Buffer.GetSeries(SelectedSource.SelectedXChannel?.Index ?? -1, yChannel.Index) ?? ([], []);
    }

    public int CopySeries(ChannelViewModel yChannel, double[] xs, double[] ys)
    {
        var source = SelectedSource;
        var x = source?.SelectedXChannel;
        return source is null || x is null ? 0 : source.Buffer.CopySeries(x.Index, yChannel.Index, xs, ys);
    }

    public int CopyValidPairs(ChannelViewModel yChannel, double[] xs, double[] ys)
    {
        return SelectedSource?.CopyValidPairs(yChannel, xs, ys) ?? 0;
    }

    public int CopyValidPairsSince(long afterVersion, ChannelViewModel yChannel, double[] xs, double[] ys)
    {
        return SelectedSource?.CopyValidPairsSince(afterVersion, yChannel, xs, ys) ?? 0;
    }

    public bool IsBufferVersionAvailable(long version)
    {
        return SelectedSource?.IsBufferVersionAvailable(version) ?? true;
    }

    public int CopyValidPairs(InputSourceViewModel source, ChannelViewModel yChannel, double[] xs, double[] ys)
        => source.CopyValidPairs(yChannel, xs, ys);

    public int CopyValidPairsSince(InputSourceViewModel source, long afterVersion, ChannelViewModel yChannel, double[] xs, double[] ys)
        => source.CopyValidPairsSince(afterVersion, yChannel, xs, ys);

    public bool IsBufferVersionAvailable(InputSourceViewModel source, long version)
        => source.IsBufferVersionAvailable(version);

    public IReadOnlyList<ChannelViewModel> SelectedLeftChannels => SelectedSource?.SelectedLeftChannels ?? [];
    public IReadOnlyList<ChannelViewModel> SelectedRightChannels => SelectedSource?.SelectedRightChannels ?? [];
    public IReadOnlyList<TraceSelection> SelectedTraces => Sources
        .SelectMany(source => source.SelectedLeftChannels.Select(channel => new TraceSelection(source, channel, TraceAxisSide.Left))
            .Concat(source.SelectedRightChannels.Select(channel => new TraceSelection(source, channel, TraceAxisSide.Right))))
        .ToArray();

    [RelayCommand]
    private void TogglePause()
    {
        IsPaused = !IsPaused;
        Status = IsPaused ? "Plot paused; acquisition continues." : "Streaming.";
    }

    partial void OnIsPausedChanged(bool value)
    {
        OnPropertyChanged(nameof(PauseButtonText));
        if (!value)
        {
            RaiseDirtyAppendIfAllowed();
        }
    }

    [RelayCommand]
    public void Clear()
    {
        foreach (var source in Sources)
        {
            source.Clear();
        }

        RaisePlotDataChanged(PlotDataChangeKind.Clear);
    }

    [RelayCommand]
    public void Autoscale()
    {
        AutoScaleX = true;
        AutoScaleLeftY = true;
        AutoScaleRightY = true;
        RaisePlotDataChanged(PlotDataChangeKind.Autoscale);
    }

    public async Task SaveRawCsvAsync(string path)
    {
        if (Sources.Count <= 1)
        {
            await File.WriteAllTextAsync(path, Sources.FirstOrDefault()?.GetRawCsvText() ?? string.Empty, _cancellation.Token).ConfigureAwait(false);
            return;
        }

        Directory.CreateDirectory(path);
        foreach (var source in Sources)
        {
            var fileName = SanitizeFileName(source.DisplayName) + ".csv";
            await File.WriteAllTextAsync(Path.Combine(path, fileName), source.GetRawCsvText(), _cancellation.Token).ConfigureAwait(false);
        }
    }

    partial void OnSelectedXChannelChanged(ChannelViewModel? value)
    {
        if (!_updatingSelectedSource && SelectedSource is { } source && source.SelectedXChannel != value)
        {
            source.SelectedXChannel = value;
        }
    }

    partial void OnSelectedSourceChanged(InputSourceViewModel? value)
    {
        try
        {
            _updatingSelectedSource = true;
            OnPropertyChanged(nameof(Channels));
            SelectedXChannel = value?.SelectedXChannel;
            OnPropertyChanged(nameof(SelectedLeftChannels));
            OnPropertyChanged(nameof(SelectedRightChannels));
        }
        finally
        {
            _updatingSelectedSource = false;
        }
    }

    partial void OnAutoScaleXChanged(bool value) => RaisePlotDataChanged(PlotDataChangeKind.Autoscale);

    partial void OnAutoScaleLeftYChanged(bool value) => RaisePlotDataChanged(PlotDataChangeKind.Autoscale);

    partial void OnAutoScaleRightYChanged(bool value) => RaisePlotDataChanged(PlotDataChangeKind.Autoscale);

    partial void OnXAutoscaleModeChanged(XAutoscaleMode value)
    {
        var option = GetXAutoscaleModeOption(value);
        if (SelectedXAutoscaleModeOption != option)
        {
            SelectedXAutoscaleModeOption = option;
        }

        OnPropertyChanged(nameof(IsSteppedExpansionSelected));
        RaisePlotDataChanged(PlotDataChangeKind.Autoscale);
        if (!_loadingPreferences)
        {
            _ = SavePreferencesAsync();
        }
    }

    partial void OnSelectedXAutoscaleModeOptionChanged(XAutoscaleModeOption value)
    {
        if (XAutoscaleMode != value.Mode)
        {
            XAutoscaleMode = value.Mode;
        }
    }

    partial void OnSteppedFutureSpaceSecondsChanged(int value)
    {
        var clamped = UserPreferences.ClampSteppedFutureSpaceSeconds(value);
        if (clamped != value)
        {
            SteppedFutureSpaceSeconds = clamped;
            return;
        }

        RaisePlotDataChanged(PlotDataChangeKind.Autoscale);
        if (!_loadingPreferences)
        {
            _ = SavePreferencesAsync();
        }
    }

    private async Task LoadPreferencesAsync()
    {
        var preferences = await _preferencesService.LoadAsync().ConfigureAwait(false);
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            try
            {
                _loadingPreferences = true;
                XAutoscaleMode = preferences.XAutoscaleMode;
                SteppedFutureSpaceSeconds = preferences.SteppedFutureSpaceSeconds;
            }
            finally
            {
                _loadingPreferences = false;
            }
        });
    }

    private async Task SavePreferencesAsync()
    {
        try
        {
            await _preferencesService.SaveAsync(new UserPreferences(XAutoscaleMode, SteppedFutureSpaceSeconds)).ConfigureAwait(false);
        }
        catch
        {
        }
    }

    private static XAutoscaleModeOption GetXAutoscaleModeOption(XAutoscaleMode mode)
        => XAutoscaleModeOptionValues.FirstOrDefault(x => x.Mode == mode) ?? XAutoscaleModeOptionValues[0];

    private bool ShouldUpdatePlot()
    {
        if (_plotUpdateClock.Elapsed < MinimumPlotUpdateInterval)
        {
            return false;
        }

        _plotUpdateClock.Restart();
        return true;
    }

    private void RaisePlotDataChanged(PlotDataChangeKind kind)
    {
        if (kind is PlotDataChangeKind.Clear)
        {
            _dirtySourceVersions.Clear();
        }

        PlotDataChanged?.Invoke(this, new PlotDataChangedEventArgs(kind, Sources.Sum(x => x.Buffer.Version)));
    }

    private void RaisePlotDataChanged(SourceDataChangedEventArgs sourceArgs)
    {
        if (sourceArgs.Kind == PlotDataChangeKind.Append)
        {
            _dirtySourceVersions[sourceArgs.Source] = sourceArgs.BufferVersion;
            if (IsPaused)
            {
                return;
            }

            if (!ShouldUpdatePlot())
            {
                return;
            }
        }

        if (sourceArgs.Kind is PlotDataChangeKind.SelectionChanged or PlotDataChangeKind.XChannelChanged or PlotDataChangeKind.Rebuild)
        {
            OnPropertyChanged(nameof(SelectedTraces));
        }

        if (ReferenceEquals(sourceArgs.Source, SelectedSource))
        {
            try
            {
                _updatingSelectedSource = true;
                OnPropertyChanged(nameof(Channels));
                SelectedXChannel = sourceArgs.Source.SelectedXChannel;
                OnPropertyChanged(nameof(SelectedLeftChannels));
                OnPropertyChanged(nameof(SelectedRightChannels));
            }
            finally
            {
                _updatingSelectedSource = false;
            }
        }

        Status = BuildStatus();
        HasError = Sources.Any(x => x.HasError);
        ErrorMessage = string.Join(Environment.NewLine, Sources.Where(x => x.HasError).Select(x => $"{x.DisplayName}: {x.ErrorMessage}"));

        if (sourceArgs.Kind == PlotDataChangeKind.Append)
        {
            RaiseDirtyAppend();
            return;
        }

        _dirtySourceVersions.Remove(sourceArgs.Source);
        PlotDataChanged?.Invoke(this, new PlotDataChangedEventArgs(sourceArgs.Source, sourceArgs.Kind, sourceArgs.BufferVersion, Sources.Sum(x => x.Buffer.Version)));
    }

    private void RaiseDirtyAppendIfAllowed()
    {
        if (_dirtySourceVersions.Count == 0 || IsPaused || !ShouldUpdatePlot())
        {
            return;
        }

        RaiseDirtyAppend();
    }

    private void RaiseDirtyAppend()
    {
        var dirtySources = new Dictionary<InputSourceViewModel, long>(_dirtySourceVersions);
        foreach (var source in dirtySources.Keys)
        {
            _dirtySourceVersions.Remove(source);
        }

        PlotDataChanged?.Invoke(this, new PlotDataChangedEventArgs(
            PlotDataChangeKind.Append,
            dirtySources,
            Sources.Sum(x => x.Buffer.Version)));
    }

    public async ValueTask DisposeAsync()
    {
        _cancellation.Cancel();
        if (_preferencesLoadTask is not null)
        {
            try { await _preferencesLoadTask.ConfigureAwait(false); }
            catch { }
        }

        foreach (var source in Sources.ToArray())
        {
            await source.DisposeAsync().ConfigureAwait(false);
        }
        _cancellation.Dispose();
    }

    public void AddSource(InputSourceViewModel source)
    {
        source.DataChanged += (_, args) => RaisePlotDataChanged(args);
        source.PropertyChanged += (_, args) =>
        {
            if (ReferenceEquals(source, SelectedSource) && args.PropertyName == nameof(InputSourceViewModel.SelectedXChannel))
            {
                try
                {
                    _updatingSelectedSource = true;
                    SelectedXChannel = source.SelectedXChannel;
                }
                finally
                {
                    _updatingSelectedSource = false;
                }
            }
        };
        Sources.Add(source);
        if (_started)
        {
            source.Start();
        }

        SelectedSource = source;
    }

    public void AddSource(InputSourceConfig config)
    {
        var source = new InputSourceViewModel(config);
        AddSource(source);
        SelectedSource = source;
        RaisePlotDataChanged(PlotDataChangeKind.SelectionChanged);
    }

    public async Task RemoveSourceAsync(InputSourceViewModel source)
    {
        if (!Sources.Remove(source))
        {
            return;
        }

        await source.DisposeAsync().ConfigureAwait(false);
        if (ReferenceEquals(SelectedSource, source))
        {
            SelectedSource = Sources.FirstOrDefault();
        }

        RaisePlotDataChanged(PlotDataChangeKind.SelectionChanged);
    }

    private string BuildStatus()
    {
        if (Sources.Count == 0)
        {
            return "No sources configured.";
        }

        var running = Sources.Count(x => !x.IsStopped && !x.HasError);
        var failed = Sources.Count(x => x.HasError);
        return failed == 0
            ? $"{running}/{Sources.Count} sources active."
            : $"{running}/{Sources.Count} sources active; {failed} source(s) stopped with errors.";
    }

    private static IReadOnlyList<InputSourceViewModel> CreateSources(AppConfig config)
        => config.Sources.Select(x => new InputSourceViewModel(x)).ToArray();

    private static string SanitizeFileName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var chars = value.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray();
        var sanitized = new string(chars).Trim();
        return sanitized.Length == 0 ? "source" : sanitized;
    }
}

public sealed record XAutoscaleModeOption(XAutoscaleMode Mode, string Label);

public sealed class PlotDataChangedEventArgs(PlotDataChangeKind kind, long bufferVersion) : EventArgs
{
    public PlotDataChangedEventArgs(InputSourceViewModel source, PlotDataChangeKind kind, long sourceBufferVersion, long bufferVersion)
        : this(kind, bufferVersion)
    {
        Source = source;
        SourceBufferVersion = sourceBufferVersion;
    }

    public PlotDataChangedEventArgs(PlotDataChangeKind kind, IReadOnlyDictionary<InputSourceViewModel, long> dirtySourceVersions, long bufferVersion)
        : this(kind, bufferVersion)
    {
        DirtySourceVersions = dirtySourceVersions;
    }

    public InputSourceViewModel? Source { get; }
    public PlotDataChangeKind Kind { get; } = kind;
    public long SourceBufferVersion { get; } = bufferVersion;
    public IReadOnlyDictionary<InputSourceViewModel, long> DirtySourceVersions { get; } = new Dictionary<InputSourceViewModel, long>();
    public long BufferVersion { get; } = bufferVersion;

    public bool TryGetSourceVersion(InputSourceViewModel source, out long version)
    {
        if (DirtySourceVersions.TryGetValue(source, out version))
        {
            return true;
        }

        if (ReferenceEquals(Source, source))
        {
            version = SourceBufferVersion;
            return true;
        }

        version = source.Buffer.Version;
        return Kind != PlotDataChangeKind.Append;
    }
}

public enum PlotDataChangeKind
{
    Append,
    Clear,
    SelectionChanged,
    XChannelChanged,
    Autoscale,
    Rebuild,
}
