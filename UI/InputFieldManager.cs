using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Il2CppTMPro;
using SimpleLabels.Data;
using SimpleLabels.Services;
using SimpleLabels.Settings;
using SimpleLabels.Utils;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Logger = SimpleLabels.Utils.Logger;
using Il2CppScheduleOne;
using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.UI;
using Il2CppScheduleOne.UI.Stations;

namespace SimpleLabels.UI
{
    public class InputFieldManager
    {
        internal static Dictionary<string, TMP_InputField> InputFields = new Dictionary<string, TMP_InputField>();
        internal static Dictionary<string, TMP_InputField> NumericInputFields = new Dictionary<string, TMP_InputField>();
        internal static Dictionary<string, GameObject> ContainersGameObjects = new Dictionary<string, GameObject>();
        internal static Dictionary<string, Button> ToggleOnOffButtons = new Dictionary<string, Button>();
        internal static Dictionary<string, TextMeshProUGUI> EntityIndicatorNames = new Dictionary<string, TextMeshProUGUI>();

        internal static Dictionary<string, Vector2> SupportedUITypes = new Dictionary<string, Vector2>()
        {
            { "StorageMenu", new Vector2(0.5f, 0.75f) },
            { "PackagingStation", new Vector2(0.5f, 0.75f) },
            { "ChemistryStation", new Vector2(0.5f, 0.75f) },
            { "LabOven", new Vector2(0.5f, 0.75f) },
            { "BrickPress", new Vector2(0.5f, 0.75f) },
            { "Cauldron", new Vector2(0.5f, 0.75f) },
            { "MixingStation", new Vector2(0.5f, 0.75f) },
            { "DryingRack", new Vector2(0.5f, 0.75f) },
            { "MushroomSpawnStation", new Vector2(0.5f, 0.75f) }
        };

        private static TMP_InputField _currentInputField;
        private static TMP_InputField _currentNumericInputField;

        public static void SetCurrentInputFields(TMP_InputField inputField, TMP_InputField numericInputField)
        {
            _currentInputField = inputField;
            _currentNumericInputField = numericInputField;
        }

        private static string _pendingItemColor;
        private static string _pendingItemText;

        private static bool _isApplyingItemFeedback;

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
    
            foreach (var entityName in EntityIndicatorNames.Values)
            {
                if (entityName == null) continue;
                GameObject.Destroy(entityName.gameObject);
            }
    
            EntityIndicatorNames.Clear();

            ColorPickerManager.Terminate();
            _currentInputField = null;
            _currentNumericInputField = null;
            _pendingItemColor = null;
            _pendingItemText = null;
            _isApplyingItemFeedback = false;
        }

        public static void ActivateInputField(string gameObjectName)
        {
            var container = FindByKey(ContainersGameObjects, gameObjectName);
            if (container != null)
                container.SetActive(true);

            var button = FindByKey(ToggleOnOffButtons, gameObjectName);
            if (button != null)
            {
                button.GetComponent<Image>().sprite = SpriteManager.LoadEmbeddedSprite("On.png", Vector4.zero);
                button.gameObject.SetActive(true);
            }
        }

        public static void DeactivateInputField(string gameObjectName)
        {
            var container = FindByKey(ContainersGameObjects, gameObjectName);
            if (container != null)
                container.SetActive(false);

            var button = FindByKey(ToggleOnOffButtons, gameObjectName);
            if (button != null)
            {
                button.GetComponent<Image>().sprite = SpriteManager.LoadEmbeddedSprite("Off.png", Vector4.zero);
                button.gameObject.SetActive(true);
            }
        }

        public static void DisableToggleOnOffButton(string gameObjectName)
        {
            var button = FindByKey(ToggleOnOffButtons, gameObjectName);
            if (button != null)
                button.gameObject.SetActive(false);
        }

        public static TMP_InputField GetInputField(string gameObjectName)
        {
            return FindByKey(InputFields, gameObjectName);
        }

        public static TMP_InputField GetNumericInputField(string gameObjectName)
        {
            return FindByKey(NumericInputFields, gameObjectName);
        }

        public static TextMeshProUGUI GetEntityNameIndicator(string gameObjectName)
        {
            return FindByKey(EntityIndicatorNames, gameObjectName);
        }

        private static T FindByKey<T>(Dictionary<string, T> dictionary, string key)
        {
            var entry = dictionary.FirstOrDefault(x => x.Key.Contains(key));
            return entry.Value;
        }

        private static void CreateInputFields()
        {
            foreach (var uiType in SupportedUITypes)
            {
                var ui = GetSupportedUI(uiType.Key);
                if (ui == null)
                {
                    Logger.Error($"Couldn't find the {uiType.Key} UI instance.");
                    continue;
                }

                // Create container GameObject
                GameObject containerGameObject = GUIManager.InitializeGUI(ui, uiType.Value, uiType.Key);
                ContainersGameObjects.Add(uiType.Key, containerGameObject);

                // Create On/Off Buttons
                Button toggleOnOffButton = GUIManager.CreateOnOffButton(ui, uiType.Key);
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

        // The 0.4.6 UI hierarchy no longer exposes the old UI/Stations/... paths.
        // Resolve the actual panel instances instead of relying on scene object names.
        private static GameObject GetSupportedUI(string key)
        {
            return key switch
            {
                "StorageMenu" => StorageMenu.Instance?.gameObject,
                "PackagingStation" => PackagingStationCanvas.Instance?.gameObject,
                "ChemistryStation" => ChemistryStationInterface.Instance?.gameObject,
                "LabOven" => LabOvenCanvas.Instance?.gameObject,
                "BrickPress" => BrickPressCanvas.Instance?.gameObject,
                "Cauldron" => CauldronInterface.Instance?.gameObject,
                "MixingStation" => MixingStationInterface.Instance?.gameObject,
                "DryingRack" => DryingRackInterface.Instance?.gameObject,
                "MushroomSpawnStation" => MushroomSpawnStationInterface.Instance?.gameObject,
                _ => null
            };
        }

        private static TMP_InputField CreateInputField(GameObject parent, string namePrefix)
        {
            try
            {
                namePrefix = ExtractNamePrefix(namePrefix);
                var inputObject = CreateInputFieldObject(parent, namePrefix + "_InputField", new Vector2(550, 40), new Vector2(-55, 10));
                
                var background = SetupInputFieldBackground(inputObject, ModSettings.LabelDefaultColor.Value);
                var textArea = CreateTextArea(inputObject.transform);
                var placeholder = CreatePlaceholder(textArea.transform);
                
                var inputField = ConfigureInputField(inputObject, textArea, placeholder, 30);
                SetupTextInputListeners(inputField);
                
                inputObject.SetActive(true);
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
                namePrefix = ExtractNamePrefix(namePrefix);
                var inputObject = CreateInputFieldObject(parent, namePrefix + "_NumericInputField", new Vector2(90, 40), new Vector2(280, 10));
                
                SetupInputFieldBackground(inputObject, "#FFFFFF");
                AddOutline(inputObject);
                var textArea = CreateTextArea(inputObject.transform);
                var placeholder = CreateNumericPlaceholder(inputObject.transform);
                
                var inputField = ConfigureInputField(inputObject, textArea, placeholder, 2);
                inputField.contentType = TMP_InputField.ContentType.IntegerNumber;
                SetupNumericInputListeners(inputField);
                
                inputObject.SetActive(true);
                return inputField;
            }
            catch (Exception e)
            {
                Logger.Error($"Failed to create numeric input field: {e.Message}");
                return null;
            }
        }

        private static string ExtractNamePrefix(string namePrefix)
        {
            return namePrefix.Substring(namePrefix.LastIndexOf('/') + 1);
        }

        private static GameObject CreateInputFieldObject(GameObject parent, string objectName, Vector2 sizeDelta, Vector2 position)
        {
            var inputObject = new GameObject(objectName);
            inputObject.layer = 5; // UI Layer
            inputObject.transform.SetParent(parent.transform, false);
            
            var rectTransform = inputObject.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = sizeDelta;
            rectTransform.anchoredPosition = position;
            
            return inputObject;
        }

        private static Image SetupInputFieldBackground(GameObject inputObject, string colorHex)
        {
            var background = inputObject.AddComponent<Image>();
            background.type = Image.Type.Sliced;
            background.sprite = SpriteManager.LoadEmbeddedSprite("UISmallSprite.png", new Vector4(5, 5, 5, 5));
            background.color = ColorUtility.TryParseHtmlString(colorHex, out var color) ? color : Color.red;
            return background;
        }

        private static void AddOutline(GameObject inputObject)
        {
            var outline = inputObject.AddComponent<Outline>();
            outline.effectColor = new Color(0, 0, 0, 0.15f);
            outline.effectDistance = new Vector2(1, 1);
        }

        private static TMP_InputField ConfigureInputField(GameObject inputObject, GameObject textArea, GameObject placeholder, int characterLimit)
        {
            var inputField = inputObject.AddComponent<TMP_InputField>();
            inputField.textViewport = textArea.GetComponent<RectTransform>();
            inputField.textComponent = textArea.GetComponent<TextMeshProUGUI>();
            inputField.placeholder = placeholder.GetComponent<TextMeshProUGUI>();
            inputField.characterLimit = characterLimit;
            return inputField;
        }

        private static void SetupTextInputListeners(TMP_InputField inputField)
        {
            inputField.onValueChanged.AddListener((UnityAction<string>)((string text) =>
            {
                if (DevUtils.IsStorageOrStationOpen())
                    OnInputTextChangeVisualFeedback(text, inputField);
            }));
            
            inputField.onSubmit.AddListener((UnityAction<string>)((string text) =>
            {
                if (DevUtils.IsStorageOrStationOpen())
                    OnInputTextSubmit(text, inputField);
            }));
        }

        private static void SetupNumericInputListeners(TMP_InputField inputField)
        {
            inputField.onValueChanged.AddListener((UnityAction<string>)((string text) =>
            {
                ValidateNumericRange(text, inputField);
            }));

            inputField.onSubmit.AddListener((UnityAction<string>)((string text) =>
            {
                ValidateNumericRange(text, inputField);
                OnNumericInputTextChange(text, inputField);
            }));
        }

        private static void OnInputTextChangeVisualFeedback(string text, TMP_InputField inputField)
        {
            if (_isApplyingItemFeedback)
                return;

            if (!DevUtils.IsStorageOrStationOpen())
            {
                inputField.DeactivateInputField();
                _currentInputField = null;  
                return;
            }

            string textInBrackets = GetFirstTextInCurlyBrackets(text);
            if (string.IsNullOrEmpty(textInBrackets))
            {
                _pendingItemColor = null;
                _pendingItemText = null;
                return;
            }

            if (Registry.ItemExists(textInBrackets))
            {
                ItemDefinition itemDefinition = Registry.GetItem(textInBrackets);
                Color spriteColor = SpriteManager.GetAverageColor(itemDefinition.Icon);
                
                string cleanedText = RemoveCurlyBracketsContent(text);

                if (String.IsNullOrEmpty(cleanedText))
                {
                    cleanedText = itemDefinition.Name;
                }

                _isApplyingItemFeedback = true;
                inputField.text = cleanedText;
                _isApplyingItemFeedback = false;

                inputField.GetComponent<Image>().color = spriteColor;

                _pendingItemColor = "#" + ColorUtility.ToHtmlStringRGB(spriteColor);
                _pendingItemText = cleanedText;
            }
            else
            {
                _pendingItemColor = null;
                _pendingItemText = null;
            }
        }

        private static void OnInputTextSubmit(string text, TMP_InputField inputField)
        {
            // Reset input field state
            if (!DevUtils.IsStorageOrStationOpen())
            {
                inputField.DeactivateInputField();
                _currentInputField = null;
                _pendingItemColor = null;
                _pendingItemText = null;
                return;
            }

            string entityGuid = LabelTracker.GetCurrentlyManagedEntityGuid();
            if (string.IsNullOrEmpty(entityGuid))
            {
                _pendingItemColor = null;
                _pendingItemText = null;
                return;
            }

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

                inputField.text = finalText;
                inputField.GetComponent<Image>().color = spriteColor;
            }
            else if (_pendingItemColor != null && _pendingItemText != null && _pendingItemText == finalText)
            {
                finalColor = _pendingItemColor;
            }

            _pendingItemColor = null;
            _pendingItemText = null;

            LabelService.UpdateLabel(entityGuid, text: finalText, color: finalColor);
        }

        private static void ValidateNumericRange(string text, TMP_InputField inputField)
        {
            if (string.IsNullOrEmpty(text))
                return;

            if (int.TryParse(text, out int value))
            {
                int clampedValue = Mathf.Clamp(value, 1, 30);
                if (value != clampedValue)
                {
                    inputField.text = clampedValue.ToString();
                }
            }
            else
            {
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
            LabelService.UpdateLabel(guid, size: value);
        }

        private static GameObject CreateTextArea(Transform parent)
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

        private static GameObject CreatePlaceholder(Transform parent)
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

        private static GameObject CreateNumericPlaceholder(Transform parent)
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
            // Extracts {itemId} from text
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
