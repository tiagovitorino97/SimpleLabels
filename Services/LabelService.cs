using System.Collections.Generic;
using SimpleLabels.Data;
using SimpleLabels.Settings;
using SimpleLabels.UI;
using UnityEngine;
using Il2CppScheduleOne.Building;
using Il2CppScheduleOne.EntityFramework;
using Logger = SimpleLabels.Utils.Logger;

namespace SimpleLabels.Services
{
    public static class LabelService
    {
        public static void CreateLabel(string guid, GameObject gameObject, string text = "", 
            string color = null, int? size = null, int? fontSize = null, string fontColor = null, bool isSync = false)
        {
            if (string.IsNullOrEmpty(guid)) return;

            color ??= ModSettings.LabelDefaultColor.Value;
            size ??= ModSettings.LabelDefaultSize.Value;
            fontSize ??= ModSettings.DEFAULT_FONT_SIZE;
            fontColor ??= ModSettings.FontDefaultColor.Value;

            LabelTracker.StoreEntity(guid, gameObject, text, color, size ?? ModSettings.LabelDefaultSize.Value, fontSize ?? ModSettings.DEFAULT_FONT_SIZE, fontColor);

            if (gameObject != null && !string.IsNullOrEmpty(text))
                LabelApplier.ApplyOrUpdateLabel(guid);

            if (!isSync && !string.IsNullOrEmpty(text))
                LabelNetworkManager.NotifyLabelChanged(guid);
        }

        public static void UpdateLabel(string guid, string text = null, string color = null,
            int? size = null, int? fontSize = null, string fontColor = null, bool isSync = false)
        {
            if (string.IsNullOrEmpty(guid)) return;

            var entity = LabelTracker.GetEntityData(guid);
            if (entity == null) return;

            LabelTracker.UpdateEntityData(guid, text, color, size, fontSize, fontColor);
            LabelApplier.ApplyOrUpdateLabel(guid);

            if (!isSync)
                LabelNetworkManager.NotifyLabelChanged(guid);
        }

        public static void RemoveLabel(string guid) => UpdateLabel(guid, text: "");

        public static void BindGameObject(string guid, GameObject go)
        {
            if (string.IsNullOrEmpty(guid) || go == null) return;

            var entity = LabelTracker.GetEntityData(guid);
            if (entity == null) return;

            LabelTracker.UpdateGameObjectReference(guid, go);

            if (!string.IsNullOrEmpty(entity.LabelText))
                LabelApplier.ApplyOrUpdateLabel(guid);
        }

        public static void BindGameObjectForGuid(string guid)
        {
            if (string.IsNullOrEmpty(guid)) return;
            var entity = LabelTracker.GetEntityData(guid);
            if (entity == null || entity.GameObject != null) return;

            foreach (var item in UnityEngine.Object.FindObjectsOfType<GridItem>())
            {
                if (item?.GUID.ToString() == guid)
                {
                    BindGameObject(guid, item.gameObject);
                    return;
                }
            }

            foreach (var item in UnityEngine.Object.FindObjectsOfType<SurfaceItem>())
            {
                if (item?.GUID.ToString() == guid)
                {
                    BindGameObject(guid, item.gameObject);
                    return;
                }
            }
        }

        public static void ApplySync(string guid, string text = null, string color = null,
            int? size = null, int? fontSize = null, string fontColor = null)
        {
            UpdateLabel(guid, text, color, size, fontSize, fontColor, isSync: true);
        }

        public static void ApplyNetworkLabels(Dictionary<string, EntityData> data)
        {
            foreach (var kvp in data)
            {
                var guid = kvp.Key;
                var info = kvp.Value;
                var existing = LabelTracker.GetEntityData(guid);

                if (existing != null)
                {
                    ApplySync(guid, info.LabelText, info.LabelColor, info.LabelSize, info.FontSize, info.FontColor);
                }
                else
                {
                    CreateLabel(guid, null, info.LabelText, info.LabelColor, info.LabelSize, info.FontSize, info.FontColor, isSync: true);
                }
            }
            LabelApplier.ForceUpdateAllLabels();
        }

        public static void SyncAll()
        {
            var map = new Dictionary<string, GameObject>();

            foreach (var item in UnityEngine.Object.FindObjectsOfType<GridItem>())
            {
                if (item == null) continue;
                var id = item.GUID.ToString();
                if (!string.IsNullOrEmpty(id) && !map.ContainsKey(id)) map.Add(id, item.gameObject);
            }

            foreach (var item in UnityEngine.Object.FindObjectsOfType<SurfaceItem>())
            {
                if (item == null) continue;
                var id = item.GUID.ToString();
                if (!string.IsNullOrEmpty(id) && !map.ContainsKey(id)) map.Add(id, item.gameObject);
            }

            foreach (var guid in LabelTracker.GetAllTrackedGuids())
            {
                var entity = LabelTracker.GetEntityData(guid);
                if (entity == null) continue;

                if (entity.GameObject == null && map.TryGetValue(guid, out var go))
                {
                    BindGameObject(guid, go);
                }
                else if (entity.GameObject != null && !string.IsNullOrEmpty(entity.LabelText))
                {
                    LabelApplier.ApplyOrUpdateLabel(guid);
                }
            }
        }
    }
}
