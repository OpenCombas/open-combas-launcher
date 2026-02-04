using CombasLauncherApp.Enums;
using static CombasLauncherApp.Services.Implementations.XeniaService;

namespace CombasLauncherApp.Services.Interfaces
{
    public interface IXeniaService
    {
        string XeniaPath { get; }
       
        bool XeniaFound { get; }

        void Initialise();

        void UpdateXeniaPath();

        bool UpdateApiAddress(string newAddress);

        void LunchXeniaProcess(string chromeHoundsDirectory);

        ImportGameDataResult ImportGameData(string gameDataFolderPath);
    }

}
