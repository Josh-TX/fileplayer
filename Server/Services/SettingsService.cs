using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Text.Json;

public class Settings
{
    public double PlaybackSpeed { get; set; } 
    public required string SortBy { get; set; }
    public bool SortDesc { get; set; }
    public int? PreferredHeight { get; set; }
    public bool UseMDate { get; set; }
    public bool CompatCodec { get; set; }
}

public class SettingsService
{
    private Settings _settings;
    private string _metadataPath = Path.Combine(BasePathHelper.BasePath, "metadata", "settings.txt");
    public SettingsService()
    {
        _settings = Load();
    }

    public Settings GetSettings()
    {
        return _settings;
    }

    public void UpdateSettings(Settings settings)
    {
        _settings = settings;
        string directoryPath = Path.GetDirectoryName(_metadataPath)!;
        if (!string.IsNullOrEmpty(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }
        string jsonContent = JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_metadataPath, jsonContent);
    }

    private Settings Load()
    {
        if (File.Exists(_metadataPath))
        {
            try
            {
                var json = File.ReadAllText(_metadataPath);
                var settings = System.Text.Json.JsonSerializer.Deserialize<Settings>(json);
                if (settings != null)
                {
                    return settings;
                }
            }
            catch (Exception)
            {
                Console.WriteLine("error reading settings.txt");
            }
        }
        return new Settings
        {
            PlaybackSpeed = 1,
            SortBy = "name",
            SortDesc = true,
            PreferredHeight = 720,
            UseMDate = false,
            CompatCodec = false
        };
    }
}
