using System.Collections.Generic;
using Newtonsoft.Json;
using SimpleLabels.UI;
using UnityEngine;
using Logger = SimpleLabels.Utils.Logger;

namespace SimpleLabels.Data
{
    public static class LabelTracker
    {
        private static string _currentlyManagedEntityGuid;

        private static readonly Dictionary<string, EntityData> EntityDataDictionary =
            new Dictionary<string, EntityData>();

        public static void TrackEntity(string guid, GameObject gameObject, string labelText, string labelColor,
            int labelSize, int fontSize, string fontColor)
        {
            if (string.IsNullOrEmpty(guid))
            {
                Logger.Error("Attempted to track entity with empty GUID");
                return;
            }

            if (!EntityDataDictionary.ContainsKey(guid))
            {
                EntityDataDictionary.Add(guid,
                    new EntityData(guid, gameObject, labelText, labelColor, labelSize, fontSize, fontColor));
                Logger.Msg($"[LabelTracker] Tracked new entity: GUID={guid}, Text='{labelText}'");
            }
            else
            {
                Logger.Warning($"Attempted to track entity with duplicate GUID {guid}");
            }
        }

        public static void UpdateLabel(string guid, string newLabelText = null, string newLabelColor = null,
            int? newLabelSize = null, int? newFontSize = null, string newFontColor = null)
        {
            if (string.IsNullOrEmpty(guid))
            {
                Logger.Error("Attempted to update entity with empty GUID");
                return;
            }

            if (EntityDataDictionary.TryGetValue(guid, out var value))
            {
                var oldText = value.LabelText;
                value.LabelText = newLabelText ?? value.LabelText;
                value.LabelColor = newLabelColor ?? value.LabelColor;
                value.LabelSize = newLabelSize ?? value.LabelSize;
                value.FontSize = newFontSize ?? value.FontSize;
                value.FontColor = newFontColor ?? value.FontColor;
                
                Logger.Msg($"[LabelTracker] Updated label: GUID={guid}, Text='{oldText}' -> '{value.LabelText}'");
            }
            else
            {
                Logger.Warning($"Attempted to update entity with non-existent GUID: {guid}");
            }

            LabelApplier.ApplyOrUpdateLabel(guid);
            LabelNetworkManager.NotifyLabelChanged(guid);
        }

        /// <summary>
        /// Updates label data from network without triggering network sync (to avoid loops).
        /// </summary>
        public static void UpdateLabelFromNetwork(string guid, string newLabelText = null, string newLabelColor = null,
            int? newLabelSize = null, int? newFontSize = null, string newFontColor = null)
        {
            if (string.IsNullOrEmpty(guid))
            {
                Logger.Error("Attempted to update entity with empty GUID");
                return;
            }

            if (EntityDataDictionary.TryGetValue(guid, out var value))
            {
                value.LabelText = newLabelText ?? value.LabelText;
                value.LabelColor = newLabelColor ?? value.LabelColor;
                value.LabelSize = newLabelSize ?? value.LabelSize;
                value.FontSize = newFontSize ?? value.FontSize;
                value.FontColor = newFontColor ?? value.FontColor;
            }
            else
            {
                Logger.Warning($"Attempted to update entity with non-existent GUID: {guid}");
                return;
            }

            // Apply/update the label visuals locally (no network sync)
            LabelApplier.ApplyOrUpdateLabel(guid);
        }


        public static void UpdateGameObjectReference(string guid, GameObject gameObject)
        {
            if (string.IsNullOrEmpty(guid))
            {
                Logger.Error("Attempted to update entity with empty GUID");
                return;
            }

            if (EntityDataDictionary.TryGetValue(guid, out var value))
                value.GameObject = gameObject;
        }

        public static Dictionary<string, EntityData> GetAllEntityData()
        {
            var copy = new Dictionary<string, EntityData>(EntityDataDictionary.Count);
            foreach (var kvp in EntityDataDictionary)
            {
                var src = kvp.Value;
                if (src == null)
                    continue;

                copy[kvp.Key] = new EntityData(
                    guid: src.Guid,
                    labelText: src.LabelText,
                    labelColor: src.LabelColor,
                    labelSize: src.LabelSize,
                    fontSize: src.FontSize,
                    fontColor: src.FontColor
                );
            }

            return copy;
        }

        public static EntityData GetEntityData(string guid)
        {
            if (string.IsNullOrEmpty(guid))
            {
                Logger.Error("Attempted to get storage with empty GUID");
                return null;
            }

            return EntityDataDictionary.TryGetValue(guid, out var entityData) ? entityData : null;
        }

        public static void SetCurrentlyManagedEntity(string guid)
        {
            _currentlyManagedEntityGuid = guid;
        }

        public static string GetCurrentlyManagedEntityGuid()
        {
            return _currentlyManagedEntityGuid;
        }

        public static string GetCurrentlyManagedEntityLabelText()
        {
            return GetEntityData(_currentlyManagedEntityGuid)?.LabelText;
        }

        public static GameObject GetCurrentlyManagedEntityGameObject()
        {
            return GetEntityData(_currentlyManagedEntityGuid)?.GameObject;
        }

        public static List<string> GetAllTrackedGuids()
        {
            return new List<string>(EntityDataDictionary.Keys);
        }

        public class EntityData
        {
            public EntityData()
            {
            }

            public EntityData(string guid, GameObject gameObject, string labelText, string labelColor,
                int labelSize, int fontSize, string fontColor)
            {
                Guid = guid;
                GameObject = gameObject;
                LabelText = labelText;
                LabelSize = labelSize;
                LabelColor = labelColor;
                FontSize = fontSize;
                FontColor = fontColor;
            }

            public EntityData(string guid, string labelText, string labelColor,
                int labelSize, int fontSize, string fontColor)
                : this(guid, null, labelText, labelColor, labelSize, fontSize, fontColor)
            {
            }

            public string Guid { get; set; }

            [JsonIgnore] public GameObject GameObject { get; set; }

            public string LabelText { get; set; }
            public int LabelSize { get; set; }
            public string LabelColor { get; set; }
            public int FontSize { get; set; }
            public string FontColor { get; set; }
        }

        public static void UpdateLocalLabelsFromNetwork(Dictionary<string, EntityData> networkedData)
        {
            Logger.Msg($"[LabelTracker] Updating local labels from network: {networkedData.Count} entities");
            
            foreach (var kvp in networkedData)
            {
                var guid = kvp.Key;
                var networkedEntityData = kvp.Value;
                if (EntityDataDictionary.ContainsKey(guid))
                {
                    UpdateLabel(guid,
                        newLabelText: networkedEntityData.LabelText,
                        newLabelColor: networkedEntityData.LabelColor,
                        newLabelSize: networkedEntityData.LabelSize,
                        newFontSize: networkedEntityData.FontSize,
                        newFontColor: networkedEntityData.FontColor);
                }
                else
                {
                    TrackEntity(guid,
                        gameObject: null,
                        labelText: networkedEntityData.LabelText,
                        labelColor: networkedEntityData.LabelColor,
                        labelSize: networkedEntityData.LabelSize,
                        fontSize: networkedEntityData.FontSize,
                        fontColor: networkedEntityData.FontColor);
                }
            }

            LabelApplier.ForceUpdateAllLabels();
        }
    }
}