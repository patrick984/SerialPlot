using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using SerialPlot.Models;

namespace SerialPlot.Services;

public sealed record UserPreferences(XAutoscaleMode XAutoscaleMode, int SteppedFutureSpaceSeconds, double PlotLineWidth)
{
    public const int DefaultSteppedFutureSpaceSeconds = 30;
    public const int MinimumSteppedFutureSpaceSeconds = 1;
    public const int MaximumSteppedFutureSpaceSeconds = 300;
    public const double DefaultPlotLineWidth = 1;
    public const double MinimumPlotLineWidth = 1;
    public const double MaximumPlotLineWidth = 10;

    public static UserPreferences Defaults { get; } = new(
        XAutoscaleMode.ContinuousFollowNewest,
        DefaultSteppedFutureSpaceSeconds,
        DefaultPlotLineWidth);

    public static int ClampSteppedFutureSpaceSeconds(int value)
        => Math.Clamp(value, MinimumSteppedFutureSpaceSeconds, MaximumSteppedFutureSpaceSeconds);

    public static double ClampPlotLineWidth(double value)
        => double.IsFinite(value)
            ? Math.Clamp(value, MinimumPlotLineWidth, MaximumPlotLineWidth)
            : DefaultPlotLineWidth;

    public UserPreferences Normalize()
    {
        var mode = Enum.IsDefined(XAutoscaleMode) ? XAutoscaleMode : Defaults.XAutoscaleMode;
        return new UserPreferences(mode, ClampSteppedFutureSpaceSeconds(SteppedFutureSpaceSeconds), ClampPlotLineWidth(PlotLineWidth));
    }
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
            if (preferences is null)
            {
                return UserPreferences.Defaults;
            }

            stream.Position = 0;
            using var document = await JsonDocument.ParseAsync(stream).ConfigureAwait(false);
            var hasFutureSpace = document.RootElement.TryGetProperty(nameof(UserPreferences.SteppedFutureSpaceSeconds), out _);
            var hasPlotLineWidth = document.RootElement.TryGetProperty(nameof(UserPreferences.PlotLineWidth), out _);
            preferences = hasFutureSpace
                ? preferences
                : preferences with { SteppedFutureSpaceSeconds = UserPreferences.DefaultSteppedFutureSpaceSeconds };
            preferences = hasPlotLineWidth
                ? preferences
                : preferences with { PlotLineWidth = UserPreferences.DefaultPlotLineWidth };
            return preferences.Normalize();
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
        await JsonSerializer.SerializeAsync(stream, preferences.Normalize(), JsonOptions).ConfigureAwait(false);
    }
}
