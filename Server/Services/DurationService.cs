using System.Collections.Concurrent;

public class DurationService
{
    private ConcurrentDictionary<Guid, int> _knownDurations;


    public DurationService()
    {
        _knownDurations = new ConcurrentDictionary<Guid, int>();
    }


    public int? TryGetCachedDuration(Guid fileId)
    {
        if (_knownDurations.TryGetValue(fileId, out var duration))
        {
            return duration;
        }
        return null;
    }

    public async Task<int> CalculateDuration(Guid fileId, string fullPath)
    {
        if (_knownDurations.TryGetValue(fileId, out var duration))
        {
            return duration;
        }
        var mediaInfo = await Xabe.FFmpeg.FFmpeg.GetMediaInfo(fullPath);
        _knownDurations[fileId] = mediaInfo.Duration.Seconds;
        return mediaInfo.Duration.Seconds;
    }
}
