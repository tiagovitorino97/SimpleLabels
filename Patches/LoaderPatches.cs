using System;
using HarmonyLib;
using Il2CppScheduleOne.EntityFramework;
using Il2CppScheduleOne.Building;
using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.Tiles;
using SimpleLabels.Data;
using SimpleLabels.Settings;
using SimpleLabels.UI;
using UnityEngine;
using Logger = SimpleLabels.Utils.Logger;

namespace SimpleLabels.Patches
{
    [HarmonyPatch]
    public class LoaderPatches
    {
        // Patch for grid items (storage racks, etc.)
        [HarmonyPatch(typeof(BuildManager), nameof(BuildManager.CreateGridItem))]
        [HarmonyPostfix]
        private static void OnGridItemCreated(GridItem __result, ItemInstance item, Grid grid, Vector2 originCoordinate, int rotation, string guid)
        {
            if (__result == null) return;

            // Use the provided guid parameter, or fall back to the result's GUID
            var entityGuid = !string.IsNullOrEmpty(guid) ? guid : __result.GUID.ToString();

            TryCreateLabelsForEntity(
                __result.gameObject,
                entityGuid,
                __result.name
            );
        }

        // Patch for wall-mounted items
        [HarmonyPatch(typeof(BuildManager), nameof(BuildManager.CreateSurfaceItem))]
        [HarmonyPostfix]
        private static void OnSurfaceItemCreated(SurfaceItem __result, ItemInstance item, Surface parentSurface, Vector3 relativePosition, Quaternion relativeRotation, string guid)
        {
            if (__result == null) return;

            // Use the provided guid parameter, or fall back to the result's GUID
            var entityGuid = !string.IsNullOrEmpty(guid) ? guid : __result.GUID.ToString();

            TryCreateLabelsForEntity(
                __result.gameObject,
                entityGuid,
                __result.name
            );
        }

        private static void TryCreateLabelsForEntity(GameObject gameObject, string guid, string originalName)
        {
            if (gameObject == null || string.IsNullOrEmpty(guid)) return;

            try
            {
                var cleanName = CleanEntityName(originalName);

                if (!LabelPlacementConfigs.LabelPlacementConfigsDictionary.ContainsKey(cleanName)) return;

                if (LabelTracker.GetEntityData(guid) == null)
                    LabelTracker.TrackEntity(
                        guid,
                        gameObject,
                        string.Empty,
                        ModSettings.LabelDefaultColor.Value,
                        ModSettings.LabelDefaultSize.Value,
                        ModSettings.DEFAULT_FONT_SIZE,
                        ModSettings.FontDefaultColor.Value
                    );
                else
                    LabelTracker.UpdateGameObjectReference(guid, gameObject);

                var entityData = LabelTracker.GetEntityData(guid);

                if (!string.IsNullOrEmpty(entityData?.LabelText))
                {
                    LabelApplier.ApplyOrUpdateLabel(guid);
                    Logger.Msg($"Processed entity: {cleanName} (GUID: {guid})");
                }
            }
            catch (Exception e)
            {
                Logger.Error($"Failed to process {originalName}: {e.Message}");
            }
        }

        public static string CleanEntityName(string originalName)
        {
            return originalName
                .Replace("(Clone)", "")
                .Replace("_Built", "")
                .Trim();
        }
    }
}