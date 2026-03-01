using CombasLauncherApp.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using UserControl = System.Windows.Controls.UserControl;

namespace CombasLauncherApp.UI.Pages.BuildManagerPage
{
    /// <summary>
    /// Interaction logic for BuildManagerPage.xaml
    /// </summary>
    public partial class BuildManagerPage : UserControl
    {
        public BuildManagerPage()
        {
            InitializeComponent();
        }

        private void BuildCollections_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (DataContext is BuildManagerPageViewModel vm)
            {
                if (e.NewValue is BuildEntry buildEntry)
                {
                    vm.SelectedCollectionBuild = buildEntry;
                }
            }
        }

  
    }
}
