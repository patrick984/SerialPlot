using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SerialPlot.CsvGen;
using SerialPlot.Models;
using SerialPlot.Services;
using Xunit;

namespace SerialPlot.Tests.CsvGen;

public sealed class CsvGeneratorTests
{
    [Fact]
    public void WaveformsProduceExpectedKnownSamples()
    {
        var options = CsvGenOptionsParser.Parse([
            "--rate", "4",
            "--channel", "t:time",
            "--channel", "i:index",
            "--channel", "s:sine:freq=1",
            "--channel", "c:cos:freq=1",
            "--channel", "q:square:freq=1",
            "--channel", "w:sawtooth:freq=1",
            "--no-realtime",
        ]);
        var generator = new CsvGenerator(options);

        Assert.Equal("0.25,1,1,6.12323E-17,1,-0.5", generator.BuildRow(1, 0.25));
    }

    [Fact]
    public async Task WritesHeaderAndFixedSampleRowsAcceptedBySerialPlotParser()
    {
        var options = CsvGenOptionsParser.Parse([
            "--samples", "3",
            "--seed", "10",
            "--precision", "8",
            "--channel", "t:time",
            "--channel", "s:sine:freq=1",
            "--channel", "n:noise:amp=0.1",
            "--no-realtime",
        ]);
        var writer = new StringWriter();

        await new CsvGenerator(options).WriteAsync(writer, CancellationToken.None);

        var lines = writer.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(4, lines.Length);
        Assert.Equal("t,s,n", lines[0]);

        var parser = new CsvStreamParser(TimestampUnit.Auto);
        var schema = parser.ParseHeader(lines[0]);
        foreach (var line in lines.Skip(1))
        {
            var row = parser.ParseRow(schema, line);
            Assert.All(row.Cells, cell => Assert.True(cell.IsValid));
        }
    }

    [Fact]
    public async Task SeededRandomOutputIsDeterministic()
    {
        var args = new[]
        {
            "--samples", "2",
            "--seed", "42",
            "--channel", "n:noise",
            "--channel", "rw:random-walk:step=0.5",
            "--no-realtime",
        };

        var first = new StringWriter();
        await new CsvGenerator(CsvGenOptionsParser.Parse(args)).WriteAsync(first, CancellationToken.None);

        var second = new StringWriter();
        await new CsvGenerator(CsvGenOptionsParser.Parse(args)).WriteAsync(second, CancellationToken.None);

        Assert.Equal(first.ToString(), second.ToString());
    }
}
