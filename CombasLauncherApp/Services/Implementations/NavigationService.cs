using CombasLauncherApp.Services.Interfaces;
using CombasLauncherApp.UI.Pages.HomePage;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace CombasLauncherApp.Services.Implementations
{
    public partial class NavigationService : ObservableObject, INavigationService
    {


        [ObservableProperty]
        private ObservableObject _currentMainPage = new HomePageViewModel();

        public void NavigateHome()
        {
            CurrentMainPage = new HomePageViewModel();
            OnMainPageChanged?.Invoke(this, new NavigationEventArgs(CurrentMainPage));
        }

        // Event and event args for IsLoading changes
        public event EventHandler<NavigationEventArgs>? OnMainPageChanged;

        public class NavigationEventArgs(ObservableObject viewModel) : EventArgs
        {
            public ObservableObject ViewModel { get; } = viewModel;
        }

    }
}
