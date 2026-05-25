using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace SerialPlot.CsvGen;

public enum ChannelKind
{
    Time,
    Index,
    Sine,
    Cos,
    Square,
    Sawtooth,
    Triangle,
    Noise,
    RandomWalk,
    Constant,
}

public sealed record ChannelDefinition(
    string Name,
    ChannelKind Kind,
    double Amplitude,
    double Offset,
    double Frequency,
    double Phase,
    double Start,
    double Step,
    double? Min,
    double? Max)
{
    public static ChannelDefinition Default(string name, ChannelKind kind) => new(
        name,
        kind,
        Amplitude: 1d,
        Offset: 0d,
        Frequency: 1d,
        Phase: 0d,
        Start: 0d,
        Step: 1d,
        Min: null,
        Max: null);
}

public static class ChannelSpecParser
{
    public static ChannelDefinition Parse(string spec)
    {
        if (string.IsNullOrWhiteSpace(spec))
        {
            throw new CsvGenConfigurationException("Channel spec must not be blank.");
        }

        var parts = spec.Split(':', StringSplitOptions.TrimEntries);
        if (parts.Length < 2)
        {
            throw new CsvGenConfigurationException($"Channel spec '{spec}' must use name:type syntax.");
        }

        var name = parts[0];
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new CsvGenConfigurationException("Channel name must not be blank.");
        }

        var kind = ParseKind(parts[1]);
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 2; i < parts.Length; i++)
        {
            var option = parts[i];
            if (string.IsNullOrWhiteSpace(option))
            {
                continue;
            }

            var equals = option.IndexOf('=', StringComparison.Ordinal);
            if (equals <= 0 || equals == option.Length - 1)
            {
                throw new CsvGenConfigurationException($"Channel option '{option}' must use key=value syntax.");
            }

            values[option[..equals]] = option[(equals + 1)..];
        }

        var channel = ChannelDefinition.Default(name, kind);
        return channel with
        {
            Amplitude = GetDouble(values, "amp", channel.Amplitude),
            Offset = GetDouble(values, "offset", channel.Offset),
            Frequency = GetDouble(values, "freq", channel.Frequency),
            Phase = GetDouble(values, "phase", channel.Phase),
            Start = GetDouble(values, "start", channel.Start),
            Step = GetDouble(values, "step", channel.Step),
            Min = GetNullableDouble(values, "min"),
            Max = GetNullableDouble(values, "max"),
        };
    }

    private static ChannelKind ParseKind(string value) => value.ToLowerInvariant() switch
    {
        "time" => ChannelKind.Time,
        "index" => ChannelKind.Index,
        "sine" => ChannelKind.Sine,
        "cos" => ChannelKind.Cos,
        "square" => ChannelKind.Square,
        "sawtooth" => ChannelKind.Sawtooth,
        "triangle" => ChannelKind.Triangle,
        "noise" => ChannelKind.Noise,
        "random-walk" => ChannelKind.RandomWalk,
        "randomwalk" => ChannelKind.RandomWalk,
        "constant" => ChannelKind.Constant,
        _ => throw new CsvGenConfigurationException($"Unsupported channel type '{value}'."),
    };

    private static double GetDouble(IReadOnlyDictionary<string, string> values, string key, double fallback)
    {
        var parsed = GetNullableDouble(values, key);
        return parsed ?? fallback;
    }

    private static double? GetNullableDouble(IReadOnlyDictionary<string, string> values, string key)
    {
        if (!values.TryGetValue(key, out var raw))
        {
            return null;
        }

        if (double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }

        throw new CsvGenConfigurationException($"Channel option '{key}' must be numeric.");
    }
}

public sealed class CsvGenConfigurationException(string message) : Exception(message);
