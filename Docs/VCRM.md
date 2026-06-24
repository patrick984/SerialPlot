# Verification Cross-Reference Matrix

This VCRM links the requirements in `Docs/Requirements.md` to verification test cases and maps every existing automated xUnit test method back to at least one parent requirement.

## Validation Summary

- Automated test traceability: PASS. All 91 existing xUnit test methods are mapped to at least one parent requirement in the automated test traceability table. The current suite executes 95 cases because theories expand into multiple cases.
- Requirement test-case coverage: PASS. Every requirement has at least one linked verification case.
- Automated-only requirement coverage: INCOMPLETE. Several UI, platform, packaging, hardware, and workflow requirements are covered by manual or inspection test cases because no direct automated test exists yet.
- Execution status: `dotnet test Tests/SerialPlot.Tests.csproj --no-restore` passed with 95 total cases, 0 failures, and 0 skipped.

## Verification Methods

- `AUT`: existing automated xUnit test.
- `MAN`: manual workflow test to be executed by a tester.
- `INS`: inspection or static verification.

## Manual And Inspection Test Case Catalog

| Test case | Method | Description |
| --- | --- | --- |
| INS-PLATFORM-001 | Inspection | Inspect project files and package references to confirm Avalonia, ScottPlot, .NET target, and cross-platform framework usage. |
| MAN-PUBLISH-001 | Manual | Publish self-contained single-file builds for Windows, macOS, and Linux runtime identifiers and confirm each artifact starts. |
| MAN-SETUP-001 | Manual | Launch with no CLI args and with partial CLI args; verify setup dialog appears, validates required settings, and starts the main window after valid input. |
| MAN-RECENT-SETUP-001 | Manual | Create recent setup entries for multiple source types; verify no-arg setup pre-fills the last used source, the recent-settings selector is filtered by source type, selecting an entry applies all fields, runtime Add Source updates history, and incomplete CLI startup ignores history. |
| MAN-MAIN-UI-001 | Manual | Launch a test source and verify the main window contains the plot, right channel panel, channel list, source selector, axis selectors, status, error band, and toolbar controls. |
| MAN-CHANNEL-001 | Manual | Stream data, change X and Y selections while running, assign traces to left/right axes, and confirm missing initial selections are ignored. |
| MAN-PLOT-001 | Manual | Verify scrolling, manual pan/zoom, autoscale toggles, axis-specific autoscale disablement, hover overlay text, PNG export, and single-source CSV save. |
| MAN-SOURCE-001 | Manual | Verify stdin, serial, TCP, UDP, and test sources with representative live streams. |
| MAN-UDP-RESEND-001 | Manual | Configure a UDP source with a resend interval and confirm the request message is resent every configured N seconds while active; confirm disabled or unset interval sends only once at startup. |
| MAN-MULTISOURCE-001 | Manual | Start multiple sources, add and remove a source at runtime, stop/fail one source, and confirm other sources continue and buffered data remains visible. |
| MAN-MULTISOURCE-EXPORT-001 | Manual | Save CSV capture with multiple active sources and confirm one exact raw CSV file is written per source. |
| MAN-CSV-LINE-001 | Manual | Feed newline-terminated CSV records, including CRLF, and confirm complete records are consumed; confirm quoted multiline fields are not required. |
| MAN-STACKED-PANELS-001 | Manual | Verify stacked plot panels share X controls, have per-panel Y axes, and support per-trace placement. |

## Requirement Coverage Matrix

| Requirement | Verification cases |
| --- | --- |
| 1.1 | INS-PLATFORM-001 |
| 1.2 | INS-PLATFORM-001 |
| 1.3 | INS-PLATFORM-001, MAN-PUBLISH-001 |
| 1.4 | MAN-PUBLISH-001 |
| 2.1 | AUT `CliConfigParserTests.StdinArgsAreComplete`, AUT `CliConfigParserTests.RepeatSourceSpecsCreateIndependentSources`, MAN-SETUP-001 |
| 2.2 | AUT `CliConfigParserTests.NoArgsRequestsSetup`, MAN-SETUP-001 |
| 2.3 | AUT `CliConfigParserTests.SerialRequiresPortAndBaud`, AUT `CliConfigParserTests.TcpRequiresHostAndPort`, MAN-SETUP-001 |
| 2.4 | MAN-SETUP-001 |
| 2.5 | AUT `SetupWindowViewModelTests.ToConfigMapsSelectedSourceFields`, AUT `CliConfigParserTests.StdinArgsAreComplete`, AUT `CliConfigParserTests.RepeatSourceSpecsCreateIndependentSources`, MAN-SETUP-001 |
| 2.6 | AUT `CliConfigParserTests.NoArgsRequestsSetup`, AUT `UserPreferencesServiceTests.MissingFileReturnsDefaults` |
| 2.7 | AUT `SetupWindowViewModelTests.ToConfigMapsSelectedSourceFields`, AUT `CliConfigParserTests.RepeatSourceSpecsCreateIndependentSources`, MAN-CHANNEL-001 |
| 2.8 | MAN-CHANNEL-001 |
| 2.9 | AUT `UserPreferencesServiceTests.ValidJsonRestoresXAutoscaleMode`, AUT `UserPreferencesServiceTests.SteppedPanModeLoadsAndSaves`, AUT `UserPreferencesServiceTests.SaveWritesSelectedModeAndFutureSpace` |
| 2.10 | AUT `UserPreferencesServiceTests.MissingFileReturnsDefaults`, AUT `UserPreferencesServiceTests.FutureSpaceSecondsClampsOutOfRangeValues`, AUT `SteppedXAxisViewportTests.UsesCustomFutureSpaceSeconds`, AUT `MainWindowViewModelTests.FutureSpaceControlIsEnabledOnlyForSteppedExpansion` |
| 2.11 | AUT `CliConfigParserTests.RepeatSourceSpecsCreateIndependentSources`, AUT `CliConfigParserTests.InvalidSourceSpecReportsPerSourceValidation` |
| 2.12 | AUT `CliConfigParserTests.RepeatSourceSpecsCreateIndependentSources`, AUT `SetupWindowViewModelTests.ToConfigMapsSelectedSourceFields`, AUT `CliConfigParserTests.UdpArgsParseResendInterval`, MAN-UDP-RESEND-001 |
| 2.13 | AUT `CliConfigParserTests.ZeroUdpResendIntervalDisablesResend`, AUT `TestCsvLineSourceTests.UdpLineSourceSendsInitialRequestOnlyWhenResendDisabled`, MAN-UDP-RESEND-001 |
| 2.14 | AUT `RecentSetupServiceTests.RecentEntriesAreCappedPerSourceType`, MAN-RECENT-SETUP-001 |
| 2.15 | AUT `SetupWindowViewModelTests.SelectingRecentEntryAppliesAllSetupFields`, AUT `RecentSetupServiceTests.SaveAndLoadRestoresLastSourceAndEntries`, MAN-RECENT-SETUP-001 |
| 2.16 | AUT `SetupWindowViewModelTests.RecentHistoryPrefillsLastUsedSourceAndMostRecentEntry`, MAN-RECENT-SETUP-001 |
| 2.17 | AUT `SetupWindowViewModelTests.RecentHistoryDropdownIsFilteredBySourceType`, AUT `SetupWindowViewModelTests.SelectingRecentEntryAppliesAllSetupFields`, MAN-RECENT-SETUP-001 |
| 2.18 | AUT `RecentSetupServiceTests.RememberMovesMatchingEntryToMostRecent`, MAN-RECENT-SETUP-001 |
| 2.19 | AUT `SetupWindowViewModelTests.InitialConfigIgnoresRecentHistory`, MAN-RECENT-SETUP-001 |
| 2.20 | AUT `RecentSetupServiceTests.MissingFileReturnsEmptyHistory`, AUT `RecentSetupServiceTests.InvalidJsonReturnsEmptyHistory` |
| 3.1 | INS-PLATFORM-001, MAN-MAIN-UI-001 |
| 3.2 | MAN-MAIN-UI-001 |
| 3.3 | MAN-MAIN-UI-001, MAN-CHANNEL-001 |
| 3.4 | MAN-CHANNEL-001 |
| 3.5 | MAN-CHANNEL-001 |
| 3.6 | MAN-CHANNEL-001 |
| 3.7 | MAN-MAIN-UI-001, MAN-CHANNEL-001 |
| 3.8 | AUT `MainWindowViewModelTests.SelectedTracesIncludeSourceIdentity`, AUT `ChannelViewModelTests.TraceBrushesCanBeAssignedAndClearedPerAxis`, MAN-CHANNEL-001 |
| 3.9 | MAN-MAIN-UI-001, MAN-MULTISOURCE-001 |
| 3.10 | AUT `MainWindowViewModelTests.XAutoscaleModeOptionsExposeFriendlyLabels`, AUT `MainWindowViewModelTests.SelectingXAutoscaleModeOptionUpdatesMode`, AUT `MainWindowViewModelTests.SettingXAutoscaleModeUpdatesSelectedOption`, MAN-PLOT-001 |
| 3.11 | AUT `MainWindowViewModelTests.AddSourceSelectsAndExposesIndependentChannelCollection`, AUT `MainWindowViewModelTests.RemoveSelectedSourceSelectsReplacementAndDropsTraces`, AUT `MainWindowViewModelTests.LateNotificationsFromRemovedSourceAreIgnored`, MAN-MULTISOURCE-001 |
| 3.12 | AUT `MainWindowViewModelTests.AddSourceSelectsAndExposesIndependentChannelCollection`, MAN-MAIN-UI-001 |
| 3.13 | MAN-MAIN-UI-001, MAN-MULTISOURCE-001 |
| 4.1 | MAN-CHANNEL-001 |
| 4.2 | AUT `CsvStreamParserTests.HeaderRejectsBlankAndDuplicateNames`, MAN-CHANNEL-001 |
| 4.3 | AUT `MainWindowViewModelTests.AppendThrottlingPreservesDirtySources`, MAN-PLOT-001 |
| 4.4 | AUT `PlotBufferTests.CircularBufferCapsRowsAndPreservesOrder`, AUT `PlotBufferTests.CircularBufferPreservesOrderAcrossMultipleWraps` |
| 4.5 | AUT `MainWindowViewModelTests.XAutoscaleModeOptionsExposeFriendlyLabels`, AUT `MainWindowViewModelTests.SelectingXAutoscaleModeOptionUpdatesMode`, AUT `MainWindowViewModelTests.SettingXAutoscaleModeUpdatesSelectedOption` |
| 4.6 | MAN-PLOT-001 |
| 4.7 | AUT `SteppedXAxisViewportTests.FirstValidDataInitializesViewportWithFutureSpace`, AUT `SteppedXAxisViewportTests.UsesCustomFutureSpaceSeconds` |
| 4.8 | AUT `SteppedXAxisViewportTests.DoesNotExpandBeforeNewestReachesThreshold`, AUT `SteppedXAxisViewportTests.ExpandsWhenNewestReachesThreshold`, AUT `SteppedXAxisViewportTests.PanModePreservesVisibleWidthWhenNewestReachesThreshold` |
| 4.9 | AUT `SteppedXAxisViewportTests.ExpansionModeExpandsEvenWhenZoomedIntoRetainedRange`, AUT `SteppedXAxisViewportTests.ExpandsWhenNewestReachesThreshold` |
| 4.9.1 | AUT `SteppedXAxisViewportTests.PanModePreservesVisibleWidthWhenNewestReachesThreshold`, AUT `SteppedXAxisViewportTests.PanModePansFarEnoughWhenRetainedRangeMovesOutsideView` |
| 4.9.2 | AUT `XRangeAnimatorTests.TickInterpolatesWithEaseOutAndCompletesAtTarget`, AUT `XRangeAnimatorTests.ResetClearsActiveAnimation` |
| 4.9.3 | AUT `SteppedXAxisViewportTests.DoesNotExpandBeforeNewestReachesThreshold`, AUT `SteppedXAxisViewportTests.PanModeDoesNotRetargetFromAnimationIntermediateRange`, MAN-PLOT-001 |
| 4.10 | MAN-PLOT-001 |
| 4.11 | MAN-PLOT-001 |
| 4.12 | AUT `MainWindowViewModelTests.PausedAppendIsRetainedUntilResume`, MAN-PLOT-001 |
| 4.13 | AUT `MainWindowViewModelTests.PausedAppendIsRetainedUntilResume` |
| 4.14 | AUT `PlotBufferTests.ClearResetsCircularBuffer`, AUT `PlotBufferTests.ClearIncrementsVersionAndResetsOldestVersion`, MAN-PLOT-001 |
| 4.15 | MAN-PLOT-001 |
| 4.16 | MAN-PLOT-001 |
| 4.17 | AUT `PlotBufferTests.FixedXyRingBufferFindsNearestPointByPixelDistance`, AUT `HoverPointIndexTests.SearchUsesIndexedCandidatesInsteadOfAllPoints`, MAN-PLOT-001 |
| 4.18 | MAN-PLOT-001 |
| 4.18.1 | AUT `HoverPointIndexTests.SearchUsesIndexedCandidatesInsteadOfAllPoints`, AUT `VisiblePointMarkerPolicyTests.CountingShortCircuitsAtThreshold` |
| 4.18.2 | AUT `PlotBufferTests.FixedXyRingBufferFindNearestReturnsNullOutsideHitRadius`, AUT `HoverPointIndexTests.SearchReturnsNullWhenNoCandidateWithinHitRadius` |
| 4.19 | MAN-PLOT-001 |
| 4.20 | MAN-PLOT-001, MAN-MULTISOURCE-EXPORT-001 |
| 4.21 | AUT `PlotBufferTests.RawCsvBufferKeepsExactLines`, MAN-PLOT-001 |
| 4.22 | MAN-PLOT-001 |
| 4.23 | AUT `MainWindowViewModelTests.SelectedTracesIncludeSourceIdentity`, MAN-MULTISOURCE-001 |
| 4.24 | AUT `MainWindowViewModelTests.SelectedTracesIncludeSourceIdentity` |
| 4.25 | AUT `MainWindowViewModelTests.AddSourceSelectsAndExposesIndependentChannelCollection`, MAN-MULTISOURCE-001 |
| 4.26 | MAN-MULTISOURCE-001 |
| 4.27 | MAN-STACKED-PANELS-001 |
| 4.28 | MAN-STACKED-PANELS-001 |
| 5.1 | AUT `CsvStreamParserTests.ParsesNumbersDatesUnixAndGaps`, MAN-SOURCE-001 |
| 5.2 | AUT `SetupWindowViewModelTests.VisibilityFlagsFollowSelectedSource`, AUT `CliConfigParserTests.StdinArgsAreComplete`, AUT `CliConfigParserTests.SerialRequiresPortAndBaud`, AUT `CliConfigParserTests.TcpRequiresHostAndPort`, MAN-SOURCE-001 |
| 5.3 | AUT `CliConfigParserTests.StdinArgsAreComplete`, AUT `CliConfigParserTests.NoArgsWithRedirectedStdinStartsStdinSource` |
| 5.4 | AUT `CliConfigParserTests.SerialRequiresPortAndBaud`, AUT `SetupWindowViewModelTests.ToConfigMapsSelectedSourceFields` |
| 5.5 | MAN-SOURCE-001 |
| 5.6 | MAN-SOURCE-001 |
| 5.7 | AUT `CliConfigParserTests.TcpRequiresHostAndPort`, MAN-SOURCE-001 |
| 5.8 | AUT `SetupWindowViewModelTests.VisibilityFlagsFollowSelectedSource`, MAN-SOURCE-001 |
| 5.9 | AUT `CliConfigParserTests.UdpArgsParseResendInterval`, AUT `TestCsvLineSourceTests.UdpLineSourceResendsRequestAtConfiguredInterval`, MAN-UDP-RESEND-001 |
| 5.10 | MAN-SOURCE-001 |
| 5.11 | MAN-SOURCE-001, MAN-CSV-LINE-001 |
| 5.12 | AUT `TestCsvLineSourceTests.IndependentSourcesProduceDifferentRandomWalks`, MAN-SOURCE-001 |
| 5.13 | AUT `CliConfigParserTests.RepeatSourceSpecsCreateIndependentSources`, AUT `MainWindowViewModelTests.AddSourceSelectsAndExposesIndependentChannelCollection`, MAN-MULTISOURCE-001 |
| 5.14 | AUT `MainWindowViewModelTests.AddSourceSelectsAndExposesIndependentChannelCollection`, AUT `MainWindowViewModelTests.SelectedTracesIncludeSourceIdentity`, MAN-MULTISOURCE-001 |
| 5.15 | MAN-MULTISOURCE-001 |
| 5.16 | AUT `MainWindowViewModelTests.RemoveSelectedSourceSelectsReplacementAndDropsTraces`, AUT `MainWindowViewModelTests.LateNotificationsFromRemovedSourceAreIgnored`, MAN-MULTISOURCE-001 |
| 5.17 | AUT `CliConfigParserTests.TestSourceRequiresNoConnectionSettings`, AUT `TestCsvLineSourceTests.IndependentSourcesProduceDifferentRandomWalks` |
| 5.18 | MAN-MAIN-UI-001 |
| 6.1 | AUT `CsvGen.CsvGeneratorTests.WritesHeaderAndFixedSampleRowsAcceptedBySerialPlotParser`, AUT `CsvStreamParserTests.HeaderRejectsBlankAndDuplicateNames` |
| 6.2 | AUT `CsvStreamParserTests.HeaderRejectsBlankAndDuplicateNames`, MAN-CHANNEL-001 |
| 6.3 | AUT `CsvStreamParserTests.HeaderRejectsBlankAndDuplicateNames` |
| 6.4 | MAN-CSV-LINE-001 |
| 6.5 | MAN-CSV-LINE-001 |
| 6.6 | INS-PLATFORM-001 |
| 6.7 | AUT `CsvStreamParserTests.RowRejectsMismatchedColumnCount` |
| 6.8 | AUT `CsvStreamParserTests.RowRejectsMismatchedColumnCount`, MAN-MULTISOURCE-001 |
| 6.9 | MAN-MULTISOURCE-001 |
| 7.1 | AUT `CsvStreamParserTests.ParsesNumbersDatesUnixAndGaps` |
| 7.2 | AUT `CsvStreamParserTests.ParsesNumbersDatesUnixAndGaps` |
| 7.3 | AUT `CsvStreamParserTests.ParsesNumbersDatesUnixAndGaps` |
| 7.4 | AUT `CsvStreamParserTests.ParsesNumbersDatesUnixAndGaps` |
| 7.5 | AUT `CsvStreamParserTests.ParsesNumbersDatesUnixAndGaps`, MAN-CSV-LINE-001 |
| 7.6 | AUT `CsvStreamParserTests.ParsesNumbersDatesUnixAndGaps` |
| 7.7 | AUT `CsvStreamParserTests.ParsesNumbersDatesUnixAndGaps`, MAN-CSV-LINE-001 |
| 7.8 | AUT `CliConfigParserTests.StdinArgsAreComplete`, AUT `SetupWindowViewModelTests.ToConfigMapsSelectedSourceFields` |
| 7.9 | AUT `CsvStreamParserTests.ColumnEligibilityFollowsObservedValues` |
| 7.10 | AUT `CsvStreamParserTests.ColumnEligibilityFollowsObservedValues` |
| 7.11 | AUT `CsvStreamParserTests.ColumnEligibilityFollowsObservedValues` |
| 7.12 | AUT `CsvStreamParserTests.ColumnEligibilityFollowsObservedValues` |
| 7.13 | AUT `CsvStreamParserTests.ParsesNumbersDatesUnixAndGaps`, AUT `PlotBufferTests.CopyValidPairsDropsInvalidMissingAndNonFiniteSamples` |
| 7.14 | AUT `CsvStreamParserTests.ColumnEligibilityFollowsObservedValues` |
| 8.1 | MAN-PLOT-001 |
| 8.2 | MAN-MULTISOURCE-EXPORT-001 |
| 8.3 | AUT `PlotBufferTests.RawCsvBufferKeepsExactLines`, MAN-MULTISOURCE-EXPORT-001 |
| 9.1 | AUT `CsvGen.CsvGenOptionsParserTests.DefaultsProvideUsefulLiveChannels`, AUT `CsvGen.CsvGeneratorTests.WritesHeaderAndFixedSampleRowsAcceptedBySerialPlotParser` |
| 9.2 | AUT `CsvGen.CsvGenOptionsParserTests.ParsesTcpListenPort`, MAN-SOURCE-001 |

## Automated Test Traceability

| Automated test | Parent requirement(s) |
| --- | --- |
| `SteppedXAxisViewportTests.FirstValidDataInitializesViewportWithFutureSpace` | 4.7, 4.9 |
| `SteppedXAxisViewportTests.UsesCustomFutureSpaceSeconds` | 2.10, 4.7 |
| `SteppedXAxisViewportTests.DoesNotExpandBeforeNewestReachesThreshold` | 4.8, 4.9.3 |
| `SteppedXAxisViewportTests.ExpandsWhenNewestReachesThreshold` | 4.8, 4.9 |
| `SteppedXAxisViewportTests.ExpansionModeExpandsEvenWhenZoomedIntoRetainedRange` | 4.9 |
| `SteppedXAxisViewportTests.PanModePreservesVisibleWidthWhenNewestReachesThreshold` | 4.8, 4.9.1 |
| `SteppedXAxisViewportTests.PanModePansFarEnoughWhenRetainedRangeMovesOutsideView` | 4.9.1 |
| `SteppedXAxisViewportTests.PanModeDoesNotRetargetFromAnimationIntermediateRange` | 4.9.3 |
| `SteppedXAxisViewportTests.ResetClearsCurrentTarget` | 4.5 |
| `SteppedXAxisViewportTests.InvalidEstimateFallsBackToSmallPositiveSpan` | 4.7 |
| `TestCsvLineSourceTests.IndependentSourcesProduceDifferentRandomWalks` | 5.12, 5.17 |
| `TestCsvLineSourceTests.UdpLineSourceSendsInitialRequestOnlyWhenResendDisabled` | 2.13 |
| `TestCsvLineSourceTests.UdpLineSourceResendsRequestAtConfiguredInterval` | 5.9 |
| `SetupWindowViewModelTests.VisibilityFlagsFollowSelectedSource` | 2.5, 5.2, 5.8 |
| `SetupWindowViewModelTests.ChangingSourceRaisesVisibilityNotifications` | 2.5 |
| `SetupWindowViewModelTests.ToConfigMapsSelectedSourceFields` | 2.5, 2.7, 2.12, 5.4, 7.8 |
| `SetupWindowViewModelTests.RecentHistoryPrefillsLastUsedSourceAndMostRecentEntry` | 2.16 |
| `SetupWindowViewModelTests.RecentHistoryDropdownIsFilteredBySourceType` | 2.17 |
| `SetupWindowViewModelTests.SelectingRecentEntryAppliesAllSetupFields` | 2.15, 2.17 |
| `SetupWindowViewModelTests.InitialConfigIgnoresRecentHistory` | 2.19 |
| `VisiblePointMarkerPolicyTests.ShowsMarkersWhenVisiblePointCountIsBelowThreshold` | 4.17 |
| `VisiblePointMarkerPolicyTests.HidesMarkersWhenVisiblePointCountReachesThreshold` | 4.18.1 |
| `VisiblePointMarkerPolicyTests.IgnoresInvalidAndOutsideVisibleRangePoints` | 4.18.1 |
| `VisiblePointMarkerPolicyTests.CountingShortCircuitsAtThreshold` | 4.18.1 |
| `MainWindowViewModelTests.XAutoscaleModeOptionsExposeFriendlyLabels` | 3.10, 4.5 |
| `MainWindowViewModelTests.SelectingXAutoscaleModeOptionUpdatesMode` | 3.10, 4.5 |
| `MainWindowViewModelTests.SettingXAutoscaleModeUpdatesSelectedOption` | 3.10, 4.5 |
| `MainWindowViewModelTests.FutureSpaceControlIsEnabledOnlyForSteppedExpansion` | 2.10 |
| `MainWindowViewModelTests.AddSourceSelectsAndExposesIndependentChannelCollection` | 3.11, 3.12, 4.25, 5.13, 5.14 |
| `MainWindowViewModelTests.SelectedTracesIncludeSourceIdentity` | 3.8, 4.23, 4.24, 5.14 |
| `MainWindowViewModelTests.RemoveSelectedSourceSelectsReplacementAndDropsTraces` | 3.11, 5.16 |
| `MainWindowViewModelTests.LateNotificationsFromRemovedSourceAreIgnored` | 3.11, 5.16 |
| `MainWindowViewModelTests.AppendThrottlingPreservesDirtySources` | 4.3, 5.13 |
| `MainWindowViewModelTests.PausedAppendIsRetainedUntilResume` | 4.12, 4.13 |
| `PlotBufferTests.CircularBufferCapsRowsAndPreservesOrder` | 4.4 |
| `PlotBufferTests.CircularBufferPreservesOrderAcrossMultipleWraps` | 4.4 |
| `PlotBufferTests.VersionIncrementsAndRowsEnumerateChronologicallyAfterWrap` | 4.4 |
| `PlotBufferTests.ClearResetsCircularBuffer` | 4.14 |
| `PlotBufferTests.ClearIncrementsVersionAndResetsOldestVersion` | 4.14 |
| `PlotBufferTests.CopyValidPairsDropsInvalidMissingAndNonFiniteSamples` | 7.13 |
| `PlotBufferTests.CopyValidPairsSinceOnlyCopiesNewerRows` | 4.3 |
| `PlotBufferTests.CopyValidPairsSinceAfterWrapSkipsOnlyOldestRowWhenAfterVersionIsOldestVersion` | 4.3, 4.4 |
| `PlotBufferTests.CopyValidPairsSinceAfterWrapCopiesOnlyTailRows` | 4.3, 4.4 |
| `PlotBufferTests.CopyValidPairsSinceAfterWrapCopiesAllRetainedRowsWhenAfterVersionIsStale` | 4.3, 4.4 |
| `PlotBufferTests.FixedXyRingBufferReportsEmptyPartialFullWrappedAndClearedSegments` | 4.4 |
| `PlotBufferTests.FixedXyRingBufferDropsInvalidAppendsAndNeverWritesNaN` | 7.13 |
| `PlotBufferTests.FixedXyRingBufferDropsNonAscendingXValues` | 4.3 |
| `PlotBufferTests.FixedXyRingBufferReportsOldestNewestAndRecentSpacingAfterWrap` | 4.4, 4.7 |
| `PlotBufferTests.FixedXyRingBufferFindsNearestPointByPixelDistance` | 4.17 |
| `PlotBufferTests.FixedXyRingBufferFindNearestReturnsNullOutsideHitRadius` | 4.18.2 |
| `PlotBufferTests.RawCsvBufferKeepsExactLines` | 4.21, 8.3 |
| `HoverPointIndexTests.SearchUsesIndexedCandidatesInsteadOfAllPoints` | 4.17, 4.18.1 |
| `HoverPointIndexTests.RebuildIgnoresInvalidAndOutsideVisibleXRangePoints` | 4.18.1 |
| `HoverPointIndexTests.SearchReturnsNullWhenNoCandidateWithinHitRadius` | 4.18.2 |
| `UserPreferencesServiceTests.MissingFileReturnsDefaults` | 2.6, 2.10 |
| `UserPreferencesServiceTests.ValidJsonRestoresXAutoscaleMode` | 2.9 |
| `UserPreferencesServiceTests.InvalidJsonReturnsDefaults` | 2.9 |
| `UserPreferencesServiceTests.UnknownModeReturnsDefaults` | 2.9 |
| `UserPreferencesServiceTests.FutureSpaceSecondsClampsOutOfRangeValues` | 2.10 |
| `UserPreferencesServiceTests.SteppedPanModeLoadsAndSaves` | 2.9 |
| `UserPreferencesServiceTests.SaveWritesSelectedModeAndFutureSpace` | 2.9 |
| `RecentSetupServiceTests.MissingFileReturnsEmptyHistory` | 2.20 |
| `RecentSetupServiceTests.InvalidJsonReturnsEmptyHistory` | 2.20 |
| `RecentSetupServiceTests.SaveAndLoadRestoresLastSourceAndEntries` | 2.15 |
| `RecentSetupServiceTests.RememberMovesMatchingEntryToMostRecent` | 2.18 |
| `RecentSetupServiceTests.RecentEntriesAreCappedPerSourceType` | 2.14 |
| `XRangeAnimatorTests.TickInterpolatesWithEaseOutAndCompletesAtTarget` | 4.9.2 |
| `XRangeAnimatorTests.ResetClearsActiveAnimation` | 4.9.2 |
| `CsvStreamParserTests.HeaderRejectsBlankAndDuplicateNames` | 4.2, 6.1, 6.2, 6.3 |
| `CsvStreamParserTests.RowRejectsMismatchedColumnCount` | 6.7, 6.8 |
| `CsvStreamParserTests.ParsesNumbersDatesUnixAndGaps` | 5.1, 7.1, 7.2, 7.3, 7.4, 7.6, 7.13 |
| `CsvStreamParserTests.ColumnEligibilityFollowsObservedValues` | 7.9, 7.10, 7.11, 7.12, 7.14 |
| `CliConfigParserTests.NoArgsRequestsSetup` | 2.2, 2.6 |
| `CliConfigParserTests.NoArgsWithRedirectedStdinStartsStdinSource` | 5.3 |
| `CliConfigParserTests.StdinArgsAreComplete` | 2.1, 2.5, 5.3, 7.8 |
| `CliConfigParserTests.UdpArgsParseResendInterval` | 2.12, 5.9 |
| `CliConfigParserTests.ZeroUdpResendIntervalDisablesResend` | 2.13 |
| `CliConfigParserTests.NegativeUdpResendIntervalIsInvalid` | 2.12 |
| `CliConfigParserTests.SerialRequiresPortAndBaud` | 2.3, 5.2, 5.4 |
| `CliConfigParserTests.TcpRequiresHostAndPort` | 2.3, 5.2, 5.7 |
| `CliConfigParserTests.TestSourceRequiresNoConnectionSettings` | 5.17 |
| `CliConfigParserTests.RepeatSourceSpecsCreateIndependentSources` | 2.1, 2.11, 2.12, 5.13 |
| `CliConfigParserTests.InvalidSourceSpecReportsPerSourceValidation` | 2.11 |
| `ChannelViewModelTests.TraceBrushesCanBeAssignedAndClearedPerAxis` | 3.8 |
| `CsvGen.CsvGenOptionsParserTests.DefaultsProvideUsefulLiveChannels` | 9.1 |
| `CsvGen.CsvGenOptionsParserTests.ParsesRepeatedChannelSpecsAndFiniteSamples` | 9.1 |
| `CsvGen.CsvGenOptionsParserTests.RejectsDuplicateNamesAndConflictingLimits` | 9.1 |
| `CsvGen.CsvGenOptionsParserTests.ParsesTcpListenPort` | 9.2 |
| `CsvGen.CsvGeneratorTests.WaveformsProduceExpectedKnownSamples` | 9.1 |
| `CsvGen.CsvGeneratorTests.WritesHeaderAndFixedSampleRowsAcceptedBySerialPlotParser` | 6.1, 9.1 |
| `CsvGen.CsvGeneratorTests.SeededRandomOutputIsDeterministic` | 9.1 |

## Automated Coverage Gaps

The following requirements are covered only by manual or inspection cases in this VCRM and do not currently have direct automated test coverage:

1.1, 1.2, 1.3, 1.4, 2.4, 2.8, 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 3.7, 3.9, 3.13, 4.1, 4.6, 4.10, 4.11, 4.15, 4.16, 4.18, 4.19, 4.20, 4.22, 4.26, 4.27, 4.28, 5.5, 5.6, 5.10, 5.11, 5.15, 5.16, 5.18, 6.4, 6.5, 6.6, 6.9, 7.5, 7.7, 8.1, 8.2, 9.2.

Requirements 4.27 and 4.28 are especially important to review because the current design document describes a single ScottPlot plot area, while these requirements call for stacked plot panels and per-trace placement.
