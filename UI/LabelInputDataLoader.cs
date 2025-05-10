﻿using System;
using Il2CppTMPro;
using SimpleLabels.Data;
using SimpleLabels.Settings;
using UnityEngine;
using UnityEngine.UI;
using Logger = SimpleLabels.Utils.Logger;

namespace SimpleLabels.UI
{
    public class LabelInputDataLoader
    {
        public static void LoadLabelData(string entityGuid, GameObject entityGameObject, GameObject inputGameObject, string entityName = "")
        {
            var inputGameObjectName = inputGameObject.name.Replace("(Clone)", "").Replace("_Built", "")
                .Replace("Mk2", "").Replace("_", "").Trim();
            InputFieldManager.DeactivateInputField(inputGameObjectName);
            if (!ModSettings.ShowInput.Value) return;
            InputFieldManager.ActivateInputField(inputGameObjectName);

            try
            {
                if (string.IsNullOrEmpty(entityGuid)) return;
                LabelTracker.SetCurrentlyManagedEntity(entityGuid);
                var inputField = InputFieldManager.GetInputField(inputGameObjectName);
                var numericInputField = InputFieldManager.GetNumericInputField(inputGameObjectName);
                var entityNameIndicator = InputFieldManager.GetEntityNameIndicator(inputGameObjectName);

                // Load existing label data
                var entityData = LabelTracker.GetEntityData(entityGuid);
                inputField.text = entityData?.LabelText ?? string.Empty;
                entityNameIndicator.text = entityName;
                inputField.GetComponent<Image>().color = ColorUtility.TryParseHtmlString(
                    entityData?.LabelColor ?? ModSettings.LabelDefaultColor.Value, out var color)
                    ? color
                    : Color.red;
                inputField.GetComponentInChildren<TextMeshProUGUI>().color = ColorUtility.TryParseHtmlString(
                    entityData?.FontColor ?? ModSettings.FontDefaultColor.Value, out color)
                    ? color
                    : Color.red;
                numericInputField.text =
                    entityData?.LabelSize.ToString() ?? ModSettings.LabelDefaultSize.Value.ToString();

                // Focus if enabled
                if (ModSettings.AutoFocusInput.Value)
                {
                    inputField.ActivateInputField();
                    InputFieldManager._currentInputField = inputField;
                    InputFieldManager._currentNumericInputField = numericInputField;
                }

                //Track the entity if not already tracked
                if (LabelTracker.GetEntityData(entityGuid) != null) return;
                LabelTracker.TrackEntity(
                    entityGuid,
                    entityGameObject,
                    entityData?.LabelText ?? string.Empty,
                    entityData?.LabelColor ?? ModSettings.LabelDefaultColor.Value,
                    entityData?.LabelSize ?? ModSettings.LabelDefaultSize.Value,
                    entityData?.FontSize ?? ModSettings.DEFAULT_FONT_SIZE,
                    entityData?.FontColor ?? ModSettings.FontDefaultColor.Value
                );
            }
            catch (Exception e)
            {
                Logger.Error($"Failed to handle storage open: {e.Message}");
            }
        }
    }
}