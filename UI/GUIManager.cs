using System.Collections.Generic;
using Il2CppTMPro;
using SimpleLabels.Settings;
using SimpleLabels.Utils;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace SimpleLabels.UI
{
    public class GUIManager
    {
        private static bool _isOn;
        public static Image toggleButtonBackGround;
        public static GameObject InitializeGUI(GameObject parent, Vector2 anchorPosition, string namePrefix)
        {
            namePrefix = namePrefix.Substring(namePrefix.LastIndexOf('/') + 1);
            //Create Container GameObject
            var containerGameObject = new GameObject(namePrefix + "_Container");
            containerGameObject.layer = 5; //UI Layer
            containerGameObject.transform.SetParent(parent.transform, false);
            
            var containeRectTransform = containerGameObject.AddComponent<RectTransform>();
            containeRectTransform.anchorMin = anchorPosition;
            containeRectTransform.anchorMax = anchorPosition;
            containeRectTransform.pivot = new Vector2(0.5f, 0.5f);
            containeRectTransform.sizeDelta = new Vector2(700, 200);
            
            var containerBackGround = containerGameObject.AddComponent<Image>();
            containerBackGround.type = Image.Type.Sliced;
            containerBackGround.sprite = SpriteManager.LoadEmbeddedSprite("UIBigSprite.png", new Vector4(20, 20, 20, 20));
            containerBackGround.color = new Color(0.39215687f, 0.39215687f, 0.39215687f, 0.39215687f);
            
            //Create EntityName TextMeshProUGUI
            var entityNameTextGameObject = new GameObject("EntityNameText");
            entityNameTextGameObject.layer = 5; //UI Layer
            entityNameTextGameObject.transform.SetParent(containerGameObject.transform, false);
            
            var entityNameText = entityNameTextGameObject.AddComponent<TextMeshProUGUI>();
            entityNameText.text = "Entity Name";
            entityNameText.color = Color.white;
            entityNameText.fontSize = 22;
            entityNameText.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
            entityNameText.alignment = TextAlignmentOptions.Center;
            entityNameText.enableWordWrapping = false;
            entityNameText.overflowMode = TextOverflowModes.Overflow;
            entityNameText.margin = new Vector4(15, 10, 0,0);
            
            InputFieldManager.EntityInicatorNames.Add(namePrefix, entityNameText);
            
            var entityNameRectTransform = entityNameTextGameObject.GetComponent<RectTransform>();
            entityNameRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            entityNameRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            entityNameRectTransform.pivot = new Vector2(0.5f, 0.5f);
            entityNameRectTransform.anchoredPosition = new Vector2(0, 75);
            entityNameRectTransform.sizeDelta = new Vector2(700, 50);
            
            //Create Indicator Name TextMeshProUGUI
            var indicatorNameTextGameObject = new GameObject("IndicatorNameText");
            indicatorNameTextGameObject.layer = 5; //UI Layer
            indicatorNameTextGameObject.transform.SetParent(containerGameObject.transform, false);
            
            var indicatorNameText = indicatorNameTextGameObject.AddComponent<TextMeshProUGUI>();
            indicatorNameText.text = "Name:";
            indicatorNameText.color = Color.white;
            indicatorNameText.fontSize = 16;
            indicatorNameText.fontStyle = FontStyles.Bold | FontStyles.UpperCase ;
            indicatorNameText.alignment = TextAlignmentOptions.Left;
            indicatorNameText.enableWordWrapping = false;
            indicatorNameText.overflowMode = TextOverflowModes.Overflow;
            
            var indicatorNameRectTransform = indicatorNameTextGameObject.GetComponent<RectTransform>();
            indicatorNameRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            indicatorNameRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            indicatorNameRectTransform.pivot = new Vector2(0.5f, 0.5f);
            indicatorNameRectTransform.anchoredPosition = new Vector2(-275, 45);
            indicatorNameRectTransform.sizeDelta = new Vector2(100, 25);
            
            //Create Indicator Size TextMeshProUGUI
            var indicatorSizeTextGameObject = new GameObject("IndicatorSizeText");
            indicatorSizeTextGameObject.layer = 5; //UI Layer
            indicatorSizeTextGameObject.transform.SetParent(containerGameObject.transform, false);
            
            var indicatorSizeText = indicatorSizeTextGameObject.AddComponent<TextMeshProUGUI>();
            indicatorSizeText.text = "Size:";
            indicatorSizeText.color = Color.white;
            indicatorSizeText.fontSize = 16;
            indicatorSizeText.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
            indicatorSizeText.alignment = TextAlignmentOptions.Left;
            indicatorSizeText.enableWordWrapping = false;
            indicatorSizeText.overflowMode = TextOverflowModes.Overflow;
            
            var indicatorSizeRectTransform = indicatorSizeTextGameObject.GetComponent<RectTransform>();
            indicatorSizeRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            indicatorSizeRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            indicatorSizeRectTransform.pivot = new Vector2(0.5f, 0.5f);
            indicatorSizeRectTransform.anchoredPosition = new Vector2(288, 45);
            indicatorSizeRectTransform.sizeDelta = new Vector2(100, 25);
            
            //Create Indicator Font Color TextMeshProUGUI
            var indicatorFontColorTextGameObject = new GameObject("IndicatorFontColorText");
            indicatorFontColorTextGameObject.layer = 5; //UI Layer
            indicatorFontColorTextGameObject.transform.SetParent(containerGameObject.transform, false);
            
            var indicatorFontColorText = indicatorFontColorTextGameObject.AddComponent<TextMeshProUGUI>();
            indicatorFontColorText.text = "Label Color:";
            indicatorFontColorText.color = Color.white;
            indicatorFontColorText.fontSize = 16;
            indicatorFontColorText.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
            indicatorFontColorText.alignment = TextAlignmentOptions.Left;
            indicatorFontColorText.enableWordWrapping = false;
            indicatorFontColorText.overflowMode = TextOverflowModes.Overflow;
            
            var indicatorFontColorRectTransform = indicatorFontColorTextGameObject.GetComponent<RectTransform>();
            indicatorFontColorRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            indicatorFontColorRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            indicatorFontColorRectTransform.pivot = new Vector2(0.5f, 0.5f);
            indicatorFontColorRectTransform.anchoredPosition = new Vector2(-275, -35);
            indicatorFontColorRectTransform.sizeDelta = new Vector2(100, 25);
            
            //Create Indicator Label Color TextMeshProUGUI
            var indicatorLabelColorTextGameObject = new GameObject("IndicatorLabelColorText");
            indicatorLabelColorTextGameObject.layer = 5; //UI Layer
            indicatorLabelColorTextGameObject.transform.SetParent(containerGameObject.transform, false);
            
            var indicatorLabelColorText = indicatorLabelColorTextGameObject.AddComponent<TextMeshProUGUI>();
            indicatorLabelColorText.text = "Font Color:";
            indicatorLabelColorText.color = Color.white;
            indicatorLabelColorText.fontSize = 16;
            indicatorLabelColorText.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
            indicatorLabelColorText.alignment = TextAlignmentOptions.Left;
            indicatorLabelColorText.enableWordWrapping = false;
            indicatorLabelColorText.overflowMode = TextOverflowModes.Overflow;
            
            var indicatorLabelColorRectTransform = indicatorLabelColorTextGameObject.GetComponent<RectTransform>();
            indicatorLabelColorRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            indicatorLabelColorRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            indicatorLabelColorRectTransform.pivot = new Vector2(0.5f, 0.5f);
            indicatorLabelColorRectTransform.anchoredPosition = new Vector2(80, -35);
            indicatorLabelColorRectTransform.sizeDelta = new Vector2(100, 25);

            return containerGameObject;
        }

        public static Button createOnOffButton(GameObject parent, string namePrefix)
        {
            namePrefix = namePrefix.Substring(namePrefix.LastIndexOf('/') + 1);
            //Create Container GameObject
            var toggleButtonGameObject = new GameObject(namePrefix + "_ToggleButton");
            toggleButtonGameObject.layer = 5; //UI Layer
            toggleButtonGameObject.transform.SetParent(parent.transform, false);
            
            var toggleButtonRectTransform = toggleButtonGameObject.AddComponent<RectTransform>();
            toggleButtonRectTransform.anchorMin = new Vector2(0, 0);
            toggleButtonRectTransform.anchorMax = new Vector2(0, 0);
            toggleButtonRectTransform.pivot = new Vector2(0, 0);
            toggleButtonRectTransform.anchoredPosition = new Vector2(10, 10);
            toggleButtonRectTransform.sizeDelta = new Vector2(50, 46.2f);
            
            
           toggleButtonBackGround = toggleButtonGameObject.AddComponent<Image>();
           toggleButtonBackGround.color = new Color(1, 1, 1, 0.9f);
            toggleButtonBackGround.sprite = SpriteManager.LoadEmbeddedSprite("On.png", Vector4.zero);
            _isOn = true;
            
            var toggleButton = toggleButtonGameObject.AddComponent<Button>();
            toggleButton.image = toggleButtonBackGround;
            var keepNamePrefix = namePrefix;
            toggleButton.onClick.AddListener((UnityAction)(() =>
            {
                _isOn = !_isOn;
                if (_isOn)
                {
                    
                    InputFieldManager.ActivateInputField(keepNamePrefix);
                    ModSettings.ShowInput.Value = true;
                }
                else
                {
                    InputFieldManager.DeactivateInputField(keepNamePrefix);
                    ModSettings.ShowInput.Value = false;
                }
                
            }));
            
            return toggleButton;


        }
    }
}