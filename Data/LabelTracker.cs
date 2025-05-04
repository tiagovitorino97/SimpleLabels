using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using Logger = SimpleLabels.Utils.Logger;

namespace SimpleLabels.Data
{
    public static class LabelTracker
    {
        private static string _currentlyManagedEntityGuid;

        private static readonly Dictionary<string, EntityData> EntityDataDictionary =
            new Dictionary<string, EntityData>(); // <Guid, EntityData>

        public static void TrackEntity(string guid, GameObject gameObject, string labelText, string labelColor,
            int labelSize, int fontSize, string fontColor)
        {
            if (string.IsNullOrEmpty(guid))
            {
                Logger.Error("Attempted to track entity with empty GUID");
                return;
            }

            if (!EntityDataDictionary.ContainsKey(guid))
                EntityDataDictionary.Add(guid,
                    new EntityData(guid, gameObject, labelText, labelColor, labelSize, fontSize, fontColor));
            else
                Logger.Warning($"Attempted to track entity with duplicate GUID: {guid}");
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
                value.LabelText = newLabelText ?? value.LabelText;
                value.LabelColor = newLabelColor ?? value.LabelColor;
                value.LabelSize = newLabelSize ?? value.LabelSize;
                value.FontSize = newFontSize ?? value.FontSize;
                value.FontColor = newFontColor ?? value.FontColor;
            }
            else
            {
                Logger.Warning($"Attempted to update entity with non-existent GUID: {guid}");
            }
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
            else
                Logger.Warning($"Attempted to update entity with non-existent GUID: {guid}");
        }

        public static Dictionary<string, EntityData> GetAllEntityData()
        {
            return new Dictionary<string, EntityData>(EntityDataDictionary);
        }

        public static EntityData GetEntityData(string guid)
        {
            if (string.IsNullOrEmpty(guid))
            {
                Logger.Error("Attempted to get storage with empty GUID");
                return null;
            }

            if (EntityDataDictionary.TryGetValue(guid, out var entityData)) return entityData;

            Logger.Msg($"Attempted to get storage with non-existent GUID: {guid}");
            return null;
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
            public string LabelColor { get; set; } // Store color as a hex string (e.g., "#FFFFFF")
            public int FontSize { get; set; }
            public string FontColor { get; set; }
        }
    }
}