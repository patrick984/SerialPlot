using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.CommandLine;
using SerialPlot.Models;

namespace SerialPlot.Services;

public static class CliConfigParser
{
    public static ConfigParseResult Parse(string[] args) => Parse(args, false);

    public static ConfigParseResult Parse(string[] args, bool stdinRedirected)
    {
        if (args.Length == 0)
        {
            if (stdinRedirected)
            {
                return new ConfigParseResult(AppConfig.Defaults(), true, null, false);
            }

            return new ConfigParseResult(AppConfig.Defaults(), false, null, false);
        }

        var commandResult = BuildCommand().Parse(args);
        if (commandResult.Errors.Count > 0)
        {
            return new ConfigParseResult(AppConfig.Defaults(), false, commandResult.Errors[0].Message, true);
        }

        var values = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (!arg.StartsWith("--", StringComparison.Ordinal))
            {
                return Invalid($"Unexpected argument '{arg}'.");
            }

            var name = arg;
            string? value = null;
            var equalsIndex = arg.IndexOf('=', StringComparison.Ordinal);
            if (equalsIndex > 0)
            {
                name = arg[..equalsIndex];
                value = arg[(equalsIndex + 1)..];
            }
            else if (i + 1 < args.Length && !args[i + 1].StartsWith("--", StringComparison.Ordinal))
            {
                value = args[++i];
            }

            if (value is null)
            {
                return Invalid($"Missing value for {name}.");
            }

            if (!KnownOptions.Contains(name))
            {
                return Invalid($"Unknown option {name}.");
            }

            if (!values.TryGetValue(name, out var list))
            {
                list = [];
                values[name] = list;
            }

            list.Add(value);
        }

        if (!TryGetEnum(values, "--source", out SourceType source, ParseSourceType))
        {
            return Invalid("Missing or invalid --source.");
        }

        if (!TryGetEnum(values, "--timestamp-unit", out TimestampUnit unit, ParseTimestampUnit))
        {
            unit = TimestampUnit.Auto;
        }

        if (!TryGetPositiveInt(values, "--buffer-size", AppConfig.DefaultBufferSize, out var bufferSize))
        {
            return Invalid("--buffer-size must be a positive integer.");
        }

        if (!TryGetPositiveInt(values, "--baud", null, out var baud))
        {
            return Invalid("--baud must be a positive integer.");
        }

        if (!TryGetPort(values, out var port))
        {
            return Invalid("--port must be between 1 and 65535.");
        }

        var config = new AppConfig(
            source,
            Last(values, "--serial-port"),
            baud,
            Last(values, "--host"),
            port,
            Last(values, "--udp-message"),
            bufferSize ?? AppConfig.DefaultBufferSize,
            unit,
            Last(values, "--x"),
            SplitChannels(values, "--y-left"),
            SplitChannels(values, "--y-right"));

        var validation = Validate(config);
        return validation is null
            ? new ConfigParseResult(config, true, null, true)
            : new ConfigParseResult(config, false, validation, true);

        ConfigParseResult Invalid(string error) => new(AppConfig.Defaults(), false, error, true);
    }

    public static string? Validate(AppConfig config)
    {
        if (config.BufferSize <= 0)
        {
            return "Buffer size must be greater than zero.";
        }

        return config.Source switch
        {
            SourceType.Stdin => null,
            SourceType.Serial when string.IsNullOrWhiteSpace(config.SerialPort) => "Serial source requires a serial port.",
            SourceType.Serial when config.Baud is null or <= 0 => "Serial source requires a baud rate.",
            SourceType.Serial => null,
            SourceType.Tcp when string.IsNullOrWhiteSpace(config.Host) => "TCP source requires a host.",
            SourceType.Tcp when config.Port is null => "TCP source requires a port.",
            SourceType.Tcp => null,
            SourceType.Udp when string.IsNullOrWhiteSpace(config.Host) => "UDP source requires a host.",
            SourceType.Udp when config.Port is null => "UDP source requires a port.",
            SourceType.Udp when config.UdpMessage is null => "UDP source requires a request message.",
            SourceType.Udp => null,
            _ => "Unsupported source type.",
        };
    }

    private static readonly HashSet<string> KnownOptions = new(StringComparer.OrdinalIgnoreCase)
    {
        "--source",
        "--serial-port",
        "--baud",
        "--host",
        "--port",
        "--udp-message",
        "--buffer-size",
        "--timestamp-unit",
        "--x",
        "--y-left",
        "--y-right",
    };

    private static RootCommand BuildCommand()
    {
        var command = new RootCommand("SerialPlot live CSV plotter");
        foreach (var option in KnownOptions)
        {
            command.Add(new Option<string>(option));
        }

        return command;
    }

    private static bool TryGetEnum<T>(Dictionary<string, List<string>> values, string key, out T parsed, Func<string, T?> parser)
        where T : struct
    {
        var raw = Last(values, key);
        if (raw is null)
        {
            parsed = default;
            return false;
        }

        var result = parser(raw);
        parsed = result ?? default;
        return result.HasValue;
    }

    private static SourceType? ParseSourceType(string value) => value.ToLowerInvariant() switch
    {
        "stdin" => SourceType.Stdin,
        "serial" => SourceType.Serial,
        "tcp" => SourceType.Tcp,
        "udp" => SourceType.Udp,
        _ => null,
    };

    private static TimestampUnit? ParseTimestampUnit(string value) => value.ToLowerInvariant() switch
    {
        "auto" => TimestampUnit.Auto,
        "seconds" => TimestampUnit.Seconds,
        "milliseconds" => TimestampUnit.Milliseconds,
        "microseconds" => TimestampUnit.Microseconds,
        "nanoseconds" => TimestampUnit.Nanoseconds,
        _ => null,
    };

    private static string? Last(Dictionary<string, List<string>> values, string key)
        => values.TryGetValue(key, out var list) && list.Count > 0 ? list[^1] : null;

    private static bool TryGetPositiveInt(Dictionary<string, List<string>> values, string key, int? defaultValue, out int? parsed)
    {
        var raw = Last(values, key);
        if (raw is null)
        {
            parsed = defaultValue;
            return true;
        }

        if (int.TryParse(raw, NumberStyles.None, CultureInfo.InvariantCulture, out var value) && value > 0)
        {
            parsed = value;
            return true;
        }

        parsed = null;
        return false;
    }

    private static bool TryGetPort(Dictionary<string, List<string>> values, out int? port)
    {
        if (!TryGetPositiveInt(values, "--port", null, out port))
        {
            return false;
        }

        return port is null or (>= 1 and <= 65535);
    }

    private static IReadOnlyList<string> SplitChannels(Dictionary<string, List<string>> values, string key)
    {
        if (!values.TryGetValue(key, out var list))
        {
            return Array.Empty<string>();
        }

        return list.SelectMany(x => x.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Where(x => x.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }
}
