using CombasLauncherApp.Services.Implementations;

namespace CombasLauncherApp.Services.Interfaces
{
    public interface INavigationService
    {
        void NavigateHome();

        event EventHandler<NavigationService.NavigationEventArgs>? OnMainPageChanged;
    }
}
