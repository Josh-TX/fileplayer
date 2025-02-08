using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.RegularExpressions;

/// <summary>
/// Stores the progress (as a float), and the duration (as an int) of each file being tracked
/// </summary>
public class FileInfoService
{
    private ConcurrentDictionary<Guid, (float?, int?)> _fileInfos;
    private Timer? _shortDebounceTimer;
    private Timer? _longTimer;
    private string _metadataPath = Path.Combine(BasePathHelper.BasePath, "metadata", "fileinfos.txt");
    private string _ffmpegPath = DownloadService.GetFFmpegPath();

    //these determine the debounce time for the shortDebounceTimer and the longTimer
    private const int _shortSeconds = 4;
    private const int _longSeconds = 10;

    public FileInfoService()
    {
        _fileInfos = Load();
    }

    public void FileRenamed(string oldName, string newName, long size)
    {
        var oldId = FileIdHelper.GetId(oldName, size);
        if (_fileInfos.TryRemove(oldId, out var tuple))
        {
            var newId = FileIdHelper.GetId(newName, size);
            _fileInfos[newId] = tuple;
            QueueSave();
        }
    }

    public void FileRemoved(Guid fileId)
    {
        _fileInfos.TryRemove(fileId, out var _);
        QueueSave();
    }

    public (float?, int?)? GetInfoTuple(Guid fileId)
    {
        if (_fileInfos.TryGetValue(fileId, out var tuple))
        {
            return tuple;
        }
        return null;
    }

    public void SetProgress(Guid fileId, float progress)
    {
        if (_fileInfos.TryGetValue(fileId, out var tuple))
        {
            tuple.Item1 = progress;
            _fileInfos[fileId] = tuple;
        }
        else
        {
            _fileInfos[fileId] = (progress, null);
        }
        QueueSave();
    }

    public int? TryGetCachedDuration(Guid fileId)
    {
        if (_fileInfos.TryGetValue(fileId, out var tuple))
        {
            return tuple.Item2;
        }
        return null;
    }

    /// <summary>
    /// This is kinda slow to calculate (about 100 ms)
    /// </summary>
    public async Task<int?> CalculateDuration(Guid fileId, string fullPath)
    {
        if (_fileInfos.TryGetValue(fileId, out var tuple) && tuple.Item2.HasValue)
        {
            return tuple.Item2;
        }
        if (!FileTypeHelper.IsMediaFile(fullPath))
        {
            return null;
        }
        try
        {
            var duration = await GetMediaDuration(fullPath);
            var seconds = (int)Math.Round(duration ?? 0);
            if (_fileInfos.TryGetValue(fileId, out tuple))
            {
                tuple.Item2 = seconds;
                _fileInfos[fileId] = tuple;
            }
            else
            {
                _fileInfos[fileId] = (null, seconds);
            }
            QueueSave();
            return seconds;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private async Task<double?> GetMediaDuration(string videoPath)
    {
        var process = Process.Start(new ProcessStartInfo
        {
            FileName = _ffmpegPath,
            Arguments = $"-i \"{videoPath}\"",
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        });

        string output = await process!.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        var match = Regex.Match(output, @"Duration: (\d{2}):(\d{2}):(\d{2}\.\d{2})");
        if (!match.Success) return null;

        return TimeSpan.Parse($"{match.Groups[1].Value}:{match.Groups[2].Value}:{match.Groups[3].Value}").TotalSeconds;
    }


    private void QueueSave()
    {
        if (_shortDebounceTimer == null)
        {
            _shortDebounceTimer = new Timer(TimerCallback, null, _shortSeconds * 1000, Timeout.Infinite);
        }
        else
        {
            _shortDebounceTimer.Change(_shortSeconds * 1000, Timeout.Infinite);
        }
        if (_longTimer == null) 
        {
            _longTimer = new Timer(TimerCallback, null, _longSeconds * 1000, Timeout.Infinite);
        }
    }

    private void TimerCallback(object? state)
    {
        _shortDebounceTimer!.Change(Timeout.Infinite, Timeout.Infinite);
        _longTimer?.Dispose(); //I was getting errors without null checking. Not sure how though
        _longTimer = null;
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
            foreach (var kvp in _fileInfos)
            {
                var item1 = kvp.Value.Item1.HasValue ? kvp.Value.Item1.ToString() : "_";
                var item2 = kvp.Value.Item2.HasValue ? kvp.Value.Item2.ToString() : "_";
                writer.WriteLine($"{FileIdHelper.IdToBase64(kvp.Key)} {item1} {item2}");
            }
        }
    }

    private ConcurrentDictionary<Guid, (float?, int?)> Load()
    {
        var dictionary = new ConcurrentDictionary<Guid, (float?, int?)>();
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
                    var progress = parts[1] != "_" ? float.Parse(parts[1]) : (float?)null;
                    var date = parts[2] != "_" ? int.Parse(parts[2]) : (int?)null;
                    dictionary[fileId] = (progress, date);
                }
                catch (Exception)
                {
                    Console.WriteLine("error reading progress.txt");
                }
            }
        }
        return dictionary;
    }
}
