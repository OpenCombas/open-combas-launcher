using System;
using System.Collections.Generic;
using System.Text;

namespace CombasLauncherApp.Enums
{
    public enum ImportChromeHoundsIsoResult
    {
        Success,
        Skipped,
        IsoFolderNotFound,
        IsoExtractionFailed,
        Aborted,
        ExceptionThrown
    }
}
