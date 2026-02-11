using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.ObjectScripts;
using Il2CppScheduleOne.Storage;
using Il2CppScheduleOne.UI;
using SimpleLabels.Data;
using SimpleLabels.Settings;
using SimpleLabels.UI;
using SimpleLabels.Utils;
using UnityEngine;


namespace SimpleLabels.Patches
{
    /// <summary>
    /// Harmony patches for StorageMenu.Open. Loads label data into the input UI and updates color pickers when storage is opened.
    /// </summary>
    /// <remarks>
    /// OnStorageOpened runs after Open(StorageEntity). Skips if entity type has no placement config. Loads label data
    /// via LabelInputDataLoader, updates color pickers, and optionally sets picker colors from stored items
    /// (AutomaticallySetLabelColorOptions). GetStorageGuid resolves GUID from StorageEntity for persistence.
    /// </remarks>
    [HarmonyPatch(typeof(StorageMenu))]
    public class StoragePatches
    {
        [HarmonyPatch(typeof(StorageMenu), nameof(StorageMenu.Open), typeof(StorageEntity))]
        [HarmonyPostfix]
        public static void OnStorageOpened(StorageMenu __instance, StorageEntity entity)
        {
            var openedStorageEntityName = LoaderPatches.CleanEntityName(__instance.OpenedStorageEntity.name);
            if (!LabelPlacementConfigs.LabelPlacementConfigsDictionary.ContainsKey(openedStorageEntityName))
                return;
            
            var inputGameObject = __instance.gameObject;
            var storageGameObject = entity.gameObject;
            var storageGuid = GetStorageGuid(entity);
            var storageEntityName = entity.StorageEntityName;

            var inputGameObjectName = LoaderPatches.CleanGameObjectName(inputGameObject.name);
            InputFieldManager.DeactivateInputField(inputGameObjectName);

            LabelInputDataLoader.LoadLabelData(storageGuid, storageGameObject, inputGameObject, storageEntityName);

            ColorPickerManager.UpdateAllColorPickers(ColorPickerType.Label);

            // Set temporary colors based on stored items
            UpdateColorPickersFromStorageItems(entity);
        }

        private static void UpdateColorPickersFromStorageItems(StorageEntity entity)
        {
            if (!ModSettings.AutomaticallySetLabelColorOptions.Value) return;

            const int maxColorSlots = 8;

            var itemInstances = entity.GetAllItems();
            if (itemInstances == null || itemInstances.Count == 0) return;

            var uniqueColors = new HashSet<Color32>(new ColorComparer());
            var colorsToApply = new List<Color>();

            foreach (var itemInstance in itemInstances)
            {
                var icon = itemInstance.Icon;
                if (icon == null) continue;

                // Get the average color from the sprite
                var averageColor = SpriteManager.GetAverageColor(icon);
                Color32 color32 = averageColor;

                // Only add if it's a new unique color
                if (uniqueColors.Add(color32))
                {
                    colorsToApply.Add(averageColor);

                    if (colorsToApply.Count >= maxColorSlots) break;
                }
            }

            for (var i = 0; i < colorsToApply.Count; i++)
                ColorPickerManager.SetLabelColorPickerButtonColor(i, colorsToApply[i]);
        }

        public static string GetStorageGuid(StorageEntity entity)
        {
            if (entity.TryGetComponent<PlaceableStorageEntity>(out var placeable))
                return placeable.GUID.ToString();

            if (entity.TryGetComponent<SurfaceStorageEntity>(out var surface))
                return surface.GUID.ToString();
            return null;
        }

        [HarmonyPatch]
        public class StorageMenuAllOpenPatches
        {
            private static IEnumerable<MethodBase> TargetMethods()
            {
                yield return typeof(StorageMenu).GetMethod(nameof(StorageMenu.Open), new[] { typeof(StorageEntity) });
                yield return typeof(StorageMenu).GetMethod(nameof(StorageMenu.Open),
                    new[] { typeof(IItemSlotOwner), typeof(string), typeof(string) });
                yield return typeof(StorageMenu).GetMethod(nameof(StorageMenu.Open),
                    new[]
                    {
                        typeof(string), typeof(string), typeof(IItemSlotOwner)
                    }); // Check for parameter order differences
            }

            private static void Postfix(StorageMenu __instance)
            {
                try
                {
                    if (__instance.OpenedStorageEntity == null)
                    {
                        DisableInputField(__instance);
                        return;
                    }
                }
                catch (Exception)
                {
                    return;
                }
                
                var openedStorageEntityName = LoaderPatches.CleanEntityName(__instance.OpenedStorageEntity.name);
                if (!LabelPlacementConfigs.LabelPlacementConfigsDictionary.ContainsKey(openedStorageEntityName))
                    DisableInputField(__instance);
            }
        }

        private static void DisableInputField(StorageMenu instance)
        {
            var inputGameObjectName = LoaderPatches.CleanGameObjectName(instance.gameObject.name);
            InputFieldManager.DeactivateInputField(inputGameObjectName);
            InputFieldManager.DisableToggleOnOffButton(inputGameObjectName);
        }
    }
}