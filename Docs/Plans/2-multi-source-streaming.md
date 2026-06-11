# Multi-Source Streaming Implementation Plan

## Summary

SerialPlot supports multiple independent CSV input sources by treating each source as its own streaming unit with independent connection settings, parser state, schema, buffers, channel eligibility, X channel selection, and raw CSV capture.

## Implemented Shape

- `AppConfig` keeps existing single-source settings and exposes a `Sources` collection for multi-source runs.
- Repeatable `--source-spec` command-line values configure multiple sources while preserving existing single-source command-line options.
- `InputSourceViewModel` owns per-source acquisition, buffering, parsing, channel selection, status, and errors.
- `MainWindowViewModel` coordinates sources, exposes the active source for sidebar editing, and aggregates selected traces across all sources for plotting.
- Plot trace identity includes source, channel, and axis side, allowing different sources to use matching column names or indexes without collisions.
- Runtime source management is exposed through a source manager window that can add and remove sources while other sources continue running.
- Multi-source CSV save writes one file per source into a selected folder.

## Follow-Up

Stacked plot panels require a deeper plot-host refactor from a single named `AvaPlot` to a dynamic collection of linked plot controls. The data model now separates sources and trace identity so that refactor can be made without reworking ingestion.
