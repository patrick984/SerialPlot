# Serial CSV Plotter Requirements

## 1. Technology and Platform

1.1 The application shall be implemented with AvaloniaUI.

1.2 The application shall use ScottPlot for plotting.

1.3 The application shall be cross-platform.

1.4 The initial version shall support self-contained single-file builds for Windows, macOS, and Linux.

## 2. Configuration

2.1 All runtime settings shall be configurable using command-line options.

2.2 If no command-line options are specified, the application shall show a setup dialog for selecting required settings.

2.3 If some command-line options are specified but required settings are missing, the application shall show the setup dialog.

2.4 The setup dialog is required only for the initial runtime configuration. Saved/reusable configuration files are not required for the initial version, except for persisted UI preferences explicitly required elsewhere in this document.

2.5 The configurable source settings shall include:

- Source type: standard input, serial, TCP, or UDP.
- Serial port name.
- Serial baud rate.
- TCP host and port.
- UDP host and port.
- UDP request message.
- Circular buffer point count.
- Unix timestamp unit.
- Initial X channel selection.
- Initial Y channel selections (left axis).
- Initial Y channel selections (right axis, optional).

2.6 The default circular buffer size shall be 100,000 points.

2.7 Initial X/Y channel selections supplied by command-line option shall be applied after the CSV header is read.

2.8 Initial X/Y channel selections that refer to missing columns shall be ignored.

2.9 The application shall persist and restore the user's last selected X autoscale mode and stepped future-space duration across launches.

2.10 The stepped expand future-space duration shall be user-configurable, default to 30 seconds, and accept values from 1 to 300 seconds.

2.11 The application shall support configuring multiple input sources from the command line using repeatable source specifications while preserving existing single-source command-line options.

2.12 Each configured input source shall have its own source type settings, buffer size, timestamp unit, source name, initial X channel, and initial Y channel selections.

## 3. Main Window

3.1 The main window shall contain a ScottPlot SignalXY plot.

3.2 The main window shall contain a channel selection panel on the right side.

3.3 The channel selection panel shall list all CSV columns from the header.

3.4 The channel selection panel shall allow the user to select one X channel.

3.5 The channel selection panel shall allow the user to select one or more Y channels.

3.6 The user shall be able to change X and Y channel selections while streaming is active.

3.7 The plot shall provide left and right Y axes.

3.8 Each selected Y channel shall be manually assignable to either the left or right Y axis.

3.9 The application shall display runtime errors in pop-up pannel at the bottom of the main window.

3.10 The plot controls shall provide independent persistent autoscale toggles for the X axis and Y axis.

3.11 The main window shall provide runtime source management for adding and removing input sources while other sources continue running.

3.12 The channel selection panel shall allow selecting the active source whose independent channels and X selection are being edited.

3.13 The application shall show source-specific status and error information.

## 4. Plot Behavior

4.1 Plotting shall start only after the user selects channels or valid initial channel selections are applied from command-line options.

4.2 The application shall connect to the data source and read the CSV header before plotting starts so the available channels can be discovered.

4.3 The plot shall support real-time scrolling.

4.4 The plot shall retain accumulated data up to the configured circular buffer point count.

4.5 The X axis shall support selectable autoscale modes, including continuous follow-newest, stepped expand, and stepped pan.

4.6 Continuous follow-newest mode shall autoscale the X axis to the newest buffer window.

4.7 Stepped expand mode shall use the persisted future-space duration to reserve expected incoming X space ahead of the newest data.

4.8 In stepped expand and stepped pan modes, the X range shall update when the newest visible data reaches about 90% of the current visible X range.

4.9 Stepped expand mode shall expand the visible X range from the retained minimum X to the newest X plus future-space room.

4.9.1 Stepped pan mode shall preserve the current X zoom width and shift the view right by about half the visible X width when triggered.

4.9.2 Stepped X transitions shall animate over about 300 ms with an ease-out curve.

4.9.3 Stepped mode shall reduce pixel flicker caused by continuously changing the X axis.

4.10 X and Y autoscale state shall be controlled independently.

4.11 User pan or zoom shall disable autoscale only for the affected axis.

4.12 The application shall provide pause and resume controls.

4.13 When plotting is paused, data acquisition shall continue and incoming data shall continue to be buffered in the background.

4.14 The application shall provide a clear plot control.

4.15 The application shall provide autoscale controls.

4.16 The application shall support manual zoom and pan.

4.17 The plot shall show a cursor overlay on mouse hover for the visible point nearest to the mouse in screen distance.

4.18 The cursor overlay shall show the nearest point's channel or series name, X value, and Y value.

4.18.1 Cursor hover shall remain responsive for large buffers by throttling pointer processing and avoiding full point scans on each pointer event.

4.18.2 The cursor overlay shall hide when no visible point is within 30 screen pixels of the mouse.

4.19 The application shall support exporting the plot as a PNG image.

4.20 The application shall support saving captured CSV data.

4.21 Saved captured CSV data shall contain raw incoming rows exactly as received.

4.22 Autoscaled Y axes shall include normal plot padding using ScottPlot's default Y autoscale behavior.

4.23 The plot shall support traces from multiple input sources in the same visible plot.

4.24 Each plotted Y trace shall belong to one source and shall use that source's selected X channel.

4.25 Each source shall have independent X channel selection.

4.26 Shared X autoscale, pan, and zoom shall use the union of visible X ranges from all plotted traces.

4.27 The application shall support stacked plot panels with shared X controls and per-panel Y axes.

4.28 Trace placement shall be assignable per trace so traces from the same source may be overlaid or separated.

## 5. Data Sources

5.1 The data source shall be live-streamed CSV rows.

5.2 Supported source types shall be standard input, serial, TCP, and UDP.

5.3 For standard input, `--source stdin` shall be sufficient configuration.

5.4 For serial sources, the required settings shall be serial port name and baud rate.

5.5 Serial sources shall use 8 data bits, no parity, 1 stop bit, and no flow control.

5.6 TCP sources shall be client-only.

5.7 TCP sources shall connect to a configured host and port.

5.8 UDP sources shall send a configured request message to a configured host and port.

5.9 UDP sources shall bind to the same local port used to send the request message and shall receive responses on that port.

5.10 UDP request and response payloads shall be ASCII CSV lines terminated by `\n` or `\r\n`.

5.11 The expected maximum data rate shall be 1 kHz.

5.12 The application shall support multiple simultaneous live CSV data sources.

5.13 Each input source shall have an independent parser, CSV schema, circular data buffer, and raw CSV capture buffer.

5.14 If one source ends or fails, only that source shall stop; other sources shall continue running, and the stopped source's buffered data shall be preserved.

5.15 Removing a source at runtime shall stop and dispose that source without stopping other sources.

## 6. CSV Format

6.1 The first row shall be the CSV header.

6.2 Column names shall be inferred from the CSV header.

6.3 Duplicate or blank column names in the header shall be rejected.

6.4 Each incoming data row shall be one complete CSV record terminated by a newline.

6.5 Quoted multiline CSV fields are not required.

6.6 The CSV dialect shall not be configurable in the initial version.

6.7 The number of columns shall remain the same after the header.

6.8 If a data row has a different number of columns than the header, the application shall stop processing the stream, show an error state, and preserve the plotted data.

6.9 CSV header and row validation shall be applied independently per source.

## 7. Value Parsing and Channel Eligibility

7.1 The application shall support parsing integer values.

7.2 The application shall support parsing floating-point values.

7.3 The application shall support parsing date/time values.

7.4 Supported date/time parsing shall include ISO 8601 values.

7.5 Supported date/time parsing shall include date/time values accepted by the current operating system culture.

7.6 Supported date/time parsing shall include Unix timestamps.

7.7 Unix timestamp parsing shall support seconds, milliseconds, microseconds, and nanoseconds.

7.8 The Unix timestamp unit shall be configurable.

7.9 Date/time columns shall be selectable only as the X channel.

7.10 Y channels shall be numeric columns only.

7.11 A column shall be considered selectable if any valid numeric or date/time values are observed for that column, subject to X/Y eligibility rules.

7.12 A column with mixed valid and invalid values shall remain selectable if it has at least one valid value.

7.13 Missing, blank, or invalid field values in data rows shall be plotted as gaps.

7.14 Non-numeric and non-date columns shall not be selectable.

## 8. Multi-Source Capture Export

8.1 If one source is active, saving captured CSV data may write a single CSV file.

8.2 If multiple sources are active, saving captured CSV data shall write one CSV file per source into a selected folder.

8.3 Multi-source CSV export shall preserve each source's raw incoming rows exactly as received.
