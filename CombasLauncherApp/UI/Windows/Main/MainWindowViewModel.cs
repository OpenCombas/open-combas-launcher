using CombasLauncherApp.Services;
using CombasLauncherApp.Services.Implementations;
using CombasLauncherApp.Services.Interfaces;
using CombasLauncherApp.UI.Pages.DeveloperPages;
using CombasLauncherApp.UI.Pages.HomePage;
using CombasLauncherApp.UI.Pages.SettingsPage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CombasLauncherApp.UI
{
    public partial class MainWindowViewModel: ObservableObject
    {
        private readonly IXeniaService _xeniaService = ServiceProvider.GetService<IXeniaService>();

        public string Version => AppService.Instance.CurrentVersion;

        [ObservableProperty]
        private bool _isDeveloperAllowed = AppService.Instance.IsDeveloperModeEnabled;

        [ObservableProperty]
        private bool _isInstallComplete = AppService.Instance.IsInstallComplete;

        [ObservableProperty]
        private bool _chromeHoundsExtracted;

        [ObservableProperty]
        private bool _isXeniaFound;

        [ObservableProperty]
        private string? _xeniaPath;
        
        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private bool _isSettingsOpen;

        [ObservableProperty]
        private bool _isStatusOpen;

        [ObservableProperty]
        private bool _isDeveloperMenuOpen;

        [ObservableProperty]
        private SettingsPageViewModel _settingsPageViewModel = new();

        [ObservableProperty]
        private DeveloperHomePageViewModel _developerHomePageViewModel = new();

        [ObservableProperty]
        private ObservableObject? _currentPage = new HomePageViewModel();

        private readonly INavigationService _navigationService = ServiceProvider.GetService<INavigationService>();

        public MainWindowViewModel()
        {
            AppService.Instance.OnIsLoadingChanged += AppService_OnIsLoadingChanged;
            AppService.Instance.OnIsInstallCompleteChanged += AppService_OnIsInstallCompleteChanged;
            _navigationService.OnMainPageChanged += NavigationService_OnMainPageChanged;
        }

        private void AppService_OnIsInstallCompleteChanged(object? sender, AppService.IsInstallCompleteChangedEventArgs e)
        {
            IsInstallComplete = e.IsInstallComplete;
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
            IsSettingsOpen = false;
            IsStatusOpen = false;
            IsDeveloperMenuOpen = !IsDeveloperMenuOpen;
        }

        [RelayCommand]
        private void OpenSettingsPage()
        {
            IsDeveloperMenuOpen = false;
            IsStatusOpen = false;
            IsSettingsOpen = !IsSettingsOpen;
        }

        [RelayCommand]
        private void OpenStatusPage()
        {
            IsDeveloperMenuOpen = false;
            IsSettingsOpen = false;

            if (!IsStatusOpen)
            {
                XeniaPath = _xeniaService.XeniaPath;
                IsXeniaFound = _xeniaService.XeniaFound;
                ChromeHoundsExtracted = AppService.Instance.ChromeHoundsExtracted;
            }


            IsStatusOpen = !IsStatusOpen;
        }
    }

}
