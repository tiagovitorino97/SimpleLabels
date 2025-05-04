using System.Collections.Generic;
using HarmonyLib;
using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.ObjectScripts;
using Il2CppScheduleOne.Storage;
using Il2CppScheduleOne.UI;
using SimpleLabels.UI;
using UnityEngine;
using Logger = SimpleLabels.Utils.Logger;


namespace SimpleLabels.Patches
{
    [HarmonyPatch(typeof(StorageMenu))]
    public class StoragePatches
    {
        private static string _currentStorageGuid;
        private static GameObject _currentStorageGameObject;
        private static GameObject _currentInputGameObject;

        private static readonly List<string> _allowedStorageNames = new List<string>
        {
            "Small Storage Rack", "Safe", "Display Cabinet",
            "Medium Storage Rack", "Wall-Mounted Shelf",
            "Large Storage Rack", "Coffee Table", "Table"
        };

        [HarmonyPatch(typeof(StorageMenu), nameof(StorageMenu.Open), typeof(StorageEntity))]
        [HarmonyPostfix]
        public static void OnStorageOpened(StorageMenu __instance, StorageEntity entity)
        {
            _currentInputGameObject = __instance.gameObject;
            _currentStorageGameObject = entity.gameObject;
            _currentStorageGuid = GetStorageGuid(entity);
            if (!_allowedStorageNames.Contains(entity.StorageEntityName)) return;

            Logger.Msg($"CurrentStationGameObject = {_currentStorageGameObject.name}");
            Logger.Msg($"CurrentInputGameObject = {_currentInputGameObject.name}");
            Logger.Msg($"CurrentStationGuid = {_currentStorageGuid}");

            LabelInputDataLoader.LoadLabelData(_currentStorageGuid, _currentStorageGameObject, _currentInputGameObject);
        }

        private static string GetStorageGuid(StorageEntity entity)
        {
            if (entity.TryGetComponent<PlaceableStorageEntity>(out var placeable))
                return placeable.GUID.ToString();

            if (entity.TryGetComponent<SurfaceStorageEntity>(out var surface))
                return surface.GUID.ToString();

            Logger.Warning($"No GUID found for storage entity: {entity.StorageEntityName}");
            return null;
        }

        [HarmonyPatch(typeof(StorageMenu), nameof(StorageMenu.Open), typeof(IItemSlotOwner), typeof(string),
            typeof(string))]
        [HarmonyPostfix]
        public static void OnStorageOpenedWithOwner(StorageMenu __instance, IItemSlotOwner owner, string title,
            string subtitle)
        {
            Logger.Msg("Unwanted storage opened, not showing input.");
            _currentStorageGameObject = __instance.gameObject;
            InputFieldManager.DeactivateInputField(_currentStorageGameObject.name);
        }
    }
}