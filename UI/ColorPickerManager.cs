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

namespace SimpleLabels.UI
{
    public enum ColorPickerType
    {
        Label,
        Font
    }

    public static class ColorPickerManager
    {
        private static readonly Dictionary<TMP_InputField, GameObject> LabelColorPickers =
            new Dictionary<TMP_InputField, GameObject>();

        private static readonly Dictionary<TMP_InputField, GameObject> FontColorPickers =
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
                rectTransform.anchorMax = new Vector2(0.5f, 0f);
                rectTransform.pivot = new Vector2(0.5f, 1f);
                rectTransform.anchoredPosition = new Vector2(type == ColorPickerType.Font ? 240 : -100, -20);
                rectTransform.sizeDelta = new Vector2(300, 40);

                var buttonSize = 30f;
                var spacing = 5f;
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
                    buttonImage.color =
                        ColorUtility.TryParseHtmlString(colorOptions.Values.ElementAt(i).Value, out var color)
                            ? color
                            : Color.white;

                    var outline = button.AddComponent<Outline>();
                    outline.effectColor = Color.black;
                    outline.effectDistance = new Vector2(1, 1);

                    var buttonComponent = button.AddComponent<Button>();
                    var colorIndex = i; // Capture the index for the lambda
                    buttonComponent.onClick.AddListener((UnityAction)(() =>
                    {
                        var currentOptions = type == ColorPickerType.Label
                            ? ModSettings.LabelColorOptionsDictionary
                            : ModSettings.FontColorOptionsDictionary;

                        if (ColorUtility.TryParseHtmlString(currentOptions.Values.ElementAt(colorIndex).Value,
                                out var currentColor)) OnColorSelected(inputField, currentColor, type);
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

        private static void OnColorSelected(TMP_InputField inputField, Color selectedColor, ColorPickerType type)
        {
            var colorHex = "#" + ColorUtility.ToHtmlStringRGB(selectedColor);

            if (type == ColorPickerType.Label)
            {
                inputField.GetComponent<Image>().color = selectedColor;
                LabelTracker.UpdateLabel(LabelTracker.GetCurrentlyManagedEntityGuid(), newLabelColor: colorHex);
                LabelApplier.ApplyOrUpdateLabel(LabelTracker.GetCurrentlyManagedEntityGuid());
            }
            else
            {
                inputField.GetComponentInChildren<TextMeshProUGUI>().color = selectedColor;
                LabelTracker.UpdateLabel(LabelTracker.GetCurrentlyManagedEntityGuid(), newFontColor: colorHex);
                LabelApplier.ApplyOrUpdateLabel(LabelTracker.GetCurrentlyManagedEntityGuid());
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