using HarmonyLib;
using Il2CppScheduleOne.EntityFramework;
using Il2CppScheduleOne.ObjectScripts;
using Il2CppScheduleOne.StationFramework;
using Il2CppScheduleOne.UI.Stations;
using SimpleLabels.UI;
using UnityEngine;
using Logger = SimpleLabels.Utils.Logger;

namespace SimpleLabels.Patches
{
    /// <summary>
    /// Harmony patches for station canvases (Packaging, DryingRack, BrickPress, Cauldron, LabOven, Mixing, etc.). Load label UI on open.
    /// </summary>
    /// <remarks>
    /// Each nested class patches a canvas Open/SetIsOpen. When open, HandleStationOpen calls LabelInputDataLoader.LoadLabelData
    /// with the station GUID, GameObject, and display name. Stations use the station GameObject as the input container.
    /// </remarks>
    public class StationPatches
    {
        private static void HandleStationOpen(GridItem station, string stationType)
        {
            var stationGuid = station.GUID.ToString();
            var stationGameObject = station.gameObject;

            LabelInputDataLoader.LoadLabelData(stationGuid, stationGameObject, stationGameObject, stationType);
        }

        [HarmonyPatch(typeof(PackagingStationCanvas), nameof(PackagingStationCanvas.SetIsOpen))]
        private static class PackagingStationPatch
        {
            [HarmonyPostfix]
            public static void Postfix(PackagingStation station, bool open)
            {
                if (open) HandleStationOpen(station, "Packaging Station");
            }
        }

        [HarmonyPatch(typeof(DryingRackCanvas), nameof(DryingRackCanvas.SetIsOpen))]
        private static class DryingRackPatch
        {
            [HarmonyPostfix]
            public static void Postfix(DryingRack rack, bool open)
            {
                if (open) HandleStationOpen(rack, "Drying Rack");
            }
        }

        [HarmonyPatch(typeof(BrickPressCanvas), nameof(BrickPressCanvas.SetIsOpen))]
        private static class BrickPressPatch
        {
            [HarmonyPostfix]
            public static void Postfix(BrickPress press, bool open)
            {
                if (open) HandleStationOpen(press, "Brick Press");
            }
        }

        [HarmonyPatch(typeof(CauldronCanvas), nameof(CauldronCanvas.SetIsOpen))]
        private static class CauldronPatch
        {
            [HarmonyPostfix]
            public static void Postfix(Cauldron cauldron, bool open)
            {
                if (open) HandleStationOpen(cauldron, "Cauldron");
            }
        }

        [HarmonyPatch(typeof(LabOvenCanvas), nameof(LabOvenCanvas.SetIsOpen))]
        private static class LabOvenPatch
        {
            [HarmonyPostfix]
            public static void Postfix(LabOven oven, bool open, bool removeUI)
            {
                if (open) HandleStationOpen(oven, "Lab Oven");
            }
        }

        [HarmonyPatch(typeof(MixingStationCanvas), nameof(MixingStationCanvas.Open))]
        private static class MixingStationPatch
        {
            [HarmonyPostfix]
            public static void Postfix(MixingStationCanvas __instance, MixingStation station)
            {
                HandleStationOpen(station, "Mixing Station");
            }
        }

        [HarmonyPatch(typeof(ChemistryStationCanvas), nameof(ChemistryStationCanvas.Open))]
        private static class ChemistryStationPatch
        {
            [HarmonyPostfix]
            public static void Postfix(ChemistryStationCanvas __instance, ChemistryStation station)
            {
                HandleStationOpen(station, "Chemistry Station");
            }
        }

        [HarmonyPatch(typeof(MushroomSpawnStationInterface), nameof(MushroomSpawnStationInterface.Open))]
        private static class MushroomSpawnStationPatch
        {
            [HarmonyPostfix]
            public static void Postfix(MushroomSpawnStationInterface __instance, MushroomSpawnStation station)
            {
                HandleStationOpen(station, "Mushroom Spawn Station");
            }
        }
    }
}