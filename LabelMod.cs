using HarmonyLib;
using Il2CppScheduleOne.EntityFramework;
using Il2CppScheduleOne.Management;
using Il2CppScheduleOne.ObjectScripts;
using Il2CppScheduleOne.Persistence;
using Il2CppScheduleOne.Persistence.Datas;
using Il2CppScheduleOne.Persistence.Loaders;
using Il2CppScheduleOne.Storage;
using Il2CppScheduleOne.UI;
using Il2CppScheduleOne.UI.Management;
using Il2CppScheduleOne.UI.Stations;
using Il2CppTMPro;
using MelonLoader;
using MelonLoader.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;


namespace SimpleLabels
{
    public class LabelMod : MelonMod
    {
        public static LabelMod Instance { get; private set; }

        private TMP_InputField customStorageInputField;
        private Dictionary<string, TMP_InputField> customStationInputFields = new Dictionary<string, TMP_InputField>();
        private string[] stationNames = { "MixingStation", "PackagingStation", "DryingRack", "BrickPress", "Cauldron", "LabOven", "ChemistryStation" };

        private bool wasStorageRackOpen = false;
        private bool wasStationOpen = false;
        GameObject openStorageGameObject;
        GridItem openStationGameObject;
        private string openEntityGameObjectGUID;
        private string openEntityName;

        //Mod Data
        private string dataFolderPath;

        //Mod settings
        private MelonPreferences_Category mainSettings;
        private MelonPreferences_Category clipboardSettings;
        private MelonPreferences_Category colorSettings;
        private MelonPreferences_Category debugSettings;
        private MelonPreferences_Entry<bool> modSettingsAutoFocusLabel;
        private MelonPreferences_Entry<bool> modSettingsConsoleDebug;
        private MelonPreferences_Entry<bool> modSettingsShowInputLabel;
        private MelonPreferences_Entry<bool> modSettingsShowClipboardRoutesLabels;
        private MelonPreferences_Entry<bool> modSettingsShowClipboardStationsLabels;
        private Dictionary<string, MelonPreferences_Entry<string>> modSettingsColors = new Dictionary<string, MelonPreferences_Entry<string>>();

        //Label Data
        private LabelData labelData;
        private string labelDataFilePath;
        private LabelData unsavedLabelData; //Stores labelData until onSaveStart is called

        //Label Prefab
        private GameObject labelPrefab;




        public override void OnInitializeMelon()
        {
            Instance = this;
            LoggerInstance.Msg("SimpleLabels mod initializing...");

            //Mod Config
            dataFolderPath = Path.Combine(MelonEnvironment.ModsDirectory, "SimpleLabels");
            EnsureDataDirectoryExists();

            //Label Data
            labelDataFilePath = Path.Combine(dataFolderPath, "Labels.json");
            unsavedLabelData = new LabelData();

            MelonCoroutines.Start(WaitAndHook());

            mainSettings = MelonPreferences.CreateCategory("SimpleLabels_01_Main", "Main Settings");

            modSettingsAutoFocusLabel = mainSettings.CreateEntry("Input label auto-focus", true);
            modSettingsAutoFocusLabel.OnEntryValueChanged.Subscribe(modSettingsAutoFocusLabelOnChange);

            modSettingsShowInputLabel = mainSettings.CreateEntry("Show input label", true);
            modSettingsShowInputLabel.OnEntryValueChanged.Subscribe(modSettingsShowInputLabelOnChange);

            clipboardSettings = MelonPreferences.CreateCategory("SimpleLabels_02_Clipboard", "Clipboard Options");

            modSettingsShowClipboardRoutesLabels = clipboardSettings.CreateEntry("Routes names", true);
            modSettingsShowClipboardRoutesLabels.OnEntryValueChanged.Subscribe(modSettingsShowClipboardRoutesLabelsOnChange);

            modSettingsShowClipboardStationsLabels = clipboardSettings.CreateEntry("Stations names", true);
            modSettingsShowClipboardStationsLabels.OnEntryValueChanged.Subscribe(modSettingsShowClipboardStationsLabelsOnChange);

            colorSettings = MelonPreferences.CreateCategory("SimpleLabels_03_Colors", "Colors");
            InitializeColorSettings();

            debugSettings = MelonPreferences.CreateCategory("SimpleLabels_04_Debug", "Debug");
            modSettingsConsoleDebug = debugSettings.CreateEntry("Show console debug", false);
            modSettingsConsoleDebug.OnEntryValueChanged.Subscribe(modSettingsConsoleDebugOnChange);

            try
            {
                // Try to get the ModManagerPhoneApp.ModSettingsEvents type via reflection
                var modSettingsEventsType = Type.GetType("ModManagerPhoneApp.ModSettingsEvents, ModManagerPhoneApp");

                if (modSettingsEventsType != null)
                {
                    var eventInfo = modSettingsEventsType.GetEvent("OnPreferencesSaved");

                    var handler = Delegate.CreateDelegate(eventInfo.EventHandlerType, this, "colorPickerUpdateUserColor");

                    eventInfo.AddEventHandler(null, handler);

                    LoggerInstance.Msg("Successfully subscribed to Mod Manager save event.");
                }
                else
                {
                    LoggerInstance.Msg("Mod Manager not found - skipping event subscription");
                }
            }
            catch (Exception ex)
            {
                LoggerInstance.Warning($"Could not subscribe to Mod Manager event (Mod Manager may not be installed/compatible): {ex.Message}");
            }
        }

        public override void OnDeinitializeMelon()
        {
            try
            {
                var modSettingsEventsType = Type.GetType("ModManagerPhoneApp.ModSettingsEvents, ModManagerPhoneApp");

                if (modSettingsEventsType != null)
                {
                    var eventInfo = modSettingsEventsType.GetEvent("OnPreferencesSaved");
                    var handler = Delegate.CreateDelegate(eventInfo.EventHandlerType, this, "colorPickerUpdateUserColor");
                    eventInfo.RemoveEventHandler(null, handler);

                    LoggerInstance.Msg("Unsubscribed from Mod Manager save event.");
                }
            }
            catch { /* Ignore errors */ }
        }

        public void InitializeColorSettings()
        {
            string[] colorSettingNames =
                {
            "Color 1", "Color 2", "Color 3", "Color 4", "Color 5",
            "Color 6", "Color 7", "Color 8", "Color 9"
        };

            Color[] colors = new Color[]
                {
            Color.white,                                     // Pure White
            new Color(1f, 0.8f, 0.6f),                      // Light Orange-Yellow
            new Color(0.7f, 0.9f, 0.7f),                      // Light Mint Green
            new Color(0.6f, 0.8f, 1f),                      // Light Blue
            new Color(1f, 0.7f, 0.85f),                     // Light Pink
            new Color(0.9f, 0.75f, 0.5f),                    // Muted Gold
            new Color(0.75f, 0.65f, 0.9f),                   // Light Lavender
            new Color(0.6f, 0.85f, 0.6f),                    // Soft Lime Green
            new Color(0.85f, 0.85f, 0.85f)                   // Very Light Grey
                };

            for (int i = 0; i < colorSettingNames.Length; i++)
            {
                var currentColorIndex = i; // Create a local copy of 'i'
                var entry = colorSettings.CreateEntry(colorSettingNames[i], ColorUtility.ToHtmlStringRGB(colors[i]));
                modSettingsColors.Add(colorSettingNames[i], entry);

                // Subscribe to a single change event handler, using the captured local copy
                entry.OnEntryValueChanged.Subscribe((oldValue, newValue) => OnColorSettingChanged(oldValue, newValue, currentColorIndex));
            }
        }

        public void colorPickerUpdateUserColor()
        {
            MelonCoroutines.Start(UpdateColorsAfterDelay());
        }

        private System.Collections.IEnumerator UpdateColorsAfterDelay()
        {
            yield return new WaitForSeconds(0.2f);

            // Update colors for customStorageInputField
            if (customStorageInputField != null)
            {
                var colorPickerObj = customStorageInputField.transform.Find("ColorPicker");
                if (colorPickerObj != null)
                {
                    int colorIndex = 0;
                    foreach (var child in colorPickerObj.transform)
                    {
                        // Use TryCast instead of direct cast
                        var colorButton = child.Cast<Transform>();
                        if (colorButton == null)
                        {
                            if (Instance.modSettingsConsoleDebug.Value) MelonLogger.Warning($"Skipping child: Unable to cast {child.GetType()} to Transform");
                            continue;
                        }

                        if (Instance.modSettingsConsoleDebug.Value) MelonLogger.Msg($"Iterating through colorButton: {colorButton.name}");
                        if (colorButton.name.StartsWith("ColorButton_") && colorIndex < modSettingsColors.Count)
                        {
                            if (Instance.modSettingsConsoleDebug.Value) MelonLogger.Msg($"Found ColorButton with correct prefix. Checking modSettingsColors at index: {colorIndex}");
                            if (modSettingsColors.ElementAt(colorIndex).Value != null)
                            {
                                string hexColor = "#" + modSettingsColors.ElementAt(colorIndex).Value.Value;
                                if (Instance.modSettingsConsoleDebug.Value) MelonLogger.Msg($"Attempting to parse color: {hexColor}");
                                if (ColorUtility.TryParseHtmlString(hexColor, out Color color))
                                {
                                    var imageComponent = colorButton.GetComponent<Image>();
                                    if (imageComponent != null)
                                    {
                                        imageComponent.color = color;
                                        if (Instance.modSettingsConsoleDebug.Value) MelonLogger.Msg($"Updated color of {colorButton.name} to {color}");
                                    }
                                    else
                                    {
                                        if (Instance.modSettingsConsoleDebug.Value) MelonLogger.Warning($"Image component not found on {colorButton.name}");
                                    }
                                }
                                else
                                {
                                    if (Instance.modSettingsConsoleDebug.Value) MelonLogger.Error($"Failed to parse color string: {hexColor}");
                                }
                            }
                            else
                            {
                                if (Instance.modSettingsConsoleDebug.Value) MelonLogger.Warning($"modSettingsColors[{colorIndex}].Value is null.");
                            }
                            colorIndex++;
                        }
                    }
                }
                else
                {
                    if (Instance.modSettingsConsoleDebug.Value) MelonLogger.Warning("ColorPicker transform not found for customStorageInputField.");
                }
            }

            // Update colors for customStationInputFields
            if (Instance.modSettingsConsoleDebug.Value) MelonLogger.Msg($"Iterating through customStationInputFields. Count: {customStationInputFields?.Count}");
            foreach (var stationInputField in customStationInputFields)
            {
                if (Instance.modSettingsConsoleDebug.Value) MelonLogger.Msg($"Processing stationInputField with key: {stationInputField.Key}, Value: {stationInputField.Value?.name}");
                if (stationInputField.Value != null)
                {
                    var colorPickerObj = stationInputField.Value.transform.Find("ColorPicker");
                    if (colorPickerObj != null)
                    {
                        int colorIndex = 0;
                        foreach (var child in colorPickerObj.transform)
                        {
                            // Use TryCast instead of direct cast
                            var colorButton = child.Cast<Transform>();
                            if (colorButton == null)
                            {
                                if (Instance.modSettingsConsoleDebug.Value) MelonLogger.Warning($"Skipping child: Unable to cast {child.GetType()} to Transform");
                                continue;
                            }

                            if (Instance.modSettingsConsoleDebug.Value) MelonLogger.Msg($"Iterating through colorButton: {colorButton.name}, Index: {colorIndex}");
                            if (colorButton.name.StartsWith("ColorButton_") && colorIndex < modSettingsColors.Count)
                            {
                                if (Instance.modSettingsConsoleDebug.Value) MelonLogger.Msg($"Found ColorButton with correct prefix. Checking modSettingsColors at index: {colorIndex}");
                                if (modSettingsColors.ElementAt(colorIndex).Value != null)
                                {
                                    string hexColor = "#" + modSettingsColors.ElementAt(colorIndex).Value.Value;
                                    if (Instance.modSettingsConsoleDebug.Value) MelonLogger.Msg($"Attempting to parse color: {hexColor}");
                                    if (ColorUtility.TryParseHtmlString(hexColor, out Color color))
                                    {
                                        var imageComponent = colorButton.GetComponent<Image>();
                                        if (imageComponent != null)
                                        {
                                            imageComponent.color = color;
                                            if (Instance.modSettingsConsoleDebug.Value) MelonLogger.Msg($"Updated color of {colorButton.name} for {stationInputField.Value.name} to {color}");
                                        }
                                        else
                                        {
                                            if (Instance.modSettingsConsoleDebug.Value) MelonLogger.Warning($"Image component not found on {colorButton.name} for {stationInputField.Value.name}");
                                        }
                                    }
                                    else
                                    {
                                        if (Instance.modSettingsConsoleDebug.Value) MelonLogger.Error($"Failed to parse color string for {stationInputField.Value.name}: {hexColor}");
                                    }
                                }
                                else
                                {
                                    if (Instance.modSettingsConsoleDebug.Value) MelonLogger.Warning($"modSettingsColors[{colorIndex}].Value is null for {stationInputField.Value.name}.");
                                }
                                colorIndex++;
                            }
                        }
                    }
                    else
                    {
                        if (Instance.modSettingsConsoleDebug.Value) MelonLogger.Warning($"ColorPicker transform not found for {stationInputField.Value?.name}.");
                    }
                }
                else
                {
                    if (Instance.modSettingsConsoleDebug.Value) MelonLogger.Warning("stationInputField.Value is null.");
                }
            }
        }

        private void modSettingsAutoFocusLabelOnChange(bool oldValue, bool newValue)
        {
            modSettingsAutoFocusLabel.Value = newValue;
        }
        private void modSettingsConsoleDebugOnChange(bool oldValue, bool newValue)
        {
            modSettingsConsoleDebug.Value = newValue;
        }
        private void modSettingsShowInputLabelOnChange(bool oldValue, bool newValue)
        {
            modSettingsShowClipboardRoutesLabels.Value = newValue;
        }
        private void modSettingsShowClipboardRoutesLabelsOnChange(bool oldValue, bool newValue)
        {
            modSettingsShowClipboardRoutesLabels.Value = newValue;
        }
        private void modSettingsShowClipboardStationsLabelsOnChange(bool oldValue, bool newValue)
        {
            modSettingsShowClipboardStationsLabels.Value = newValue;
        }

        private void OnColorSettingChanged(string oldValue, string newValue, int index)
        {

            if (Instance.modSettingsConsoleDebug.Value)
                MelonLogger.Msg($"Color setting '{modSettingsColors.ElementAt(index)}' changed from '{oldValue}' to '{newValue}'");

            if (Regex.IsMatch(newValue, @"^[0-9A-Fa-f]{6}$"))
            {
                modSettingsColors.ElementAt(index).Value.Value = newValue;
                if (Instance.modSettingsConsoleDebug.Value)
                    MelonLogger.Msg($"New value '{newValue}' is a valid HTML color string and has been set.");
            }
            else
            {
                modSettingsColors.ElementAt(index).Value.Value = oldValue;
                if (Instance.modSettingsConsoleDebug.Value)
                    MelonLogger.Warning($"New value '{newValue}' is not a valid HTML color string. String has been set to old value.");
            }

        }

        public override void OnSceneWasInitialized(int buildIndex, string sceneName)
        {

            //MAYBE USEFUL IN THE FUTURE

        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            if (Instance.modSettingsConsoleDebug.Value) MelonLogger.Msg($"Scene loaded: {sceneName}");

            if (sceneName == "Main") ActivateSimpleLabelsMod();

            else if (sceneName == "Menu") DeactivateSimpleLabelsMod();
        }

        public void ActivateSimpleLabelsMod()
        {
           
            MelonCoroutines.Start(WaitAndSubscribe());
            InitializeLabelPrefab();

            GameObject storageUI = GameObject.Find("UI/StorageMenu");

            if (storageUI == null)
            {
                LoggerInstance.Error("Could not find the 'UI/StorageMenu' GameObject.");
                return;
            }
           

            customStorageInputField = CreateInputField(storageUI, new Vector2(0.5f, 0.65f));
            CreateColorPicker(customStorageInputField);
            if (Instance.modSettingsConsoleDebug.Value) MelonLogger.Msg("Label input field created for StorageMenu.");
            

            // Create all the station custom input fields
            GameObject stationsUI = GameObject.Find("UI/Stations");
            if (stationsUI == null)
            {
                LoggerInstance.Error("Could not find the 'UI/Stations' GameObject.");
                return;
            }
            
            foreach (string stationName in stationNames)
            {
                Transform stationTransform = stationsUI.transform.Find(stationName);
                if (stationTransform != null)
                {
                    customStationInputFields[stationName] = CreateInputField(stationTransform.gameObject, new Vector2(0.5f, 0.55f));
                    CreateColorPicker(customStationInputFields[stationName]);
                    if (Instance.modSettingsConsoleDebug.Value) MelonLogger.Msg($"Label input field created for {stationName}.");
                }
                else
                {
                    LoggerInstance.Warning($"Could not find UI for {stationName}.");
                }
            }



            LoadLabelData();

            wasStorageRackOpen = false;
            wasStationOpen = false;
        }
        public void DeactivateSimpleLabelsMod()
        {
            unsavedLabelData = new LabelData();
            LabelTracker.UntrackAllStorage();
            if (Instance.modSettingsConsoleDebug.Value) MelonLogger.Msg("Cleared unsaved label data");

            wasStorageRackOpen = false;
            wasStationOpen = false;

            // Destroy all created prefabs
            if (labelPrefab != null)
            {
                GameObject.Destroy(labelPrefab);
                labelPrefab = null;
                if (Instance.modSettingsConsoleDebug.Value) MelonLogger.Msg("Destroyed label prefab.");
            }

            // Destroy all custom input fields
            if (customStorageInputField != null)
            {
                GameObject.Destroy(customStorageInputField.gameObject);
                customStorageInputField = null;
                if (Instance.modSettingsConsoleDebug.Value) MelonLogger.Msg("Destroyed custom storage input field.");
            }

            foreach (var inputField in customStationInputFields.Values)
            {
                if (inputField != null)
                {
                    GameObject.Destroy(inputField.gameObject);
                }
            }
            customStationInputFields.Clear();


            if (Instance.modSettingsConsoleDebug.Value) MelonLogger.Msg("Destroyed all custom station input fields.");
        }


        private System.Collections.IEnumerator WaitAndHook()
        {
            while (SaveManager.Instance == null && LoadManager.Instance == null)
                yield return null;

            UnityEvent onSaveStart = SaveManager.Instance.onSaveStart;
            onSaveStart.AddListener((UnityAction)OnSaveStart);
        }

        private void OnSaveStart()
        {
            LoggerInstance.Msg("Saving label data...");

            foreach (var kvp in unsavedLabelData.Labels)
            {
                labelData.Labels[kvp.Key] = kvp.Value;
            }

            foreach (var guid in unsavedLabelData.Labels.Keys)
            {
                if (string.IsNullOrEmpty(unsavedLabelData.Labels[guid]?.Text))// Remove any empty strings 
                {
                    labelData.Labels.Remove(guid);
                }
            }

            SaveLabelData();

            if (Instance.modSettingsConsoleDebug.Value) MelonLogger.Msg("Saved all label changes");
        }


        [HarmonyPatch(typeof(StorageMenu), nameof(StorageMenu.Open), new Type[] { typeof(StorageEntity) })]
        class StorageMenu_Open_Patch
        {

            static void Postfix(StorageMenu __instance, StorageEntity entity)
            {

                if (!Instance.modSettingsShowInputLabel.Value) return;

                if (Instance.modSettingsConsoleDebug.Value) MelonLogger.Msg($"Storage menu opened for: {entity?.StorageEntityName}");

                if (entity is UnityEngine.Component component && component.gameObject != null)
                {
                    try
                    {
                        Instance.openStorageGameObject = component.gameObject;
                        Instance.openEntityName = entity.name;
                        var placeableStorage = Instance.openStorageGameObject.GetComponent<Il2CppScheduleOne.ObjectScripts.PlaceableStorageEntity>();
                        var surfaceStorage = Instance.openStorageGameObject.GetComponent<Il2CppScheduleOne.ObjectScripts.SurfaceStorageEntity>();
                        if (placeableStorage != null)
                        {
                            Instance.openEntityGameObjectGUID = placeableStorage.GUID.ToString();
                            if (Instance.modSettingsConsoleDebug.Value) MelonLogger.Msg($"StorageEntity GUID: {Instance.openEntityGameObjectGUID}");
                        }
                        else if (surfaceStorage != null)
                        {
                            Instance.openEntityGameObjectGUID = surfaceStorage.GUID.ToString();
                            if (Instance.modSettingsConsoleDebug.Value) MelonLogger.Msg($"StorageEntity GUID: {Instance.openEntityGameObjectGUID}");
                        }
                        else
                        {
                            if (Instance.modSettingsConsoleDebug.Value) MelonLogger.Warning($"PlaceableStorageEntity component not found on {entity.StorageEntityName}");
                            Instance.openEntityGameObjectGUID = null; // Ensure GUID is null if component is missing
                        }
                    }
                    catch (Exception ex)
                    {
                        if (Instance.modSettingsConsoleDebug.Value) MelonLogger.Error($"Error accessing GameObject or PlaceableStorageEntity: {ex.Message}");
                        Instance.openEntityGameObjectGUID = null; // Ensure GUID is null on error
                    }

                    if (!string.IsNullOrEmpty(Instance.openEntityGameObjectGUID) && Instance.IsValidStorageName(entity.StorageEntityName))
                    {
                        Instance.customStorageInputField.gameObject.SetActive(true); //Show input field

                        // Load existing label if it exists
                        if (Instance.unsavedLabelData.Labels.TryGetValue(Instance.openEntityGameObjectGUID, out LabelInfo existingUnsavedLabelInfo))
                        {
                            Instance.customStorageInputField.text = existingUnsavedLabelInfo.Text;
                            ColorUtility.TryParseHtmlString("#" + existingUnsavedLabelInfo.Color, out Color color);
                            Instance.customStorageInputField.GetComponent<Image>().color = color;
                            if (Instance.modSettingsConsoleDebug.Value)
                                MelonLogger.Msg($"Loaded existing unsaved label: {existingUnsavedLabelInfo.Text}");
                        }
                        else if (Instance.labelData.Labels.TryGetValue(Instance.openEntityGameObjectGUID, out LabelInfo existingLabelInfo))
                        {
                            Instance.customStorageInputField.text = existingLabelInfo.Text;
                            ColorUtility.TryParseHtmlString("#" + existingLabelInfo.Color, out Color color);
                            Instance.customStorageInputField.GetComponent<Image>().color = color;
                            if (Instance.modSettingsConsoleDebug.Value)
                                MelonLogger.Msg($"Loaded existing label: {existingLabelInfo.Text}");
                        }
                        else
                        {
                            Instance.customStorageInputField.text = string.Empty;
                            Instance.customStorageInputField.GetComponent<Image>().color = Color.white;
                        }

                        if (Instance.modSettingsAutoFocusLabel.Value)
                        {
                            if (Instance.modSettingsConsoleDebug.Value) MelonLogger.Msg("Focusing on input field.");
                            var inputField = __instance.gameObject.transform.Find("CustomInputField")?.GetComponent<TMP_InputField>();
                            inputField?.Select();
                            inputField?.ActivateInputField();
                        }
                        else
                        {
                            if (Instance.modSettingsConsoleDebug.Value) MelonLogger.Msg("Not focusing on input field.");
                        }

                        Instance.wasStorageRackOpen = true;
                        LabelTracker.TrackStorage(Instance.openEntityGameObjectGUID, entity.gameObject, Instance.customStorageInputField.text.ToString());
                    }
                    else
                    {
                        if (Instance.modSettingsConsoleDebug.Value && entity != null) MelonLogger.Msg($"StorageEntity {entity.StorageEntityName} is not a target for labeling or its GameObject is null.");
                        if (entity != null && !(entity is UnityEngine.Component))
                        {
                            if (Instance.modSettingsConsoleDebug.Value) MelonLogger.Msg($"StorageEntity '{entity.StorageEntityName}' is not a UnityEngine.Component.");
                        }
                    }
                }
                else
                {
                    if (Instance.modSettingsConsoleDebug.Value && entity != null) MelonLogger.Msg($"StorageEntity '{entity.StorageEntityName}' is not a UnityEngine.Component or its GameObject is null.");
                }
            }
        }

        private System.Collections.IEnumerator WaitAndSubscribe()
        {
            while (StorageMenu.Instance == null)
                yield return null;

            StorageMenu.Instance.onClosed.AddListener((UnityAction)OnStorageMenuClosed);
        }

        private void OnStorageMenuClosed()
        {
            if (!Instance.modSettingsShowInputLabel.Value) return;
            Instance.customStorageInputField.gameObject.SetActive(false); //Hide input field
            Color color = Instance.customStorageInputField.GetComponent<Image>().color;

            try
            {
                if (Instance.wasStorageRackOpen)
                {

                    string labelText = Instance.customStorageInputField.text;
                    labelText = Regex.Replace(labelText, @"[\n\r]", "");
                    string guid = Instance.openEntityGameObjectGUID;
                    if (Instance.modSettingsConsoleDebug.Value) MelonLogger.Msg($"Saved label: {labelText} for GUID: {guid}");

                    if (!string.IsNullOrEmpty(guid))
                    {
                        if (!Instance.unsavedLabelData.Labels.ContainsKey(guid))
                        {
                            if (Instance.modSettingsConsoleDebug.Value) MelonLogger.Msg($"No unsavedLabelData entry, creating new. Color: {ColorUtility.ToHtmlStringRGBA(color)}");

                            Instance.unsavedLabelData.Labels[guid] = new LabelInfo("", ColorUtility.ToHtmlStringRGBA(Color.white));
                        }
                        Instance.unsavedLabelData.Labels[guid].Text = labelText;
                        Instance.unsavedLabelData.Labels[guid].Color = ColorUtility.ToHtmlStringRGBA(color);

                        Instance.UpdateLabelPrefabInGameObject(guid, labelText, Instance.openStorageGameObject, Instance.openEntityName, color);
                    }

                    Instance.wasStorageRackOpen = false; //Reset Flag
                    LabelTracker.UpdateLabelText(guid, labelText);
                }

            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Error during menu close: {ex.Message}");
            }


        }

        [HarmonyPatch(typeof(MixingStationCanvas), nameof(MixingStationCanvas.Open))]
        public static class MixingStationCanvas_Open_Patch
        {
            [HarmonyPostfix]
            public static void Postfix(MixingStationCanvas __instance, MixingStation station)
            {
                if (!Instance.modSettingsShowInputLabel.Value) return;
                Instance.wasStationOpen = true;

                if (Instance.modSettingsConsoleDebug.Value) MelonLogger.Msg($"MixingStationCanvas opened for: {station?.name}");

                if (station != null && station.gameObject != null)
                {
                    try
                    {
                        Instance.openStorageGameObject = station.gameObject;
                        Instance.openEntityName = station.name;

                        var mixingStationMk2 = Instance.openStorageGameObject.GetComponent<MixingStationMk2>();
                        var mixingStation = Instance.openStorageGameObject.GetComponent<MixingStation>();
                        if (mixingStationMk2 != null)
                        {
                            Instance.openEntityGameObjectGUID = mixingStationMk2.GUID.ToString();
                            if (Instance.modSettingsConsoleDebug.Value) MelonLogger.Msg($"MixingStationMk2 GUID: {Instance.openEntityGameObjectGUID}");
                        }
                        else if (mixingStation != null)
                        {
                            Instance.openEntityGameObjectGUID = mixingStation.GUID.ToString();
                            if (Instance.modSettingsConsoleDebug.Value) MelonLogger.Msg($"MixingStation GUID: {Instance.openEntityGameObjectGUID}");
                        }
                        else
                        {
                            if (Instance.modSettingsConsoleDebug.Value) MelonLogger.Warning($"MixingStationMk2 or MixingStation component not found on {station.name}");
                            Instance.openEntityGameObjectGUID = null;
                        }

                        if (!string.IsNullOrEmpty(Instance.openEntityGameObjectGUID))
                        {

                            Instance.customStationInputFields["MixingStation"].gameObject.SetActive(true);


                            RectTransform inputRT = Instance.customStationInputFields["MixingStation"].GetComponent<RectTransform>();
                            inputRT.anchoredPosition = new Vector2(0, 120);


                            if (Instance.unsavedLabelData.Labels.TryGetValue(Instance.openEntityGameObjectGUID, out LabelInfo existingUnsavedLabel))
                            {
                                Instance.customStationInputFields["MixingStation"].text = existingUnsavedLabel.Text;
                                ColorUtility.TryParseHtmlString("#" + existingUnsavedLabel.Color, out Color color);
                                Instance.customStationInputFields["MixingStation"].GetComponent<Image>().color = color;
                                if (Instance.modSettingsConsoleDebug.Value) MelonLogger.Msg($"Loaded existing unsaved label: {existingUnsavedLabel}");
                            }
                            else if (Instance.labelData.Labels.TryGetValue(Instance.openEntityGameObjectGUID, out LabelInfo existingLabel))
                            {
                                Instance.customStationInputFields["MixingStation"].text = existingLabel.Text;
                                ColorUtility.TryParseHtmlString("#" + existingLabel.Color, out Color color);
                                Instance.customStationInputFields["MixingStation"].GetComponent<Image>().color = color;
                                if (Instance.modSettingsConsoleDebug.Value)
                                    if (Instance.modSettingsConsoleDebug.Value) MelonLogger.Msg($"Loaded existing label: {existingLabel}");
                            }
                            else
                            {
                                Instance.customStationInputFields["MixingStation"].text = string.Empty;
                                Instance.customStationInputFields["MixingStation"].GetComponent<Image>().color = Color.white;
                            }

                            if (Instance.modSettingsAutoFocusLabel.Value)
                            {
                                Instance.customStationInputFields["MixingStation"].Select();
                                Instance.customStationInputFields["MixingStation"].ActivateInputField();
                            }
                            LabelTracker.TrackStorage(Instance.openEntityGameObjectGUID, station.gameObject, Instance.customStationInputFields["MixingStation"].text.ToString());
                        }
                    }
                    catch (Exception ex)
                    {
                        if (Instance.modSettingsConsoleDebug.Value) MelonLogger.Error($"Error in MixingStationCanvas_Open_Patch: {ex.Message}");
                    }
                }
            }
        }

        [HarmonyPatch(typeof(MixingStationCanvas), nameof(MixingStationCanvas.Close))]
        public static class MixingStationCanvas_Close_Patch
        {
            [HarmonyPostfix]
            public static void Postfix(MixingStationCanvas __instance, bool enablePlayerControl)
            {
                if (!Instance.modSettingsShowInputLabel.Value) return;
                if (!Instance.wasStationOpen) return;
                try
                {
                    Color color = Instance.customStationInputFields["MixingStation"].GetComponent<Image>().color;
                    Instance.customStationInputFields["MixingStation"].gameObject.SetActive(false);

                    string labelText = Instance.customStationInputFields["MixingStation"].text;
                    labelText = Regex.Replace(labelText, @"[\n\r]", "");
                    string guid = Instance.openEntityGameObjectGUID;

                    if (!string.IsNullOrEmpty(guid))
                    {
                        if (Instance.modSettingsConsoleDebug.Value) MelonLogger.Msg($"Saving mixing station label: {labelText} for GUID: {guid}");
                        if (!Instance.unsavedLabelData.Labels.ContainsKey(guid))
                        {
                            if (Instance.modSettingsConsoleDebug.Value) MelonLogger.Msg($"No unsavedLabelData entry, creating new. Color: {ColorUtility.ToHtmlStringRGBA(color)}");

                            Instance.unsavedLabelData.Labels[guid] = new LabelInfo("", ColorUtility.ToHtmlStringRGBA(Color.white));
                        }
                        Instance.unsavedLabelData.Labels[guid].Text = labelText;
                        Instance.unsavedLabelData.Labels[guid].Color = ColorUtility.ToHtmlStringRGBA(color);
                        Instance.UpdateLabelPrefabInGameObject(guid, labelText, Instance.openStorageGameObject, Instance.openEntityName, color);

                        LabelTracker.UpdateLabelText(guid, labelText);
                    }
                }
                catch (Exception ex)
                {
                    Instance.LoggerInstance.Error($"Error during MixingStationCanvas close: {ex.Message}");
                }
            }
        }

        [HarmonyPatch(typeof(ChemistryStationCanvas), nameof(ChemistryStationCanvas.Open))]
        public static class ChemistryStationCanvas_Open_Patch
        {
            [HarmonyPostfix]
            public static void Postfix(ChemistryStationCanvas __instance, ChemistryStation station)
            {
                if (!Instance.modSettingsShowInputLabel.Value) return;
                Instance.wasStationOpen = true;
                if (Instance.modSettingsConsoleDebug.Value) MelonLogger.Msg($"ChemistryStationCanvas opened for: {station?.name}");

                if (station != null && station.gameObject != null)
                {
                    try
                    {
                        Instance.openStorageGameObject = station.gameObject;
                        Instance.openEntityName = station.name;

                        // Try to get GUID

                        var chemistryStation = Instance.openStorageGameObject.GetComponent<ChemistryStation>();
                        if (chemistryStation != null)
                        {
                            Instance.openEntityGameObjectGUID = chemistryStation.GUID.ToString();
                            if (Instance.modSettingsConsoleDebug.Value) MelonLogger.Msg($"ChemistryStation GUID: {Instance.openEntityGameObjectGUID}");
                        }
                        else
                        {
                            if (Instance.modSettingsConsoleDebug.Value) MelonLogger.Warning($"ChemistryStation component not found on {station.name}");
                            Instance.openEntityGameObjectGUID = null;
                        }

                        if (!string.IsNullOrEmpty(Instance.openEntityGameObjectGUID))
                        {

                            Instance.customStationInputFields["ChemistryStation"].gameObject.SetActive(true);

                            // Position the input field relative to ChemistryStation UI
                            RectTransform inputRT = Instance.customStationInputFields["ChemistryStation"].GetComponent<RectTransform>();
                            inputRT.anchoredPosition = new Vector2(0, 120); // Adjust as needed

                            // Load existing label if it exists
                            if (Instance.unsavedLabelData.Labels.TryGetValue(Instance.openEntityGameObjectGUID, out LabelInfo existingUnsavedLabel))
                            {
                                Instance.customStationInputFields["ChemistryStation"].text = existingUnsavedLabel.Text;
                                ColorUtility.TryParseHtmlString("#" + existingUnsavedLabel.Color, out Color color);
                                Instance.customStationInputFields["ChemistryStation"].GetComponent<Image>().color = color;
                                if (Instance.modSettingsConsoleDebug.Value) MelonLogger.Msg($"Loaded existing unsaved label: {existingUnsavedLabel}");
                            }
                            else if (Instance.labelData.Labels.TryGetValue(Instance.openEntityGameObjectGUID, out LabelInfo existingLabel))
                            {
                                Instance.customStationInputFields["ChemistryStation"].text = existingLabel.Text;
                                ColorUtility.TryParseHtmlString("#" + existingLabel.Color, out Color color);
                                Instance.customStationInputFields["ChemistryStation"].GetComponent<Image>().color = color;
                                if (Instance.modSettingsConsoleDebug.Value) MelonLogger.Msg($"Loaded existing label: {existingLabel}");
                            }
                            else
                            {
                                Instance.customStationInputFields["ChemistryStation"].text = string.Empty;
                                Instance.customStationInputFields["ChemistryStation"].GetComponent<Image>().color = Color.white;
                            }

                            if (Instance.modSettingsAutoFocusLabel.Value)
                            {
                                Instance.customStationInputFields["ChemistryStation"].Select();
                                Instance.customStationInputFields["ChemistryStation"].ActivateInputField();
                            }
                            LabelTracker.TrackStorage(Instance.openEntityGameObjectGUID, station.gameObject, Instance.customStationInputFields["ChemistryStation"].text.ToString());
                        }
                    }
                    catch (Exception ex)
                    {
                        if (Instance.modSettingsConsoleDebug.Value) MelonLogger.Error($"Error in ChemistryStationCanvas_Open_Patch: {ex.Message}");
                    }
                }
            }
        }

        [HarmonyPatch(typeof(ChemistryStationCanvas), nameof(ChemistryStationCanvas.Close))]
        public static class ChemistryStationCanvas_Close_Patch
        {
            [HarmonyPostfix]
            public static void Postfix(ChemistryStationCanvas __instance, bool removeUI)
            {
                if (!Instance.modSettingsShowInputLabel.Value) return;
                if (!Instance.wasStationOpen) return;
                try
                {
                    Instance.customStationInputFields["ChemistryStation"].gameObject.SetActive(false);
                    Color color = Instance.customStationInputFields["ChemistryStation"].GetComponent<Image>().color;
                    string labelText = Instance.customStationInputFields["ChemistryStation"].text;
                    labelText = Regex.Replace(labelText, @"[\n\r]", "");
                    string guid = Instance.openEntityGameObjectGUID;

                    if (!string.IsNullOrEmpty(guid))
                    {
                        if (Instance.modSettingsConsoleDebug.Value) MelonLogger.Msg($"Saving chemistry station label: {labelText} for GUID: {guid}");
                        if (!Instance.unsavedLabelData.Labels.ContainsKey(guid))
                        {

                            if (Instance.modSettingsConsoleDebug.Value) MelonLogger.Msg($"No unsavedLabelData entry, creating new. Color: {ColorUtility.ToHtmlStringRGBA(color)}");
                            Instance.unsavedLabelData.Labels[guid] = new LabelInfo("", ColorUtility.ToHtmlStringRGBA(Color.white));
                        }
                        Instance.unsavedLabelData.Labels[guid].Text = labelText;
                        Instance.unsavedLabelData.Labels[guid].Color = ColorUtility.ToHtmlStringRGBA(color);
                        Instance.UpdateLabelPrefabInGameObject(guid, labelText, Instance.openStorageGameObject, Instance.openEntityName, color);

                        LabelTracker.UpdateLabelText(guid, labelText);
                    }
                }
                catch (Exception ex)
                {
                    Instance.LoggerInstance.Error($"Error during ChemistryStationCanvas close: {ex.Message}");
                }
            }
        }

        private void StationCanvasOnOpenHandler(GridItem station)
        {

            if (!Instance.modSettingsShowInputLabel.Value) return;
            wasStationOpen = true;
            openStorageGameObject = station.gameObject;
            string GUID = station.GUID.ToString();
            string stationName = station.name.ToString().Replace("(Clone)", "").Trim().Replace("_Built", "").Trim();
            if (stationName == "PackagingStation_Mk2") stationName = "PackagingStation";

            // Show and position the input field
            Instance.customStationInputFields[stationName].gameObject.SetActive(true);

            // Position the input field relative to MixingStation UI
            RectTransform inputRT = Instance.customStationInputFields[stationName].GetComponent<RectTransform>();
            inputRT.anchoredPosition = new Vector2(0, 120); // Adjust as needed

            // Load existing label if it exists
            if (Instance.unsavedLabelData.Labels.TryGetValue(GUID, out LabelInfo existingUnsavedLabel))
            {
                Instance.customStationInputFields[stationName].text = existingUnsavedLabel.Text;
                ColorUtility.TryParseHtmlString("#" + existingUnsavedLabel.Color, out Color color);
                Instance.customStationInputFields[stationName].GetComponent<Image>().color = color;
                if (Instance.modSettingsConsoleDebug.Value) MelonLogger.Msg($"Loaded existing unsaved label: {existingUnsavedLabel}");
            }
            else if (Instance.labelData.Labels.TryGetValue(GUID, out LabelInfo existingLabel))
            {
                Instance.customStationInputFields[stationName].text = existingLabel.Text;
                ColorUtility.TryParseHtmlString("#" + existingLabel.Color, out Color color);
                Instance.customStationInputFields[stationName].GetComponent<Image>().color = color;
                if (Instance.modSettingsConsoleDebug.Value) MelonLogger.Msg($"Loaded existing label: {existingLabel}");
            }
            else
            {
                Instance.customStationInputFields[stationName].text = string.Empty;
                Instance.customStationInputFields[stationName].GetComponent<Image>().color = Color.white;
            }

            if (Instance.modSettingsAutoFocusLabel.Value)
            {
                Instance.customStationInputFields[stationName].Select();
                Instance.customStationInputFields[stationName].ActivateInputField();
            }
            LabelTracker.TrackStorage(GUID, station.gameObject, Instance.customStorageInputField.text.ToString());
        }

        private void StationCanvasOnCloseHandler(GridItem station)
        {
            if (!Instance.modSettingsShowInputLabel.Value) return;
            if (!wasStationOpen) return;
            string guid = station.GUID.ToString();
            string stationName = station.name.ToString().Replace("(Clone)", "").Trim().Replace("_Built", "").Trim();
            if (stationName == "PackagingStation_Mk2") stationName = "PackagingStation";


            try
            {
                Instance.customStationInputFields[stationName].gameObject.SetActive(false);
                Color color = Instance.customStationInputFields[stationName].GetComponent<Image>().color;

                string labelText = Instance.customStationInputFields[stationName].text;
                labelText = Regex.Replace(labelText, @"[\n\r]", "");

                if (!string.IsNullOrEmpty(guid))
                {
                    if (Instance.modSettingsConsoleDebug.Value) MelonLogger.Msg($"Saving mixing station label: {labelText} for GUID: {guid}");
                    if (!Instance.unsavedLabelData.Labels.ContainsKey(guid))
                    {
                        if (Instance.modSettingsConsoleDebug.Value) MelonLogger.Msg($"No unsavedLabelData entry, creating new. Color: {ColorUtility.ToHtmlStringRGBA(color)}");
                        Instance.unsavedLabelData.Labels[guid] = new LabelInfo("", ColorUtility.ToHtmlStringRGBA(Color.white));
                    }
                    Instance.unsavedLabelData.Labels[guid].Text = labelText;
                    Instance.unsavedLabelData.Labels[guid].Color = ColorUtility.ToHtmlStringRGBA(color);

                    Instance.UpdateLabelPrefabInGameObject(guid, labelText, station.gameObject, stationName, color);

                    LabelTracker.UpdateLabelText(guid, labelText);
                }
            }
            catch (Exception ex)
            {
                Instance.LoggerInstance.Error($"Error during MixingStationCanvas close: {ex.Message}");
            }
            wasStationOpen = false;
        }

        [HarmonyPatch]
        public static class StationCanvas_SetIsOpen_Patch
        {
            private static void HandleSetIsOpen<T>(T station, bool open, string stationName, bool removeUI = false) where T : GridItem
            {
                if (open)
                {
                    Instance.openStationGameObject = station;
                    Instance.StationCanvasOnOpenHandler(station);
                }
                else
                {
                    Instance.StationCanvasOnCloseHandler(Instance.openStationGameObject);
                }
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(PackagingStationCanvas), nameof(PackagingStationCanvas.SetIsOpen))]
            public static void PackagingStationCanvas_Postfix(PackagingStation station, bool open, bool removeUI)
            {
                HandleSetIsOpen(station, open, "PackagingStation", removeUI);
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(DryingRackCanvas), nameof(DryingRackCanvas.SetIsOpen))]
            public static void DryingRackCanvas_Postfix(DryingRack rack, bool open)
            {
                HandleSetIsOpen(rack, open, "DryingRack");
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(BrickPressCanvas), nameof(BrickPressCanvas.SetIsOpen))]
            public static void BrickPressCanvas_Postfix(BrickPress press, bool open, bool removeUI)
            {
                HandleSetIsOpen(press, open, "BrickPress", removeUI);
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(CauldronCanvas), nameof(CauldronCanvas.SetIsOpen))]
            public static void CauldronCanvas_Postfix(Cauldron cauldron, bool open, bool removeUI)
            {
                HandleSetIsOpen(cauldron, open, "Cauldron", removeUI);
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(LabOvenCanvas), nameof(LabOvenCanvas.SetIsOpen))]
            public static void LabOvenCanvas_Postfix(LabOven oven, bool open, bool removeUI)
            {
                HandleSetIsOpen(oven, open, "LabOven", removeUI);
            }
        }





        private TMP_InputField CreateInputField(GameObject parentUI, Vector2 vector)
        {
            try
            {
                GameObject inputFieldGameObject = new GameObject("CustomInputField");
                inputFieldGameObject.layer = 5; // UI layer
                inputFieldGameObject.transform.SetParent(parentUI.transform, false);

                RectTransform parentRectTransform = parentUI.GetComponent<RectTransform>();
                RectTransform rectTransform = inputFieldGameObject.AddComponent<RectTransform>();
                rectTransform.anchorMin = vector;
                rectTransform.anchorMax = vector;
                rectTransform.pivot = new Vector2(0.5f, 0.5f);
                rectTransform.sizeDelta = new Vector2(550, 50);

                float inputFieldHeight = parentRectTransform.rect.height;
                rectTransform.anchoredPosition = new Vector2(0, -inputFieldHeight / 2);

                Image bg = inputFieldGameObject.AddComponent<Image>();
                bg.color = new Color(1, 1, 1, 0.9f);

                Outline outline = inputFieldGameObject.AddComponent<Outline>();
                outline.effectColor = new Color(0, 0, 0, 0.5f);
                outline.effectDistance = new Vector2(2, 2);

                GameObject textAreaGO = new GameObject("TextArea");
                RectTransform textRT = textAreaGO.AddComponent<RectTransform>();
                textRT.SetParent(inputFieldGameObject.transform, false);
                textRT.anchorMin = Vector2.zero;
                textRT.anchorMax = Vector2.one;
                textRT.offsetMin = new Vector2(20, 10);
                textRT.offsetMax = new Vector2(-20, -10);

                GameObject placeholderGO = new GameObject("Placeholder");
                RectTransform placeholderRT = placeholderGO.AddComponent<RectTransform>();
                placeholderRT.SetParent(inputFieldGameObject.transform, false);
                placeholderRT.anchorMin = Vector2.zero;
                placeholderRT.anchorMax = Vector2.one;
                placeholderRT.offsetMin = new Vector2(20, 10);
                placeholderRT.offsetMax = new Vector2(-20, -10);

                TMP_InputField tempInputField = new TMP_InputField();
                tempInputField = inputFieldGameObject.AddComponent<TMP_InputField>();

                var textComponent = textAreaGO.AddComponent<TextMeshProUGUI>();
                textComponent.fontSize = 24; // Larger font
                textComponent.color = Color.black;
                textComponent.alignment = TextAlignmentOptions.Left;
                textComponent.enableWordWrapping = false;

                var placeholderText = placeholderGO.AddComponent<TextMeshProUGUI>();
                placeholderText.text = "Label";
                placeholderText.fontSize = 24; // Larger font
                placeholderText.color = new Color(0.5f, 0.5f, 0.5f);
                placeholderText.alignment = TextAlignmentOptions.Left;
                placeholderText.enableWordWrapping = false;

                tempInputField.textViewport = textRT;
                tempInputField.textComponent = textComponent;
                tempInputField.placeholder = placeholderText;
                tempInputField.characterLimit = 30;

                tempInputField.contentType = TMP_InputField.ContentType.Standard;

                inputFieldGameObject.SetActive(false); //Initialize hidden
                LoggerInstance.Msg($"Custom {parentUI.name} input field created successfully");

                return tempInputField;
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Failed to create input field: {ex.Message}");
                return null;
            }
        }

        private GameObject CreateColorPicker(TMP_InputField inputField)
        {
            try
            {
                // Create the color picker container
                GameObject colorPickerGO = new GameObject("ColorPicker");
                colorPickerGO.layer = 5; // UI layer
                colorPickerGO.transform.SetParent(inputField.transform, false);

                // Set up the RectTransform
                RectTransform inputFieldRT = inputField.GetComponent<RectTransform>();
                RectTransform rectTransform = colorPickerGO.AddComponent<RectTransform>();
                rectTransform.anchorMin = new Vector2(0.5f, 1f); // Anchor to the top-middle
                rectTransform.anchorMax = new Vector2(0.5f, 1f);
                rectTransform.pivot = new Vector2(0.5f, 1f); // Pivot at the top-middle
                rectTransform.sizeDelta = new Vector2(-130, 40);

                // Calculate the vertical position to be below the input field
                float inputFieldHeight = inputFieldRT.rect.height;
                rectTransform.anchoredPosition = new Vector2(0, -inputFieldHeight / 2 -30); // Position 30 units below the bottom of the input field

                // Add background (optional)
                Image bg = colorPickerGO.AddComponent<Image>();
                bg.color = new Color(1, 1, 1, 0.7f);

                // Define colors to display (10 colors)
                List<Color> colors = new List<Color>();

                foreach (var pair in modSettingsColors)
                {
                    ColorUtility.TryParseHtmlString("#" + pair.Value.Value, out Color color);
                    colors.Add(color);
                }

                // Create color buttons
                float buttonSize = 30f;
                float spacing = 10f;
                float totalWidth = (colors.Count * buttonSize) + ((colors.Count - 1) * spacing);
                float startX = -totalWidth / 2 + buttonSize / 2;

                for (int i = 0; i < colors.Count; i++)
                {
                    GameObject colorButtonGO = new GameObject($"ColorButton_{i}");
                    colorButtonGO.layer = 5; // UI layer
                    colorButtonGO.transform.SetParent(colorPickerGO.transform, false);

                    // Set up button transform
                    RectTransform buttonRT = colorButtonGO.AddComponent<RectTransform>();
                    buttonRT.sizeDelta = new Vector2(buttonSize, buttonSize);
                    buttonRT.anchoredPosition = new Vector2(startX + i * (buttonSize + spacing), 0);

                    // Add image component
                    Image buttonImage = colorButtonGO.AddComponent<Image>();
                    buttonImage.color = colors[i];

                    // Add button component
                    Button button = colorButtonGO.AddComponent<Button>();
                    button.onClick.AddListener((UnityAction)(() => onColorPickerColorSelect(buttonImage.color)));

                    // Add outline to make buttons more visible
                    Outline outline = colorButtonGO.AddComponent<Outline>();
                    outline.effectColor = Color.black;
                    outline.effectDistance = new Vector2(1, 1);
                }

                // Initially hide the color picker
                colorPickerGO.SetActive(true);
                LoggerInstance.Msg("Color picker created successfully");

                return colorPickerGO;
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Failed to create color picker: {ex.Message}");
                return null;
            }
        }

        private void onColorPickerColorSelect(Color buttonImage)
        {
            var imageComponent = Instance.customStorageInputField.IsActive() ? Instance.customStorageInputField.GetComponent<Image>() : null;
            if (imageComponent == null)
            {
                foreach (var kvp in Instance.customStationInputFields)
                {
                    TMP_InputField inputField = kvp.Value; // Access the value of the KeyValuePair
                    if (inputField.IsActive())
                    {
                        imageComponent = inputField.GetComponent<Image>();
                        if (Instance.modSettingsConsoleDebug.Value) MelonLogger.Msg($"Changed color to: {buttonImage.ToString()}");
                    }
                }
            }
            else
            {
                if (Instance.modSettingsConsoleDebug.Value) MelonLogger.Msg($"Changed color to: {buttonImage.ToString()}");
            }


            if (imageComponent != null)
            {
                imageComponent.color = buttonImage;
            }
            else
            {
                LoggerInstance.Warning("Image component not found on openStorageGameObject.");
            }
        }


        private void EnsureDataDirectoryExists()
        {
            try
            {
                if (!Directory.Exists(dataFolderPath))
                {
                    Directory.CreateDirectory(dataFolderPath);
                    LoggerInstance.Msg("Created SimpleLabels data directory");
                }
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Failed to create data directory: {ex.Message}");
            }
        }

        private void LoadLabelData()
        {
            try
            {
                if (File.Exists(labelDataFilePath))
                {
                    string json = File.ReadAllText(labelDataFilePath);
                    labelData = JsonConvert.DeserializeObject<LabelData>(json) ?? new LabelData();
                    LoggerInstance.Msg($"Loaded label data with {labelData.Labels.Count} entries");

                }
                else
                {
                    labelData = new LabelData();
                    LoggerInstance.Msg("Created new empty label data file");
                }
            }
            catch (Exception ex)
            {
                labelData = new LabelData();
                LoggerInstance.Error($"Failed to load label data: {ex.Message}");
                LoggerInstance.Warning("Using empty label data");
            }
        }

        private void SaveLabelData()
        {
            try
            {
                string json = JsonConvert.SerializeObject(labelData, Formatting.Indented);
                File.WriteAllText(labelDataFilePath, json);
                LoggerInstance.Msg($"Saved {labelData.Labels.Count} label entries");
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Failed to save label data: {ex.Message}");
            }
        }

        public void InitializeLabelPrefab()
        {
            try
            {
                Instance.labelPrefab = new GameObject("LabelPrefab");
                Instance.labelPrefab.SetActive(false);
                Instance.labelPrefab.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);

                GameObject labelObject = new GameObject("LabelObject");
                labelObject.transform.SetParent(Instance.labelPrefab.transform);
                labelObject.transform.localPosition = Vector3.zero;
                labelObject.transform.localScale = Vector3.one;

                GameObject paperBackground = GameObject.CreatePrimitive(PrimitiveType.Cube);
                paperBackground.name = "PaperBackground";
                paperBackground.transform.SetParent(labelObject.transform);
                paperBackground.transform.localPosition = Vector3.zero;
                paperBackground.transform.localScale = new Vector3(2f, 0.6f, 0.1f);

                Material whiteMatte = new Material(Shader.Find("Universal Render Pipeline/Simple Lit"));

                if (whiteMatte != null)
                {
                    paperBackground.GetComponent<Renderer>().material = whiteMatte;
                }
                else
                {
                    LoggerInstance.Error("Couldn't find material to reuse.");
                }

                GameObject textObject = new GameObject("LabelText");
                textObject.transform.SetParent(labelObject.transform);
                textObject.transform.localPosition = new Vector3(0, 0, -0.051f);
                textObject.transform.localScale = Vector3.one;

                TextMeshPro textMesh = textObject.AddComponent<TextMeshPro>();
                textMesh.fontSizeMin = 1.4f;
                textMesh.fontSizeMax = 3;
                textMesh.fontSize = 2;
                textMesh.fontStyle = FontStyles.Bold;
                textMesh.enableAutoSizing = true;
                textMesh.alignment = TextAlignmentOptions.Center;
                textMesh.color = Color.black;
                textMesh.enableWordWrapping = true;
                textMesh.margin = new Vector4(0.1f, 0.1f, 0.1f, 0.1f);

                RectTransform textRect = textObject.GetComponent<RectTransform>();
                // Keep the initial size, but the text will expand it vertically if needed
                textRect.sizeDelta = new Vector2(1.8f, 0.5f);
                textRect.anchorMin = new Vector2(0.5f, 0.5f);
                textRect.anchorMax = new Vector2(0.5f, 0.5f);
                textRect.pivot = new Vector2(0.5f, 0.5f);

                LoggerInstance.Msg("Label prefab initialized successfully");
            }
            catch (Exception ex)
            {
                LoggerInstance.Msg($"Failed to initialize label prefab: {ex.Message}");
            }


        }


        [HarmonyPatch(typeof(GridItemLoader), "LoadAndCreate")]
        [HarmonyPatch(new Type[] { typeof(string) })]
        class GridItemLoaderPatch //Initial ground loaded Entities
        {
            static void Postfix(GridItem __result, string mainPath)
            {
                if (__result != null)
                {
                    GameObject GO = __result.gameObject;
                    string objectName = __result.name;
                    string objectGuid = __result.GUID.ToString();

                    if (Instance.modSettingsConsoleDebug.Value) MelonLogger.Msg($"GridItemLoaderPatch objectName: {objectName}");
                    if (Instance.modSettingsConsoleDebug.Value) MelonLogger.Warning($"Loading ObjectName: {objectName}");
                    if (Instance.IsValidStorageName(objectName))
                    {
                        if (Instance.modSettingsConsoleDebug.Value) MelonLogger.Msg($"Processing object name: {objectName} (GUID: {objectGuid})");

                        try
                        {
                            string initialLabelText = Instance.labelData.Labels.TryGetValue(objectGuid, out LabelInfo labelInfo) ? labelInfo.Text : "";

                            Color color;
                            try
                            {
                                ColorUtility.TryParseHtmlString("#" + labelInfo.Color, out color);
                            }
                            catch
                            {
                                color = Color.white;
                            }

                            Instance.AddLabelPrefabToGameObject(GO, objectName, objectGuid, color);
                            LabelTracker.TrackStorage(objectGuid, GO, initialLabelText);
                        }
                        catch (Exception ex)
                        {
                            Instance.LoggerInstance.Error($"Failed to process storage rack {objectName}: {ex.Message}");
                        }

                    }
                }
            }
        }

        [HarmonyPatch(typeof(SurfaceItemLoader), "LoadAndCreate")]
        [HarmonyPatch(new Type[] { typeof(string) })]
        class SurfaceItemLoaderPatch //Initial wall loaded Entities
        {
            static void Postfix(SurfaceItem __result, string mainPath)
            {
                if (__result != null)
                {
                    GameObject GO = __result.gameObject;
                    string objectName = __result.name;
                    string objectGuid = __result.GUID.ToString();

                    if (Instance.modSettingsConsoleDebug.Value) MelonLogger.Msg($"SurfaceItemLoaderPatch objectName: {objectName}");

                    if (objectName.Contains("WallMountedShelf"))
                    {
                        if (Instance.modSettingsConsoleDebug.Value) MelonLogger.Msg($"Processing storage rack: {objectName} (GUID: {objectGuid})");

                        try
                        {
                            string initialLabelText = Instance.labelData.Labels.TryGetValue(objectGuid, out LabelInfo labelInfo) ? labelInfo.Text : "";
                            Color color;
                            try
                            {
                                ColorUtility.TryParseHtmlString("#" + labelInfo.Color, out  color);
                            }
                            catch
                            {
                                color = Color.white;
                            }
                            
                            
                            
                            Instance.AddLabelPrefabToGameObject(GO, objectName, objectGuid, color);
                            LabelTracker.TrackStorage(objectGuid, GO, initialLabelText);
                        }
                        catch (Exception ex)
                        {
                            Instance.LoggerInstance.Error($"Failed to process storage rack {objectName}: {ex.Message}");
                        }

                    }
                }
            }
        }

        public void AddLabelPrefabToGameObject(GameObject parentGO, string parentOBName, string parentOBGUID, Color color)
        {
            try
            {
                // Validate inputs (as before)
                if (parentGO == null)
                {
                    LoggerInstance.Error("Cannot add label prefab - parent GameObject is null");
                    return;
                }

                if (string.IsNullOrEmpty(parentOBGUID))
                {
                    LoggerInstance.Warning($"Cannot add label prefab - parent GameObject GUID is empty/null");
                    return;
                }

                if (string.IsNullOrEmpty(parentOBName))
                {
                    LoggerInstance.Warning($"Cannot add label prefab - parent GameObject Name is empty/null");
                    return;
                }

                if (Instance.modSettingsConsoleDebug.Value) MelonLogger.Msg($"Adding labels to {parentOBName} (GUID: {parentOBGUID})");

                // Get the base object name (remove "(Clone)" if present)
                string baseObjectName = parentOBName.Replace("(Clone)", "").Trim();

                // Define label configurations for different object names
                Dictionary<string, List<LabelSideConfig>> labelConfigurations = new Dictionary<string, List<LabelSideConfig>>()
        {
            {
                "StorageRack_Small", new List<LabelSideConfig>()
                {
                    new LabelSideConfig(new Vector3(0f, 0.750f, -0.254f), Quaternion.identity),       // Front
                    new LabelSideConfig(new Vector3(0.50f, 0.750f, 0f), Quaternion.Euler(0, -90, 0)),  // Right
                    new LabelSideConfig(new Vector3(0f, 0.750f, 0.254f), Quaternion.Euler(0, 180, 0)), // Back
                    new LabelSideConfig(new Vector3(-0.50f, 0.750f, 0f), Quaternion.Euler(0, 90, 0))     // Left
                }
            },
            {
                "StorageRack_Medium", new List<LabelSideConfig>()
                {
                    new LabelSideConfig(new Vector3(0f, 0.750f, -0.254f), Quaternion.identity),       // Front
                    new LabelSideConfig(new Vector3(0.75f, 0.750f, 0f), Quaternion.Euler(0, -90, 0)),  // Right
                    new LabelSideConfig(new Vector3(0f, 0.750f, 0.254f), Quaternion.Euler(0, 180, 0)), // Back
                    new LabelSideConfig(new Vector3(-0.75f, 0.750f, 0f), Quaternion.Euler(0, 90, 0))     // Left
                }
            },
            {
                "StorageRack_Large", new List<LabelSideConfig>()
                {
                    new LabelSideConfig(new Vector3(0f, 0.750f, -0.254f), Quaternion.identity),       // Front
                    new LabelSideConfig(new Vector3(1f, 0.750f, 0f), Quaternion.Euler(0, -90, 0)),     // Right
                    new LabelSideConfig(new Vector3(0f, 0.750f, 0.254f), Quaternion.Euler(0, 180, 0)),  // Back
                    new LabelSideConfig(new Vector3(-1f, 0.750f, 0f), Quaternion.Euler(0, 90, 0))      // Left
                }
            },
            {
                "WallMountedShelf_Built", new List<LabelSideConfig>()
                {
                    new LabelSideConfig(new Vector3(0f, 0.255f, 0.43f), Quaternion.Euler(0, 180, 0))        // Front
                }
            },
            {
                "MixingStationMk2_Built", new List<LabelSideConfig>()
                {
                    new LabelSideConfig(new Vector3(-0.7f, 1.08f, 0.178f), Quaternion.Euler(19, 180, 0)) // Front
                }
            },
            {
                "MixingStation_Built", new List<LabelSideConfig>()
                {
                    new LabelSideConfig(new Vector3(-0.6f, 0.925f, 0.5f), Quaternion.Euler(0, 180, 0)) // Front
                }
            },
            {
                "PackagingStation_Mk2", new List<LabelSideConfig>()
                {
                    new LabelSideConfig(new Vector3(0, 0.925f, 0.504f), Quaternion.Euler(0, 180, 0)) // Front
                }
            },
            {
                "PackagingStation", new List<LabelSideConfig>()
                {
                    new LabelSideConfig(new Vector3(0, 0.925f, 0.504f), Quaternion.Euler(0, 180, 0)) // Front
                }
            },
            {
                "BrickPress", new List<LabelSideConfig>()
                {
                    new LabelSideConfig(new Vector3(0, 0.96f, 0.405f), Quaternion.Euler(0, 180, 0)) // Front
                }
            },
            {
                "Cauldron_Built", new List<LabelSideConfig>()
                {
                    new LabelSideConfig(new Vector3(0, 0.305f, 0.425f), Quaternion.Euler(90, 180, 0)) // Front
                }
            },
            {
                "ChemistryStation_Built", new List<LabelSideConfig>()
                {
                    new LabelSideConfig(new Vector3(0, 0.925f, 0.504f), Quaternion.Euler(0, 180, 0)) // Front
                }
            },
            {
                "LabOven_Built", new List<LabelSideConfig>()
                {
                    new LabelSideConfig(new Vector3(0, 0.925f, 0.504f), Quaternion.Euler(0, 180, 0)) // Front
                }
            },
            {
                "DryingRack_Built", new List<LabelSideConfig>()
                {
                    new LabelSideConfig(new Vector3(0, 1.92f, -0.03f), Quaternion.Euler(0, 0, 0)), // Front
                    new LabelSideConfig(new Vector3(0, 1.92f, 0.03f), Quaternion.Euler(0, 180, 0)) // Back
                }
            }



                    
            // Add configurations for other object names
        };

                if (labelConfigurations.TryGetValue(baseObjectName, out List<LabelSideConfig> sidesToLabel))
                {
                    int labelsCreated = 0;
                    for (int i = 0; i < sidesToLabel.Count; i++)
                    {
                        try
                        {
                            LabelSideConfig config = sidesToLabel[i];
                            GameObject labelInstance = GameObject.Instantiate(Instance.labelPrefab, parentGO.transform);

                            if (labelInstance == null)
                            {
                                LoggerInstance.Error($"Failed to instantiate label {i + 1} for {parentOBName}");
                                continue;
                            }

                            labelInstance.transform.localPosition = config.Position;
                            labelInstance.transform.localRotation = config.Rotation;
                            labelInstance.name = $"Label_{i + 1}";

                            var textMesh = labelInstance.GetComponentInChildren<TextMeshPro>();
                            if (textMesh == null)
                            {
                                LoggerInstance.Warning($"Failed to find TextMeshPro component on label {i + 1}");
                                continue;
                            }

                            var material = labelInstance.GetComponentInChildren<MeshRenderer>().material;
                            if (material == null)
                            {
                                LoggerInstance.Warning($"Failed to find MeshRenderer component on label {i + 1}");
                                continue;
                            }

                            // Set initial text if we have saved data for this rack
                            if (Instance.unsavedLabelData.Labels.TryGetValue(parentOBGUID, out LabelInfo unsavedLabelText))
                            {
                                textMesh.text = unsavedLabelText.Text;
                                ColorUtility.TryParseHtmlString("#" + unsavedLabelText.Color, out Color unsavedLabelTextColor);
                                material.color = unsavedLabelTextColor;
                                labelInstance.SetActive(true);
                            }
                            else if (Instance.labelData.Labels.TryGetValue(parentOBGUID, out LabelInfo labelText))
                            {
                                textMesh.text = labelText.Text;
                                ColorUtility.TryParseHtmlString("#" + labelText.Color, out Color labelTextColor);
                                material.color = labelTextColor;
                                labelInstance.SetActive(true);
                            }
                            else
                            {
                                labelInstance.SetActive(false);
                            }

                            labelsCreated++;
                        }
                        catch (Exception ex)
                        {
                            LoggerInstance.Error($"Error creating label {i + 1} for {parentOBName}: {ex.Message}");
                        }
                    }

                    if (Instance.modSettingsConsoleDebug.Value) MelonLogger.Msg($"Successfully created {labelsCreated}/{sidesToLabel.Count} labels for {parentOBName}");
                }
                else
                {
                    if (Instance.modSettingsConsoleDebug.Value) MelonLogger.Msg($"No label configuration found for {parentOBName}");
                }
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Failed to add label prefab to {parentOBName}: {ex}");
            }
        }

        // Helper struct to hold position and rotation for a label side
        private struct LabelSideConfig
        {
            public Vector3 Position { get; }
            public Quaternion Rotation { get; }

            public LabelSideConfig(Vector3 position, Quaternion rotation)
            {
                Position = position;
                Rotation = rotation;
            }
        }

        public void UpdateLabelPrefabInGameObject(string GUID, string labelText, GameObject parentGO, string parentName, Color color)
        {
            try
            {
                if (string.IsNullOrEmpty(GUID))
                {
                    LoggerInstance.Warning("UpdateLabelPrefabInGameObject called with empty GUID");
                    return;
                }

                if (parentGO == null)
                {
                    LoggerInstance.Error($"Cannot update labels - parent GameObject is null (GUID: {GUID})");
                    return;
                }

                if (string.IsNullOrEmpty(parentName))
                {
                    LoggerInstance.Warning("UpdateLabelPrefabInGameObject called with empty parentName");
                    return;
                }

                if (Instance.modSettingsConsoleDebug.Value) MelonLogger.Msg($"Updating labels for {parentName} (GUID: {GUID})");

                if (!LabelTracker.StorageEntities.ContainsKey(GUID))
                {
                    LoggerInstance.Warning($"Storage entity {parentName} not tracked (GUID: {GUID})");
                    return;
                }

                // Check for existing label prefabs
                bool hasLabelPrefabs = parentGO.GetComponentsInChildren<Transform>(true)
                    .Any(child => child.gameObject.name == "LabelText");

                if (Instance.modSettingsConsoleDebug.Value) MelonLogger.Msg($"Label prefabs exist: {hasLabelPrefabs}");

                if (!hasLabelPrefabs)
                {
                    AddLabelPrefabToGameObject(parentGO, parentName, GUID, color);
                    return; // New labels will initialize with the correct text
                }

                // Update existing labels
                var labelObjects = parentGO.GetComponentsInChildren<Transform>(true)
                    .Where(child => child.gameObject.name.StartsWith("Label_"));

                foreach (var labelObject in labelObjects)
                {
                    try
                    {
                        if (string.IsNullOrEmpty(labelText))
                        {
                            labelObject.gameObject.SetActive(false);
                            if (Instance.modSettingsConsoleDebug.Value) LoggerInstance.Msg($"Disabled label {labelObject.gameObject.name} (empty text)");
                        }
                        else
                        {
                            labelObject.gameObject.SetActive(true);
                            var material = labelObject.GetComponentInChildren<MeshRenderer>().material;
                            var textMesh = labelObject.GetComponentInChildren<TextMeshPro>(true);

                            if (textMesh != null)
                            {
                                textMesh.text = labelText;
                                if (Instance.modSettingsConsoleDebug.Value) LoggerInstance.Msg($"Updated label {labelObject.gameObject.name} with text: {labelText}");
                                if(material != null)
                                {
                                    material.color = color;
                                }
                                else
                                {
                                    LoggerInstance.Warning($"MeshRenderer component missing on {labelObject.gameObject.name}");
                                }
                            }
                            else
                            {
                                LoggerInstance.Warning($"TextMeshPro component missing on {labelObject.gameObject.name}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LoggerInstance.Error($"Failed to update label {labelObject.gameObject.name}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Failed to update label prefabs for {parentName} (GUID: {GUID}): {ex}");
            }
        }

        private bool IsValidStorageName(string storageName)
        {
            if (Instance.modSettingsConsoleDebug.Value) MelonLogger.Msg($"IsValidStorageName string storageName: {storageName}");

            storageName = storageName.Replace("(Clone)", "").Trim();

            string[] allowedNames =
                {
                "Small Storage Rack", "StorageRack_Small",
                "Medium Storage Rack", "StorageRack_Medium",
                "Large Storage Rack", "StorageRack_Large",
                "Wall-Mounted Shelf", "WallMountedShelf_Built",
                "PackagingStation_Mk2", "PackagingStation",
                "MixingStationMk2_Built", "MixingStation_Built",
                "BrickPress",
                "Cauldron_Built",
                "ChemistryStation_Built",
                "LabOven_Built",
                "DryingRack_Built"


                };
            return allowedNames.Contains(storageName);
        }
        

        public static class LabelTracker
        {
            public static Dictionary<string, Tuple<string, GameObject, string>> StorageEntities = new Dictionary<string, Tuple<string, GameObject, string>>();

            public static void TrackStorage(string guid, GameObject GO, string labelText = "") //GO Not used atm
            {
                if (string.IsNullOrEmpty(guid))
                {
                    Instance.LoggerInstance.Warning("Attempted to track storage with empty GUID");
                    return;
                }

                if (!StorageEntities.ContainsKey(guid))
                {
                    StorageEntities.Add(guid, new Tuple<string, GameObject, string>(guid, GO, labelText));
                }
                else
                {
                    if (Instance.modSettingsConsoleDebug.Value) MelonLogger.Msg($"GUID: {guid} is already tracked");
                }
            }

            public static void UpdateLabelText(string guid, string newLabelText)
            {
                if (StorageEntities.ContainsKey(guid))
                {
                    var storedData = StorageEntities[guid];
                    StorageEntities[guid] = new Tuple<string, GameObject, string>(storedData.Item1, storedData.Item2, newLabelText);
                }
                else
                {
                    Instance.LoggerInstance.Warning($"Attempted to update label for untracked GUID: {guid}");
                }
            }

            public static void UntrackStorage(string guid)
            {
                if (StorageEntities.ContainsKey(guid))
                {
                    StorageEntities.Remove(guid);
                }
                else if (Instance.modSettingsConsoleDebug.Value)
                {
                    Instance.LoggerInstance.Msg($"Attempted to untrack non-existent GUID: {guid}");
                }
            }

            public static string GetLabelText(string guid)
            {
                if (StorageEntities.TryGetValue(guid, out var tuple))
                {
                    return tuple.Item3;
                }
                else
                {
                    if (Instance.modSettingsConsoleDebug.Value) MelonLogger.Msg($"GUID not found when trying to get label text: {guid}");
                    return null;
                }
            }

            public static void UntrackAllStorage()
            {
                int count = StorageEntities.Count;
                StorageEntities.Clear();
            }

            public static void LogStorageEntities()
            {
                if (StorageEntities.Count == 0)
                {
                    if (Instance.modSettingsConsoleDebug.Value) MelonLogger.Msg("StorageEntities is empty.");
                    return;
                }

                Instance.LoggerInstance.Msg($"Logging {StorageEntities.Count} tracked storage entities:");

                foreach (var kvp in StorageEntities)
                {
                    string guid = kvp.Key;
                    string labelText = kvp.Value.Item3;
                    string gameObjectName = kvp.Value.Item2 != null ? kvp.Value.Item2.name : "null";

                    if (Instance.modSettingsConsoleDebug.Value) MelonLogger.Msg($"GUID: {guid}, GameObject Name: {gameObjectName}, Label Text: {labelText}");
                }
            }
        }



        [Serializable]
        public class LabelInfo
        {
            public string Text;
            public string Color;

            public LabelInfo(string text, string color)
            {
                Text = text;
                Color = color;
            }
        }
        [Serializable]
        public class LabelData
        {
            public Dictionary<string, LabelInfo> Labels = new Dictionary<string, LabelInfo>();
        }

        //CLIPBOARD FUNCTIONALITY
        [HarmonyPatch(typeof(RouteListFieldUI), "Refresh")]
        public static class RouteListFieldUIRefreshPatch
        {
            [HarmonyPostfix]
            public static void Postfix(Il2CppSystem.Collections.Generic.List<AdvancedTransitRoute> newVal, RouteListFieldUI __instance)
            {
                if (!Instance.modSettingsShowClipboardRoutesLabels.Value) return;
                var routeData = new Dictionary<string, string>();
                var routeDataIndexer = 0;

                foreach (AdvancedTransitRoute route in newVal)
                {
                    AdvancedTransitRouteData data = route.GetData();
                    if (Instance.modSettingsConsoleDebug.Value) MelonLogger.Msg($"Route from {data?.SourceGUID} to {data?.DestinationGUID}");

                    routeData[data.SourceGUID] = data.DestinationGUID;

                }

                GameObject routesUIContents = __instance.gameObject.transform.Find("Contents").gameObject;

                foreach (Il2CppSystem.Object child in routesUIContents.transform)
                {
                    Transform childTransform = child.TryCast<Transform>(); // Safely cast to Transform
                    if (childTransform == null)
                    {
                        if (Instance.modSettingsConsoleDebug.Value) MelonLogger.Warning($"Skipping child: Unable to cast {child.GetType()} to Transform");
                        continue;
                    }

                    if (Instance.modSettingsConsoleDebug.Value) MelonLogger.Msg($"Processing child: {childTransform.name}");

                    if (childTransform.name.Contains("Entry") && childTransform.gameObject.active)
                    {
                        var sourceGUID = routeData.ElementAt(routeDataIndexer).Key;
                        var destinationGUID = routeData.ElementAt(routeDataIndexer).Value;

                        Transform sourceLabelTransform = childTransform.Find("Source/Label");
                        var textComponent = sourceLabelTransform.GetComponentInChildren<TextMeshProUGUI>();
                        if (sourceLabelTransform != null)
                        {
                            if (textComponent.text != "None")
                            {
                                var sourceLabel = sourceLabelTransform.GetComponent<Il2CppTMPro.TextMeshProUGUI>();
                                if (sourceLabel != null)
                                {
                                    string sourceLabelText = LabelTracker.GetLabelText(sourceGUID);
                                    if (sourceLabelText != null && sourceLabelText != "")
                                    {
                                        sourceLabel.text = sourceLabelText;
                                    }
                                }
                                else if (Instance.modSettingsConsoleDebug.Value) MelonLogger.Warning($"TextMeshProUGUI component not found on {sourceLabelTransform.name}");
                            }
                            else if (Instance.modSettingsConsoleDebug.Value) MelonLogger.Msg($"TextMeshProUGUI component text is None {sourceLabelTransform.name}");
                        }
                        else if (Instance.modSettingsConsoleDebug.Value) MelonLogger.Warning($"Transform 'Source/Label' not found under {childTransform.name}");

                        Transform destinationLabelTransform = childTransform.Find("Destination/Label");
                        textComponent = destinationLabelTransform.GetComponentInChildren<TextMeshProUGUI>();
                        if (destinationLabelTransform != null)
                        {

                            if (textComponent.text != "None")
                            {

                                var destinationLabel = destinationLabelTransform.GetComponent<Il2CppTMPro.TextMeshProUGUI>();
                                if (destinationLabel != null)
                                {
                                    string destinationLabelText = LabelTracker.GetLabelText(destinationGUID);
                                    if (destinationLabelText != null && destinationLabelText != "")
                                    {
                                        destinationLabel.text = destinationLabelText;
                                    }
                                }
                                else if (Instance.modSettingsConsoleDebug.Value) MelonLogger.Warning($"TextMeshProUGUI component not found on {destinationLabelTransform.name}");

                            }
                            else if (Instance.modSettingsConsoleDebug.Value) MelonLogger.Msg($"TextMeshProUGUI component text is None {destinationLabelTransform.name}");

                        }
                        else if (Instance.modSettingsConsoleDebug.Value) MelonLogger.Warning($"Transform 'Destination/Label' not found under {childTransform.name}");

                        routeDataIndexer++;
                    }


                }


            }
        }

        [HarmonyPatch(typeof(ObjectListFieldUI), "Refresh")]
        public static class ObjectListFieldUIRefreshPatch
        {
            [HarmonyPostfix]
            public static void Postfix(Il2CppSystem.Collections.Generic.List<BuildableItem> newVal, ObjectListFieldUI __instance)
            {
                if (!Instance.modSettingsShowClipboardStationsLabels.Value) return;
                var objectsData = new List<string>();
                var objectsDataIndexer = 0;

                foreach (BuildableItem obj in newVal)
                {
                    if (Instance.modSettingsConsoleDebug.Value) MelonLogger.Msg($"ObjectListFieldUI, BuildableItem.GUID =  {obj.GUID.ToString()}");
                    objectsData.Add(obj.GUID.ToString());

                }

                GameObject objectsUIContents = __instance.gameObject.transform.Find("Contents").gameObject;

                foreach (Il2CppSystem.Object child in objectsUIContents.transform)
                {
                    Transform childTransform = child.TryCast<Transform>(); // Safely cast to Transform
                    if (childTransform == null)
                    {
                        if (Instance.modSettingsConsoleDebug.Value) MelonLogger.Warning($"Skipping child: Unable to cast {child.GetType()} to Transform");
                        continue;
                    }

                    if (Instance.modSettingsConsoleDebug.Value) MelonLogger.Msg($"Processing child: {childTransform.name}");

                    Transform objectLabelTransform = childTransform.Find("Title");
                    var textComponent = objectLabelTransform.GetComponentInChildren<TextMeshProUGUI>();

                    if (childTransform.name.Contains("Entry") && childTransform.gameObject.active && textComponent.gameObject.active)
                    {
                        var objectGUID = objectsData[objectsDataIndexer];

                        if (objectLabelTransform != null && objectLabelTransform.gameObject.active)
                        {

                            var sourceLabel = objectLabelTransform.GetComponent<Il2CppTMPro.TextMeshProUGUI>();
                            if (sourceLabel != null)
                            {
                                string sourceLabelText = LabelTracker.GetLabelText(objectGUID);
                                if (sourceLabelText != null && sourceLabelText != "")
                                {
                                    sourceLabel.text = sourceLabelText;
                                }
                            }
                            else if (Instance.modSettingsConsoleDebug.Value) MelonLogger.Warning($"TextMeshProUGUI component not found on {objectLabelTransform.name}");


                        }
                        else if (Instance.modSettingsConsoleDebug.Value) MelonLogger.Warning($"Transform 'Source/Label' not found under {childTransform.name}");

                        objectsDataIndexer++;
                    }


                }

            }
        }


    }

}