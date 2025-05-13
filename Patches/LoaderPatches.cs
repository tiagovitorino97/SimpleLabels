using System;
using HarmonyLib;
using Il2CppScheduleOne.EntityFramework;
using Il2CppScheduleOne.Persistence.Loaders;
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
        // Patch for ground items (storage racks, etc.)
        [HarmonyPatch(typeof(GridItemLoader), nameof(GridItemLoader.LoadAndCreate))]
        [HarmonyPostfix]
        private static void OnGridItemLoaded(GridItem __result)
        {
            if (__result == null) return;

            TryCreateLabelsForEntity(
                __result.gameObject,
                __result.GUID.ToString(),
                __result.name
            );
        }

        // Patch for wall-mounted items
        [HarmonyPatch(typeof(SurfaceItemLoader), nameof(SurfaceItemLoader.LoadAndCreate))]
        [HarmonyPostfix]
        private static void OnSurfaceItemLoaded(SurfaceItem __result)
        {
            if (__result == null) return;

            TryCreateLabelsForEntity(
                __result.gameObject,
                __result.GUID.ToString(),
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