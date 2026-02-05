using CombasLauncherApp.Models;
using System.Windows;
using UserControl = System.Windows.Controls.UserControl;

namespace CombasLauncherApp.UI.Pages.DeveloperPages
{
    /// <summary>
    /// Interaction logic for DeveloperHomePage.xaml
    /// </summary>
    public partial class DeveloperHomePage : UserControl
    {
        public DeveloperHomePage()
        {
            InitializeComponent();
        }


        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if ((sender as System.Windows.Controls.CheckBox)?.DataContext is MapEntry map)
            {
                var vm = (DeveloperHomePageViewModel)DataContext;
                vm.TryToggleMapEnabled(map);
            }
        }
        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if ((sender as System.Windows.Controls.CheckBox)?.DataContext is MapEntry map)
            {
                var vm = (DeveloperHomePageViewModel)DataContext;
                vm.TryToggleMapEnabled(map);
            }
        }
    }
}
