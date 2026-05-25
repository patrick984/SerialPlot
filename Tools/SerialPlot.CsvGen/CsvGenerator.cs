using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SerialPlot.CsvGen;

public sealed class CsvGenerator(CsvGenOptions options)
{
    private readonly string _format = "G" + options.Precision.ToString(CultureInfo.InvariantCulture);
    private readonly WaveformChannel[] _channels = options.Channels
        .Select((channel, index) => new WaveformChannel(channel, new Random((options.Seed ?? Environment.TickCount) + index)))
        .ToArray();

    public async Task WriteAsync(TextWriter writer, CancellationToken cancellationToken)
    {
        await writer.WriteLineAsync(string.Join(options.Delimiter, options.Channels.Select(x => Escape(x.Name)))).ConfigureAwait(false);

        var maxSamples = options.EffectiveSampleCount();
        var stopwatch = Stopwatch.StartNew();
        for (long sample = 0; !maxSamples.HasValue || sample < maxSamples.Value; sample++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var timeSeconds = sample / options.RateHz;
            await writer.WriteLineAsync(BuildRow(sample, timeSeconds)).ConfigureAwait(false);

            if (options.Realtime)
            {
                var target = TimeSpan.FromSeconds((sample + 1) / options.RateHz);
                var delay = target - stopwatch.Elapsed;
                if (delay > TimeSpan.Zero)
                {
                    await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                }
            }
        }
    }

    public string BuildRow(long sample, double timeSeconds)
    {
        var values = new string[_channels.Length];
        for (var i = 0; i < _channels.Length; i++)
        {
            values[i] = _channels[i].Next(sample, timeSeconds).ToString(_format, CultureInfo.InvariantCulture);
        }

        return string.Join(options.Delimiter, values);
    }

    private string Escape(string value)
    {
        if (!value.Contains(options.Delimiter, StringComparison.Ordinal) && !value.Contains('"', StringComparison.Ordinal))
        {
            return value;
        }

        return "\"" + value.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
    }
}

public sealed class WaveformChannel
{
    private readonly ChannelDefinition _definition;
    private readonly Random _random;
    private double _walkValue;

    public WaveformChannel(ChannelDefinition definition, Random random)
    {
        _definition = definition;
        _random = random;
        _walkValue = definition.Start;
    }

    public double Next(long sample, double timeSeconds) => _definition.Kind switch
    {
        ChannelKind.Time => timeSeconds,
        ChannelKind.Index => sample,
        ChannelKind.Sine => _definition.Offset + _definition.Amplitude * Math.Sin(Angle(timeSeconds)),
        ChannelKind.Cos => _definition.Offset + _definition.Amplitude * Math.Cos(Angle(timeSeconds)),
        ChannelKind.Square => _definition.Offset + _definition.Amplitude * (Math.Sin(Angle(timeSeconds)) >= 0d ? 1d : -1d),
        ChannelKind.Sawtooth => _definition.Offset + _definition.Amplitude * ((2d * Fraction(_definition.Frequency * timeSeconds + PhaseCycles())) - 1d),
        ChannelKind.Triangle => _definition.Offset + _definition.Amplitude * ((4d * Math.Abs(Fraction(_definition.Frequency * timeSeconds + PhaseCycles()) - 0.5d)) - 1d),
        ChannelKind.Noise => _definition.Offset + _definition.Amplitude * ((2d * _random.NextDouble()) - 1d),
        ChannelKind.RandomWalk => NextWalk(),
        ChannelKind.Constant => _definition.Offset,
        _ => double.NaN,
    };

    private double NextWalk()
    {
        _walkValue += _definition.Step * ((2d * _random.NextDouble()) - 1d);
        if (_definition.Min.HasValue)
        {
            _walkValue = Math.Max(_definition.Min.Value, _walkValue);
        }

        if (_definition.Max.HasValue)
        {
            _walkValue = Math.Min(_definition.Max.Value, _walkValue);
        }

        return _walkValue;
    }

    private double Angle(double timeSeconds) => (2d * Math.PI * _definition.Frequency * timeSeconds) + _definition.Phase;

    private double PhaseCycles() => _definition.Phase / (2d * Math.PI);

    private static double Fraction(double value)
    {
        var fraction = value - Math.Floor(value);
        return fraction < 0d ? fraction + 1d : fraction;
    }
}
