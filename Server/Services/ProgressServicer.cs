public class ProgressService
{
    private Dictionary<Guid, (float, DateOnly)> _progress;


    public ProgressService()
    {
        _progress = new Dictionary<Guid, (float, DateOnly)>();
    }


    public (float, DateOnly?) GetProgressTuple(Guid fileId)
    {
        if (_progress.TryGetValue(fileId, out var tuple))
        {
            return tuple;
        }
        return (0,  null);
    }
}
