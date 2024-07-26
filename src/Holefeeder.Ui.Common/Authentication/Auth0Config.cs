namespace Holefeeder.Ui.Common.Authentication;

public class Auth0Config
{
    public required string Domain { get; init; }
    public required string ClientId { get; init; }
    public required string Audience { get; init; }

    public required string Authority { get; init; }
}
