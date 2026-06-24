using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using SerialPlot.Services;
using Xunit;

namespace SerialPlot.Tests;

public sealed class TestCsvLineSourceTests
{
    [Fact]
    public async Task IndependentSourcesProduceDifferentRandomWalks()
    {
        await using var first = new TestCsvLineSource();
        await using var second = new TestCsvLineSource();

        var firstRows = await ReadRowsAsync(first, 2);
        var secondRows = await ReadRowsAsync(second, 2);

        Assert.NotEqual(firstRows[1].Split(',')[4], secondRows[1].Split(',')[4]);
    }

    [Fact]
    public async Task UdpLineSourceSendsInitialRequestOnlyWhenResendDisabled()
    {
        using var server = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0));
        var port = ((IPEndPoint)server.Client.LocalEndPoint!).Port;
        await using var source = new UdpLineSource("127.0.0.1", port, "poll");
        using var readCancellation = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var readTask = RunUntilCanceledAsync(source, readCancellation.Token);

        Assert.Equal("poll", await ReceiveStringAsync(server, readCancellation.Token));

        using var secondReceiveCancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => ReceiveStringAsync(server, secondReceiveCancellation.Token));

        await readCancellation.CancelAsync();
        await readTask;
    }

    [Fact]
    public async Task UdpLineSourceResendsRequestAtConfiguredInterval()
    {
        using var server = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0));
        var port = ((IPEndPoint)server.Client.LocalEndPoint!).Port;
        await using var source = new UdpLineSource("127.0.0.1", port, "poll", resendIntervalSeconds: 1);
        using var readCancellation = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var readTask = RunUntilCanceledAsync(source, readCancellation.Token);

        Assert.Equal("poll", await ReceiveStringAsync(server, readCancellation.Token));
        Assert.Equal("poll", await ReceiveStringAsync(server, readCancellation.Token));

        await readCancellation.CancelAsync();
        await readTask;
    }

    private static async Task<string[]> ReadRowsAsync(ICsvLineSource source, int dataRows)
    {
        var rows = new string[dataRows + 1];
        var index = 0;
        await foreach (var row in source.ReadLinesAsync(CancellationToken.None))
        {
            rows[index++] = row;
            if (index == rows.Length)
            {
                return rows;
            }
        }

        return rows;
    }

    private static async Task RunUntilCanceledAsync(ICsvLineSource source, CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var _ in source.ReadLinesAsync(cancellationToken))
            {
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private static async Task<string> ReceiveStringAsync(UdpClient client, CancellationToken cancellationToken)
    {
        var result = await client.ReceiveAsync(cancellationToken);
        return System.Text.Encoding.ASCII.GetString(result.Buffer);
    }
}
