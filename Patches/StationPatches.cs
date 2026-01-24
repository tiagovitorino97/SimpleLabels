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
        private static string _currentStationGuid;
        private static GameObject _currentStationGameObject;
        private static string _currentStationGameObjectCleanName;


        private static void HandleStationOpen(GridItem station, string stationType)
        {
            _currentStationGameObject = station.gameObject;
            _currentStationGuid = station.GUID.ToString();
            _currentStationGameObjectCleanName = _currentStationGameObject.name.Replace("(Clone)", "")
                .Replace("_Built", "").Replace("Mk2", "").Replace("_", "").Trim();

            LabelInputDataLoader.LoadLabelData(_currentStationGuid, _currentStationGameObject,
                _currentStationGameObject, stationType);
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