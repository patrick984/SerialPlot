using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace SerialPlot.CsvGen;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        if (args.Length == 1 && args[0] is "--help" or "-h")
        {
            Console.Error.WriteLine(HelpText);
            return 0;
        }

        CsvGenOptions options;
        try
        {
            options = CsvGenOptionsParser.Parse(args);
        }
        catch (CsvGenConfigurationException ex)
        {
            Console.Error.WriteLine(ex.Message);
            Console.Error.WriteLine("Run with --help for usage.");
            return 2;
        }

        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            cts.Cancel();
        };

        try
        {
            if (options.TcpListenPort is { } port)
            {
                await WriteTcpAsync(options, port, cts.Token).ConfigureAwait(false);
            }
            else
            {
                await new CsvGenerator(options).WriteAsync(Console.Out, cts.Token).ConfigureAwait(false);
            }

            return 0;
        }
        catch (OperationCanceledException)
        {
            return 0;
        }
        catch (IOException)
        {
            return 0;
        }
    }

    private static async Task WriteTcpAsync(CsvGenOptions options, int port, CancellationToken cancellationToken)
    {
        var listener = new TcpListener(IPAddress.Loopback, port);
        listener.Start();
        try
        {
            using var client = await listener.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(false);
            client.NoDelay = true;
            await using var stream = client.GetStream();
            await using var writer = new StreamWriter(stream) { AutoFlush = true };
            await new CsvGenerator(options).WriteAsync(writer, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            listener.Stop();
        }
    }

    private const string HelpText = """
serialplot-csvgen writes generated CSV test data to stdout.

Usage:
  serialplot-csvgen [options]

Options:
  --rate <hz>              Sample rate in Hz. Default: 100.
  --channel <spec>         Repeatable channel spec: name:type:key=value:key=value.
  --samples <count>        Stop after count samples.
  --duration <seconds>     Stop after duration seconds. Cannot combine with --samples.
  --seed <int>             Use deterministic random values.
  --delimiter <char>       CSV delimiter. Default: comma.
  --precision <digits>     Numeric precision. Default: 6.
  --no-realtime            Emit as fast as possible.
  --tcp-listen <port>      Listen on localhost TCP port and stream to one client.

Channel types:
  time, index, sine, cos, square, sawtooth, triangle, noise, random-walk, constant

Examples:
  serialplot-csvgen --rate 100 --channel t:time --channel volts:sine:freq=1:amp=2
  serialplot-csvgen --tcp-listen 5001 --rate 1000 --channel time:time --channel sine:sine
  serialplot-csvgen --samples 1000 --no-realtime --seed 7
""";
}
