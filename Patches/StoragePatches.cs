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
    [HarmonyPatch(typeof(StorageMenu))]
    public class StoragePatches
    {
        [HarmonyPatch(typeof(StorageMenu), nameof(StorageMenu.Open),
            new[] { typeof(StorageEntity), typeof(Il2CppSystem.Action) })]
        [HarmonyPostfix]
        public static void OnStorageOpened(StorageMenu __instance, StorageEntity entity)
        {
            var openedStorageEntityName = LoaderPatches.CleanEntityName(__instance.OpenedStorageEntity.name);
            if (!LabelPlacementConfigs.Placements.ContainsKey(openedStorageEntityName))
                return;
            
            var storageGameObject = entity.gameObject;
            var storageGuid = GetStorageGuid(entity);
            var storageEntityName = entity.StorageEntityName;

            InputFieldManager.DeactivateInputField("StorageMenu");

            LabelInputDataLoader.LoadLabelData(storageGuid, storageGameObject, "StorageMenu", storageEntityName);

            ColorPickerManager.UpdateAllColorPickers(ColorPickerType.Label);
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
                // Schedule I 0.4.6 added an on-close callback to every StorageMenu.Open overload.
                // Do not yield null: Harmony treats that as a fatal patching error.
                yield return typeof(StorageMenu).GetMethod(nameof(StorageMenu.Open),
                    new[] { typeof(StorageEntity), typeof(Il2CppSystem.Action) });
                yield return typeof(StorageMenu).GetMethod(nameof(StorageMenu.Open),
                    new[] { typeof(IItemSlotOwner), typeof(string), typeof(string), typeof(Il2CppSystem.Action) });
                yield return typeof(StorageMenu).GetMethod(nameof(StorageMenu.Open),
                    new[] { typeof(string), typeof(string), typeof(IItemSlotOwner), typeof(Il2CppSystem.Action) });
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
                if (!LabelPlacementConfigs.Placements.ContainsKey(openedStorageEntityName))
                    DisableInputField(__instance);
            }
        }

        private static void DisableInputField(StorageMenu instance)
        {
            InputFieldManager.DeactivateInputField("StorageMenu");
            InputFieldManager.DisableToggleOnOffButton("StorageMenu");
        }
    }
}
