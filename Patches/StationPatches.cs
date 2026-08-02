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
    public class StationPatches
    {
        private static void HandleStationOpen(GridItem station, string stationType, string inputUIKey)
        {
            var stationGuid = station.GUID.ToString();
            var stationGameObject = station.gameObject;

            LabelInputDataLoader.LoadLabelData(stationGuid, stationGameObject, inputUIKey, stationType);
        }

        [HarmonyPatch(typeof(PackagingStationCanvas), nameof(PackagingStationCanvas.Open))]
        private static class PackagingStationPatch
        {
            [HarmonyPostfix]
            public static void Postfix(PackagingStation station)
            {
                HandleStationOpen(station, "Packaging Station", "PackagingStation");
            }
        }

        [HarmonyPatch(typeof(DryingRackInterface), nameof(DryingRackInterface.Open))]
        private static class DryingRackPatch
        {
            [HarmonyPostfix]
            public static void Postfix(DryingRack rack)
            {
                HandleStationOpen(rack, "Drying Rack", "DryingRack");
            }
        }

        [HarmonyPatch(typeof(BrickPressCanvas), nameof(BrickPressCanvas.Open))]
        private static class BrickPressPatch
        {
            [HarmonyPostfix]
            public static void Postfix(BrickPress press)
            {
                HandleStationOpen(press, "Brick Press", "BrickPress");
            }
        }

        [HarmonyPatch(typeof(CauldronInterface), nameof(CauldronInterface.Open))]
        private static class CauldronPatch
        {
            [HarmonyPostfix]
            public static void Postfix(Cauldron cauldron)
            {
                HandleStationOpen(cauldron, "Cauldron", "Cauldron");
            }
        }

        [HarmonyPatch(typeof(LabOvenCanvas), nameof(LabOvenCanvas.Open))]
        private static class LabOvenPatch
        {
            [HarmonyPostfix]
            public static void Postfix(LabOven oven)
            {
                HandleStationOpen(oven, "Lab Oven", "LabOven");
            }
        }

        [HarmonyPatch(typeof(MixingStationInterface), nameof(MixingStationInterface.Open))]
        private static class MixingStationPatch
        {
            [HarmonyPostfix]
            public static void Postfix(MixingStationInterface __instance, MixingStation station)
            {
                HandleStationOpen(station, "Mixing Station", "MixingStation");
            }
        }

        [HarmonyPatch(typeof(ChemistryStationInterface), nameof(ChemistryStationInterface.Open))]
        private static class ChemistryStationPatch
        {
            [HarmonyPostfix]
            public static void Postfix(ChemistryStationInterface __instance, ChemistryStation station)
            {
                HandleStationOpen(station, "Chemistry Station", "ChemistryStation");
            }
        }

        [HarmonyPatch(typeof(MushroomSpawnStationInterface), nameof(MushroomSpawnStationInterface.Open))]
        private static class MushroomSpawnStationPatch
        {
            [HarmonyPostfix]
            public static void Postfix(MushroomSpawnStationInterface __instance, MushroomSpawnStation station)
            {
                HandleStationOpen(station, "Mushroom Spawn Station", "MushroomSpawnStation");
            }
        }
    }
}
