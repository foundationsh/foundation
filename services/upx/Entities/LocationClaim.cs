namespace Foundation.Services.UPx;

public class LocationClaim
{
    public float Latitude { get; set; } = default!;
    
    public float Longitude { get; set; } = default!;

    public long Timestamp { get; set; } = default!;

    public TimeSpan Age => DateTime.Now - DateTimeOffset.FromUnixTimeSeconds(Timestamp).DateTime;

    public Guid? ConnectionId { get; set; }
}