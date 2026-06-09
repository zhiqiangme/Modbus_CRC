using System.Text.Json;
using Project.Core;
using Project.Models;

namespace Project.Infrastructure;

public sealed class AppPreferencesService : IAppPreferencesService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _settingsFilePath;

    public AppPreferencesService()
    {
        var directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ModbusFrameTool");

        Directory.CreateDirectory(directory);
        _settingsFilePath = Path.Combine(directory, "app-preferences.json");
    }

    public async Task<AppPreferences> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_settingsFilePath))
        {
            return new AppPreferences();
        }

        try
        {
            await using var stream = File.OpenRead(_settingsFilePath);
            var preferences = await JsonSerializer.DeserializeAsync<AppPreferences>(stream, JsonOptions, cancellationToken);
            return preferences ?? new AppPreferences();
        }
        catch (JsonException)
        {
            // JSON 文件损坏时返回默认配置，避免应用启动失败。
            return new AppPreferences();
        }
        catch (IOException)
        {
            // 文件读取失败时返回默认配置。
            return new AppPreferences();
        }
    }

    public async Task SaveAsync(AppPreferences preferences, CancellationToken cancellationToken = default)
    {
        await using var stream = File.Create(_settingsFilePath);
        await JsonSerializer.SerializeAsync(stream, preferences, JsonOptions, cancellationToken);
    }
}
