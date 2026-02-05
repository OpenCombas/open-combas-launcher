using CombasLauncherApp.Enums;

namespace CombasLauncherApp.Services.Interfaces
{
    public interface ITailScaleService
    {
        bool IsTailScaleRunning();
        Task<TailScaleInstallResult> InstallTailScaleAsync(string authKey);

    }
}
