using System;
using Il2CppTMPro;
using SimpleLabels.Data;
using SimpleLabels.Patches;
using SimpleLabels.Services;
using SimpleLabels.Settings;
using UnityEngine;
using UnityEngine.UI;
using Logger = SimpleLabels.Utils.Logger;

namespace SimpleLabels.UI
{
    public class LabelInputDataLoader
    {
        public static void LoadLabelData(string entityGuid, GameObject entityGameObject, string inputUIKey, string entityName = "")
        {
            InputFieldManager.DeactivateInputField(inputUIKey);

            try
            {
                if (string.IsNullOrEmpty(entityGuid)) return;

                LabelTracker.SetCurrentlyManagedEntity(entityGuid);
                var inputField = InputFieldManager.GetInputField(inputUIKey);
                var numericInputField = InputFieldManager.GetNumericInputField(inputUIKey);
                var entityNameIndicator = InputFieldManager.GetEntityNameIndicator(inputUIKey);
                if (inputField == null || numericInputField == null || entityNameIndicator == null)
                {
                    Logger.Warning($"The {inputUIKey} label controls are not initialized yet.");
                    return;
                }
                var entityData = LabelTracker.GetEntityData(entityGuid);

                if (entityData != null && entityData.GameObject == null && entityGameObject != null)
                {
                    LabelService.BindGameObject(entityGuid, entityGameObject);
                }

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

                if (LabelTracker.GetEntityData(entityGuid) == null)
                {
                    LabelService.CreateLabel(
                        entityGuid,
                        entityGameObject,
                        entityData?.LabelText ?? string.Empty,
                        entityData?.LabelColor ?? ModSettings.LabelDefaultColor.Value,
                        entityData?.LabelSize ?? ModSettings.LabelDefaultSize.Value,
                        entityData?.FontSize ?? ModSettings.DEFAULT_FONT_SIZE,
                        entityData?.FontColor ?? ModSettings.FontDefaultColor.Value
                    );
                }

                if (ModSettings.ShowInput.Value)
                {
                    InputFieldManager.ActivateInputField(inputUIKey);
                    if (ModSettings.AutoFocusInput.Value)
                    {
                        inputField.ActivateInputField();
                        InputFieldManager.SetCurrentInputFields(inputField, numericInputField);
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error($"Failed to handle storage open: {e.Message}");
            }
        }
    }
}
