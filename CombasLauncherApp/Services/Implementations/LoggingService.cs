using System.IO;
using CombasLauncherApp.Services.Interfaces;

namespace CombasLauncherApp.Services.Implementations;

public class LoggingService(string logPath) : ILoggingService
{

    private const string LogName = "ApplicationLogs.txt";
    
    private const string LogDateTimeFormat = "yyyyMMddhhmmss";



    public void LogInformation(string message)
    {
        Directory.CreateDirectory(logPath);
        using var textWriter = File.AppendText(Path.Combine(logPath, LogName));
        textWriter.WriteLine($"INFO: {DateTime.Now.ToString(LogDateTimeFormat)} : {message}");
    }

    public void LogError(string message)
    {
        Directory.CreateDirectory(logPath);
        using var textWriter = File.AppendText(Path.Combine(logPath, LogName));
        textWriter.WriteLine($"ERROR: {DateTime.Now.ToString(LogDateTimeFormat)} : {message}");
    }

    public void ShowLogs()
    {
        try
        {
            Directory.CreateDirectory(logPath); // Ensure the folder exists

            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = logPath,
                UseShellExecute = true,
                Verb = "open"
            };
            System.Diagnostics.Process.Start(psi);
        }
        catch (Exception ex)
        {
            LogError($"Failed to open logs folder: {ex.Message}");
            var messageBoxService = ServiceProvider.GetService<IMessageBoxService>();
            messageBoxService?.ShowError("Unable to open the logs folder.");
        }

    }


}