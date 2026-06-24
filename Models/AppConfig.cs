using System;
using System.Collections.Generic;

namespace SerialPlot.Models;

public enum SourceType
{
    Stdin,
    Serial,
    Tcp,
    Udp,
    Test,
}

public enum TimestampUnit
{
    Auto,
    Seconds,
    Milliseconds,
    Microseconds,
    Nanoseconds,
}

public sealed record AppConfig(
    SourceType Source,
    string? SerialPort,
    int? Baud,
    string? Host,
    int? Port,
    string? UdpMessage,
    int? UdpResendIntervalSeconds,
    int BufferSize,
    TimestampUnit TimestampUnit,
    string? InitialX,
    IReadOnlyList<string> InitialYLeft,
    IReadOnlyList<string> InitialYRight)
{
    public const int DefaultBufferSize = 100_000;
    public IReadOnlyList<InputSourceConfig> Sources { get; init; } =
    [
        new(
            "Source 1",
            Source,
            SerialPort,
            Baud,
            Host,
            Port,
            UdpMessage,
            UdpResendIntervalSeconds,
            BufferSize,
            TimestampUnit,
            InitialX,
            InitialYLeft,
            InitialYRight),
    ];

    public static AppConfig Defaults() => new(
        SourceType.Stdin,
        null,
        null,
        null,
        null,
        null,
        null,
        DefaultBufferSize,
        TimestampUnit.Auto,
        null,
        Array.Empty<string>(),
        Array.Empty<string>());
}

public sealed record InputSourceConfig(
    string Name,
    SourceType Source,
    string? SerialPort,
    int? Baud,
    string? Host,
    int? Port,
    string? UdpMessage,
    int? UdpResendIntervalSeconds,
    int BufferSize,
    TimestampUnit TimestampUnit,
    string? InitialX,
    IReadOnlyList<string> InitialYLeft,
    IReadOnlyList<string> InitialYRight);

public sealed record ConfigParseResult(AppConfig Config, bool IsComplete, string? Error, bool HadAnyArgs);
