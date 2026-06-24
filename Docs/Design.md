# SerialPlot Design

SerialPlot is a desktop CSV stream plotter built with .NET, Avalonia, CommunityToolkit.Mvvm, and ScottPlot. It accepts live CSV rows from standard input, serial ports, TCP sockets, UDP sockets, or an internal test generator, then plots selected columns in near real time.

The codebase follows an MVVM shape for application state and user choices, with a deliberate code-behind bridge for ScottPlot operations that are easier and safer to manage imperatively.

## Goals

- Start quickly from either CLI arguments or an interactive setup window.
- Read one or more live CSV sources concurrently.
- Infer channel eligibility from observed data instead of requiring schema metadata.
- Keep recent samples in bounded memory.
- Update plots smoothly without rendering on every incoming row.
- Preserve enough raw CSV to export the retained capture.

## Project Layout

- `Program.cs` and `App.axaml.cs` bootstrap Avalonia, parse CLI configuration, and choose between setup flow and the main plot window.
- `Models/` contains configuration records, parsed CSV models, channel state, and autoscale enums.
- `Services/` contains input source implementations, CSV parsing, ring buffers, viewport/autoscale helpers, preferences, and CLI parsing.
- `ViewModels/` contains app, source, channel, and setup state exposed to Avalonia bindings.
- `Views/` contains Avalonia XAML screens and code-behind. `MainWindow.axaml.cs` owns ScottPlot synchronization, file pickers, pointer interaction, and plot overlays.
- `Tests/` covers parser, buffer, view model, autoscale, hover, preferences, and CLI behavior.
- `Tools/SerialPlot.CsvGen/` is a companion CLI for generating synthetic CSV streams.

## Startup Flow

`Program.Main` creates the Avalonia app and starts a classic desktop lifetime. During framework initialization, `App.OnFrameworkInitializationCompleted` calls `CliConfigParser.Parse`.

If CLI arguments are complete, `App` opens `MainWindow` with a `MainWindowViewModel`. If no complete configuration is available, it opens `SetupWindow` first. With no startup arguments, the setup window loads recent setup history and pre-fills the form from the last used source type. With incomplete startup arguments, recent setup history is ignored and the parsed CLI/default values are shown with the validation error. Closing setup without a config shuts the app down, while a valid config opens the main window.

When standard input is redirected and no arguments are supplied, the parser treats stdin as a complete source. This supports shell pipelines such as:

```bash
dotnet run --project Tools/SerialPlot.CsvGen -- --rate 100 --channel t:time --channel volts:sine:freq=1:amp=2 | dotnet run -- --source stdin --x t --y-left volts
```

## Configuration Model

`AppConfig` represents the initial application configuration and retains legacy single-source fields. Its `Sources` property is the multi-source configuration used by the runtime.

Each `InputSourceConfig` defines:

- display name
- source type
- serial, TCP, or UDP connection details, including optional UDP resend interval
- retained buffer size
- timestamp parsing mode
- optional initial X channel
- optional initial left and right Y channels

CLI parsing supports either classic single-source options or repeated `--source-spec` values for multiple sources.

Recent setup history is stored separately from `AppConfig`. `RecentSetupHistory` keeps the last used source type and up to five complete setup entries per source type. `RecentSetupService` loads invalid or missing history as empty and saves accepted setup entries after initial setup and runtime Add Source.

## UI Structure

### Setup Window

`SetupWindow.axaml` is a fixed-width, vertically resizable configuration form. It binds to `SetupWindowViewModel`, which exposes source type, recent setup entries, connection settings, buffer size, timestamp unit, and optional initial channel selections.

The form conditionally shows serial, network, and UDP request fields based on `SourceType`. UDP sources may also configure a request resend interval, where zero disables periodic resend. When recent history is enabled, the form shows a recent-settings dropdown filtered to the selected source type; choosing an entry applies all saved setup fields. The window automatically resizes vertically to fit visible setup content up to 80% of the current screen working area, then relies on its scroll viewer for overflow. Validation reuses `CliConfigParser.Validate`, keeping CLI and UI requirements aligned.

### Main Window

`MainWindow.axaml` is split into three main areas:

- top toolbar for pause, clear, autoscale controls, export menu, source management, and settings
- central ScottPlot `AvaPlot` with a canvas overlay for hover marker and label
- right channel panel for selecting the active source, X channel, left/right Y traces, and aggregate status

The right panel binds to `MainWindowViewModel.SelectedSource` and exposes that source's `Channels`. Each `ChannelViewModel` tracks whether the channel can be used for X or Y, whether it is selected on left or right axis, and the brush assigned by the plot. Axis selectors place L/R labels before their checkboxes and use the trace brush for selected axis controls.

`SettingsWindow` binds directly to `MainWindowViewModel` and live-applies stepped future-space and plot line-width changes. The Export toolbar button opens a menu containing Save CSV and Export PNG commands.

The main UI intentionally keeps plotting imperative in `MainWindow.axaml.cs`. ScottPlot plottables, axis objects, marker state, PNG export, pointer gestures, and hover overlays are view concerns and do not live in the view model.

### Source Manager

`SourceManagerWindow` binds to the same `MainWindowViewModel` as the main window. It lists configured sources, status, errors, and channel count. Adding a source reuses `SetupWindow`; removing a source disposes it and triggers a plot selection refresh.

## Data Flow

The live data path is:

1. `MainWindow` attaches to `MainWindowViewModel` and calls `Start`.
2. `MainWindowViewModel.Start` loads preferences, then starts each `InputSourceViewModel`.
3. Each input source creates an `ICsvLineSource` using `CsvLineSourceFactory`.
4. `InputSourceViewModel.ReadLoopAsync` reads lines on a long-running background task.
5. The first line is parsed as a CSV header by `CsvStreamParser.ParseHeader`.
6. Later lines are parsed by `CsvStreamParser.ParseRow`.
7. Parsed cells are appended to `PlotBuffer`; raw lines are appended to `RawCsvBuffer`.
8. Column eligibility is updated through `ColumnState`.
9. Source-level notifications are marshaled to the UI thread and raised as `SourceDataChangedEventArgs`.
10. `MainWindowViewModel` coalesces source notifications into `PlotDataChangedEventArgs`.
11. `MainWindow.RefreshPlot` synchronizes ScottPlot series and copies new valid pairs into per-series fixed XY buffers.
12. ScottPlot axis ranges, marker visibility, hover state, and rendering are updated.

In compact form:

```text
ICsvLineSource
  -> CsvStreamParser
  -> InputSourceViewModel
  -> PlotBuffer + RawCsvBuffer + ChannelViewModel eligibility
  -> SourceDataChangedEventArgs
  -> MainWindowViewModel throttling/coalescing
  -> PlotDataChangedEventArgs
  -> MainWindow ScottPlot series
```

## Input Sources

All sources implement `ICsvLineSource`, which exposes `ReadLinesAsync(CancellationToken)`.

- `StandardInputLineSource` reads UTF-8 stdin.
- `SerialLineSource` wraps `System.IO.Ports.SerialPort` and reads newline-delimited rows.
- `TcpLineSource` connects to a TCP endpoint and reads ASCII lines.
- `UdpLineSource` binds to an ephemeral local port, sends an optional request message to the remote endpoint, optionally resends it at the configured interval, then splits received datagrams into newline-delimited rows.
- `TestCsvLineSource` generates time, sine, sawtooth, noise, and random-walk channels at 1000 Hz.

This interface keeps transport concerns separate from CSV parsing and UI state.

## CSV Parsing And Channel Eligibility

`CsvStreamParser` uses CsvHelper with invariant culture and no header handling. SerialPlot treats the first line as the header itself, then parses rows against that schema.

Cells become `ParsedCell` values:

- blank or unparsable fields become gaps
- numeric fields become finite numeric values
- plausible Unix timestamps are converted to Unix milliseconds
- date/time strings are parsed as local timestamps and converted to Unix milliseconds

`ColumnState` observes valid cells to infer whether a channel can be used as X or Y:

- numeric and date/time columns can be X
- only numeric columns can be Y

This allows the UI to enable channel controls only after enough data has arrived to prove eligibility.

## Buffering

There are two buffer layers with different responsibilities.

`PlotBuffer` is the source-level row ring buffer. It stores one `double[]` per parsed row and assigns monotonically increasing row versions. Invalid cells are stored as `double.NaN`. The buffer can copy all valid X/Y pairs or only pairs since a known version.

`RawCsvBuffer` retains the most recent raw lines for CSV export. Its capacity is `buffer size + 1` so it can hold the header plus the retained rows.

`FixedXyRingBuffer` is the view-level plot buffer for a selected trace. It stores X and Y arrays directly for ScottPlot `SignalXY` plottables. It rejects non-finite values and non-increasing X values, which keeps the plotted signal ordered.

The separation matters: `PlotBuffer` preserves row-oriented source data for channel changes, while `FixedXyRingBuffer` is optimized for currently selected trace rendering.

## Plot Update Strategy

Incoming source data can arrive much faster than the UI should render. SerialPlot throttles at two levels:

- `InputSourceViewModel` delays append notifications by at least 33 ms.
- `MainWindowViewModel` also limits plot append events to at least 33 ms.

Append notifications are coalesced by source version. On each plot event, `MainWindow.RefreshPlot` synchronizes selected traces:

- removes ScottPlot series for deselected channels
- creates series for newly selected channels
- rebuilds trace buffers after selection, X-channel, clear, or stale-version changes
- incrementally appends only rows newer than the last source version when possible

Each selected trace uses two ScottPlot `SignalXY` plottables over the same backing arrays. This handles wrapped ring-buffer segments by displaying the older and newer contiguous ranges separately.

## Autoscale And Interaction

The toolbar exposes independent X, left Y, and right Y autoscale toggles.

X autoscale supports three modes:

- continuous follow: set X limits directly to retained data extent
- stepped expansion: expand the visible range when newest data approaches the edge
- stepped pan: pan by a step when newest data approaches the edge

`SteppedXAxisViewport` computes target ranges. `XRangeAnimator` smooths range changes, and `MainWindow` temporarily disables axis antialiasing during animation for performance.

Pointer interaction disables autoscale for the relevant axis:

- bottom axis interaction disables X autoscale
- left axis interaction disables left Y autoscale
- right axis interaction disables right Y autoscale
- general plot interaction disables all autoscale axes

Hover handling is throttled to about 30 Hz. `HoverPointIndex` and per-series cache state find nearby plotted points and show a marker plus label on the overlay canvas.

## Multi-Source Behavior

`MainWindowViewModel.Sources` holds all active sources. Each source owns its own parser, buffers, channels, cancellation token, and read task.

The selected source controls the channel list shown in the right panel and the currently displayed X-channel selector. Trace selection is global across sources: `SelectedTraces` flattens selected left and right channels from every source, so the plot can show channels from multiple sources at once.

Saving CSV differs by source count:

- one source writes the retained raw CSV to the selected file
- multiple sources writes one CSV file per source into a selected folder

## Threading And Lifetime

Source reading runs on background tasks. Mutable buffer state is protected by `_gate` inside `InputSourceViewModel`. UI-facing notifications are posted through `Dispatcher.UIThread`.

`MainWindowViewModel.DisposeAsync` cancels its token, waits for preference loading, disposes all sources, and releases the cancellation token source. Each source cancels its read loop, awaits completion, disposes the underlying line source, and then disposes its token source.

`MainWindow` disposes the attached view model during window closing.

## Persistence

`UserPreferencesService` stores UI preferences for X autoscale mode, stepped future space, and global plot line width. Preferences are loaded before sources start so initial plot behavior reflects saved settings. Preference save failures are intentionally ignored, keeping plotting functional when settings cannot be written.

`RecentSetupService` stores recent setup entries in `recent-setups.json` under the SerialPlot application data directory. This history is used only to pre-fill setup forms and is not a saved project format. Missing, corrupt, or unwritable history does not block setup or plotting.

## Error Handling

Parsing and transport exceptions inside `InputSourceViewModel.ReadLoopAsync` stop only the affected source. The source records `ErrorMessage`, `HasError`, and `IsStopped`, then raises a rebuild notification.

`MainWindowViewModel` aggregates source errors into the main error band and status text. Multi-source operation can continue when one source fails.

## Extension Points

To add a new transport, implement `ICsvLineSource`, add a `SourceType`, extend `CsvLineSourceFactory`, and update setup/CLI validation.

To add a new parsed cell kind, extend `CsvStreamParser.ParseCell` and `ColumnState` eligibility rules.

To add a new autoscale behavior, extend `XAutoscaleMode`, add an `XAutoscaleModeOption`, and update `MainWindow.ApplyAutoscale` and any helper tests.

To add new plot commands, prefer keeping ScottPlot-specific logic in `MainWindow.axaml.cs` unless the command represents app state that should be testable in a view model.

## Current Design Tradeoffs

- The main window code-behind is relatively large because ScottPlot state is not easily represented as pure bindings. This is pragmatic, but future plot features should be kept cohesive to avoid turning it into unrelated UI logic.
- Source and plot update throttling favor UI responsiveness over displaying every row immediately. Rows are still retained in buffers; the plot catches up on the next append event.
- `FixedXyRingBuffer` requires increasing X values. This works for time-series streams but means non-monotonic X data is skipped at the trace layer.
- Channel eligibility depends on observed data. Until valid rows arrive, controls may be disabled or incomplete even though the header is known.
- Raw CSV export only includes retained lines, not the full historical stream.
