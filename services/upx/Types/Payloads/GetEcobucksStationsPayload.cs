using Foundation.Core.SDK.API.REST;

namespace Foundation.Services.UPx.Types.Payloads;

public class GetEcobucksStationsPayload : BasePayload
{
    public List<LocationClaim>? Stations { get; set; } = default!;
}
