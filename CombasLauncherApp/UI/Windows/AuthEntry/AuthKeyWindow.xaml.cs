using System.Windows;

namespace CombasLauncherApp.UI.Windows.AuthEntry
{
    /// <summary>
    /// Interaction logic for AuthKeyWindow.xaml
    /// </summary>
    public partial class AuthKeyWindow
    {
        public string? AuthKey { get; private set; }

        public AuthKeyWindow()
        {
            InitializeComponent();
        }
        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            AuthKey = AuthKeyBox.Password;
            DialogResult = true;
        }
    }
}
