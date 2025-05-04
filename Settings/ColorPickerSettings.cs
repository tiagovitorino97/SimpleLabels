using System.Collections.Generic;
using System.Text.RegularExpressions;
using MelonLoader;
using SimpleLabels.UI;
using UnityEngine;
using Logger = SimpleLabels.Utils.Logger;


namespace SimpleLabels.Settings
{
    public class ColorPickerSettings : MelonMod
    {
        public static void Initialize()
        {
            ModSettings.LabelColorOptionsDictionary = new Dictionary<string, MelonPreferences_Entry<string>>();
            ModSettings.FontColorOptionsDictionary = new Dictionary<string, MelonPreferences_Entry<string>>();
            CreateDefaultColorOptions();
        }

        private static void CreateDefaultColorOptions()
        {
            string[] colorNames =
            {
                "Color 1", "Color 2", "Color 3", "Color 4", "Color 5",
                "Color 6", "Color 7", "Color 8", "Color 9"
            };

            Color[] defaultColors =
            {
                Color.white,
                new Color(1f, 0.8f, 0.6f),
                new Color(0.7f, 0.9f, 0.7f),
                new Color(0.6f, 0.8f, 1f),
                new Color(1f, 0.7f, 0.85f),
                new Color(0.9f, 0.75f, 0.5f),
                new Color(0.75f, 0.65f, 0.9f),
                new Color(0.6f, 0.85f, 0.6f),
                new Color(0.85f, 0.85f, 0.85f)
            };

            for (var i = 0; i < colorNames.Length; i++)
            {
                // Create entry for Label Color
                var labelEntry = ModSettings.LabelColorPickerCategory.CreateEntry(colorNames[i],
                    "#" + ColorUtility.ToHtmlStringRGB(defaultColors[i]));
                var colorIndex = i;
                labelEntry.OnEntryValueChanged.Subscribe((oldVal, newVal) =>
                    OnColorChanged(colorIndex, "Label", oldVal, newVal)); // Added "Label"
                ModSettings.LabelColorOptionsDictionary.Add(colorNames[i], labelEntry);

                // Create entry for Font Color
                var fontEntry = ModSettings.FontColorPickerCategory.CreateEntry(colorNames[i],
                    "#" + ColorUtility.ToHtmlStringRGB(defaultColors[i]));
                var fontColorIndex = i;
                fontEntry.OnEntryValueChanged.Subscribe((oldVal, newVal) =>
                    OnColorChanged(fontColorIndex, "Font", oldVal, newVal)); // Added "Font"
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