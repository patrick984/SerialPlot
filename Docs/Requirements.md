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
