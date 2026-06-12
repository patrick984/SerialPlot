using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using SerialPlot.Models;
using SerialPlot.Services;

namespace SerialPlot.ViewModels;

public partial class SetupWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _sourceName = "Source";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowSerialSettings))]
    [NotifyPropertyChangedFor(nameof(ShowNetworkSettings))]
    [NotifyPropertyChangedFor(nameof(ShowUdpMessage))]
    private SourceType _source = SourceType.Stdin;

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

    private static IReadOnlyList<string> Split(string value)
        => value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
