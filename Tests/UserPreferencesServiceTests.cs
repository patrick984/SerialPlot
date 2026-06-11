using System;
using System.IO;
using System.Threading.Tasks;
using SerialPlot.Models;
using SerialPlot.Services;
using Xunit;

namespace SerialPlot.Tests;

public sealed class UserPreferencesServiceTests
{
    [Fact]
    public async Task MissingFileReturnsDefaults()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "preferences.json");
        var service = new UserPreferencesService(path);

        var preferences = await service.LoadAsync();

        Assert.Equal(XAutoscaleMode.ContinuousFollowNewest, preferences.XAutoscaleMode);
        Assert.Equal(UserPreferences.DefaultSteppedFutureSpaceSeconds, preferences.SteppedFutureSpaceSeconds);
    }

    [Fact]
    public async Task ValidJsonRestoresXAutoscaleMode()
    {
        var path = CreateTempPath();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllTextAsync(path, """{"XAutoscaleMode":"SteppedExpansion"}""");
        var service = new UserPreferencesService(path);

        var preferences = await service.LoadAsync();

        Assert.Equal(XAutoscaleMode.SteppedExpansion, preferences.XAutoscaleMode);
        Assert.Equal(UserPreferences.DefaultSteppedFutureSpaceSeconds, preferences.SteppedFutureSpaceSeconds);
    }

    [Fact]
    public async Task InvalidJsonReturnsDefaults()
    {
        var path = CreateTempPath();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllTextAsync(path, "{not-json");
        var service = new UserPreferencesService(path);

        var preferences = await service.LoadAsync();

        Assert.Equal(XAutoscaleMode.ContinuousFollowNewest, preferences.XAutoscaleMode);
    }

    [Fact]
    public async Task UnknownModeReturnsDefaults()
    {
        var path = CreateTempPath();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllTextAsync(path, """{"XAutoscaleMode":999}""");
        var service = new UserPreferencesService(path);

        var preferences = await service.LoadAsync();

        Assert.Equal(XAutoscaleMode.ContinuousFollowNewest, preferences.XAutoscaleMode);
    }

    [Fact]
    public async Task FutureSpaceSecondsClampsOutOfRangeValues()
    {
        var path = CreateTempPath();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllTextAsync(path, """{"XAutoscaleMode":"SteppedExpansion","SteppedFutureSpaceSeconds":999}""");
        var service = new UserPreferencesService(path);

        var preferences = await service.LoadAsync();

        Assert.Equal(XAutoscaleMode.SteppedExpansion, preferences.XAutoscaleMode);
        Assert.Equal(UserPreferences.MaximumSteppedFutureSpaceSeconds, preferences.SteppedFutureSpaceSeconds);
    }

    [Fact]
    public async Task SteppedPanModeLoadsAndSaves()
    {
        var path = CreateTempPath();
        var service = new UserPreferencesService(path);

        await service.SaveAsync(new UserPreferences(XAutoscaleMode.SteppedPan, 60));
        var preferences = await service.LoadAsync();

        Assert.Equal(XAutoscaleMode.SteppedPan, preferences.XAutoscaleMode);
        Assert.Equal(60, preferences.SteppedFutureSpaceSeconds);
    }

    [Fact]
    public async Task SaveWritesSelectedModeAndFutureSpace()
    {
        var path = CreateTempPath();
        var service = new UserPreferencesService(path);

        await service.SaveAsync(new UserPreferences(XAutoscaleMode.SteppedExpansion, 45));
        var preferences = await service.LoadAsync();

        Assert.Equal(XAutoscaleMode.SteppedExpansion, preferences.XAutoscaleMode);
        Assert.Equal(45, preferences.SteppedFutureSpaceSeconds);
    }

    private static string CreateTempPath()
        => Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "preferences.json");
}
