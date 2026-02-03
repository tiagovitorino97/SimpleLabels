using System.Collections.Generic;
using Il2CppTMPro;
using SimpleLabels.Settings;
using SimpleLabels.Utils;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace SimpleLabels.UI
{
    /// <summary>
    /// Builds the label input GUI: container, entity name text, indicator labels, and structure for input fields.
    /// </summary>
    /// <remarks>
    /// InitializeGUI creates a container at the given anchor, adds "Entity Name", "Name:", "Size:", "Label Color:",
    /// "Font Color:" indicators, and returns the container. InputFieldManager attaches actual input fields to
    /// this structure. Used by CreateInputFields when setting up per–station-type UI.
    /// </remarks>
    public class GUIManager
    {
        public static Image ToggleButtonBackground;
        
        /// <summary>
        /// Creates the label GUI container and indicator texts for the given parent and name prefix.
        /// </summary>
        /// <remarks>
        /// Extracts name prefix from path (after last '/'). Container uses UIBigSprite, 700x200. Registers
        /// entity name text in InputFieldManager.EntityInicatorNames for the prefix.
        /// </remarks>
        public static GameObject InitializeGUI(GameObject parent, Vector2 anchorPosition, string namePrefix)
        {
            namePrefix = ExtractNamePrefix(namePrefix);
            var container = CreateContainer(parent, anchorPosition, namePrefix);
            
            CreateEntityNameText(container, namePrefix);
            CreateIndicatorText(container, "IndicatorNameText", "Name:", new Vector2(-275, 45));
            CreateIndicatorText(container, "IndicatorSizeText", "Size:", new Vector2(288, 45));
            CreateIndicatorText(container, "IndicatorFontColorText", "Label Color:", new Vector2(-275, -35));
            CreateIndicatorText(container, "IndicatorLabelColorText", "Font Color:", new Vector2(80, -35));

            return container;
        }

        private static string ExtractNamePrefix(string namePrefix)
        {
            return namePrefix.Substring(namePrefix.LastIndexOf('/') + 1);
        }

        private static GameObject CreateContainer(GameObject parent, Vector2 anchorPosition, string namePrefix)
        {
            var container = new GameObject(namePrefix + "_Container");
            container.layer = 5; // UI Layer
            container.transform.SetParent(parent.transform, false);
            
            var rectTransform = container.AddComponent<RectTransform>();
            rectTransform.anchorMin = anchorPosition;
            rectTransform.anchorMax = anchorPosition;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = new Vector2(700, 200);
            
            var background = container.AddComponent<Image>();
            background.type = Image.Type.Sliced;
            background.sprite = SpriteManager.LoadEmbeddedSprite("UIBigSprite.png", new Vector4(20, 20, 20, 20));
            background.color = new Color(0.39215687f, 0.39215687f, 0.39215687f, 0.39215687f);
            
            return container;
        }

        private static void CreateEntityNameText(GameObject container, string namePrefix)
        {
            var textObject = new GameObject("EntityNameText");
            textObject.layer = 5; // UI Layer
            textObject.transform.SetParent(container.transform, false);
            
            var text = textObject.AddComponent<TextMeshProUGUI>();
            text.text = "Entity Name";
            text.color = Color.white;
            text.fontSize = 22;
            text.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
            text.alignment = TextAlignmentOptions.Center;
            text.enableWordWrapping = false;
            text.overflowMode = TextOverflowModes.Overflow;
            text.margin = new Vector4(15, 10, 0, 0);
            
            InputFieldManager.EntityInicatorNames.Add(namePrefix, text);
            
            var rectTransform = textObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = new Vector2(0, 75);
            rectTransform.sizeDelta = new Vector2(700, 50);
        }

        private static void CreateIndicatorText(GameObject container, string objectName, string text, Vector2 position)
        {
            var textObject = new GameObject(objectName);
            textObject.layer = 5; // UI Layer
            textObject.transform.SetParent(container.transform, false);
            
            var textComponent = textObject.AddComponent<TextMeshProUGUI>();
            textComponent.text = text;
            textComponent.color = Color.white;
            textComponent.fontSize = 16;
            textComponent.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
            textComponent.alignment = TextAlignmentOptions.Left;
            textComponent.enableWordWrapping = false;
            textComponent.overflowMode = TextOverflowModes.Overflow;
            
            var rectTransform = textObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = position;
            rectTransform.sizeDelta = new Vector2(100, 25);
        }

        public static Button CreateOnOffButton(GameObject parent, string namePrefix)
        {
            namePrefix = ExtractNamePrefix(namePrefix);
            var buttonObject = CreateToggleButtonObject(parent, namePrefix);
            var button = SetupToggleButton(buttonObject, namePrefix);
            
            return button;
        }

        private static GameObject CreateToggleButtonObject(GameObject parent, string namePrefix)
        {
            var buttonObject = new GameObject(namePrefix + "_ToggleButton");
            buttonObject.layer = 5; // UI Layer
            buttonObject.transform.SetParent(parent.transform, false);
            
            var rectTransform = buttonObject.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0, 0);
            rectTransform.anchorMax = new Vector2(0, 0);
            rectTransform.pivot = new Vector2(0, 0);
            rectTransform.anchoredPosition = new Vector2(10, 10);
            rectTransform.sizeDelta = new Vector2(50, 46.2f);
            
            ToggleButtonBackground = buttonObject.AddComponent<Image>();
            ToggleButtonBackground.color = new Color(1, 1, 1, 0.9f);
            ToggleButtonBackground.sprite = SpriteManager.LoadEmbeddedSprite("On.png", Vector4.zero);
            
            return buttonObject;
        }

        private static Button SetupToggleButton(GameObject buttonObject, string namePrefix)
        {
            var button = buttonObject.AddComponent<Button>();
            button.image = ToggleButtonBackground;
            
            var isOn = true;
            button.onClick.AddListener((UnityAction)(() =>
            {
                isOn = !isOn;
                if (isOn)
                {
                    InputFieldManager.ActivateInputField(namePrefix);
                    ModSettings.ShowInput.Value = true;
                    ToggleButtonBackground.sprite = SpriteManager.LoadEmbeddedSprite("On.png", Vector4.zero);
                }
                else
                {
                    InputFieldManager.DeactivateInputField(namePrefix);
                    ModSettings.ShowInput.Value = false;
                    ToggleButtonBackground.sprite = SpriteManager.LoadEmbeddedSprite("Off.png", Vector4.zero);
                }
            }));
            
            return button;
        }
    }
}