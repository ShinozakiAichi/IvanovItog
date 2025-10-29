using System.IO;
using System.Text.Json;
using System.Threading;

namespace IvanovItog.App.Services;

public class LocalSettingsService
{
    private const string SettingsFileName = "appsettings.user.json";
    private readonly string _settingsPath;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public LocalSettingsService()
    {
        _settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, SettingsFileName);
    }

    public async Task<AppUserSettings> LoadAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            if (!File.Exists(_settingsPath))
            {
                return AppUserSettings.Default;
            }

            await using var stream = File.OpenRead(_settingsPath);
            var settings = await JsonSerializer.DeserializeAsync<AppUserSettings>(stream);
            return settings ?? AppUserSettings.Default;
        }
        catch
        {
            return AppUserSettings.Default;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task SaveAsync(AppUserSettings settings)
    {
        await _semaphore.WaitAsync();
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_settingsPath)!);
            await using var stream = File.Create(_settingsPath);
            await JsonSerializer.SerializeAsync(stream, settings, new JsonSerializerOptions { WriteIndented = true });
        }
        finally
        {
            _semaphore.Release();
        }
    }
}

public record AppUserSettings(string Theme, string AttachmentsPath, bool NotificationsEnabled)
{
    public static AppUserSettings Default => new("Light", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "IvanovItog", "Attachments"), true);
}
