using CFCHub.Domain.Shared;

namespace CFCHub.Domain.Scheduling;

public class Track : AggregateRoot<TrackId>
{
    public string Name { get; private set; }
    public TrackType Type { get; private set; }

    private Track() 
    { 
        Name = null!;
    }

    public Track(TrackId id, string name, TrackType type) : base(id)
    {
        Name = name;
        Type = type;
    }
}
