using System.Collections;
using System.Collections.Generic;

public class ProgressService
{
    private Dictionary<Guid, (float, DateOnly)> _progress;
    private Timer? _shortDebounceTimer;
    private Timer? _longTimer;
    private string _metadataPath = "metadata/progress.txt";
    private const int _shortSeconds = 4;
    private const int _longSeconds = 10;

    public ProgressService()
    {
        _progress = Load();
    }


    public (float, DateOnly)? GetProgressTuple(Guid fileId)
    {
        if (_progress.TryGetValue(fileId, out var tuple))
        {
            return tuple;
        }
        return null;
    }

    public void SetProgress(Guid fileId, float progress)
    {
        _progress[fileId] = (progress, DateOnly.FromDateTime(DateTime.Now));
        QueueSave();

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
        _longTimer!.Dispose();
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
            foreach (var kvp in _progress)
            {
                writer.WriteLine($"{FileIdHelper.IdToBase64(kvp.Key)} {kvp.Value.Item1} {kvp.Value.Item2.DayNumber}");
            }
        }
    }

    private Dictionary<Guid, (float, DateOnly)> Load()
    {
        var dictionary = new Dictionary<Guid, (float, DateOnly)>();
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
                    var progress = float.Parse(parts[1]);
                    var date = DateOnly.FromDayNumber(int.Parse(parts[2]));
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
