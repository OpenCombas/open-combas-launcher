using CombasLauncherApp.Services;
using CombasLauncherApp.Services.Implementations;
using CombasLauncherApp.Services.Interfaces;
using CombasLauncherApp.UI.Pages.DeveloperPages;
using CombasLauncherApp.UI.Pages.HomePage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CombasLauncherApp.UI
{
    public partial class MainWindowViewModel: ObservableObject
    {
        public string Version => AppService.Instance.CurrentVersion;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private ObservableObject? _currentPage = new HomePageViewModel();

        private readonly INavigationService _navigationService = ServiceProvider.GetService<INavigationService>();

        public MainWindowViewModel()
        {
            AppService.Instance.OnIsLoadingChanged += AppService_OnIsLoadingChanged;
            _navigationService.OnMainPageChanged += NavigationService_OnMainPageChanged;
        }

        private void NavigationService_OnMainPageChanged(object? sender, NavigationService.NavigationEventArgs e)
        {
            CurrentPage = e.ViewModel;
        }

        private void AppService_OnIsLoadingChanged(object? sender, AppService.IsLoadingChangedEventArgs e)
        {
            IsLoading = e.IsLoading;
        }

        [RelayCommand]
        private void OpenDeveloperMenu()
        {
            CurrentPage = new DeveloperHomePageViewModel();
        }
    }

}
