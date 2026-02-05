using System.IO;
using CombasLauncherApp.Services.Interfaces;

namespace CombasLauncherApp.Services.Implementations;

public class LoggingService : ILoggingService
{

    private const string LogDateTimeFormat = "yyyyMMddHHmmss";
    private const string LogFilePrefix = "ApplicationLogs_";
    private const string LogFileExtension = ".txt";
    private readonly string _currentLogFile;
    private readonly string _logPath;
    private const int MaxDaysLogged = 10;

    public LoggingService(string logPath)
    {
        _logPath = logPath;
        Directory.CreateDirectory(_logPath);

        CleanupOldLogs();
        _currentLogFile = GetLogFileName();
    }

    private string GetLogFileName()
    {
        var now = DateTime.Now;
        return Path.Combine(_logPath, $"{LogFilePrefix}{now.ToString(LogDateTimeFormat)}{LogFileExtension}");
    }

    private void CleanupOldLogs()
    {
        var now = DateTime.Now;
        var files = Directory.GetFiles(_logPath, $"{LogFilePrefix}*{LogFileExtension}");
        foreach (var file in files)
        {
            var fileName = Path.GetFileNameWithoutExtension(file);
            var datePart = fileName?.Replace(LogFilePrefix, "");
            if (DateTime.TryParseExact(datePart, LogDateTimeFormat, null, System.Globalization.DateTimeStyles.None, out var fileDate) && (now - fileDate).TotalDays > MaxDaysLogged)
            {
                try
                {
                    File.Delete(file);
                }
                catch
                {
                    // Ignore errors
                }
            }
        }
    }

    public void LogInformation(string message)
    {
        WriteLog("INFO", message);
    }

    public void LogWarning(string message)
    {
        WriteLog("WARNING", message);
    }

    public void LogError(string message)
    {
        WriteLog("ERROR", message);
    }

    private void WriteLog(string level, string message)
    {
        Directory.CreateDirectory(_logPath);
        using var textWriter = File.AppendText(_currentLogFile);
        textWriter.WriteLine($"{level}: {DateTime.Now.ToString(LogDateTimeFormat)} : {message}");
    }

    public void ShowLogs()
    {
        try
        {
            Directory.CreateDirectory(_logPath);

            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = _logPath,
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
