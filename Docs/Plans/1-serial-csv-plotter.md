# Serial CSV Plotter Implementation Plan

This plan tracks the initial implementation of the live CSV plotting app described in `Docs/Requirements.md`.

## Scope

- Runtime configuration through command-line options and a setup dialog when required settings are missing.
- Streaming CSV acquisition from stdin, serial, TCP, and UDP sources.
- CSV header and row validation with per-cell numeric/date parsing.
- Circular plot buffers with gaps represented by `NaN`.
- Avalonia main window with ScottPlot, channel selection, pause/resume, clear, autoscale, PNG export, raw CSV save, and bottom error panel.
- Unit/integration-style tests for parsing, buffering, CLI validation, and simulated sources.
- Publish notes for single-file self-contained builds.

## Defaults

- Buffer size defaults to 100,000 points.
- Timestamp unit defaults to `auto`.
- CSV uses comma delimiter with single-line records.
- Initial channel selections that do not match the header are ignored.
- UDP binds and sends on the configured port.
