using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SerialPlot.Models;

namespace SerialPlot.Services;

public interface ICsvLineSource : IAsyncDisposable
{
    IAsyncEnumerable<string> ReadLinesAsync(CancellationToken cancellationToken);
}

public static class CsvLineSourceFactory
{
    public static ICsvLineSource Create(AppConfig config) => config.Source switch
    {
        SourceType.Stdin => new StandardInputLineSource(),
        SourceType.Serial => new SerialLineSource(config.SerialPort!, config.Baud!.Value),
        SourceType.Tcp => new TcpLineSource(config.Host!, config.Port!.Value),
        SourceType.Udp => new UdpLineSource(config.Host!, config.Port!.Value, config.UdpMessage ?? string.Empty),
        SourceType.Test => new TestCsvLineSource(),
        _ => throw new InvalidOperationException("Unsupported source type."),
    };

    public static ICsvLineSource Create(InputSourceConfig config) => config.Source switch
    {
        SourceType.Stdin => new StandardInputLineSource(),
        SourceType.Serial => new SerialLineSource(config.SerialPort!, config.Baud!.Value),
        SourceType.Tcp => new TcpLineSource(config.Host!, config.Port!.Value),
        SourceType.Udp => new UdpLineSource(config.Host!, config.Port!.Value, config.UdpMessage ?? string.Empty),
        SourceType.Test => new TestCsvLineSource(),
        _ => throw new InvalidOperationException("Unsupported source type."),
    };
}

public sealed class StandardInputLineSource : ICsvLineSource
{
    private readonly StreamReader _reader = new(Console.OpenStandardInput(), Encoding.UTF8);

    public IAsyncEnumerable<string> ReadLinesAsync(CancellationToken cancellationToken)
        => new TextReaderLineSource(_reader).ReadLinesAsync(cancellationToken);

    public ValueTask DisposeAsync()
    {
        _reader.Dispose();
        return ValueTask.CompletedTask;
    }
}

public sealed class TextReaderLineSource(TextReader reader) : ICsvLineSource
{
    public async IAsyncEnumerable<string> ReadLinesAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            if (line is null)
            {
                yield break;
            }

            yield return line;
        }
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

public sealed class TestCsvLineSource : ICsvLineSource
{
    private const double RateHz = 1000d;
    private static int NextSeed = Environment.TickCount;
    private readonly Random _random = new(Interlocked.Increment(ref NextSeed));
    private double _walk;

    public async IAsyncEnumerable<string> ReadLinesAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        yield return "time,sine,sawtooth,noise,random";

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        for (long sample = 0; !cancellationToken.IsCancellationRequested; sample++)
        {
            var time = sample / RateHz;
            var sine = Math.Sin(2d * Math.PI * time);
            var sawtooth = (2d * (time - Math.Floor(time))) - 1d;
            var noise = (2d * _random.NextDouble()) - 1d;
            _walk += 0.05d * ((2d * _random.NextDouble()) - 1d);

            yield return string.Create(
                System.Globalization.CultureInfo.InvariantCulture,
                $"{time:G6},{sine:G6},{sawtooth:G6},{noise:G6},{_walk:G6}");

            var target = TimeSpan.FromSeconds((sample + 1) / RateHz);
            var delay = target - stopwatch.Elapsed;
            if (delay > TimeSpan.Zero)
            {
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

public sealed class SerialLineSource : ICsvLineSource
{
    private readonly SerialPort _port;

    public SerialLineSource(string portName, int baud)
    {
        _port = new SerialPort(portName, baud, Parity.None, 8, StopBits.One)
        {
            Handshake = Handshake.None,
            NewLine = "\n",
        };
    }

    public async IAsyncEnumerable<string> ReadLinesAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        _port.Open();
        using var registration = cancellationToken.Register(() =>
        {
            try { _port.Close(); }
            catch (InvalidOperationException) { }
        });

        while (!cancellationToken.IsCancellationRequested)
        {
            string line;
            try
            {
                line = await Task.Run(() => _port.ReadLine(), cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                yield break;
            }
            catch (InvalidOperationException) when (cancellationToken.IsCancellationRequested)
            {
                yield break;
            }

            yield return line.TrimEnd('\r', '\n');
        }
    }

    public ValueTask DisposeAsync()
    {
        _port.Dispose();
        return ValueTask.CompletedTask;
    }
}

public sealed class TcpLineSource(string host, int port) : ICsvLineSource
{
    private TcpClient? _client;

    public async IAsyncEnumerable<string> ReadLinesAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        _client = new TcpClient
        {
            NoDelay = true,
        };
        await _client.ConnectAsync(host, port, cancellationToken).ConfigureAwait(false);
        await using var stream = _client.GetStream();
        using var reader = new StreamReader(stream, Encoding.ASCII);
        while (!cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            if (line is null)
            {
                yield break;
            }

            yield return line;
        }
    }

    public ValueTask DisposeAsync()
    {
        _client?.Dispose();
        return ValueTask.CompletedTask;
    }
}

public sealed class UdpLineSource(string host, int port, string message) : ICsvLineSource
{
    private UdpClient? _client;

    public async IAsyncEnumerable<string> ReadLinesAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        _client = new UdpClient(new IPEndPoint(IPAddress.Any, 0));
        var endpoint = new IPEndPoint((await Dns.GetHostAddressesAsync(host, cancellationToken).ConfigureAwait(false))[0], port);
        var request = Encoding.ASCII.GetBytes(message);
        await _client.SendAsync(request, endpoint, cancellationToken).ConfigureAwait(false);

        var pending = string.Empty;
        while (!cancellationToken.IsCancellationRequested)
        {
            var result = await _client.ReceiveAsync(cancellationToken).ConfigureAwait(false);
            pending += Encoding.ASCII.GetString(result.Buffer);
            while (TryTakeLine(ref pending, out var line))
            {
                yield return line;
            }
        }
    }

    public ValueTask DisposeAsync()
    {
        _client?.Dispose();
        return ValueTask.CompletedTask;
    }

    private static bool TryTakeLine(ref string pending, out string line)
    {
        var index = pending.IndexOf('\n', StringComparison.Ordinal);
        if (index < 0)
        {
            line = string.Empty;
            return false;
        }

        line = pending[..index].TrimEnd('\r');
        pending = pending[(index + 1)..];
        return true;
    }
}
