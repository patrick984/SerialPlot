using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SerialPlot.Models;
using SerialPlot.Services;
using Xunit;

namespace SerialPlot.Tests;

public sealed class RecentSetupServiceTests
{
    [Fact]
    public async Task MissingFileReturnsEmptyHistory()
    {
        var service = new RecentSetupService(CreateTempPath());

        var history = await service.LoadAsync();

        Assert.Equal(SourceType.Stdin, history.LastSource);
        Assert.Empty(history.Entries);
    }

    [Fact]
    public async Task InvalidJsonReturnsEmptyHistory()
    {
        var path = CreateTempPath();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllTextAsync(path, "{not-json");
        var service = new RecentSetupService(path);

        var history = await service.LoadAsync();

        Assert.Equal(SourceType.Stdin, history.LastSource);
        Assert.Empty(history.Entries);
    }

    [Fact]
    public async Task SaveAndLoadRestoresLastSourceAndEntries()
    {
        var path = CreateTempPath();
        var service = new RecentSetupService(path);
        var history = RecentSetupHistory.Empty.Remember(UdpConfig("imu", 55123), DateTimeOffset.UtcNow);

        await service.SaveAsync(history);
        var loaded = await service.LoadAsync();

        Assert.Equal(SourceType.Udp, loaded.LastSource);
        var entry = Assert.Single(loaded.Entries);
        Assert.Equal("imu", entry.Config.Name);
        Assert.Equal(55123, entry.Config.Port);
    }

    [Fact]
    public async Task RememberMovesMatchingEntryToMostRecent()
    {
        var path = CreateTempPath();
        var service = new RecentSetupService(path);
        var config = UdpConfig("imu", 55123);
        await service.SaveAsync(RecentSetupHistory.Empty.Remember(config, DateTimeOffset.UtcNow.AddMinutes(-5)));

        await service.RememberAsync(config);
        var loaded = await service.LoadAsync();

        Assert.Single(loaded.Entries);
        Assert.Equal(SourceType.Udp, loaded.LastSource);
    }

    [Fact]
    public async Task RecentEntriesAreCappedPerSourceType()
    {
        var path = CreateTempPath();
        var service = new RecentSetupService(path);
        var history = RecentSetupHistory.Empty;
        for (var i = 0; i < 7; i++)
        {
            history = history.Remember(UdpConfig($"udp-{i}", 55000 + i), DateTimeOffset.UtcNow.AddMinutes(i));
        }

        await service.SaveAsync(history);
        var loaded = await service.LoadAsync();

        var udpEntries = loaded.Entries.Where(x => x.Config.Source == SourceType.Udp).ToArray();
        Assert.Equal(RecentSetupHistory.MaxEntriesPerSource, udpEntries.Length);
        Assert.DoesNotContain(udpEntries, x => x.Config.Name == "udp-0");
        Assert.DoesNotContain(udpEntries, x => x.Config.Name == "udp-1");
    }

    private static InputSourceConfig UdpConfig(string name, int port) => new(
        name,
        SourceType.Udp,
        null,
        null,
        "127.0.0.1",
        port,
        "poll",
        5,
        AppConfig.DefaultBufferSize,
        TimestampUnit.Auto,
        "time",
        ["ax"],
        ["lat"]);

    private static string CreateTempPath()
        => Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "recent-setups.json");
}
