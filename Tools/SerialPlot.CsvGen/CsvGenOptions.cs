using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace SerialPlot.CsvGen;

public sealed record CsvGenOptions(
    double RateHz,
    IReadOnlyList<ChannelDefinition> Channels,
    long? Samples,
    double? DurationSeconds,
    int? Seed,
    char Delimiter,
    int Precision,
    bool Realtime)
{
    public static CsvGenOptions Defaults() => new(
        RateHz: 100d,
        Channels:
        [
            ChannelSpecParser.Parse("t:time"),
            ChannelSpecParser.Parse("sine:sine:freq=1"),
            ChannelSpecParser.Parse("noise:noise:amp=0.1"),
        ],
        Samples: null,
        DurationSeconds: null,
        Seed: null,
        Delimiter: ',',
        Precision: 6,
        Realtime: true);

    public long? EffectiveSampleCount()
    {
        if (Samples.HasValue)
        {
            return Samples;
        }

        if (DurationSeconds.HasValue)
        {
            return Math.Max(0, (long)Math.Ceiling(DurationSeconds.Value * RateHz));
        }

        return null;
    }
}

public static class CsvGenOptionsParser
{
    public static CsvGenOptions Parse(string[] args)
    {
        var defaults = CsvGenOptions.Defaults();
        var rate = defaults.RateHz;
        var samples = defaults.Samples;
        var duration = defaults.DurationSeconds;
        var seed = defaults.Seed;
        var delimiter = defaults.Delimiter;
        var precision = defaults.Precision;
        var realtime = defaults.Realtime;
        var channelSpecs = new List<string>();

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            var (name, inlineValue) = SplitOption(arg);
            var value = inlineValue;

            if (name == "--no-realtime")
            {
                realtime = false;
                continue;
            }

            value ??= TakeValue(args, ref i, name);
            switch (name)
            {
                case "--rate":
                    rate = ParsePositiveDouble(value, name);
                    break;
                case "--channel":
                    channelSpecs.Add(value);
                    break;
                case "--samples":
                    samples = ParseNonNegativeLong(value, name);
                    break;
                case "--duration":
                    duration = ParseNonNegativeDouble(value, name);
                    break;
                case "--seed":
                    seed = ParseInt(value, name);
                    break;
                case "--delimiter":
                    delimiter = value.Length == 1
                        ? value[0]
                        : throw new CsvGenConfigurationException("--delimiter must be exactly one character.");
                    break;
                case "--precision":
                    precision = ParsePositiveInt(value, name);
                    break;
                default:
                    throw new CsvGenConfigurationException($"Unknown option {name}.");
            }
        }

        if (samples.HasValue && duration.HasValue)
        {
            throw new CsvGenConfigurationException("--samples and --duration cannot be combined.");
        }

        var channels = channelSpecs.Count == 0
            ? defaults.Channels
            : channelSpecs.Select(ChannelSpecParser.Parse).ToArray();

        var duplicates = channels.GroupBy(x => x.Name, StringComparer.Ordinal)
            .FirstOrDefault(x => x.Count() > 1);
        if (duplicates is not null)
        {
            throw new CsvGenConfigurationException($"Duplicate channel name '{duplicates.Key}'.");
        }

        return new CsvGenOptions(rate, channels, samples, duration, seed, delimiter, precision, realtime);
    }

    private static (string Name, string? Value) SplitOption(string arg)
    {
        if (!arg.StartsWith("--", StringComparison.Ordinal))
        {
            throw new CsvGenConfigurationException($"Unexpected argument '{arg}'.");
        }

        var equals = arg.IndexOf('=', StringComparison.Ordinal);
        return equals > 0 ? (arg[..equals], arg[(equals + 1)..]) : (arg, null);
    }

    private static string TakeValue(string[] args, ref int index, string name)
    {
        if (index + 1 >= args.Length || args[index + 1].StartsWith("--", StringComparison.Ordinal))
        {
            throw new CsvGenConfigurationException($"Missing value for {name}.");
        }

        return args[++index];
    }

    private static double ParsePositiveDouble(string value, string name)
    {
        var parsed = ParseDouble(value, name);
        return parsed > 0d ? parsed : throw new CsvGenConfigurationException($"{name} must be greater than zero.");
    }

    private static double ParseNonNegativeDouble(string value, string name)
    {
        var parsed = ParseDouble(value, name);
        return parsed >= 0d ? parsed : throw new CsvGenConfigurationException($"{name} must be zero or greater.");
    }

    private static double ParseDouble(string value, string name)
        => double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : throw new CsvGenConfigurationException($"{name} must be numeric.");

    private static long ParseNonNegativeLong(string value, string name)
        => long.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed) && parsed >= 0
            ? parsed
            : throw new CsvGenConfigurationException($"{name} must be a non-negative integer.");

    private static int ParsePositiveInt(string value, string name)
        => int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed) && parsed > 0
            ? parsed
            : throw new CsvGenConfigurationException($"{name} must be a positive integer.");

    private static int ParseInt(string value, string name)
        => int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : throw new CsvGenConfigurationException($"{name} must be an integer.");
}
