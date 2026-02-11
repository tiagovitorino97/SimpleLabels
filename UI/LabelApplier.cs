using System;
using System.Collections.Generic;
using Il2CppTMPro;
using SimpleLabels.Data;
using UnityEngine;
using Logger = SimpleLabels.Utils.Logger;

namespace SimpleLabels.UI
{
    /// <summary>
    /// Applies physical label GameObjects to entities: parents, positions, scales, and sets text/colors from EntityData.
    /// </summary>
    /// <remarks>
    /// Reads from LabelTracker and LabelPlacementConfigs. Uses LabelPrefabManager for pooled instances.
    /// CleanEntityName strips (Clone), _Built, etc. to match config keys. Multiple placements per entity
    /// (e.g. storage racks) get multiple instances; EnsureLabelCount keeps count in sync. Empty text
    /// triggers RemoveLabels and returns instances to the pool.
    /// </remarks>
    public class LabelApplier
    {
        private static readonly Dictionary<string, List<GameObject>> _entityLabels =
            new Dictionary<string, List<GameObject>>();

        /// <summary>
        /// Ensures the entity has the correct number of label instances, configures them from EntityData and placement config.
        /// </summary>
        /// <remarks>
        /// No-op if GUID empty or entity/GameObject missing. Uses CleanEntityName to look up placements;
        /// if none, logs and returns. Empty LabelText removes labels. Otherwise ensures count, then
        /// ConfigureLabel for each (parent, position, rotation, scale, PaperBackground color, LabelText).
        /// </remarks>
        public static void ApplyOrUpdateLabel(string guid)
        {
            if (string.IsNullOrEmpty(guid))
                return;

            var entityData = LabelTracker.GetEntityData(guid);
            if (entityData?.GameObject == null)
                return;

            var entityType = CleanEntityName(entityData.GameObject.name);
            if (!LabelPlacementConfigs.LabelPlacementConfigsDictionary.TryGetValue(entityType, out var labelPlacements))
                return;

            if (String.IsNullOrEmpty(entityData.LabelText))
            {
                RemoveLabels(guid);
                return;
            }

            EnsureLabelCount(guid, labelPlacements.Count);

            for (var i = 0; i < labelPlacements.Count; i++)
            {
                ConfigureLabel(_entityLabels[guid][i], entityData, labelPlacements[i]);
            }
        }

        private static void ConfigureLabel(GameObject labelInstance, EntityData entityData,
            LabelPlacement placement)
        {
            if (labelInstance == null)
                labelInstance = LabelPrefabManager.GetLabelInstance();

            // Ensure the label is active
            if (!labelInstance.activeSelf)
                labelInstance.SetActive(true);

            labelInstance.transform.SetParent(entityData.GameObject.transform, false);
            labelInstance.transform.localPosition = placement.LocalPosition;
            labelInstance.transform.localRotation = placement.Rotation;

            var baseScale = 0.1f;
            var scaleFactor = 1f + (entityData.LabelSize - 1) / 9f;
            var newScale = baseScale * scaleFactor;
            labelInstance.transform.localScale = new Vector3(newScale, newScale, baseScale);

            var paperBackground = labelInstance.transform.Find("LabelObject/PaperBackground");
            if (paperBackground != null && paperBackground.TryGetComponent<Renderer>(out var renderer))
                renderer.material.color = ColorUtility.TryParseHtmlString(entityData.LabelColor, out var color)
                    ? color
                    : Color.red;

            var textMeshPro = labelInstance.transform.Find("LabelObject/LabelText")?.GetComponent<TextMeshPro>();
            if (textMeshPro != null)
            {
                textMeshPro.text = entityData.LabelText;
                textMeshPro.color = ColorUtility.TryParseHtmlString(entityData.FontColor, out var color)
                    ? color
                    : Color.red;
            }
        }

        private static void EnsureLabelCount(string guid, int requiredCount)
        {
            if (!_entityLabels.TryGetValue(guid, out var labelInstances))
            {
                labelInstances = new List<GameObject>();
                _entityLabels.Add(guid, labelInstances);
            }

            while (labelInstances.Count < requiredCount) 
                labelInstances.Add(LabelPrefabManager.GetLabelInstance());

            while (labelInstances.Count > requiredCount)
            {
                LabelPrefabManager.ReturnToPool(labelInstances[labelInstances.Count - 1]);
                labelInstances.RemoveAt(labelInstances.Count - 1);
            }
        }

        private static string CleanEntityName(string originalName)
        {
            return originalName.Replace("(Clone)", "")
                .Replace("_Built", "")
                .Trim();
        }

        /// <summary>
        /// Returns all label instances for the entity to the pool and removes the entity from the label map.
        /// </summary>
        /// <remarks>
        /// Used when LabelText is cleared or entity is removed. No-op if the entity has no labels.
        /// </remarks>
        public static void RemoveLabels(string guid)
        {
            if (!_entityLabels.TryGetValue(guid, out var labelInstances)) return;

            foreach (var labelInstance in labelInstances)
            {
                if (labelInstance == null) continue;
                LabelPrefabManager.ReturnToPool(labelInstance);
            }

            _entityLabels.Remove(guid);
        }

        public static void Terminate()
        {
            foreach (var labelInstances in _entityLabels.Values)
                foreach (var labelInstance in labelInstances)
                {
                    if (labelInstance == null) continue;
                    LabelPrefabManager.ReturnToPool(labelInstance);
                }

            _entityLabels.Clear();
        }

        /// <summary>
        /// Re-applies all tracked labels. Use after scene load or network sync when GameObjects may have changed.
        /// </summary>
        /// <remarks>
        /// Iterates LabelTracker.GetAllTrackedGuids() and calls ApplyOrUpdateLabel for each. Skips entities
        /// with null GameObject (e.g. not yet loaded).
        /// </remarks>
        public static void ForceUpdateAllLabels()
        {
            foreach (var guid in LabelTracker.GetAllTrackedGuids())
            {
                ApplyOrUpdateLabel(guid);
            }

        }
    }
}
