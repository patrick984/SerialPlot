using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using SerialPlot.Models;
using SerialPlot.Services;

namespace SerialPlot.ViewModels;

public partial class SetupWindowViewModel : ViewModelBase
{
    private readonly RecentSetupHistory _recentSetupHistory;
    private bool _applyingRecentSetup;

    [ObservableProperty]
    private string _sourceName = "Source";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowSerialSettings))]
    [NotifyPropertyChangedFor(nameof(ShowNetworkSettings))]
    [NotifyPropertyChangedFor(nameof(ShowUdpMessage))]
    private SourceType _source = SourceType.Stdin;

    [ObservableProperty]
    private RecentSetupOption? _selectedRecentSetup;

    [ObservableProperty]
    private string _serialPort = string.Empty;

    [ObservableProperty]
    private int _baud = 115200;

    [ObservableProperty]
    private string _host = "127.0.0.1";

    [ObservableProperty]
    private int _port = 5000;

    [ObservableProperty]
    private string _udpMessage = string.Empty;

    [ObservableProperty]
    private int _udpResendIntervalSeconds;

    [ObservableProperty]
    private int _bufferSize = AppConfig.DefaultBufferSize;

    [ObservableProperty]
    private TimestampUnit _timestampUnit = TimestampUnit.Auto;

    [ObservableProperty]
    private string _initialX = string.Empty;

    [ObservableProperty]
    private string _initialYLeft = string.Empty;

    [ObservableProperty]
    private string _initialYRight = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string? _errorMessage;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool ShowSerialSettings => Source is SourceType.Serial;
    public bool ShowNetworkSettings => Source is SourceType.Tcp or SourceType.Udp;
    public bool ShowUdpMessage => Source is SourceType.Udp;

    public IReadOnlyList<SourceType> Sources { get; } = Enum.GetValues<SourceType>();
    public IReadOnlyList<TimestampUnit> TimestampUnits { get; } = Enum.GetValues<TimestampUnit>();
    public ObservableCollection<RecentSetupOption> RecentSetups { get; } = [];
    public bool HasRecentSetups => RecentSetups.Count > 0;

    public SetupWindowViewModel()
        : this(null, RecentSetupHistory.Empty)
    {
    }

    public SetupWindowViewModel(AppConfig? initialConfig, RecentSetupHistory recentSetupHistory)
    {
        _recentSetupHistory = recentSetupHistory.Normalize();

        if (initialConfig is { } config)
        {
            ApplyConfig(config.Sources[0]);
            RefreshRecentSetups(selectMostRecent: false);
            return;
        }

        Source = _recentSetupHistory.LastSource;
        RefreshRecentSetups(selectMostRecent: true);
    }

    public AppConfig ToConfig()
    {
        var sourceConfig = new InputSourceConfig(
            string.IsNullOrWhiteSpace(SourceName) ? "Source" : SourceName.Trim(),
            Source,
            string.IsNullOrWhiteSpace(SerialPort) ? null : SerialPort.Trim(),
            Baud,
            string.IsNullOrWhiteSpace(Host) ? null : Host.Trim(),
            Port,
            Source == SourceType.Udp ? UdpMessage : null,
            Source == SourceType.Udp && UdpResendIntervalSeconds > 0 ? UdpResendIntervalSeconds : null,
            BufferSize,
            TimestampUnit,
            string.IsNullOrWhiteSpace(InitialX) ? null : InitialX.Trim(),
            Split(InitialYLeft),
            Split(InitialYRight));

        return new AppConfig(
            sourceConfig.Source,
            sourceConfig.SerialPort,
            sourceConfig.Baud,
            sourceConfig.Host,
            sourceConfig.Port,
            sourceConfig.UdpMessage,
            sourceConfig.UdpResendIntervalSeconds,
            sourceConfig.BufferSize,
            sourceConfig.TimestampUnit,
            sourceConfig.InitialX,
            sourceConfig.InitialYLeft,
            sourceConfig.InitialYRight)
        {
            Sources = [sourceConfig],
        };
    }

    public bool TryBuild(out AppConfig config)
    {
        config = ToConfig();
        ErrorMessage = CliConfigParser.Validate(config);
        return ErrorMessage is null;
    }

    partial void OnSourceChanged(SourceType value)
    {
        if (_applyingRecentSetup)
        {
            return;
        }

        RefreshRecentSetups(selectMostRecent: false);
    }

    partial void OnSelectedRecentSetupChanged(RecentSetupOption? value)
    {
        if (_applyingRecentSetup || value is null)
        {
            return;
        }

        ApplyConfig(value.Config);
    }

    private void RefreshRecentSetups(bool selectMostRecent)
    {
        _applyingRecentSetup = true;
        RecentSetups.Clear();
        foreach (var entry in _recentSetupHistory.Entries
            .Where(x => x.Config.Source == Source)
            .OrderByDescending(x => x.LastUsedUtc))
        {
            RecentSetups.Add(new RecentSetupOption(Describe(entry.Config), entry.Config));
        }

        SelectedRecentSetup = selectMostRecent ? RecentSetups.FirstOrDefault() : null;
        OnPropertyChanged(nameof(HasRecentSetups));
        _applyingRecentSetup = false;

        if (selectMostRecent && SelectedRecentSetup is { } selected)
        {
            ApplyConfig(selected.Config);
        }
    }

    private void ApplyConfig(InputSourceConfig config)
    {
        _applyingRecentSetup = true;
        SourceName = config.Name;
        Source = config.Source;
        SerialPort = config.SerialPort ?? string.Empty;
        Baud = config.Baud ?? 115200;
        Host = config.Host ?? "127.0.0.1";
        Port = config.Port ?? 5000;
        UdpMessage = config.UdpMessage ?? string.Empty;
        UdpResendIntervalSeconds = config.UdpResendIntervalSeconds ?? 0;
        BufferSize = config.BufferSize;
        TimestampUnit = config.TimestampUnit;
        InitialX = config.InitialX ?? string.Empty;
        InitialYLeft = string.Join(", ", config.InitialYLeft);
        InitialYRight = string.Join(", ", config.InitialYRight);
        ErrorMessage = null;
        _applyingRecentSetup = false;

        RefreshRecentSetups(selectMostRecent: false);
    }

    private static string Describe(InputSourceConfig config)
    {
        var endpoint = config.Source switch
        {
            SourceType.Serial => string.IsNullOrWhiteSpace(config.SerialPort)
                ? "Serial"
                : $"{config.SerialPort} @ {config.Baud}",
            SourceType.Tcp => $"{config.Host}:{config.Port}",
            SourceType.Udp => $"{config.Host}:{config.Port}",
            SourceType.Stdin => "Standard input",
            SourceType.Test => "Test source",
            _ => config.Source.ToString(),
        };

        return string.IsNullOrWhiteSpace(config.Name)
            ? endpoint
            : $"{config.Name} - {endpoint}";
    }

    private static IReadOnlyList<string> Split(string value)
        => value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}

public sealed record RecentSetupOption(string Label, InputSourceConfig Config)
{
    public override string ToString() => Label;
}
