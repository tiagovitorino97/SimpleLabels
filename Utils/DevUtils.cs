using Il2CppScheduleOne.UI;
using Il2CppScheduleOne.UI.Stations;

namespace SimpleLabels.Utils;

public class DevUtils
{
    public static bool IsStorageOrStationOpen()
    {
        return IsActive(StorageMenu.Instance) ||
               IsActive(BrickPressCanvas.Instance) ||
               IsActive(CauldronInterface.Instance) ||
               IsActive(ChemistryStationInterface.Instance) ||
               IsActive(DryingRackInterface.Instance) ||
               IsActive(LabOvenCanvas.Instance) ||
               IsActive(MixingStationInterface.Instance) ||
               IsActive(PackagingStationCanvas.Instance) ||
               IsActive(MushroomSpawnStationInterface.Instance);
    }

    private static bool IsActive(UnityEngine.MonoBehaviour panel) =>
        panel != null && panel.gameObject.activeInHierarchy;
}
