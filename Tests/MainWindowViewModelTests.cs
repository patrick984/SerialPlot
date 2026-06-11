using System;
using System.IO;
using System.Linq;
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

    private static MainWindowViewModel CreateViewModel()
        => new(
            AppConfig.Defaults(),
            new TextReaderLineSource(TextReader.Null),
            new UserPreferencesService(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "preferences.json")));
}
