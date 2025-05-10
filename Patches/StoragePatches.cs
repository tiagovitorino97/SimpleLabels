using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.ObjectScripts;
using Il2CppScheduleOne.Storage;
using Il2CppScheduleOne.UI;
using SimpleLabels.Settings;
using SimpleLabels.UI;
using SimpleLabels.Utils;
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
            string storageEntityName = entity.StorageEntityName;
            Logger.Msg($"CurrentStorageGameObject = {_currentStorageGameObject.name}");

            var inputGameObjectName = _currentInputGameObject.name.Replace("(Clone)", "").Replace("_Built", "")
                .Replace("Mk2", "").Replace("_", "").Trim();
            InputFieldManager.DeactivateInputField(inputGameObjectName);

            LabelInputDataLoader.LoadLabelData(_currentStorageGuid, _currentStorageGameObject, _currentInputGameObject, storageEntityName);

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

        private static string GetStorageGuid(StorageEntity entity)
        {
            if (entity.TryGetComponent<PlaceableStorageEntity>(out var placeable))
                return placeable.GUID.ToString();

            if (entity.TryGetComponent<SurfaceStorageEntity>(out var surface))
                return surface.GUID.ToString();

            Logger.Warning($"No GUID found for storage entity: {entity.StorageEntityName}");
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
                var storageName = __instance.TitleLabel?.text;
                if (string.IsNullOrEmpty(storageName)) return;
                if (_allowedStorageNames.Contains(storageName)) return;

                _currentInputGameObject = __instance.gameObject;
                var inputGameObjectName = _currentInputGameObject.name.Replace("(Clone)", "").Replace("_Built", "")
                    .Replace("Mk2", "").Replace("_", "").Trim();
                InputFieldManager.DeactivateInputField(inputGameObjectName);
                InputFieldManager.DisableToggleOnOffButton(inputGameObjectName);
            }
        }
    }

    public class ColorComparer : IEqualityComparer<Color32>
    {
        private const int ColorTolerance = 15;

        public bool Equals(Color32 x, Color32 y)
        {
            return Math.Abs(x.r - y.r) <= ColorTolerance &&
                   Math.Abs(x.g - y.g) <= ColorTolerance &&
                   Math.Abs(x.b - y.b) <= ColorTolerance;
        }

        public int GetHashCode(Color32 color)
        {
            var r = color.r / ColorTolerance;
            var g = color.g / ColorTolerance;
            var b = color.b / ColorTolerance;

            return (r << 16) | (g << 8) | b;
        }
    }
}