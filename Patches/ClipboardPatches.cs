using HarmonyLib;
using Il2CppScheduleOne.EntityFramework;
using Il2CppScheduleOne.Management;
using Il2CppScheduleOne.UI.Management;
using Il2CppSystem.Collections.Generic;
using Il2CppTMPro;
using SimpleLabels.Data;
using SimpleLabels.Settings;
using UnityEngine;
using Logger = SimpleLabels.Utils.Logger;

namespace SimpleLabels.Patches
{
    /// <summary>
    /// Harmony patches for clipboard UI (RouteListFieldUI, ObjectListFieldUI, ObjectFieldUI). Show custom labels in route/object lists.
    /// </summary>
    /// <remarks>
    /// OnRouteListRefresh updates Source/Label and Destination/Label with LabelTracker text for route GUIDs.
    /// OnObjectListRefresh and OnObjectFieldRefresh do the same for station/object lists. Gated by
    /// ModSettings.ShowClipboardRoutes, ShowClipboardStations, ShowClipboardStationsOutput.
    /// </remarks>
    [HarmonyPatch]
    public class ClipboardPatches
    {
        [HarmonyPatch(typeof(RouteListFieldUI), nameof(RouteListFieldUI.Refresh))]
        [HarmonyPostfix]
        public static void OnRouteListRefresh(RouteListFieldUI __instance, Il2CppSystem.Collections.Generic.List<AdvancedTransitRoute> newVal)
        {
            if (!ModSettings.ShowClipboardRoutes.Value) return;

            var routesUIContents = __instance.gameObject.transform.Find("Contents")?.gameObject
                                   ?? __instance.gameObject.transform.Find("ScrollArea/Contents")?.gameObject;
            if (routesUIContents == null)
            {
                Logger.Error("[Clipboard] Couldn't find RouteList contents container");
                return;
            }

            foreach (var entry in routesUIContents.transform)
            {
                var entryTransform = entry.TryCast<Transform>();
                if (entryTransform == null || !entryTransform.name.Contains("Entry") ||
                    !entryTransform.gameObject.active)
                    continue;

                var routeEntryUI = entryTransform.GetComponent<RouteEntryUI>();
                if (routeEntryUI?.AssignedRoute?.GetData() == null)
                {
                    Logger.Warning($"[Clipboard] Missing route data on {entryTransform.name}");
                    continue;
                }

                var routeData = routeEntryUI.AssignedRoute.GetData();
                UpdateRouteLabel(entryTransform, "Source/Label", routeData.SourceGUID);
                UpdateRouteLabel(entryTransform, "Destination/Label", routeData.DestinationGUID);
            }
        }

        private static void UpdateRouteLabel(Transform entryTransform, string labelPath, string guid)
        {
            var labelTransform = entryTransform.Find(labelPath);
            if (labelTransform == null)
            {
                Logger.Warning($"[Clipboard] Missing label path: {labelPath}");
                return;
            }

            var label = labelTransform.GetComponentInChildren<TextMeshProUGUI>();
            if (label == null)
            {
                Logger.Warning($"[Clipboard] Missing TextMeshPro component at {labelPath}");
                return;
            }
            
            if (string.IsNullOrEmpty(guid)) return;
            var entityData = LabelTracker.GetEntityData(guid);
            if (entityData != null && !string.IsNullOrEmpty(entityData.LabelText)) label.text = entityData.LabelText;
        }

        [HarmonyPatch(typeof(ObjectListFieldUI), nameof(ObjectListFieldUI.Refresh))]
        [HarmonyPostfix]
        public static void OnObjectListRefresh(ObjectListFieldUI __instance, Il2CppSystem.Collections.Generic.List<BuildableItem> newVal)
        {
            if (!ModSettings.ShowClipboardStations.Value) return;

            var objectsUIContents = __instance.gameObject.transform.Find("Contents")?.gameObject;
            if (objectsUIContents == null)
            {
                Logger.Error("[Clipboard] Couldn't find ObjectList contents container");
                return;
            }

            foreach (var entry in objectsUIContents.transform)
            {
                var entryTransform = entry.TryCast<Transform>();
                if (entryTransform == null || !entryTransform.name.Contains("Entry") ||
                    !entryTransform.gameObject.active)
                    continue;

                var objectLabelTransform = entryTransform.Find("Title");
                if (objectLabelTransform == null)
                {
                    Logger.Warning($"[Clipboard] Missing title on {entryTransform.name}");
                    continue;
                }

                var textComponent = objectLabelTransform.GetComponentInChildren<TextMeshProUGUI>();
                if (textComponent == null)
                {
                    Logger.Warning($"[Clipboard] Missing text component on {entryTransform.name}");
                    continue;
                }

                var index = entryTransform.GetSiblingIndex();
                if (index >= newVal.Count)
                {
                    //Logger.Warning($"[Clipboard] Index out of range: {index}");
                    continue;
                }

                var objectGUID = newVal._items[index].GUID.ToString();
                var entityData = LabelTracker.GetEntityData(objectGUID);
                if (entityData != null && !string.IsNullOrEmpty(entityData.LabelText))
                    textComponent.text = entityData.LabelText;
            }
        }

        [HarmonyPatch(typeof(ObjectFieldUI), nameof(ObjectFieldUI.Refresh))]
        [HarmonyPostfix]
        public static void OnObjectFieldRefresh(ObjectFieldUI __instance, BuildableItem newVal)
        {
            if (!ModSettings.ShowClipboardStationsOutput.Value) return;
            // Skip if debug is disabled to avoid triggering Mod Manager logging
            if (ModSettings.ShowDebug != null && !ModSettings.ShowDebug.Value) return;
            // null is expected when ObjectFieldUI is used for non-BuildableItem purposes (e.g., clipboard)
            if (newVal == null)
            {
                return;
            }

            var objectFieldUILabelTransform = __instance.gameObject.transform.Find("Selection/Label");
            if (objectFieldUILabelTransform == null)
            {
                Logger.Error("[Clipboard] Missing label transform in ObjectField");
                return;
            }

            var textComponent = objectFieldUILabelTransform.GetComponentInChildren<TextMeshProUGUI>();
            if (textComponent == null)
            {
                Logger.Error("[Clipboard] Missing text component in ObjectField");
                return;
            }

            var entityData = LabelTracker.GetEntityData(newVal.GUID.ToString());
            if (entityData != null && !string.IsNullOrEmpty(entityData.LabelText))
                textComponent.text = entityData.LabelText;
        }
    }
}