using System.Windows;

namespace CombasLauncherApp.Services.Interfaces;

public interface IMessageBoxService
{
    MessageBoxResult Show(string text, string caption = "", MessageBoxButton buttons = MessageBoxButton.OK, MessageBoxImage icon = MessageBoxImage.None);
    MessageBoxResult ShowError(string text, MessageBoxButton buttons = MessageBoxButton.OK);
    MessageBoxResult ShowWarning(string text, MessageBoxButton buttons = MessageBoxButton.OK);
    MessageBoxResult ShowInformation(string text, MessageBoxButton buttons = MessageBoxButton.OK);
}