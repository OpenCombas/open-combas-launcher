using CombasLauncherApp.Services;
using CombasLauncherApp.Services.Implementations;
using CombasLauncherApp.Services.Interfaces;
using CombasLauncherApp.UI.Pages.DeveloperPages;
using CombasLauncherApp.UI.Pages.HomePage;
using CombasLauncherApp.UI.Pages.SettingsPage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HelixToolkit.Wpf;
using Microsoft.VisualBasic.ApplicationServices;
using Microsoft.VisualBasic.Logging;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using static SoulsFormats.MQB;
using Application = System.Windows.Application;


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
        private Model3D? _sceneModel;

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
            LoadObjModel( Path.Combine(AppService.BaseDir,"UI", "Resources","Logo.obj"));
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

       
        private void LoadObjModel(string path)
        {
            var importer = new ModelImporter();

            var model = importer.Load(path);

            // Set all GeometryModel3D materials to gray
            SetMaterial(model, new DiffuseMaterial(new SolidColorBrush(Colors.Gray)));
            SceneModel = model;
        }

        private void SetMaterial(Model3D model, Material material)
        {
            if (model is GeometryModel3D geometryModel)
            {
                geometryModel.Material = material;
                geometryModel.BackMaterial = material;
            }
            else if (model is Model3DGroup group)
            {
                foreach (var child in group.Children)
                {
                    SetMaterial(child, material);
                }
            }
        }
    }

}
