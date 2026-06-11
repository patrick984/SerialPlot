using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using SerialPlot.Models;

namespace SerialPlot.Services;

public sealed record UserPreferences(XAutoscaleMode XAutoscaleMode)
{
    public static UserPreferences Defaults { get; } = new(XAutoscaleMode.ContinuousFollowNewest);
}

public sealed class UserPreferencesService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly string _path;

    public UserPreferencesService()
        : this(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SerialPlot",
            "preferences.json"))
    {
    }

    public UserPreferencesService(string path)
    {
        _path = path;
    }

    public async Task<UserPreferences> LoadAsync()
    {
        try
        {
            if (!File.Exists(_path))
            {
                return UserPreferences.Defaults;
            }

            await using var stream = File.OpenRead(_path);
            var preferences = await JsonSerializer.DeserializeAsync<UserPreferences>(stream, JsonOptions).ConfigureAwait(false);
            return preferences is { } value && Enum.IsDefined(value.XAutoscaleMode)
                ? value
                : UserPreferences.Defaults;
        }
        catch
        {
            return UserPreferences.Defaults;
        }
    }

    public async Task SaveAsync(UserPreferences preferences)
    {
        var directory = Path.GetDirectoryName(_path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var stream = File.Create(_path);
        await JsonSerializer.SerializeAsync(stream, preferences, JsonOptions).ConfigureAwait(false);
    }
}
