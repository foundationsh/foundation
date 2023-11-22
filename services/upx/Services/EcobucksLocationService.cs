using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Foundation.Services.UPx.Entities;

namespace Foundation.Services.UPx.Services;

public interface IEcobucksLocationService
{
    Task HandleLocationWebSocketAsync(WebSocket webSocket, Guid uuid);
    void RegisterLocation(LocationClaim location);
    List<LocationClaim> GetLocations();
}

public class EcobucksLocationService : IEcobucksLocationService
{
    private ILogger<EcobucksLocationService> Logger { get; }

    private List<LocationClaim> Locations { get; } = new();

    public EcobucksLocationService(ILogger<EcobucksLocationService> logger)
    {
        Logger = logger;
    }

    public async Task HandleLocationWebSocketAsync(WebSocket webSocket, Guid uuid)
    {
        while (webSocket.State == WebSocketState.Open)
        {
            var buffer = new ArraySegment<byte>(new byte[8 * 1024]);
            var result = await webSocket.ReceiveAsync(buffer, CancellationToken.None);

            switch (result.MessageType)
            {
                case WebSocketMessageType.Close:
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                    break;

                case WebSocketMessageType.Text:
                    {
                        Logger.LogInformation("Received message: {}", Encoding.UTF8.GetString(buffer.ToArray()).TrimEnd('\0'));   

                        var decoded = JsonSerializer.Deserialize<EcobucksWebSocketMessage>(buffer);
                        if (decoded is null)
                        {
                            continue;
                        }

                        switch (decoded.MessageType)
                        {
                            case EcobucksWebSocketMessageType.LocationClaim:
                                {
                                    var location = decoded.Payload?.Deserialize<LocationClaim>();
                                    if (location is null)
                                    {
                                        continue;
                                    }

                                    location.ConnectionId = uuid;

                                    RegisterLocation(location);
                                }
                                break;
                        }
                    }
                    break;
            }
        }
    }

    public void RegisterLocation(LocationClaim location)
    {
        Locations.Add(location);
        Task.Delay(2 * 60 * 1000).ContinueWith(_ => Locations.Remove(location));
    }

    public List<LocationClaim> GetLocations()
    {
        return Locations;
    }
}