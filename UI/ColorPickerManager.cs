using System;
using System.Collections.Generic;
using System.Linq;
using Il2CppTMPro;
using SimpleLabels.Data;
using SimpleLabels.Settings;
using SimpleLabels.Utils;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Logger = SimpleLabels.Utils.Logger;

namespace SimpleLabels.UI
{
    public enum ColorPickerType
    {
        Label,
        Font
    }

    public static class ColorPickerManager
    {
        public static Dictionary<TMP_InputField, GameObject> LabelColorPickers =
            new Dictionary<TMP_InputField, GameObject>();

        public static Dictionary<TMP_InputField, GameObject> FontColorPickers =
            new Dictionary<TMP_InputField, GameObject>();

        public static void CreateColorPicker(TMP_InputField inputField, ColorPickerType type)
        {
            try
            {
                var pickerName = type == ColorPickerType.Label ? "LabelColorPicker" : "FontColorPicker";
                var colorPickers = type == ColorPickerType.Label ? LabelColorPickers : FontColorPickers;
                var colorOptions = type == ColorPickerType.Label
                    ? ModSettings.LabelColorOptionsDictionary
                    : ModSettings.FontColorOptionsDictionary;

                var colorPicker = new GameObject(pickerName);
                colorPicker.transform.SetParent(inputField.transform, false);
                colorPicker.layer = 5; //UI Layer

                var rectTransform = colorPicker.AddComponent<RectTransform>();
                rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                rectTransform.pivot = new Vector2(0.5f, 0.5f);
                rectTransform.anchoredPosition = new Vector2(type == ColorPickerType.Font ? 245 : -110, -80);
                rectTransform.sizeDelta = new Vector2(300, 40);

                var buttonSize = 30f;
                var spacing = 3f;
                var numberOfButtons = colorOptions.Values.Count;
                var startX = -(numberOfButtons * buttonSize + (numberOfButtons - 1) * spacing) / 2;

                for (var i = 0; i < numberOfButtons; i++)
                {
                    var button = new GameObject($"ColorButton_{i}");
                    button.transform.SetParent(colorPicker.transform, false);
                    button.layer = 5;

                    var buttonRectTransform = button.AddComponent<RectTransform>();
                    buttonRectTransform.sizeDelta = new Vector2(buttonSize, buttonSize);
                    buttonRectTransform.anchoredPosition = new Vector2(startX + i * (buttonSize + spacing), 0);

                    var buttonImage = button.AddComponent<Image>();
                    buttonImage.type = Image.Type.Sliced;
                    buttonImage.sprite = SpriteManager.LoadEmbeddedSprite("UISmallSprite.png", new Vector4(5, 5, 5, 5));
                    buttonImage.color =
                        ColorUtility.TryParseHtmlString(colorOptions.Values.ElementAt(i).Value, out var color)
                            ? color
                            : Color.white;

                    var buttonComponent = button.AddComponent<Button>();
                    buttonComponent.onClick.AddListener((UnityAction)(() =>
                    {
                        // Pass the button itself to OnColorSelected instead of trying to look up the color
                        OnColorSelected(inputField, buttonComponent, type);
                    }));
                }

                colorPickers.Add(inputField, colorPicker);
                colorPicker.SetActive(true);
            }
            catch (Exception e)
            {
                Logger.Error($"Failed to create {type} color picker: {e.Message}");
            }
        }

        private static void OnColorSelected(TMP_InputField inputField, Button colorButton, ColorPickerType type)
        {
            // Get the current color directly from the button's image component
            var buttonImage = colorButton.GetComponent<Image>();
            if (buttonImage == null) return;

            var selectedColor = buttonImage.color;
            var colorHex = "#" + ColorUtility.ToHtmlStringRGB(selectedColor);

            if (type == ColorPickerType.Label)
            {
                inputField.GetComponent<Image>().color = selectedColor;
                LabelTracker.UpdateLabel(LabelTracker.GetCurrentlyManagedEntityGuid(), newLabelColor: colorHex);
            }
            else
            {
                inputField.GetComponentInChildren<TextMeshProUGUI>().color = selectedColor;
                LabelTracker.UpdateLabel(LabelTracker.GetCurrentlyManagedEntityGuid(), newFontColor: colorHex);
            }
        }

        public static void UpdateAllColorPickers(ColorPickerType type)
        {
            var pickers = type == ColorPickerType.Label ? LabelColorPickers : FontColorPickers;
            foreach (var picker in pickers) UpdateColorPickerButtons(picker.Value, type);
        }

        private static void UpdateColorPickerButtons(GameObject colorPicker, ColorPickerType type)
        {
            var colorOptions = type == ColorPickerType.Label
                ? ModSettings.LabelColorOptionsDictionary
                : ModSettings.FontColorOptionsDictionary;

            for (var i = 0; i < colorPicker.transform.childCount; i++)
            {
                var button = colorPicker.transform.GetChild(i);
                var buttonImage = button.GetComponent<Image>();
                if (buttonImage == null) continue;

                buttonImage.color =
                    ColorUtility.TryParseHtmlString(colorOptions.Values.ElementAt(i).Value, out var color)
                        ? color
                        : Color.red;
            }
        }


        public static void SetLabelColorPickerButtonColor(int buttonIndex, Color color)
        {
            try
            {
                foreach (var pickerEntry in LabelColorPickers)
                {
                    var colorPicker = pickerEntry.Value;

                    // Make sure the color picker exists and has children
                    if (colorPicker == null || colorPicker.transform.childCount <= buttonIndex ||
                        buttonIndex < 0) continue;

                    // Get the specific button at the given index
                    var buttonTransform = colorPicker.transform.GetChild(buttonIndex);
                    var buttonImage = buttonTransform.GetComponent<Image>();

                    // Update the button color if it has an Image component
                    if (buttonImage != null) buttonImage.color = color;
                }
            }
            catch (Exception e)
            {
                Logger.Error($"Failed to update label color picker button at index {buttonIndex}: {e.Message}");
            }
        }

        public static void Terminate()
        {
            TerminatePickers(LabelColorPickers);
            TerminatePickers(FontColorPickers);
        }

        private static void TerminatePickers(Dictionary<TMP_InputField, GameObject> pickers)
        {
            foreach (var picker in pickers.Values)
            {
                if (picker == null) continue;
                GameObject.Destroy(picker);
            }

            pickers.Clear();
        }
    }
}