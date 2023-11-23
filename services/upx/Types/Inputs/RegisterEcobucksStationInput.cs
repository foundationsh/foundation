using Foundation.Core.SDK.API.REST;

namespace Foundation.Services.UPx.Types.Inputs;

public class RegisterEcobucksStationInput : BasePayload
{
    public LocationClaim Location { get; set; } = default!;
}