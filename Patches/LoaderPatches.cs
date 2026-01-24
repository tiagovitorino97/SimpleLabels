using HarmonyLib;
using Il2CppScheduleOne.Building;
using Il2CppScheduleOne.EntityFramework;
using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.Persistence;
using Il2CppScheduleOne.Persistence.Loaders;
using Il2CppScheduleOne.Tiles;
using SimpleLabels.Data;
using SimpleLabels.Settings;
using SimpleLabels.UI;
using System;
using UnityEngine;
using UnityEngine.Events;
using Logger = SimpleLabels.Utils.Logger;
using LabetNetworkManager = SimpleLabels.Data.LabelNetworkManager;


namespace SimpleLabels.Patches
{
    [HarmonyPatch]
    public class LoaderPatches
    {
        // Ensure labels can be created/updated when items are instantiated from saves on all peers,
        // not only when they are built via BuildManager.
        //
        // This runs for both host and clients whenever a GridItem is instantiated (including on load).
        [HarmonyPatch(typeof(GridItem), "Awake")]
        [HarmonyPostfix]
        private static void OnGridItemAwake(GridItem __instance)
        {
            if (__instance == null) return;

            var entityGuid = __instance.GUID.ToString();
            TryCreateLabelsForEntity(
                __instance.gameObject,
                entityGuid,
                __instance.name
            );
        }

        // Same as above, but for SurfaceItem instances.
        [HarmonyPatch(typeof(SurfaceItem), "Awake")]
        [HarmonyPostfix]
        private static void OnSurfaceItemAwake(SurfaceItem __instance)
        {
            if (__instance == null) return;

            var entityGuid = __instance.GUID.ToString();
            TryCreateLabelsForEntity(
                __instance.gameObject,
                entityGuid,
                __instance.name
            );
        }

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

        [HarmonyPatch(typeof(LoadManager), "Start")]
        [HarmonyPostfix]
        private static void OnLoadManagerStart(LoadManager __instance)
        {
            __instance.onLoadComplete.AddListener(new Action(() =>
            {
                LabetNetworkManager.LoadSyncedLabels();
            }));
        }


        private static void TryCreateLabelsForEntity(GameObject gameObject, string guid, string originalName) 
        {
            if (gameObject == null || string.IsNullOrEmpty(guid)) return;

            try
            {
                var cleanName = CleanEntityName(originalName);

                if (!LabelPlacementConfigs.LabelPlacementConfigsDictionary.ContainsKey(cleanName))
                {
                    Logger.Msg($"[Loader] Entity type '{cleanName}' not supported for labels");
                    return;
                }

                var existing = LabelTracker.GetEntityData(guid);
                if (existing == null)
                {
                    Logger.Msg($"[Loader] Tracking new entity: GUID={guid}, Type={cleanName}");
                    LabelTracker.TrackEntity(
                        guid,
                        gameObject,
                        string.Empty,
                        ModSettings.LabelDefaultColor.Value,
                        ModSettings.LabelDefaultSize.Value,
                        ModSettings.DEFAULT_FONT_SIZE,
                        ModSettings.FontDefaultColor.Value
                    );
                }
                else
                {
                    Logger.Msg($"[Loader] Binding GameObject to existing entity: GUID={guid}");
                    LabelTracker.UpdateGameObjectReference(guid, gameObject);
                }

                var entityData = LabelTracker.GetEntityData(guid);

                if (!string.IsNullOrEmpty(entityData?.LabelText))
                {
                    Logger.Msg($"[Loader] Applying existing label: GUID={guid}, Text='{entityData.LabelText}'");
                    LabelApplier.ApplyOrUpdateLabel(guid);
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