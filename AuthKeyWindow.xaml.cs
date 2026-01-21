using System.Windows;

namespace CombasLauncherApp.UI
{
    public partial class AuthKeyWindow : Window
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