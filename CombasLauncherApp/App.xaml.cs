using System.Globalization;
using System.Windows;
using CombasLauncherApp.Services;
using CombasLauncherApp.Services.Implementations;
using CombasLauncherApp.Services.Interfaces;
using CombasLauncherApp.UI;

namespace CombasLauncherApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // Force culture to US English
            var culture = new CultureInfo("en-US");
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;

            base.OnStartup(e);

            //Configure services
            ServiceProvider.ConfigureServices();

            //Load Persistent App Data
            AppService.Instance.LoadPersistentAppData();

            //Load Application Resources
            var xeniaService = ServiceProvider.GetService<IXeniaService>();
            xeniaService.Initialise();

            var mainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel()
            };

            mainWindow.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // Save Persistent App Data
            AppService.Instance.SavePersistentAppData();

            base.OnExit(e);
        }
    }

}
