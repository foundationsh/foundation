using System.Text.Json;

namespace Foundation.Services.UPx.Entities;

public enum EcobucksWebSocketMessageType
{
    Authenticate,
    LocationClaim,
}

public class EcobucksWebSocketMessage
{
    public EcobucksWebSocketMessageType MessageType { get; set; } = default!;

    public JsonElement? Payload { get; set; } = default!;
}