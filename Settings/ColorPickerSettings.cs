using System.Collections.Generic;
using System.Text.RegularExpressions;
using MelonLoader;
using SimpleLabels.UI;
using UnityEngine;
using Logger = SimpleLabels.Utils.Logger;


namespace SimpleLabels.Settings
{
    public static class ColorPickerSettings
    {
        public static string[] colorNames =
        {
            "Color 1", "Color 2", "Color 3", "Color 4", "Color 5",
            "Color 6", "Color 7", "Color 8", "Color 9"
        };
        public static void Initialize()
        {
            ModSettings.LabelColorOptionsDictionary = new Dictionary<string, MelonPreferences_Entry<string>>();
            ModSettings.FontColorOptionsDictionary = new Dictionary<string, MelonPreferences_Entry<string>>();
            CreateDefaultColorOptions();
        }

        private static void CreateDefaultColorOptions()
        {
            

            Color[] defaultLabelColors =
            {
                new Color(0.30f, 0.45f, 0.65f),
                new Color(0.40f, 0.60f, 0.35f),
                new Color(0.70f, 0.55f, 0.30f),
                new Color(0.65f, 0.40f, 0.40f),
                new Color(0.40f, 0.35f, 0.60f),
                new Color(0.55f, 0.60f, 0.60f),
                new Color(0.30f, 0.65f, 0.55f),
                new Color(0.80f, 0.70f, 0.50f),
                Color.white
            };

            Color[] defaultFontColors =
            {
                new Color(0.85f, 0.85f, 0.30f),
                new Color(0.30f, 0.70f, 0.30f),
                new Color(0.80f, 0.40f, 0.20f),
                new Color(0.60f, 0.30f, 0.60f),
                new Color(0.30f, 0.50f, 0.70f),
                new Color(0.75f, 0.75f, 0.75f),
                new Color(0.20f, 0.25f, 0.40f),
                new Color(0.50f, 0.30f, 0.20f),
                Color.black
            };

            for (var i = 0; i < colorNames.Length; i++)
            {
                var labelEntry = ModSettings.LabelColorPickerCategory.CreateEntry(colorNames[i],
                    "#" + ColorUtility.ToHtmlStringRGB(defaultLabelColors[i]));
                var colorIndex = i;
                labelEntry.OnEntryValueChanged.Subscribe((oldVal, newVal) =>
                    OnColorChanged(colorIndex, "Label", oldVal, newVal));
                ModSettings.LabelColorOptionsDictionary.Add(colorNames[i], labelEntry);

                var fontEntry = ModSettings.FontColorPickerCategory.CreateEntry(colorNames[i],
                    "#" + ColorUtility.ToHtmlStringRGB(defaultFontColors[i]));
                var fontColorIndex = i;
                fontEntry.OnEntryValueChanged.Subscribe((oldVal, newVal) =>
                    OnColorChanged(fontColorIndex, "Font", oldVal, newVal));
                ModSettings.FontColorOptionsDictionary.Add(colorNames[i], fontEntry);
            }

            return;

            void OnColorChanged(int colorIndex, string pickerType, string oldVal, string newVal)
            {
                if (!Regex.IsMatch(newVal, @"^#[0-9A-Fa-f]{6}$"))
                {
                    if (pickerType == "Label")
                        ModSettings.LabelColorOptionsDictionary[colorNames[colorIndex]].Value = oldVal;
                    else if (pickerType == "Font")
                        ModSettings.FontColorOptionsDictionary[colorNames[colorIndex]].Value = oldVal;
                    Logger.Warning(
                        $"Invalid color format for {pickerType} color '{colorNames[colorIndex]}': {newVal}. Reverted to {oldVal}");
                    return;
                }

                ModManagerIntegration.RequestUIRefresh();

                if (pickerType == "Label")
                    ColorPickerManager.UpdateAllColorPickers(ColorPickerType.Label);
                else if (pickerType == "Font") ColorPickerManager.UpdateAllColorPickers(ColorPickerType.Font);
            }
        }
    }
}