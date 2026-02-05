using CombasLauncherApp.Enums;
using CombasLauncherApp.Services.Interfaces;
using System.Diagnostics;
using System.IO;

namespace CombasLauncherApp.Services.Implementations
{
    public class TailScaleService : ITailScaleService
    {
        private readonly ILoggingService _loggingService = ServiceProvider.GetService<ILoggingService>();

        public async Task<TailScaleInstallResult> InstallTailScaleAsync(string authKey)
        {
            try
            {
                // Authenticate with server
                const string loginServer = "https://headscale.opencombas.org:443";
                var tailscaleAuth = Process.Start(new ProcessStartInfo
                {
                    FileName = AppService.TailScaleExe,
                    Arguments = $"up --login-server {loginServer} --authkey {authKey}",
                    UseShellExecute = false,
                    CreateNoWindow = true
                });

                if (tailscaleAuth == null)
                {
                    _loggingService.LogError($"TailScale authenticate failed as the process returned null");
                    return TailScaleInstallResult.AuthenticationFailed;
                }

                await tailscaleAuth.WaitForExitAsync();

                // Launch Xenia to initialize its file paths

                var xeniaProcess = Process.Start(new ProcessStartInfo
                {
                    FileName = AppService.XeniaExe,
                    Arguments = $"\"{AppService.ChromeHoundsXex}\"",
                    WorkingDirectory = AppService.XeniaDir,
                    WindowStyle = ProcessWindowStyle.Minimized,
                    UseShellExecute = true
                });


                if (xeniaProcess == null)
                {
                    _loggingService.LogError($"TailScale installation failed as Xenia did not start correctly");
                    return TailScaleInstallResult.XeniaStartupFailed;
                }


                await Task.Delay(5000); // Wait 5 seconds

                // Kill Xenia process
                Process.Start(new ProcessStartInfo
                {
                    FileName = "taskkill",
                    Arguments = $"/im \"{Path.GetFileName(AppService.XeniaExe)}\" /f",
                    UseShellExecute = false,
                    CreateNoWindow = true
                });

                _loggingService.LogInformation("TailScale installation and authentication successful.");
                return TailScaleInstallResult.Success;
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Failed to Install TailScale: {ex.Message}");
                return TailScaleInstallResult.ExceptionThrown;
            }

        }


        public bool IsTailScaleRunning()
        {

            try
            {
                // Detect if tailscale is running

                var processes = Process.GetProcessesByName("tailscale-ipn");
                switch (processes.Length)
                {
                    case 0:
                        _loggingService.LogInformation("TailScale is not running.");
                        break;
                    case 1:
                        _loggingService.LogInformation("TailScale is running.");
                        break;
                    default:
                        _loggingService.LogWarning("More than one instance of TailScale is running.");
                        foreach (var proc in processes)
                        {
                            _loggingService.LogWarning($"TailScale process: Id={proc.Id}, StartTime={proc.StartTime}");
                        }
                        break;
                }

                return processes.Length > 0;
            }
            catch (Exception ex)
            {
                _loggingService.LogError($"Failed to check if TailScale is running: {ex.Message}");
                return false;


            }
        }

    }

}
