# Streaming CSV Plotter

Streaming/serial CSV plotter - plots streaming CSV values from Standard In, RS-232, TCP or UDP sockets. 

Built with C#/.NET and AvaloniaUI.

## Generated test data

The companion `serialplot-csvgen` CLI writes generated CSV data to standard output so it can be piped into SerialPlot:

```bash
dotnet run --project Tools/SerialPlot.CsvGen -- --rate 100 --channel t:time --channel volts:sine:freq=1:amp=2 | dotnet run -- --source stdin --x t --y-left volts
```

Generate a finite capture as fast as possible:

```bash
dotnet run --project Tools/SerialPlot.CsvGen -- --samples 1000 --no-realtime --seed 7
```

Channel specs use `name:type:key=value` syntax. Supported types are `time`, `index`, `sine`, `cos`, `square`, `sawtooth`, `triangle`, `noise`, `random-walk`, and `constant`.

