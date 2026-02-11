using System;
using HarmonyLib;
using UnityEngine;
using Il2CppScheduleOne.Building;
using Il2CppScheduleOne.EntityFramework;
using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.Persistence;
using Il2CppScheduleOne.Persistence.Loaders;
using Il2CppScheduleOne.Tiles;
using SimpleLabels.Data;
using SimpleLabels.Services;
using SimpleLabels.Settings;
using Logger = SimpleLabels.Utils.Logger;


namespace SimpleLabels.Patches
{
    /// <summary>
    /// Harmony patches for GridItem/SurfaceItem Awake and BuildManager create methods. Binds GameObjects to labels on spawn/load.
    /// </summary>
    /// <remarks>
    /// OnGridItemAwake / OnSurfaceItemAwake run when items are instantiated (including from saves); OnGridItemCreated /
    /// OnSurfaceItemCreated run when built. All call TryCreateLabelsForEntity: loads persisted data, creates entity
    /// if missing, binds GameObject, applies label, saves. CleanEntityName is shared with other patches.
    /// </remarks>
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
                // Load labels for this save (save folder first, else global with migration)
                LabelDataManager.LoadLabelsForCurrentSave();
                // Client requests sync (no-op for host). Essential for mid-game join.
                LabelNetworkManager.RequestLabelSyncFromHost();
                // Clients load synced labels from network
                LabelNetworkManager.LoadSyncedLabels();
                // Host syncs current labels to network (for late-joining clients)
                LabelNetworkManager.SyncLabelsToNetwork();
            }));
        }


        private const string ZeroGuid = "00000000-0000-0000-0000-000000000000";

        private static void TryCreateLabelsForEntity(GameObject gameObject, string guid, string originalName) 
        {
            if (gameObject == null || string.IsNullOrEmpty(guid)) return;
            // Skip zero GUIDs - entities not yet synced (e.g. client joining mid-game before full sync)
            if (guid == ZeroGuid) return;

            try
            {
                var cleanName = CleanEntityName(originalName);

                if (!LabelPlacementConfigs.LabelPlacementConfigsDictionary.ContainsKey(cleanName))
                    return;

                var existing = LabelTracker.GetEntityData(guid);
                if (existing == null)
                {
                    LabelService.CreateLabel(
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
                    LabelService.BindGameObject(guid, gameObject);
                }
            }
            catch (Exception e)
            {
                Logger.Error($"Failed to process {originalName}: {e.Message}");
            }
        }

        /// <summary>For LabelPlacementConfigs lookup. Strips (Clone), _Built.</summary>
        public static string CleanEntityName(string originalName)
        {
            return originalName
                .Replace("(Clone)", "")
                .Replace("_Built", "")
                .Trim();
        }

        /// <summary>For InputFieldManager lookup. Strips (Clone), _Built, Mk2, _.</summary>
        public static string CleanGameObjectName(string name)
        {
            return name.Replace("(Clone)", "")
                .Replace("_Built", "")
                .Replace("Mk2", "")
                .Replace("_", "")
                .Trim();
        }
    }
}