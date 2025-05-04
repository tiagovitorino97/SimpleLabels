using System;
using System.Collections.Generic;
using System.Linq;
using Il2CppTMPro;
using SimpleLabels.Data;
using SimpleLabels.Settings;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Logger = SimpleLabels.Utils.Logger;

// ReSharper disable All


namespace SimpleLabels.UI
{
    public class InputFieldManager
    {
        public static Dictionary<string, TMP_InputField> InputFields = new Dictionary<string, TMP_InputField>();
        public static Dictionary<string, TMP_InputField> NumericInputFields = new Dictionary<string, TMP_InputField>();

        public static Dictionary<string, Vector2> SupportedUITypes = new Dictionary<string, Vector2>()
        {
            { "UI/StorageMenu", new Vector2(0.5f, 0.65f) },
            { "UI/Stations/PackagingStation", new Vector2(0.5f, 0.6f) },
            { "UI/Stations/ChemistryStation", new Vector2(0.5f, 0.75f) },
            { "UI/Stations/LabOven", new Vector2(0.5f, 0.6f) },
            { "UI/Stations/BrickPress", new Vector2(0.5f, 0.55f) },
            { "UI/Stations/Cauldron", new Vector2(0.5f, 0.55f) },
            { "UI/Stations/MixingStation", new Vector2(0.5f, 0.65f) },
            { "UI/Stations/DryingRack", new Vector2(0.5f, 0.7f) }
        };

        public static TMP_InputField _currentInputField;
        public static TMP_InputField _currentNumericInputField;

        public static void Initialize()
        {
            CreateInputFields();
        }

        public static void Terminate()
        {
            foreach (var inputField in InputFields.Values)
            {
                if (inputField == null) continue;
                GameObject.Destroy(inputField.gameObject);
            }

            InputFields.Clear();

            foreach (var numericField in NumericInputFields.Values)
            {
                if (numericField == null) continue;
                GameObject.Destroy(numericField.gameObject);
            }

            NumericInputFields.Clear();
        }

        public static void ActivateInputField(string gameObjectName)
        {
            InputFields.First(x => x.Key.Contains(gameObjectName)).Value.gameObject
                .SetActive(true);
            if (NumericInputFields.Any(x => x.Key.Contains(gameObjectName)))
            {
                NumericInputFields.First(x => x.Key.Contains(gameObjectName)).Value.gameObject
                    .SetActive(true);
            }
        }

        public static void DeactivateInputField(string gameObjectName)
        {
            InputFields.First(x => x.Key.Contains(gameObjectName)).Value.gameObject
                .SetActive(false);
            if (NumericInputFields.Any(x => x.Key.Contains(gameObjectName)))
            {
                NumericInputFields.First(x => x.Key.Contains(gameObjectName)).Value.gameObject
                    .SetActive(false);
            }
        }

        public static TMP_InputField GetInputField(string gameObjectName)
        {
            return InputFields.First(x => x.Key.Contains(gameObjectName)).Value;
        }

        public static TMP_InputField GetNumericInputField(string gameObjectName)
        {
            return NumericInputFields.First(x => x.Key.Contains(gameObjectName)).Value;
        }

        private static void CreateInputFields()
        {
            foreach (var uiType in SupportedUITypes)
            {
                var ui = GameObject.Find(uiType.Key);
                if (ui == null)
                {
                    Logger.Error($"Couldn't find {uiType} GameObject.");
                    continue;
                }

                // Create main input field with reduced width
                InputFields.Add(uiType.Key, CreateInputField(ui, uiType.Value, uiType.Key, true));

                // Create numeric input field
                NumericInputFields.Add(uiType.Key, CreateNumericInputField(ui, uiType.Value, uiType.Key));

                ColorPickerManager.CreateColorPicker(InputFields[uiType.Key], ColorPickerType.Label);
                ColorPickerManager.CreateColorPicker(InputFields[uiType.Key], ColorPickerType.Font);
                Logger.Msg($"Created input fields for {uiType.Key}");
            }
        }

        private static TMP_InputField CreateInputField(GameObject parent, Vector2 anchorPosition, string namePrefix,
            bool withNumericField = false)
        {
            try
            {
                //Create GameObject
                namePrefix = namePrefix.Substring(namePrefix.LastIndexOf('/') + 1); // Remove the "UI/" prefix
                GameObject inputFieldGameObject = new GameObject(namePrefix + "_InputField");
                inputFieldGameObject.layer = 5; //UI Layer
                inputFieldGameObject.transform.SetParent(parent.transform, false);

                // Set up RectTransform
                RectTransform parentRect = parent.GetComponent<RectTransform>();
                RectTransform rectTransform = inputFieldGameObject.AddComponent<RectTransform>();
                rectTransform.anchorMin = anchorPosition;
                rectTransform.anchorMax = anchorPosition;
                rectTransform.pivot = new Vector2(0.5f, 0.5f);

                // If we're adding a numeric field, make the main field narrower
                float width = withNumericField ? 550 : 550;
                rectTransform.sizeDelta = new Vector2(width, 50);

                float height = parentRect.rect.height;
                float xPos = withNumericField ? -60 : 0; // Shift to the left if we have a numeric field
                rectTransform.anchoredPosition = new Vector2(xPos, -height / 2);

                //Add visual Components
                Image backGround = inputFieldGameObject.AddComponent<Image>();
                backGround.color = ColorUtility.TryParseHtmlString(ModSettings.LabelDefaultColor.Value, out var color)
                    ? color
                    : Color.red;

                Outline outline = inputFieldGameObject.AddComponent<Outline>();
                outline.effectColor = new Color(0, 0, 0, 0.5f);
                outline.effectDistance = new Vector2(2, 2);

                //Create text area
                GameObject textArea = createTextArea(inputFieldGameObject.transform);
                GameObject placeholder = createPlaceholder(inputFieldGameObject.transform);

                //Configure the input field component
                TMP_InputField inputField = inputFieldGameObject.AddComponent<TMP_InputField>();
                inputField.textViewport = textArea.GetComponent<RectTransform>();
                inputField.textComponent = textArea.GetComponent<TextMeshProUGUI>();
                inputField.placeholder = placeholder.GetComponent<TextMeshProUGUI>();
                inputField.characterLimit = 30;

                inputField.onSubmit.AddListener((UnityAction<string>)((string text) =>
                {
                    OnInputTextChange(text, inputField);
                }));

                inputFieldGameObject.SetActive(true);
                return inputField;
            }
            catch (Exception e)
            {
                Logger.Error($"Failed to create input field: {e.Message}");
                return null;
            }
        }

        private static TMP_InputField CreateNumericInputField(GameObject parent, Vector2 anchorPosition,
            string namePrefix)
        {
            try
            {
                //Create GameObject
                namePrefix = namePrefix.Substring(namePrefix.LastIndexOf('/') + 1); // Remove the "UI/" prefix
                GameObject inputFieldGameObject = new GameObject(namePrefix + "_NumericInputField");
                inputFieldGameObject.layer = 5; //UI Layer
                inputFieldGameObject.transform.SetParent(parent.transform, false);

                // Set up RectTransform
                RectTransform parentRect = parent.GetComponent<RectTransform>();
                RectTransform rectTransform = inputFieldGameObject.AddComponent<RectTransform>();
                rectTransform.anchorMin = anchorPosition;
                rectTransform.anchorMax = anchorPosition;
                rectTransform.pivot = new Vector2(0.5f, 0.5f);
                rectTransform.sizeDelta = new Vector2(100, 50); // Smaller width for numeric input
                float height = parentRect.rect.height;
                rectTransform.anchoredPosition = new Vector2(275, -height / 2); // Position to the right of main field

                //Add visual Components
                Image backGround = inputFieldGameObject.AddComponent<Image>();
                backGround.color = new Color(0.8f, 0.8f, 0.8f); // Different color for numeric field

                Outline outline = inputFieldGameObject.AddComponent<Outline>();
                outline.effectColor = new Color(0, 0, 0, 0.5f);
                outline.effectDistance = new Vector2(2, 2);

                //Create text area
                GameObject textArea = createTextArea(inputFieldGameObject.transform);
                GameObject placeholder = createNumericPlaceholder(inputFieldGameObject.transform);

                //Configure the input field component
                TMP_InputField inputField = inputFieldGameObject.AddComponent<TMP_InputField>();
                inputField.textViewport = textArea.GetComponent<RectTransform>();
                inputField.textComponent = textArea.GetComponent<TextMeshProUGUI>();
                inputField.placeholder = placeholder.GetComponent<TextMeshProUGUI>();
                inputField.characterLimit = 2; // Only need 2 characters for 1-10
                inputField.contentType = TMP_InputField.ContentType.IntegerNumber; // Allow only numbers

                // Instead of using validation, we'll check the value on change/submit events
                inputField.contentType = TMP_InputField.ContentType.IntegerNumber; // Only allow numbers
                inputField.characterLimit = 2; // Limit to 2 characters (for "10")

                // Add listeners for both value change and submit events to validate the range
                inputField.onValueChanged.AddListener((UnityAction<string>)((string text) =>
                {
                    ValidateNumericRange(text, inputField);
                }));

                inputField.onSubmit.AddListener((UnityAction<string>)((string text) =>
                {
                    ValidateNumericRange(text, inputField);
                    OnNumericInputTextChange(text, inputField);
                }));

                inputFieldGameObject.SetActive(true);
                return inputField;
            }
            catch (Exception e)
            {
                Logger.Error($"Failed to create numeric input field: {e.Message}");
                return null;
            }
        }

        private static void OnInputTextChange(string text, TMP_InputField inputField)
        {
            inputField.DeactivateInputField();
            _currentInputField = null;
            Logger.Msg($"Text on guid: {LabelTracker.GetCurrentlyManagedEntityGuid()} changed to: {text}");
            LabelTracker.UpdateLabel(guid: LabelTracker.GetCurrentlyManagedEntityGuid(), newLabelText: text);

            if (string.IsNullOrEmpty(text))
                LabelApplier.RemoveLabels(LabelTracker.GetCurrentlyManagedEntityGuid());
            else
                LabelApplier.ApplyOrUpdateLabel(LabelTracker.GetCurrentlyManagedEntityGuid());
        }

        private static void ValidateNumericRange(string text, TMP_InputField inputField)
        {
            // If empty, don't process
            if (string.IsNullOrEmpty(text))
                return;

            // Parse the input
            if (int.TryParse(text, out int value))
            {
                // Ensure value is between 1 and 10
                int clampedValue = Mathf.Clamp(value, 1, 30);

                // If the value was changed, update the field
                if (value != clampedValue)
                {
                    inputField.text = clampedValue.ToString();
                }
            }
            else
            {
                // If not a valid integer, set to 1
                inputField.text = "1";
            }
        }

        private static void OnNumericInputTextChange(string text, TMP_InputField inputField)
        {
            inputField.DeactivateInputField();
            _currentNumericInputField = null;
            if (string.IsNullOrEmpty(text))
                return;

            int value;
            if (!int.TryParse(text, out value))
                return;

            Logger.Msg($"Numeric value on guid: {LabelTracker.GetCurrentlyManagedEntityGuid()} changed to: {value}");
            LabelTracker.UpdateLabel(guid: LabelTracker.GetCurrentlyManagedEntityGuid(), newLabelSize: value);
            if (string.IsNullOrEmpty(LabelTracker.GetCurrentlyManagedEntityLabelText())) return;
            LabelApplier.ApplyOrUpdateLabel(LabelTracker.GetCurrentlyManagedEntityGuid());
        }

        private static GameObject createTextArea(Transform parent)
        {
            GameObject textArea = new GameObject("TextArea");
            textArea.transform.SetParent(parent, false);
            RectTransform rectTransform = textArea.AddComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = new Vector2(20, 10);
            rectTransform.offsetMax = new Vector2(-20, -10);

            TextMeshProUGUI textMeshPro = textArea.AddComponent<TextMeshProUGUI>();
            textMeshPro.fontSize = ModSettings.FontDefaultSize.Value;
            textMeshPro.color = ColorUtility.TryParseHtmlString(ModSettings.FontDefaultColor.Value, out var color)
                ? color
                : Color.red;
            textMeshPro.alignment = TextAlignmentOptions.Left;
            textMeshPro.enableWordWrapping = true;

            return textArea;
        }

        private static GameObject createPlaceholder(Transform parent)
        {
            GameObject placeholder = new GameObject("Placeholder");
            placeholder.transform.SetParent(parent, false);
            RectTransform rectTransform = placeholder.AddComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = new Vector2(20, 10);
            rectTransform.offsetMax = new Vector2(-20, -10);

            TextMeshProUGUI textMeshPro = placeholder.AddComponent<TextMeshProUGUI>();
            textMeshPro.text = "Label";
            textMeshPro.fontSize = ModSettings.FontDefaultSize.Value;
            textMeshPro.color = new Color(0.5f, 0.5f, 0.5f);
            textMeshPro.alignment = TextAlignmentOptions.Left;
            textMeshPro.enableWordWrapping = true;

            return placeholder;
        }

        private static GameObject createNumericPlaceholder(Transform parent)
        {
            GameObject placeholder = new GameObject("NumericPlaceholder");
            placeholder.transform.SetParent(parent, false);
            RectTransform rectTransform = placeholder.AddComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = new Vector2(20, 10);
            rectTransform.offsetMax = new Vector2(-20, -10);

            TextMeshProUGUI textMeshPro = placeholder.AddComponent<TextMeshProUGUI>();
            textMeshPro.text = "Size";
            textMeshPro.fontSize = ModSettings.FontDefaultSize.Value;
            textMeshPro.color = new Color(0.5f, 0.5f, 0.5f);
            textMeshPro.alignment = TextAlignmentOptions.Center;
            textMeshPro.enableWordWrapping = true;

            return placeholder;
        }
    }
}