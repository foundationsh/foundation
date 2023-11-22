using System.Buffers;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Foundation.Core.SDK.Auth.JWT;
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

    public async Task HandleLocationWebSocketAsync(WebSocket webSocket, Guid uuid)
    {
        var isAuthenticated = false;

        using IMemoryOwner<byte> memory = MemoryPool<byte>.Shared.Rent(1024 * 4);
        while (webSocket.State == WebSocketState.Open)
        {
            var request = await webSocket.ReceiveAsync(memory.Memory, CancellationToken.None);

            switch (request.MessageType)
            {
                case WebSocketMessageType.Close:
                    Logger.LogInformation("Received close message for connection {}.", uuid.ToString());
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                    break;

                case WebSocketMessageType.Text:
                    {
                        var message = Encoding.UTF8.GetString(memory.Memory.Span).Trim('\0');
                        Logger.LogInformation("Received message: {}", message);   

                        var decoded = JsonSerializer.Deserialize<EcobucksWebSocketMessage>(message);
                        if (decoded is null)
                            continue;

                        switch (decoded.MessageType)
                        {
                            case EcobucksWebSocketMessageType.Authenticate:
                                {
                                    var token = decoded.Payload?.Deserialize<string>();
                                    if (token is null)
                                        continue;

                                    var authResult = await AuthorizationService.CheckAuthorizationAsync(token);
                                    if (!authResult.IsValid)
                                    {
                                        await webSocket.CloseAsync(
                                            WebSocketCloseStatus.PolicyViolation,
                                            "Invalid token.",
                                            CancellationToken.None
                                        );
                                        return;
                                    }

                                    isAuthenticated = true;
                                }
                                break;

                            case EcobucksWebSocketMessageType.LocationClaim:
                                {
                                    if (!isAuthenticated)
                                        continue;

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

            memory.Memory.Span.Clear();
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