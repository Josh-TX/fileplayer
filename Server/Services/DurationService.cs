using System.Collections.Concurrent;

public class DurationService
{
    private ConcurrentDictionary<Guid, int> _knownDurations;
    private string _metadataPath = "metadata/duration.txt";
    private Timer? _debounceTimer;
    public DurationService()
    {
        _knownDurations = Load();
    }


    public int? TryGetCachedDuration(Guid fileId)
    {
        if (_knownDurations.TryGetValue(fileId, out var duration))
        {
            return duration;
        }
        return null;
    }

    /// <summary>
    /// This is kinda slow to calculate (about 100 ms)
    /// </summary>
    public async Task<int?> CalculateDuration(Guid fileId, string fullPath)
    {
        if (_knownDurations.TryGetValue(fileId, out var duration))
        {
            return duration;
        }
        try
        {
            var mediaInfo = await Xabe.FFmpeg.FFmpeg.GetMediaInfo(fullPath);
            //this function is called multiple times concurrently, hence we need to use ConcurrentDictionary
            var seconds = Convert.ToInt32(Math.Ceiling(mediaInfo.Duration.TotalSeconds));
            _knownDurations[fileId] = seconds;
            QueueSave();
            return seconds;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private void QueueSave()
    {
        if (_debounceTimer == null)
        {
            _debounceTimer = new Timer(TimerCallback, null, 10 * 1000, Timeout.Infinite);
        }
    }

    private void TimerCallback(object? state)
    {
        _debounceTimer!.Change(Timeout.Infinite, Timeout.Infinite);
        Save();
    }

    private void Save()
    {
        string directoryPath = Path.GetDirectoryName(_metadataPath)!;
        if (!string.IsNullOrEmpty(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }
        using (StreamWriter writer = new StreamWriter(_metadataPath))
        {
            foreach (var kvp in _knownDurations)
            {
                writer.WriteLine($"{FileIdHelper.IdToBase64(kvp.Key)} {kvp.Value}");
            }
        }
    }

    private ConcurrentDictionary<Guid, int> Load()
    {
        var dictionary = new ConcurrentDictionary<Guid, int>();
        if (!File.Exists(_metadataPath))
        {
            return dictionary;
        }
        using (StreamReader reader = new StreamReader(_metadataPath))
        {
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                var parts = line.Split(' ');
                try
                {
                    var fileId = FileIdHelper.Base64ToId(parts[0]);
                    var duration = int.Parse(parts[1]);
                    dictionary[fileId] = duration;
                }
                catch (Exception)
                {
                    Console.WriteLine("error reading duration.txt");
                }
            }
        }
        return dictionary;
    }
}
