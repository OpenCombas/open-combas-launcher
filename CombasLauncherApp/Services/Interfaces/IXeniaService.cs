namespace CombasLauncherApp.Services.Interfaces
{
    public interface IXeniaService
    {
        string XeniaPath { get; }
       
        bool XeniaFound { get; }

        void Initialise();

        bool UpdateApiAddress(string newAddress);

        void LunchXeniaProcess(string chromeHoundsDirectory);
    }

}
