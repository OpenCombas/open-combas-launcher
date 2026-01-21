using System;
using System.Collections.Generic;
using System.Text;
using CombasLauncherApp.Services.Implementations;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CombasLauncherApp.Services.Interfaces
{
    public interface INavigationService
    {
        void NavigateHome();

        event EventHandler<NavigationService.NavigationEventArgs>? OnMainPageChanged;
    }
}
