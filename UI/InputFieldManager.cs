using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Il2CppTMPro;
using SimpleLabels.Data;
using SimpleLabels.Settings;
using SimpleLabels.Utils;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Logger = SimpleLabels.Utils.Logger;
using Il2CppScheduleOne;
using Il2CppScheduleOne.ItemFramework;

// ReSharper disable All


namespace SimpleLabels.UI
{
    public class InputFieldManager
    {
        public static Dictionary<string, TMP_InputField> InputFields = new Dictionary<string, TMP_InputField>();
        public static Dictionary<string, TMP_InputField> NumericInputFields = new Dictionary<string, TMP_InputField>();
        public static Dictionary<string, GameObject> ContainersGameObjects = new Dictionary<string, GameObject>();
        public static Dictionary<string, Button> ToggleOnOffButtons = new Dictionary<string, Button>();
        public static Dictionary<string, TextMeshProUGUI> EntityInicatorNames = new Dictionary<string, TextMeshProUGUI>();

        public static Dictionary<string, Vector2> SupportedUITypes = new Dictionary<string, Vector2>()
        {
            { "UI/StorageMenu", new Vector2(0.5f, 0.75f) },
            { "UI/Stations/PackagingStation", new Vector2(0.5f, 0.75f) },
            { "UI/Stations/ChemistryStation", new Vector2(0.5f, 0.75f) },
            { "UI/Stations/LabOven", new Vector2(0.5f, 0.75f) },
            { "UI/Stations/BrickPress", new Vector2(0.5f, 0.75f) },
            { "UI/Stations/Cauldron", new Vector2(0.5f, 0.75f) },
            { "UI/Stations/MixingStation", new Vector2(0.5f, 0.75f) },
            { "UI/Stations/DryingRack", new Vector2(0.5f, 0.75f) },
            { "UI/Stations/MushroomSpawnStation", new Vector2(0.5f, 0.75f) }
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

            foreach (var container in ContainersGameObjects.Values)
            {
                if (container == null) continue;
                GameObject.Destroy(container);
            }

            ContainersGameObjects.Clear();

            foreach (var toggle in ToggleOnOffButtons.Values)
            {
                if (toggle == null) continue;
                GameObject.Destroy(toggle.gameObject);
            }

            ToggleOnOffButtons.Clear();
    
            foreach (var entityName in EntityInicatorNames.Values)
            {
                if (entityName == null) continue;
                GameObject.Destroy(entityName.gameObject);
            }
    
            EntityInicatorNames.Clear();

            ColorPickerManager.Terminate();
            _currentInputField = null;
            _currentNumericInputField = null;
        }

        public static void ActivateInputField(string gameObjectName)
        {
            ContainersGameObjects.First(x => x.Key.Contains(gameObjectName)).Value.gameObject
                .SetActive(true);

            ToggleOnOffButtons.First(x => x.Key.Contains(gameObjectName)).Value.GetComponent<Image>().sprite =
                SpriteManager.LoadEmbeddedSprite("On.png", Vector4.zero);
            ToggleOnOffButtons.First(x => x.Key.Contains(gameObjectName)).Value.gameObject.SetActive(true);
        }

        public static void DeactivateInputField(string gameObjectName)
        {
            ContainersGameObjects.First(x => x.Key.Contains(gameObjectName)).Value.gameObject
                .SetActive(false);

            ToggleOnOffButtons.First(x => x.Key.Contains(gameObjectName)).Value.GetComponent<Image>().sprite =
                SpriteManager.LoadEmbeddedSprite("Off.png", Vector4.zero);
            ToggleOnOffButtons.First(x => x.Key.Contains(gameObjectName)).Value.gameObject.SetActive(true);
        }

        public static void DisableToggleOnOffButton(string gameObjectName)
        {
            ToggleOnOffButtons.First(x => x.Key.Contains(gameObjectName)).Value.gameObject.SetActive(false);
        }

        public static TMP_InputField GetInputField(string gameObjectName)
        {
            return InputFields.First(x => x.Key.Contains(gameObjectName)).Value;
        }

        public static TMP_InputField GetNumericInputField(string gameObjectName)
        {
            return NumericInputFields.First(x => x.Key.Contains(gameObjectName)).Value;
        }

        public static TextMeshProUGUI GetEntityNameIndicator(string gameObjectName)
        {
            return EntityInicatorNames.First(x => x.Key.Contains(gameObjectName)).Value;
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

                // Create container GameObject
                GameObject containerGameObject = GUIManager.InitializeGUI(ui, uiType.Value, uiType.Key);
                ContainersGameObjects.Add(uiType.Key, containerGameObject);

                // Create On/Off Buttons
                Button toggleOnOffButton = GUIManager.createOnOffButton(ui, uiType.Key);
                ToggleOnOffButtons.Add(uiType.Key, toggleOnOffButton);

                // Create main input field
                TMP_InputField inputField = CreateInputField(containerGameObject, uiType.Key);
                InputFields.Add(uiType.Key, inputField);

                // Create numeric input field
                TMP_InputField numericInputField = CreateNumericInputField(containerGameObject, uiType.Key);
                NumericInputFields.Add(uiType.Key, numericInputField);

                ColorPickerManager.CreateColorPicker(InputFields[uiType.Key], ColorPickerType.Label);
                ColorPickerManager.CreateColorPicker(InputFields[uiType.Key], ColorPickerType.Font);
            }
        }

        private static TMP_InputField CreateInputField(GameObject parent, string namePrefix)
        {
            try
            {
                namePrefix = namePrefix.Substring(namePrefix.LastIndexOf('/') + 1); // Remove the "UI/" prefix

                //Create GameObject
                GameObject inputFieldGameObject = new GameObject(namePrefix + "_InputField");
                inputFieldGameObject.layer = 5; //UI Layer
                inputFieldGameObject.transform.SetParent(parent.transform, false);

                // Set up RectTransform
                RectTransform rectTransform = inputFieldGameObject.AddComponent<RectTransform>();
                rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                rectTransform.pivot = new Vector2(0.5f, 0.5f);
                rectTransform.sizeDelta = new Vector2(550, 40);
                rectTransform.anchoredPosition = new Vector2(-55, 10);

                //Add visual Components
                Image backGround = inputFieldGameObject.AddComponent<Image>();
                backGround.type = Image.Type.Sliced;
                backGround.sprite = SpriteManager.LoadEmbeddedSprite("UISmallSprite.png", new Vector4(5, 5, 5, 5));
                backGround.color = ColorUtility.TryParseHtmlString(ModSettings.LabelDefaultColor.Value, out var color)
                    ? color
                    : Color.red;

                //Create text area
                GameObject textArea = createTextArea(inputFieldGameObject.transform);
                GameObject placeholder = createPlaceholder(textArea.transform);

                //Configure the input field component
                TMP_InputField inputField = inputFieldGameObject.AddComponent<TMP_InputField>();
                inputField.textViewport = textArea.GetComponent<RectTransform>();
                inputField.textComponent = textArea.GetComponent<TextMeshProUGUI>();
                inputField.placeholder = placeholder.GetComponent<TextMeshProUGUI>();
                inputField.characterLimit = 30;
                
                // onValueChanged: Only for real-time visual feedback (curly bracket processing)
                inputField.onValueChanged.AddListener((UnityAction<string>)((string text) =>
                {
                    if(DevUtils.IsStorageOrStationOpen()) OnInputTextChangeVisualFeedback(text, inputField);
                }));
                
                // onSubmit: Only update label when user presses Enter
                inputField.onSubmit.AddListener((UnityAction<string>)((string text) =>
                {
                    if(DevUtils.IsStorageOrStationOpen()) OnInputTextSubmit(text, inputField);
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

        private static TMP_InputField CreateNumericInputField(GameObject parent, string namePrefix)
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
                rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                rectTransform.pivot = new Vector2(0.5f, 0.5f);
                rectTransform.sizeDelta = new Vector2(90, 40); // Smaller width for numeric input
                rectTransform.anchoredPosition = new Vector2(280, 10);

                //Add visual Components
                Image backGround = inputFieldGameObject.AddComponent<Image>();
                backGround.type = Image.Type.Sliced;
                backGround.sprite = SpriteManager.LoadEmbeddedSprite("UISmallSprite.png", new Vector4(5, 5, 5, 5));
                backGround.color = Color.white;
                Outline outline = inputFieldGameObject.AddComponent<Outline>();
                outline.effectColor = new Color(0, 0, 0, 0.15f);
                outline.effectDistance = new Vector2(1, 1);

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

        // Called on every keystroke for real-time visual feedback (curly bracket processing)
        private static void OnInputTextChangeVisualFeedback(string text, TMP_InputField inputField)
        {
            // Reset input field state
            if (!DevUtils.IsStorageOrStationOpen())
            {
                inputField.DeactivateInputField();
                _currentInputField = null;  
                return;
            }

            // Process text in curly brackets for special formatting (visual feedback only)
            string textInBrackets = GetFirstTextInCurlyBrackets(text);
            if (string.IsNullOrEmpty(textInBrackets))
                return;

            if (Registry.ItemExists(textInBrackets))
            {
                ItemDefinition itemDefinition = Registry.GetItem(textInBrackets);
                Color spriteColor = SpriteManager.GetAverageColor(itemDefinition.Icon);
                
                string cleanedText = RemoveCurlyBracketsContent(text);

                if (String.IsNullOrEmpty(cleanedText))
                {
                    cleanedText = itemDefinition.Name;
                }

                // Update text in input field (visual feedback only, no label update)
                inputField.text = cleanedText;

                // Apply color to the input field (visual feedback only)
                inputField.GetComponent<Image>().color = spriteColor;
            }
        }

        // Called only when user presses Enter, this is where we update the label
        private static void OnInputTextSubmit(string text, TMP_InputField inputField)
        {
            // Reset input field state
            if (!DevUtils.IsStorageOrStationOpen())
            {
                inputField.DeactivateInputField();
                _currentInputField = null;  
                return;
            }

            // Get the entity GUID
            string entityGuid = LabelTracker.GetCurrentlyManagedEntityGuid();
            if (string.IsNullOrEmpty(entityGuid))
                return;

            // Process text in curly brackets for special formatting
            string textInBrackets = GetFirstTextInCurlyBrackets(text);
            string finalText = text;
            string finalColor = null;

            if (!string.IsNullOrEmpty(textInBrackets) && Registry.ItemExists(textInBrackets))
            {
                ItemDefinition itemDefinition = Registry.GetItem(textInBrackets);
                Color spriteColor = SpriteManager.GetAverageColor(itemDefinition.Icon);
                finalColor = "#" + ColorUtility.ToHtmlStringRGB(spriteColor);
                
                string cleanedText = RemoveCurlyBracketsContent(text);

                if (String.IsNullOrEmpty(cleanedText))
                {
                    finalText = itemDefinition.Name;
                }
                else
                {
                    finalText = cleanedText;
                }

                // Update text in input field to show the cleaned version
                inputField.text = finalText;

                // Apply color to the input field
                inputField.GetComponent<Image>().color = spriteColor;
            }

            Logger.Msg($"[InputField] User submitted label change: GUID={entityGuid}, Text='{finalText}'");
            
            LabelTracker.UpdateLabel(
                guid: entityGuid, 
                newLabelText: finalText,
                newLabelColor: finalColor
            );
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

            var guid = LabelTracker.GetCurrentlyManagedEntityGuid();
            Logger.Msg($"[InputField] User changed label size: GUID={guid}, Size={value}");
            
            LabelTracker.UpdateLabel(guid: guid, newLabelSize: value);
        }

        private static GameObject createTextArea(Transform parent)
        {
            GameObject textArea = new GameObject("TextArea");
            textArea.transform.SetParent(parent, false);
            RectTransform rectTransform = textArea.AddComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.offsetMin = new Vector2(10, 0);
            rectTransform.offsetMax = new Vector2(-10, 0);

            TextMeshProUGUI textMeshPro = textArea.AddComponent<TextMeshProUGUI>();
            textMeshPro.fontSize = ModSettings.DEFAULT_FONT_SIZE;
            textMeshPro.color = ColorUtility.TryParseHtmlString(ModSettings.FontDefaultColor.Value, out var color)
                ? color
                : Color.red;
            textMeshPro.alignment = TextAlignmentOptions.Left;
            textMeshPro.enableWordWrapping = false;
            textMeshPro.fontStyle = FontStyles.Bold;

            return textArea;
        }

        private static GameObject createPlaceholder(Transform parent)
        {
            GameObject placeholder = new GameObject("Placeholder");
            placeholder.transform.SetParent(parent, false);
            RectTransform rectTransform = placeholder.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.offsetMax = new Vector2(0, 0);
            rectTransform.offsetMin = new Vector2(-265, 0);


            TextMeshProUGUI textMeshPro = placeholder.AddComponent<TextMeshProUGUI>();
            textMeshPro.text = "Press Enter to confirm changes...";
            textMeshPro.fontSize = ModSettings.DEFAULT_FONT_SIZE;
            textMeshPro.color = new Color(0.5f, 0.5f, 0.5f);
            textMeshPro.alignment = TextAlignmentOptions.Left;
            textMeshPro.enableWordWrapping = false;

            return placeholder;
        }

        private static GameObject createNumericPlaceholder(Transform parent)
        {
            GameObject placeholder = new GameObject("NumericPlaceholder");
            placeholder.transform.SetParent(parent, false);
            RectTransform rectTransform = placeholder.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.offsetMax = new Vector2(75, 25);
            rectTransform.offsetMin = new Vector2(-100, -25);


            TextMeshProUGUI textMeshPro = placeholder.AddComponent<TextMeshProUGUI>();
            textMeshPro.text = "Size";
            textMeshPro.fontSize = ModSettings.DEFAULT_FONT_SIZE;
            textMeshPro.color = new Color(0.5f, 0.5f, 0.5f);
            textMeshPro.alignment = TextAlignmentOptions.Center;
            textMeshPro.enableWordWrapping = true;

            return placeholder;
        }

        public static string GetFirstTextInCurlyBrackets(string text)
        {
            // The regex pattern \{([^}]*)\} looks for:
            // \{       - a literal opening curly bracket
            // ([^}]*) - a capturing group that matches any character except a closing curly bracket, zero or more times
            // \}       - a literal closing curly bracket
            // This pattern specifically avoids matching across nested curly brackets
            Match match = Regex.Match(text, @"\{([^}]*)\}");

            if (match.Success)
            {
                return match.Groups[1].Value;
            }

            return null;
        }

        private static string RemoveCurlyBracketsContent(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            return Regex.Replace(input, "{[^{}]*}", "").Trim();
        }
    }
}