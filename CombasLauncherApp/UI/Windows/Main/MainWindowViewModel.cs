using CombasLauncherApp.Services;
using CombasLauncherApp.Services.Implementations;
using CombasLauncherApp.Services.Interfaces;
using CombasLauncherApp.UI.Pages.DeveloperPages;
using CombasLauncherApp.UI.Pages.HomePage;
using CombasLauncherApp.UI.Pages.SettingsPage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using CombasLauncherApp.Enums;
using CombasLauncherApp.UI.Pages.BuildManagerPage;


namespace CombasLauncherApp.UI
{
    public partial class MainWindowViewModel: ObservableObject
    {
        private readonly IXeniaService _xeniaService = ServiceProvider.GetService<IXeniaService>();
        
        private readonly MediaPlayer _mediaPlayer;

        public DrawingBrush LogoBrush { get; }

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
        private bool _isBuildPageOpen;

        [ObservableProperty]
        private bool _isDeveloperMenuOpen;

        [ObservableProperty]
        private Model3D? _sceneModel;

      


        [ObservableProperty]
        private SettingsPageViewModel _settingsPageViewModel = new();

        [ObservableProperty]
        private DeveloperHomePageViewModel _developerHomePageViewModel = new();

        [ObservableProperty]
        private BuildManagerPageViewModel _buildManagerPageViewModel = new();

        [ObservableProperty]
        private ObservableObject? _currentPage = new HomePageViewModel();

        private readonly INavigationService _navigationService = ServiceProvider.GetService<INavigationService>();

        public MainWindowViewModel()
        {
            AppService.Instance.OnIsLoadingChanged += AppService_OnIsLoadingChanged;
            AppService.Instance.OnIsInstallCompleteChanged += AppService_OnIsInstallCompleteChanged;
            _navigationService.OnMainPageChanged += NavigationService_OnMainPageChanged;

            _mediaPlayer = new MediaPlayer();
            _mediaPlayer.Open(new Uri("UI/Resources/Logo.mp4", UriKind.Relative));
            _mediaPlayer.MediaEnded += (s, e) =>
            {
                _mediaPlayer.Position = TimeSpan.Zero;
                _mediaPlayer.Play();
            };

            var logoDrawing = new VideoDrawing
            {
                Player = _mediaPlayer,
                Rect = new System.Windows.Rect(0, 0, 700, 500)
            };

            LogoBrush = new DrawingBrush(logoDrawing);

            _mediaPlayer.Play();
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
            TogglePage(PageTypes.DeveloperMenu, !IsDeveloperMenuOpen);
        }

        [RelayCommand]
        private void OpenSettingsPage()
        { 
            TogglePage(PageTypes.Settings, !IsSettingsOpen);
        }

        [RelayCommand]
        private void OpenStatusPage()
        {
            if (!IsStatusOpen)
            {

                XeniaPath = _xeniaService.XeniaPath;
                IsXeniaFound = _xeniaService.XeniaFound;
                ChromeHoundsExtracted = AppService.Instance.ChromeHoundsExtracted;
            }
            
            TogglePage(PageTypes.Status, !IsStatusOpen);
        }

        [RelayCommand]
        private void OpenBuildManagerPage()
        {
            TogglePage(PageTypes.BuildManager, !IsBuildPageOpen);
        }


        /// <summary>
        /// Resets all page state flags to their default values, closing any open pages in the user interface.
        /// </summary>
        /// <remarks>Call this method to ensure that all page-related UI flags are set to closed. This is
        /// useful when resetting the application state or navigating away from the current context.</remarks>
        private void ResetPageFlags()
        {
            IsSettingsOpen = false;
            IsStatusOpen = false;
            IsDeveloperMenuOpen = false;
            IsBuildPageOpen = false;
        }

        /// <summary>
        /// Toggles the visibility state of the specified page by setting its open flag to the given value.
        /// </summary>
        /// <remarks>Calling this method will reset all page visibility flags before updating the
        /// specified page. Only the selected page will be set to the provided state; all others will be
        /// closed.</remarks>
        /// <param name="page">The page to toggle. Specifies which page's visibility state will be changed.</param>
        /// <param name="value">A value indicating whether the page should be open (<see langword="true"/>) or closed (<see
        /// langword="false"/>).</param>
        private void TogglePage(PageTypes page, bool value)
        {
            ResetPageFlags();

            switch (page)
            {
                case PageTypes.Settings:
                    IsSettingsOpen = value;
                    break;
                case PageTypes.Status:
                    IsStatusOpen = value;
                    break;
                case PageTypes.DeveloperMenu:
                    IsDeveloperMenuOpen = value;
                    break;
                case PageTypes.BuildManager:
                    IsBuildPageOpen = value;
                    break;

            }
        }

      

    }

}
