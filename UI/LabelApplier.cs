using System;
using System.Collections.Generic;
using Il2CppTMPro;
using SimpleLabels.Data;
using UnityEngine;
using Logger = SimpleLabels.Utils.Logger;

namespace SimpleLabels.UI
{
    public class LabelApplier
    {
        private static readonly Dictionary<string, List<GameObject>> _entityLabels =
            new Dictionary<string, List<GameObject>>();

        public static void ApplyOrUpdateLabel(string guid)
        {
            if (string.IsNullOrEmpty(guid))
                return;

            var entityData = LabelTracker.GetEntityData(guid);
            if (entityData?.GameObject == null)
            {
                Logger.Msg($"[LabelApplier] Cannot apply label: GUID={guid}, GameObject is null");
                return;
            }

            var entityType = CleanEntityName(entityData.GameObject.name);
            if (!LabelPlacementConfigs.LabelPlacementConfigsDictionary.TryGetValue(entityType, out var labelPlacements))
            {
                Logger.Msg($"[LabelApplier] No placement config for entity type: {entityType}");
                return;
            }

            if (String.IsNullOrEmpty(entityData.LabelText))
            {
                RemoveLabels(guid);
                return;
            }

            Logger.Msg($"[LabelApplier] Applying label: GUID={guid}, Type={entityType}, Text='{entityData.LabelText}', Placements={labelPlacements.Count}");

            EnsureLabelCount(guid, labelPlacements.Count);

            for (var i = 0; i < labelPlacements.Count; i++)
            {
                ConfigureLabel(_entityLabels[guid][i], entityData, labelPlacements[i]);
            }
        }

        private static void ConfigureLabel(GameObject labelInstance, LabelTracker.EntityData entityData,
            LabelPlacement placement)
        {
            if (labelInstance == null)
                labelInstance = LabelPrefabManager.GetLabelInstance();

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

        public static void ForceUpdateAllLabels()
        {
            foreach (var guid in LabelTracker.GetAllTrackedGuids())
            {
                ApplyOrUpdateLabel(guid);
            }

        }
    }
}
