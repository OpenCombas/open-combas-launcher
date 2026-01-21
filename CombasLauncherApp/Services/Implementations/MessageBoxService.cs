using System.Windows;
using CombasLauncherApp.Services.Interfaces;
using MessageBox = System.Windows.MessageBox;

namespace CombasLauncherApp.Services.Implementations;

public class MessageBoxService : IMessageBoxService
{
    public MessageBoxResult Show(string text, string caption = "", MessageBoxButton buttons = MessageBoxButton.OK, MessageBoxImage icon = MessageBoxImage.None)
    {
        return MessageBox.Show(text, caption, buttons, icon);
    }


    public MessageBoxResult ShowError(string text, MessageBoxButton buttons = MessageBoxButton.OK)
    {
        return Show(text, "Error", buttons, MessageBoxImage.Error);
    }

    public MessageBoxResult ShowWarning(string text, MessageBoxButton buttons = MessageBoxButton.OK)
    {
        return Show(text, "Warning", buttons, MessageBoxImage.Warning);
    }

    public MessageBoxResult ShowInformation(string text, MessageBoxButton buttons = MessageBoxButton.OK)
    {
        return Show(text, "Information", buttons, MessageBoxImage.Information);
    }



}