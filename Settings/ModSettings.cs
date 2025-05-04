using System.Collections.Generic;
using System.Text.RegularExpressions;
using MelonLoader;
using SimpleLabels.UI;
using Logger = SimpleLabels.Utils.Logger;

namespace SimpleLabels.Settings
{
    public class ModSettings
    {
        //Categories

        public static MelonPreferences_Category GeneralCategory { get; private set; }
        public static MelonPreferences_Category ClipboardCategory { get; private set; }
        public static MelonPreferences_Category LabelColorPickerCategory { get; private set; }
        public static MelonPreferences_Category LabelCategory { get; private set; }
        public static MelonPreferences_Category FontColorPickerCategory { get; private set; }
        public static MelonPreferences_Category FontCategory { get; private set; }
        public static MelonPreferences_Category DebugCategory { get; private set; }

        //General Settings
        public static MelonPreferences_Entry<bool> ShowInput { get; private set; }
        public static MelonPreferences_Entry<bool> AutoFocusInput { get; private set; }

        //Clipboard Settings
        public static MelonPreferences_Entry<bool> ShowClipboardRoutes { get; private set; }
        public static MelonPreferences_Entry<bool> ShowClipboardStations { get; private set; }
        public static MelonPreferences_Entry<bool> ShowClipboardStationsOutput { get; private set; }

        //Label Colorpicker Settings
        public static Dictionary<string, MelonPreferences_Entry<string>> LabelColorOptionsDictionary { get; set; }

        //Label Settings
        public static MelonPreferences_Entry<int> LabelDefaultSize { get; private set; }
        public static MelonPreferences_Entry<string> LabelDefaultColor { get; private set; }

        //Font Colorpicker Settings
        public static Dictionary<string, MelonPreferences_Entry<string>> FontColorOptionsDictionary { get; set; }

        //Font Settings
        public static MelonPreferences_Entry<int> FontDefaultSize { get; private set; }
        public static MelonPreferences_Entry<string> FontDefaultColor { get; private set; }


        //Debug Settings
        public static MelonPreferences_Entry<bool> ShowDebug { get; private set; }


        public static void Initialize()
        {
            CreateGeneralSettings();
            CreateClipboardSettings();
            CreateLabelColorPickerSettings();
            CreateFontColorPickerSettings();
            ColorPickerSettings.Initialize();
            CreateLabelSettings();
            CreateFontSettings();
            CreateDebugSettings();
        }

        private static void CreateGeneralSettings()
        {
            GeneralCategory = MelonPreferences.CreateCategory("SimpleLabels_01_General", "General");
            ShowInput = GeneralCategory.CreateEntry("Show input", true);
            ShowInput.OnEntryValueChanged.Subscribe(OnShowInputChanged);
            AutoFocusInput = GeneralCategory.CreateEntry("Auto focus input", true);
            AutoFocusInput.OnEntryValueChanged.Subscribe(OnAutoFocusInputChanged);
        }

        private static void CreateClipboardSettings()
        {
            ClipboardCategory = MelonPreferences.CreateCategory("SimpleLabels_02_Clipboard", "Clipboard");
            ShowClipboardRoutes = ClipboardCategory.CreateEntry("Show routes", true);
            ShowClipboardRoutes.OnEntryValueChanged.Subscribe(OnShowClipboardRoutesChanged);
            ShowClipboardStations = ClipboardCategory.CreateEntry("Show stations", true);
            ShowClipboardStations.OnEntryValueChanged.Subscribe(OnShowClipboardStationsChanged);
            ShowClipboardStationsOutput = ClipboardCategory.CreateEntry("Show stations output", true);
            ShowClipboardStationsOutput.OnEntryValueChanged.Subscribe(OnShowClipboardStationsOutputChanged);
        }

        private static void CreateLabelColorPickerSettings()
        {
            LabelColorPickerCategory =
                MelonPreferences.CreateCategory("SimpleLabels_03_ColorPicker", "Label Color Picker");
        }

        private static void CreateFontColorPickerSettings()
        {
            FontColorPickerCategory =
                MelonPreferences.CreateCategory("SimpleLabels_04_ColorPicker", "Font Color Picker");
        }

        private static void CreateLabelSettings()
        {
            LabelCategory = MelonPreferences.CreateCategory("SimpleLabels_05_Label", "Labels");
            LabelDefaultSize = LabelCategory.CreateEntry("Default size (1 - 30)", 1);
            LabelDefaultSize.OnEntryValueChanged.Subscribe(OnLabelDefaultSizeChanged);
            LabelDefaultColor = LabelCategory.CreateEntry("Default color", "#FFFFFF"); //White
            LabelDefaultColor.OnEntryValueChanged.Subscribe(OnLabelDefaultColorChanged);
        }

        private static void CreateFontSettings()
        {
            FontCategory = MelonPreferences.CreateCategory("SimpleLabels_06_Font", "Font");
            FontDefaultSize = FontCategory.CreateEntry("Default size", 24, is_hidden: true);
            FontDefaultSize.OnEntryValueChanged.Subscribe(OnFontDefaultSizeChanged);
            FontDefaultColor = FontCategory.CreateEntry("Default color", "#000000"); //Black
            FontDefaultColor.OnEntryValueChanged.Subscribe(OnFontDefaultColorChanged);
        }


        private static void CreateDebugSettings()
        {
            DebugCategory = MelonPreferences.CreateCategory("SimpleLabels_07_Debug", "Debug");
            ShowDebug = DebugCategory.CreateEntry("Show console debug", false);
            ShowDebug.OnEntryValueChanged.Subscribe(OnShowDebugChanged);
        }

        private static void OnShowInputChanged(bool oldValue, bool newValue)
        {
            ShowInput.Value = newValue;
        }

        private static void OnAutoFocusInputChanged(bool oldValue, bool newValue)
        {
            AutoFocusInput.Value = newValue;
        }

        private static void OnShowClipboardRoutesChanged(bool oldValue, bool newValue)
        {
            ShowClipboardRoutes.Value = newValue;
        }

        private static void OnShowClipboardStationsChanged(bool oldValue, bool newValue)
        {
            ShowClipboardStations.Value = newValue;
        }

        private static void OnShowClipboardStationsOutputChanged(bool oldValue, bool newValue)
        {
            ShowClipboardStationsOutput.Value = newValue;
        }

        private static void OnLabelDefaultSizeChanged(int oldValue, int newValue)
        {
            CheckValidSizeAndUpdate(newValue, LabelDefaultSize);
        }

        private static void OnLabelDefaultColorChanged(string oldValue, string newValue)
        {
            CheckCorrectFormatAndUpdate(newValue, LabelDefaultColor);
        }

        private static void OnFontDefaultSizeChanged(int oldValue, int newValue)
        {
            FontDefaultSize.Value = oldValue;
        }

        private static void OnFontDefaultColorChanged(string oldValue, string newValue)
        {
            CheckCorrectFormatAndUpdate(newValue, FontDefaultColor);
        }

        private static void OnShowDebugChanged(bool oldValue, bool newValue)
        {
            ShowDebug.Value = newValue;
        }

        private static void CheckCorrectFormatAndUpdate(string newValue, MelonPreferences_Entry<string> entry)
        {
            if (Regex.IsMatch(newValue, @"^#[0-9A-Fa-f]{6}$"))
            {
                entry.Value = newValue;
                ColorPickerManager.UpdateAllColorPickers(ColorPickerType.Label);
                ColorPickerManager.UpdateAllColorPickers(ColorPickerType.Font);
            }
            else
            {
                Logger.Warning($"Invalid color format: {newValue}. Reverted to {entry.DefaultValue}");
                entry.Value = entry.DefaultValue;
                ModManagerIntegration.RequestUIRefresh();
            }
        }

        private static void CheckValidSizeAndUpdate(int newValue, MelonPreferences_Entry<int> entry)
        {
            if (newValue >= 1 && newValue <= 30)
            {
                entry.Value = newValue;
            }
            else
            {
                Logger.Warning(
                    $"Invalid size value: {newValue}. Must be between 1 and 30. Reverted to {entry.DefaultValue}");
                entry.Value = entry.DefaultValue;
                ModManagerIntegration.RequestUIRefresh();
            }
        }
    }
}