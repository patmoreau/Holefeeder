namespace Holefeeder.Ui.Common.Authentication;

public interface IAuthNavigationManager
{
    Task LoginAsync();
    Task LogoutAsync();
}
