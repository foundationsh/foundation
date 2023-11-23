using System.Buffers;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Foundation.Core.SDK.Auth.JWT;
using Foundation.Services.UPx.Entities;

namespace Foundation.Services.UPx.Services;

public interface IEcobucksLocationService
{
    void RegisterLocation(LocationClaim location);
    List<LocationClaim> GetLocations();
}

public class EcobucksLocationService : IEcobucksLocationService
{
    private IAuthorizationService AuthorizationService { get; }
    private ILogger<EcobucksLocationService> Logger { get; }

    private List<LocationClaim> Locations { get; } = new();

    public EcobucksLocationService(
        IAuthorizationService authorizationService,
        ILogger<EcobucksLocationService> logger
    )
    {
        AuthorizationService = authorizationService;
        Logger = logger;
    }

    public void RegisterLocation(LocationClaim location)
    {
        Locations.Add(location);
        Task.Delay(2 * 60 * 1000).ContinueWith(_ => Locations.Remove(location));
    }

    public List<LocationClaim> GetLocations()
    {
        return Locations.GroupBy(l => l.StationId).Select(g => g.OrderByDescending(l => l.Timestamp).First()).ToList();
    }
}