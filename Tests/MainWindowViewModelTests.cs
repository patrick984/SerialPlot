using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using SerialPlot.Models;
using SerialPlot.Services;
using SerialPlot.ViewModels;
using Xunit;

namespace SerialPlot.Tests;

public sealed class MainWindowViewModelTests
{
    [Fact]
    public void XAutoscaleModeOptionsExposeFriendlyLabels()
    {
        var vm = CreateViewModel();

        Assert.Equal(
            ["Continuous Follow", "Stepped Expand", "Stepped Pan"],
            vm.XAutoscaleModeOptions.Select(x => x.Label).ToArray());
    }

    [Fact]
    public void SelectingXAutoscaleModeOptionUpdatesMode()
    {
        var vm = CreateViewModel();

        vm.SelectedXAutoscaleModeOption = vm.XAutoscaleModeOptions.Single(x => x.Mode == XAutoscaleMode.SteppedPan);

        Assert.Equal(XAutoscaleMode.SteppedPan, vm.XAutoscaleMode);
    }

    [Fact]
    public void SettingXAutoscaleModeUpdatesSelectedOption()
    {
        var vm = CreateViewModel();

        vm.XAutoscaleMode = XAutoscaleMode.SteppedPan;

        Assert.Equal("Stepped Pan", vm.SelectedXAutoscaleModeOption.Label);
    }

    [Fact]
    public void FutureSpaceControlIsEnabledOnlyForSteppedExpansion()
    {
        var vm = CreateViewModel();

        vm.XAutoscaleMode = XAutoscaleMode.SteppedExpansion;
        Assert.True(vm.IsSteppedExpansionSelected);

        vm.XAutoscaleMode = XAutoscaleMode.SteppedPan;
        Assert.False(vm.IsSteppedExpansionSelected);
    }

    [Fact]
    public void PlotLineWidthClampsToPreferenceRange()
    {
        var vm = CreateViewModel();

        vm.PlotLineWidth = 999;

        Assert.Equal(UserPreferences.MaximumPlotLineWidth, vm.PlotLineWidth);
    }

    [Fact]
    public void PlotLineWidthChangeRaisesRebuild()
    {
        var vm = CreateViewModel();
        PlotDataChangedEventArgs? received = null;
        vm.PlotDataChanged += (_, args) => received = args;

        vm.PlotLineWidth = 2.5;

        Assert.NotNull(received);
        Assert.Equal(PlotDataChangeKind.Rebuild, received.Kind);
    }

    [Fact]
    public void AddSourceSelectsAndExposesIndependentChannelCollection()
    {
        var vm = CreateViewModel();
        var added = new InputSourceViewModel(SourceConfig("second"), new TextReaderLineSource(TextReader.Null));

        vm.AddSource(added);

        Assert.Equal(2, vm.Sources.Count);
        Assert.Same(added, vm.SelectedSource);
        Assert.Same(added.Channels, vm.Channels);
    }

    [Fact]
    public void SelectedTracesIncludeSourceIdentity()
    {
        var sourceA = new InputSourceViewModel(SourceConfig("a"), new TextReaderLineSource(TextReader.Null));
        var sourceB = new InputSourceViewModel(SourceConfig("b"), new TextReaderLineSource(TextReader.Null));
        var vm = new MainWindowViewModel(
            AppConfig.Defaults(),
            [sourceA, sourceB],
            new UserPreferencesService(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "preferences.json")));
        var channelA = new ChannelViewModel("value", 1) { CanBeY = true, IsSelectedLeft = true };
        var channelB = new ChannelViewModel("value", 1) { CanBeY = true, IsSelectedRight = true };
        sourceA.Channels.Add(channelA);
        sourceB.Channels.Add(channelB);

        var traces = vm.SelectedTraces;

        Assert.Contains(traces, x => ReferenceEquals(x.Source, sourceA) && x.Channel == channelA && x.Side == TraceAxisSide.Left);
        Assert.Contains(traces, x => ReferenceEquals(x.Source, sourceB) && x.Channel == channelB && x.Side == TraceAxisSide.Right);
    }

    [Fact]
    public async Task RemoveSelectedSourceSelectsReplacementAndDropsTraces()
    {
        var sourceA = new InputSourceViewModel(SourceConfig("a"), new TextReaderLineSource(TextReader.Null));
        var sourceB = new InputSourceViewModel(SourceConfig("b"), new TextReaderLineSource(TextReader.Null));
        var vm = CreateViewModel(sourceA, sourceB);
        var removedChannel = new ChannelViewModel("value", 1) { CanBeY = true, IsSelectedLeft = true };
        sourceB.Channels.Add(removedChannel);

        await vm.RemoveSourceAsync(sourceB);

        Assert.DoesNotContain(sourceB, vm.Sources);
        Assert.Same(sourceA, vm.SelectedSource);
        Assert.Same(sourceA.Channels, vm.Channels);
        Assert.DoesNotContain(vm.SelectedTraces, x => ReferenceEquals(x.Source, sourceB));
    }

    [Fact]
    public async Task LateNotificationsFromRemovedSourceAreIgnored()
    {
        var sourceA = new InputSourceViewModel(SourceConfig("a"), new TextReaderLineSource(TextReader.Null));
        var sourceB = new InputSourceViewModel(SourceConfig("b"), new TextReaderLineSource(TextReader.Null));
        var vm = CreateViewModel(sourceA, sourceB);
        var events = new System.Collections.Generic.List<PlotDataChangedEventArgs>();
        vm.PlotDataChanged += (_, args) => events.Add(args);

        await vm.RemoveSourceAsync(sourceA);
        events.Clear();
        Thread.Sleep(40);
        RaiseSourceAppend(vm, sourceA, 1);

        Assert.Empty(events);
        Assert.DoesNotContain(sourceA, vm.Sources);
    }

    [Fact]
    public void AppendThrottlingPreservesDirtySources()
    {
        var sourceA = new InputSourceViewModel(SourceConfig("a"), new TextReaderLineSource(TextReader.Null));
        var sourceB = new InputSourceViewModel(SourceConfig("b"), new TextReaderLineSource(TextReader.Null));
        var vm = CreateViewModel(sourceA, sourceB);
        var events = new System.Collections.Generic.List<PlotDataChangedEventArgs>();
        vm.PlotDataChanged += (_, args) => events.Add(args);

        Thread.Sleep(40);
        RaiseSourceAppend(vm, sourceA, 1);
        RaiseSourceAppend(vm, sourceB, 1);
        Thread.Sleep(40);
        RaiseSourceAppend(vm, sourceA, 2);

        Assert.Equal(2, events.Count);
        Assert.Contains(sourceA, events[0].DirtySourceVersions.Keys);
        Assert.DoesNotContain(sourceB, events[0].DirtySourceVersions.Keys);
        Assert.Contains(sourceA, events[1].DirtySourceVersions.Keys);
        Assert.Contains(sourceB, events[1].DirtySourceVersions.Keys);
        Assert.Equal(1, events[1].DirtySourceVersions[sourceB]);
    }

    [Fact]
    public void PausedAppendIsRetainedUntilResume()
    {
        var source = new InputSourceViewModel(SourceConfig("a"), new TextReaderLineSource(TextReader.Null));
        var vm = CreateViewModel(source);
        var events = new System.Collections.Generic.List<PlotDataChangedEventArgs>();
        vm.PlotDataChanged += (_, args) => events.Add(args);

        Thread.Sleep(40);
        vm.IsPaused = true;
        RaiseSourceAppend(vm, source, 1);
        Thread.Sleep(40);
        vm.IsPaused = false;

        var append = Assert.Single(events);
        Assert.Contains(source, append.DirtySourceVersions.Keys);
        Assert.Equal(1, append.DirtySourceVersions[source]);
    }

    private static MainWindowViewModel CreateViewModel()
        => new(
            AppConfig.Defaults(),
            new TextReaderLineSource(TextReader.Null),
            new UserPreferencesService(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "preferences.json")));

    private static MainWindowViewModel CreateViewModel(params InputSourceViewModel[] sources)
        => new(
            AppConfig.Defaults(),
            sources,
            new UserPreferencesService(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "preferences.json")));

    private static void RaiseSourceAppend(MainWindowViewModel vm, InputSourceViewModel source, long version)
    {
        var method = typeof(MainWindowViewModel)
            .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
            .Single(x =>
                x.Name == "RaisePlotDataChanged"
                && x.GetParameters() is [{ ParameterType: var parameterType }]
                && parameterType == typeof(SourceDataChangedEventArgs));

        method.Invoke(vm, [new SourceDataChangedEventArgs(source, PlotDataChangeKind.Append, version)]);
    }

    private static InputSourceConfig SourceConfig(string name)
        => new(
            name,
            SourceType.Stdin,
            null,
            null,
            null,
            null,
            null,
            null,
            AppConfig.DefaultBufferSize,
            TimestampUnit.Auto,
            null,
            [],
            []);
}
