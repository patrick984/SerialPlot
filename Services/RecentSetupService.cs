using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using SerialPlot.Models;

namespace SerialPlot.Services;

public sealed record RecentSetupEntry(InputSourceConfig Config, DateTimeOffset LastUsedUtc);

public sealed record RecentSetupHistory(SourceType LastSource, IReadOnlyList<RecentSetupEntry> Entries)
{
    public const int MaxEntriesPerSource = 5;

    public static RecentSetupHistory Empty { get; } = new(SourceType.Stdin, Array.Empty<RecentSetupEntry>());

    public RecentSetupHistory Normalize()
    {
        var lastSource = Enum.IsDefined(LastSource) ? LastSource : SourceType.Stdin;
        var entries = (Entries ?? Array.Empty<RecentSetupEntry>())
            .Where(x => Enum.IsDefined(x.Config.Source))
            .GroupBy(x => EntryKey(x.Config))
            .Select(x => x.OrderByDescending(e => e.LastUsedUtc).First())
            .GroupBy(x => x.Config.Source)
            .SelectMany(x => x
                .OrderByDescending(e => e.LastUsedUtc)
                .Take(MaxEntriesPerSource))
            .OrderByDescending(x => x.LastUsedUtc)
            .ToArray();

        return new RecentSetupHistory(lastSource, entries);
    }

    public RecentSetupHistory Remember(InputSourceConfig config, DateTimeOffset now)
    {
        var updated = new RecentSetupEntry(config, now);
        var entries = Entries
            .Where(x => !string.Equals(EntryKey(x.Config), EntryKey(config), StringComparison.Ordinal))
            .Append(updated);

        return new RecentSetupHistory(config.Source, entries.ToArray()).Normalize();
    }

    private static string EntryKey(InputSourceConfig config)
        => JsonSerializer.Serialize(config, RecentSetupService.JsonOptions);
}

public sealed class RecentSetupService
{
    internal static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly string _path;

    public RecentSetupService()
        : this(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SerialPlot",
            "recent-setups.json"))
    {
    }

    public RecentSetupService(string path)
    {
        _path = path;
    }

    public async Task<RecentSetupHistory> LoadAsync()
    {
        try
        {
            if (!File.Exists(_path))
            {
                return RecentSetupHistory.Empty;
            }

            await using var stream = File.OpenRead(_path);
            var history = await JsonSerializer.DeserializeAsync<RecentSetupHistory>(stream, JsonOptions).ConfigureAwait(false);
            return history?.Normalize() ?? RecentSetupHistory.Empty;
        }
        catch
        {
            return RecentSetupHistory.Empty;
        }
    }

    public async Task SaveAsync(RecentSetupHistory history)
    {
        var directory = Path.GetDirectoryName(_path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var stream = File.Create(_path);
        await JsonSerializer.SerializeAsync(stream, history.Normalize(), JsonOptions).ConfigureAwait(false);
    }

    public async Task RememberAsync(InputSourceConfig config)
    {
        try
        {
            var history = await LoadAsync().ConfigureAwait(false);
            await SaveAsync(history.Remember(config, DateTimeOffset.UtcNow)).ConfigureAwait(false);
        }
        catch
        {
        }
    }
}
