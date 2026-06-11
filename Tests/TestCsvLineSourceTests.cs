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
}
