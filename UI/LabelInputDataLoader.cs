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
    /// <summary>
    /// Loads label data into the input UI when a station or storage is opened, and binds GameObject if needed.
    /// </summary>
    /// <remarks>
    /// Called from loader patches when storage/station menus open. Derives input key from
    /// <paramref name="inputGameObject"/> name (strip Clone, _Built, Mk2, etc.). Always loads data into
    /// input field, colors, and numeric size; visibility and focus are gated by ModSettings.ShowInput
    /// and AutoFocusInput. If entity exists but has no GameObject (e.g. from network), binds
    /// <paramref name="entityGameObject"/> and applies the label. Creates entity via LabelService if not tracked.
    /// </remarks>
    public class LabelInputDataLoader
    {
        /// <summary>
        /// Loads label data for the given entity into the input UI for the opened station/storage.
        /// </summary>
        /// <remarks>
        /// Deactivates input first, sets currently managed entity, then populates fields from LabelTracker.
        /// Binds GameObject when entity exists but ref is null. Creates entity if missing. Activates input
        /// and focuses only when ShowInput is enabled; data is always loaded so toggle-on shows current entity.
        /// </remarks>
        public static void LoadLabelData(string entityGuid, GameObject entityGameObject, GameObject inputGameObject, string entityName = "")
        {
            var inputGameObjectName = LoaderPatches.CleanGameObjectName(inputGameObject.name);
            InputFieldManager.DeactivateInputField(inputGameObjectName);

            try
            {
                if (string.IsNullOrEmpty(entityGuid)) return;

                LabelTracker.SetCurrentlyManagedEntity(entityGuid);
                var inputField = InputFieldManager.GetInputField(inputGameObjectName);
                var numericInputField = InputFieldManager.GetNumericInputField(inputGameObjectName);
                var entityNameIndicator = InputFieldManager.GetEntityNameIndicator(inputGameObjectName);

                // Load existing label data (may have come from host via network sync)
                var entityData = LabelTracker.GetEntityData(entityGuid);

                // If we have entity data from the network but no GameObject yet, bind it now
                // and apply the world label (otherwise it would never show for remotely-created labels).
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

                // Show/hide input UI based on toggle; always load data above so toggle-on shows current entity.
                if (ModSettings.ShowInput.Value)
                {
                    InputFieldManager.ActivateInputField(inputGameObjectName);
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