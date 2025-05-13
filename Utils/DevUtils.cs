using Il2CppScheduleOne.UI;
using Il2CppScheduleOne.UI.Stations;

namespace SimpleLabels.Utils;

public class DevUtils
{
    public static bool IsStorageOrStationOpen()
    {
        return StorageMenu.Instance.IsOpen ||
               BrickPressCanvas.Instance.isOpen ||
               CauldronCanvas.Instance.isOpen ||
               ChemistryStationCanvas.Instance.isOpen ||
               DryingRackCanvas.Instance.isOpen ||
               LabOvenCanvas.Instance.isOpen ||
               MixingStationCanvas.Instance.isOpen ||
               PackagingStationCanvas.Instance.isOpen;
    }
}