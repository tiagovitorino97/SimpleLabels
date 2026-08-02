using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using Logger = SimpleLabels.Utils.Logger;

namespace SimpleLabels.Data
{
    public static class LabelTracker
    {
        private static string _currentlyManagedEntityGuid;

        private static readonly Dictionary<string, EntityData> EntityDataDictionary = new();

        internal static void Clear()
        {
            EntityDataDictionary.Clear();
            _currentlyManagedEntityGuid = null;
        }

        public static void StoreEntity(string guid, GameObject gameObject, string labelText, string labelColor,
            int labelSize, int fontSize, string fontColor)
        {
            if (string.IsNullOrEmpty(guid))
            {
                Logger.Error("[LabelTracker] Cannot store entity: GUID is empty");
                return;
            }

            if (!EntityDataDictionary.ContainsKey(guid))
            {
                EntityDataDictionary.Add(guid,
                    new EntityData(guid, gameObject, labelText, labelColor, labelSize, fontSize, fontColor));
            }
            else
            {
                Logger.Warning($"[LabelTracker] Entity {guid} already exists, use UpdateEntityData to modify");
            }
        }

        public static void UpdateEntityData(string guid, string newLabelText = null, string newLabelColor = null,
            int? newLabelSize = null, int? newFontSize = null, string newFontColor = null)
        {
            if (!TryGetEntity(guid, out var entityData))
                return;

            if (newLabelText != null) entityData.LabelText = newLabelText;
            if (newLabelColor != null) entityData.LabelColor = newLabelColor;
            if (newLabelSize.HasValue) entityData.LabelSize = newLabelSize.Value;
            if (newFontSize.HasValue) entityData.FontSize = newFontSize.Value;
            if (newFontColor != null) entityData.FontColor = newFontColor;
        }

        private static bool TryGetEntity(string guid, out EntityData entityData)
        {
            entityData = null;
            
            if (string.IsNullOrEmpty(guid))
            {
                Logger.Error("Attempted to access entity with empty GUID");
                return false;
            }

            if (!EntityDataDictionary.TryGetValue(guid, out entityData))
                return false;

            return true;
        }


        public static void UpdateGameObjectReference(string guid, GameObject gameObject)
        {
            if (TryGetEntity(guid, out var entityData))
                entityData.GameObject = gameObject;
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
            return TryGetEntity(guid, out var entityData) ? entityData : null;
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


    }
}