# Serial CSV Plotter Requirements

## User Interface

1.1 Use AvaloniaUI and ScottPlot.
1.2 All settings should be specified as command line options.
1.3 Also provide a setup dialog to select settings if no command line options are specified.
1.4 Main window consists of a ScottPlot SignalXY plot, with a channel selection UI in a right-side panel.
1.5 Channel selection UI shows a list of all channels, and a mechanism to select one X parameter and one or more Y parameters.
1.6 Provide two Y axes (left and right).

## Data Sources

2.1 Data source will be live streamed CSV values.
2.2 Sources include: standard-input, RS-232/serial, TCP and UDP sockets.
2.3 Column names should be inferred from the CSV header which will be the first row.
2.4 The number of columns won't change after the header (if it does, stop with error).
2.5 Support parsing dates in addition to integers and floats.
2.6 Non-numeric columns should not be selectable - as they cannot be plotted.

## Answers

1. For `1.2`, what counts as “all settings”?
   - Source Type: Standard In, Serial, TCP, UDP.
   - Baud Rate (Serial only)
   - Host & Port (TCP & UDP)
   - Buffer Size

2. For `1.3`, if some command-line options are provided but required ones are missing, show the dialog. 

3. For `1.4`, the plot should support be real-time scrolling and show all accumulated data (up to a fixed circular buffer size).

4. For `1.5`, users should be able to change X/Y channel selections while streaming is active.

5. For `1.6`, how should Y-series be assigned to left vs right axis: manually per channel.

6. For `2.1`, every incoming row is one complete CSV record terminated by newline, no quoted multiline fields.

7. For `2.2`, for TCP sockets, the app acts as a client connecting to a host/port? For UDP send a message (command line) to a host:port, then receive data in response.

8. For RS-232/serial, which settings are required: baud rate only.

9. For `2.3`, should CSV dialect be configurable: no.

10. For `2.4`, “stop with error” means show an error state while preserving the plotted data.

11. For `2.5`, what date/time formats must be supported? ISO 8601 only, local culture formats, Unix timestamps, or a command-line specified format: all of the above.

12. For date columns, should dates be selectable as the X axis only, or also as Y values? `2.6` says non-numeric columns cannot be plotted, but dates are not numeric in the same sense: x only.

13. Missing/blank/invalid field values in data rows be plotted as gaps.

14. Is there a maximum expected data rate or row count? 1 kHz.

15. Should plotted data be retained indefinitely, capped by point count/time window, or user-configurable? Capped to a maximum point count.

16. Do you need pause/resume, clear plot, autoscale, manual zoom/pan, export image, or save captured CSV? Yes to all.

17. Should channel names be exactly the CSV headers, or should duplicate/blank header names be rejected/renamed? Reject duplicate/blanks.

18. What platforms are targeted: cross-platform.

19. Should configuration be saveable/reusable, or are command-line options and the setup dialog enough? Command line is enough for inital version.

20. Start plotting only after the user selects channels.


