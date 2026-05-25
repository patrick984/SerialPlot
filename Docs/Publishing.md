# Publishing SerialPlot

Create self-contained single-file builds from the repository root:

```bash
dotnet publish SerialPlot.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
dotnet publish SerialPlot.csproj -c Release -r osx-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
dotnet publish SerialPlot.csproj -c Release -r osx-arm64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
dotnet publish SerialPlot.csproj -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
dotnet publish SerialPlot.csproj -c Release -r linux-arm64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

Outputs are written below `bin/Release/net10.0/<rid>/publish/`.

Publish the companion CSV generator with the same runtime identifiers:

```bash
dotnet publish Tools/SerialPlot.CsvGen/SerialPlot.CsvGen.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
dotnet publish Tools/SerialPlot.CsvGen/SerialPlot.CsvGen.csproj -c Release -r osx-x64 --self-contained true -p:PublishSingleFile=true
dotnet publish Tools/SerialPlot.CsvGen/SerialPlot.CsvGen.csproj -c Release -r osx-arm64 --self-contained true -p:PublishSingleFile=true
dotnet publish Tools/SerialPlot.CsvGen/SerialPlot.CsvGen.csproj -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true
dotnet publish Tools/SerialPlot.CsvGen/SerialPlot.CsvGen.csproj -c Release -r linux-arm64 --self-contained true -p:PublishSingleFile=true
```
