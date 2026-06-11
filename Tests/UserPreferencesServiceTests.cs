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
    public async Task SaveWritesSelectedMode()
    {
        var path = CreateTempPath();
        var service = new UserPreferencesService(path);

        await service.SaveAsync(new UserPreferences(XAutoscaleMode.SteppedExpansion));
        var preferences = await service.LoadAsync();

        Assert.Equal(XAutoscaleMode.SteppedExpansion, preferences.XAutoscaleMode);
    }

    private static string CreateTempPath()
        => Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "preferences.json");
}
