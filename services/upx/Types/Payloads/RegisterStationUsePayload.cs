namespace Foundation.Services.UPx.Types.Payloads;

public class RegisterStationUsePayload
{
    public required bool Successful { get; set; }

    public string? Error { get; set; }
}
