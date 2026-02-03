using Il2CppScheduleOne.UI;
using Il2CppScheduleOne.UI.Stations;

namespace SimpleLabels.Utils;

/// <summary>
/// Dev/context helpers used by the mod (e.g. whether a storage or station UI is open).
/// </summary>
/// <remarks>
/// IsStorageOrStationOpen checks StorageMenu, BrickPress, Cauldron, Chemistry, DryingRack, LabOven,
/// MixingStation, PackagingStation, and MushroomSpawnStation. Used by InputFieldManager to avoid
/// processing when no relevant UI is open.
/// </remarks>
public class DevUtils
{
    /// <summary>
    /// Returns true if any storage menu or station canvas is currently open.
    /// </summary>
    public static bool IsStorageOrStationOpen()
    {
        return StorageMenu.Instance.IsOpen ||
               BrickPressCanvas.Instance.isOpen ||
               CauldronCanvas.Instance.isOpen ||
               ChemistryStationCanvas.Instance.isOpen ||
               DryingRackCanvas.Instance.isOpen ||
               LabOvenCanvas.Instance.isOpen ||
               MixingStationCanvas.Instance.isOpen ||
               PackagingStationCanvas.Instance.isOpen ||
               MushroomSpawnStationInterface.Instance.IsOpen;
    }
}