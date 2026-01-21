namespace CombasLauncherApp.Services.Interfaces;

public interface ILoggingService
{
    void LogInformation(string message);

    void LogError(string message);

    void ShowLogs();
}