using MelonLoader;
using System;
using HarmonyLib;
using Il2CppScheduleOne.Persistence;
using UnityEngine.Events;
using Il2CppScheduleOne.UI;
using Il2CppScheduleOne.Storage;
using UnityEngine;
using UnityEngine.UI;
using Il2CppTMPro;
using MelonLoader.Utils;
using System.IO;
using Newtonsoft.Json;
using System.Collections.Generic;
using Il2CppScheduleOne.Persistence.Loaders;
using Il2CppScheduleOne.EntityFramework;
using System.Linq;
using System.Text.RegularExpressions;

namespace SimpleLabels
{
    public class LabelMod : MelonMod
    {
        public static LabelMod Instance { get; private set; }
        private static readonly bool debug = false;
        private TMP_InputField customInputField;
        private bool wasStorageRackOpen = false;
        GameObject openEntityGameObject;
        private string openEntityGameObjectGUID;
        private string openEntityName;

        //Mod Config
        private ModConfig config;
        private string configFolderPath;
        private string configFilePath;

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
            configFolderPath = Path.Combine(MelonEnvironment.ModsDirectory, "SimpleLabels");
            configFilePath = Path.Combine(configFolderPath, "Config.json");
            EnsureConfigDirectoryExists();
            LoadConfig();

            //Label Data
            labelDataFilePath = Path.Combine(configFolderPath, "Labels.json");
            unsavedLabelData = new LabelData();

            MelonCoroutines.Start(WaitAndHook());

        }

        public override void OnSceneWasInitialized(int buildIndex, string sceneName)
        {
            if (debug) MelonLogger.Msg($"Scene loaded: {sceneName}");

            if (sceneName == "Main")
            {
                MelonCoroutines.Start(WaitAndSubscribe());
                InitializeLabelPrefab();
                
                GameObject storageUI = GameObject.Find("UI/StorageMenu");
                if (storageUI != null)
                {
                    CreateInputField(storageUI);
                    if (debug) MelonLogger.Msg("Label input field created.");

                }
                LoadLabelData();

                
                if (labelPrefab == null)
                {
                    MelonLogger.Error("Label prefab is null!");
                }
                else
                {
                    MelonLogger.Msg("Label prefab is not null.");
                }

            }
            else if (sceneName == "Menu")
            {
                unsavedLabelData = new LabelData();
                if (debug) MelonLogger.Msg("Cleared unsaved label data");
            }


        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            if (debug) MelonLogger.Msg($"Scene loaded: {sceneName}");
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
                if (string.IsNullOrEmpty(unsavedLabelData.Labels[guid]))// Remove any empty strings 
                {
                    labelData.Labels.Remove(guid);
                }
            }

            SaveLabelData();

            if (debug) MelonLogger.Msg("Saved all label changes");
        }


        [HarmonyPatch(typeof(StorageMenu), nameof(StorageMenu.Open), new Type[] { typeof(StorageEntity) })]
        class StorageMenu_Open_Patch
        {

            static void Postfix(StorageMenu __instance, StorageEntity entity)
            {
                if (debug) MelonLogger.Msg($"Storage menu opened for: {entity.StorageEntityName}");

                try
                {
                    if (entity is UnityEngine.Component component)
                    {
                        Instance.openEntityGameObject = component.gameObject;
                        Instance.openEntityName = entity.name;
                        Instance.openEntityGameObjectGUID = Instance.openEntityGameObject.GetComponent<Il2CppScheduleOne.ObjectScripts.PlaceableStorageEntity>().GUID.ToString();
                        if (debug) MelonLogger.Msg($"StorageEntity GUID: {Instance.openEntityGameObjectGUID}");

                    }

                }
                catch
                {
                    if (debug) MelonLogger.Msg($"StorageEntity is NOT a UnityEngine.Object");
                }


                if (entity.StorageEntityName == "Small Storage Rack" ||
                    entity.StorageEntityName == "Medium Storage Rack" ||
                    entity.StorageEntityName == "Large Storage Rack")
                {

                    __instance.gameObject.transform.Find("CustomInputField").gameObject.SetActive(true); //Show input field

                    // Load existing label if it exists
                    if (Instance.unsavedLabelData.Labels.TryGetValue(Instance.openEntityGameObjectGUID, out string existingUnsavedLabel))
                    {
                        Instance.customInputField.text = existingUnsavedLabel;
                        if (debug) MelonLogger.Msg($"Loaded existing unsaved label: {existingUnsavedLabel}");
                    }
                    else if (Instance.labelData.Labels.TryGetValue(Instance.openEntityGameObjectGUID, out string existingLabel))
                    {
                        Instance.customInputField.text = existingLabel;
                        if (debug) MelonLogger.Msg($"Loaded existing label: {existingLabel}");
                    }
                    else
                    {
                        Instance.customInputField.text = string.Empty;
                    }


                    if (Instance.config.AutoFocus) //Variable from config file
                    {
                        if (debug) MelonLogger.Msg("Focusing on input field.");
                        __instance.gameObject.transform.Find("CustomInputField").GetComponent<TMP_InputField>().Select();
                        __instance.gameObject.transform.Find("CustomInputField").GetComponent<TMP_InputField>().ActivateInputField();
                    }
                    else
                    {
                        if (debug) MelonLogger.Msg("Not focusing on input field.");
                    }

                    Instance.wasStorageRackOpen = true;

                    StorageTracker.TrackStorage(Instance.openEntityGameObjectGUID, entity.gameObject);

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
            StorageMenu.Instance.gameObject.transform.Find("CustomInputField").gameObject.SetActive(false); //Hide input field

            try
            {
                    if (Instance.wasStorageRackOpen)
                    {

                        string labelText = Instance.customInputField.text;
                        labelText = Regex.Replace(labelText, @"[\n\r]", "");
                        string guid = Instance.openEntityGameObjectGUID;
                        if (debug) MelonLogger.Msg($"Saved label: {labelText} for GUID: {guid}");

                        if (!string.IsNullOrEmpty(guid))
                        {
                            Instance.unsavedLabelData.Labels[guid] = labelText;

                            Instance.UpdateLabelPrefabInGameObject(guid, labelText, Instance.openEntityGameObject, Instance.openEntityName);
                        }

                        Instance.wasStorageRackOpen = false; //Reset Flag
                    }
                
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Error during menu close: {ex.Message}");
            }


        }

        private void CreateInputField(GameObject parentUI)
        {
            try
            {
                GameObject inputFieldGameObject = new GameObject("CustomInputField");
                inputFieldGameObject.layer = 5; // UI layer
                inputFieldGameObject.transform.SetParent(parentUI.transform, false);

                RectTransform rectTransform = inputFieldGameObject.AddComponent<RectTransform>();
                rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                rectTransform.pivot = new Vector2(0.5f, 0.5f);
                rectTransform.sizeDelta = new Vector2(550, 50);

                var titleGameObject = GameObject.Find("UI/StorageMenu/Container/Title");
                RectTransform titleRT = titleGameObject.GetComponent<RectTransform>();
                Vector2 titlePos = titleRT.anchoredPosition;
                Vector2 titleSize = titleRT.sizeDelta;
                rectTransform.anchoredPosition = new Vector2(titlePos.x, titlePos.y - titleSize.y + 155); // More or less vertical spacing from title

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

                customInputField = inputFieldGameObject.AddComponent<TMP_InputField>();

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

                customInputField.textViewport = textRT;
                customInputField.textComponent = textComponent;
                customInputField.placeholder = placeholderText;
                customInputField.characterLimit = 30;

                customInputField.contentType = TMP_InputField.ContentType.Standard;

                LoggerInstance.Msg("Custom input field created successfully");
            }
            catch (Exception ex) 
            {
                LoggerInstance.Error($"Failed to create input field: {ex.Message}");
            }
        }
        

        private void EnsureConfigDirectoryExists()
        {
            try
            {
                if (!Directory.Exists(configFolderPath))
                {
                    Directory.CreateDirectory(configFolderPath);
                    if (debug) MelonLogger.Msg("Created SimpleLabels config directory");
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Failed to create config directory: {ex.Message}");
            }
        }

        private void LoadConfig()
        {
            try
            {
                if (File.Exists(configFilePath))
                {
                    string json = File.ReadAllText(configFilePath);
                    config = JsonConvert.DeserializeObject<ModConfig>(json);
                    LoggerInstance.Msg("Config loaded successfully");
                }
                else
                {
                    config = new ModConfig();
                    SaveConfig();
                    LoggerInstance.Msg("Created new config file");
                }
            }
            catch (Exception ex)
            {
                config = new ModConfig();
                LoggerInstance.Error($"Failed to load config: {ex.Message}");
                LoggerInstance.Warning("Using default config values");
            }
        }

        private void SaveConfig()
        {
            try
            {
                string json = JsonConvert.SerializeObject(config, Formatting.Indented);
                File.WriteAllText(configFilePath, json);
                if (debug) MelonLogger.Msg("Config saved successfully");
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Failed to save config: {ex.Message}");
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

                GameObject chemical_bottleObject = GameObject.Find("small chemical bottle/Lid"); //lmao
                Material whitematte = chemical_bottleObject?.GetComponent<MeshRenderer>()?.material;
                if (whitematte != null)
                {
                    paperBackground.GetComponent<Renderer>().material = whitematte;
                }
                else
                {
                    LoggerInstance.Error("Couldn't find material to reuse!");
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
        class GridItemLoaderPatch //Initial loaded Entities
        {
            static void Postfix(GridItem __result, string mainPath)
            {
                if (__result != null)
                {
                    GameObject GO = __result.gameObject;
                    string objectName = __result.name;
                    string objectGuid = __result.GUID.ToString();

                    if (objectName.Contains("StorageRack"))
                    {
                        if (debug) MelonLogger.Msg($"Processing storage rack: {objectName} (GUID: {objectGuid})");

                        try
                        {
                            Instance.AddLabelPrefabToGameObject(GO, objectName, objectGuid);
                            StorageTracker.TrackStorage(objectGuid, GO);
                        }
                        catch (Exception ex)
                        {
                            Instance.LoggerInstance.Error($"Failed to process storage rack {objectName}: {ex.Message}");
                        }

                    }
                }
            }
        }

        public void AddLabelPrefabToGameObject(GameObject parentGO, string parentOBName, string parentOBGUID)
        {
            try
            {
                // Validate inputs
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

                if (debug) MelonLogger.Msg($"Adding labels to {parentOBName} (GUID: {parentOBGUID})");

                // Define positions and rotations for each side
                Vector3[] positions;
                Quaternion[] rotations = new Quaternion[4];

                if (parentOBName.Contains("Large"))
                {
                    positions = new Vector3[] {
                        new Vector3(0, 0.750f, -0.254f),  // Front
                        new Vector3(1, 0.750f, 0),        // Right
                        new Vector3(0, 0.750f, 0.254f),   // Back
                        new Vector3(-1, 0.750f, 0)        // Left
                    };
                }
                else if (parentOBName.Contains("Medium"))
                {
                    positions = new Vector3[] {
                        new Vector3(0, 0.750f, -0.254f),  // Front
                        new Vector3(0.75f, 0.750f, 0),   // Right
                        new Vector3(0, 0.750f, 0.254f),   // Back
                        new Vector3(-0.75f, 0.750f, 0)    // Left
                    };
                }
                else // Small
                {
                    positions = new Vector3[] {
                        new Vector3(0, 0.750f, -0.254f),  // Front
                        new Vector3(0.50f, 0.750f, 0),    // Right
                        new Vector3(0, 0.750f, 0.254f),    // Back
                        new Vector3(-0.50f, 0.750f, 0)    // Left
                    };
                }

                rotations = new Quaternion[] {
                    Quaternion.Euler(0, 0, 0),      // Front
                    Quaternion.Euler(0, -90, 0),    // Right
                    Quaternion.Euler(0, 180, 0),    // Back
                    Quaternion.Euler(0, 90, 0)      // Left
                };

                // Create 4 labels (one for each side)
                int labelsCreated = 0;
                for (int i = 0; i < 4; i++)
                {
                    try
                    {
                        // Instantiate the label prefab
                        GameObject labelInstance = GameObject.Instantiate(Instance.labelPrefab, parentGO.transform);

                        if (labelInstance == null)
                        {
                            LoggerInstance.Error($"Failed to instantiate label {i} for {parentOBName}");
                            continue;
                        }

                        labelInstance.transform.localPosition = positions[i];
                        labelInstance.transform.localRotation = rotations[i];

                        labelInstance.name = $"Label_{i}";

                        var textMesh = labelInstance.GetComponentInChildren<TextMeshPro>();
                        if (textMesh == null)
                        {
                            LoggerInstance.Warning($"Failed to find TextMeshPro component on label {i}");
                            continue;
                        }

                        // Set initial text if we have saved data for this rack
                        if (Instance.unsavedLabelData.Labels.TryGetValue(parentOBGUID, out string unsavedLabelText))
                        {
                            textMesh.text = unsavedLabelText;
                            labelInstance.SetActive(true);
                        }
                        else if (Instance.labelData.Labels.TryGetValue(parentOBGUID, out string labelText))
                        {
                            textMesh.text = labelText;
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
                        LoggerInstance.Error($"Error creating label {i} for {parentOBName}: {ex.Message}");
                    }
                }

                if (debug) MelonLogger.Msg($"Successfully created {labelsCreated}/4 labels for {parentOBName}");
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"Failed to add label prefab to {parentOBName}: {ex}");
            }
        }

        public void UpdateLabelPrefabInGameObject(string GUID, string labelText, GameObject parentGO, string parentName)
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

                if (debug) MelonLogger.Msg($"Updating labels for {parentName} (GUID: {GUID})");

                if (!StorageTracker.StorageEntities.ContainsKey(GUID))
                {
                    LoggerInstance.Warning($"Storage entity {parentName} not tracked (GUID: {GUID})");
                    return;
                }

                // Check for existing label prefabs
                bool hasLabelPrefabs = parentGO.GetComponentsInChildren<Transform>(true)
                    .Any(child => child.gameObject.name == "LabelText");

                if (debug) MelonLogger.Msg($"Label prefabs exist: {hasLabelPrefabs}");

                if (!hasLabelPrefabs)
                {
                    AddLabelPrefabToGameObject(parentGO, parentName, GUID);
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
                            if (debug) LoggerInstance.Msg($"Disabled label {labelObject.gameObject.name} (empty text)");
                        }
                        else
                        {
                            labelObject.gameObject.SetActive(true);
                            var textMesh = labelObject.GetComponentInChildren<TextMeshPro>(true);

                            if (textMesh != null)
                            {
                                textMesh.text = labelText;
                                if (debug) LoggerInstance.Msg($"Updated label {labelObject.gameObject.name} with text: {labelText}");
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

        public static class StorageTracker //Not used currently, may be useful later
        {
            public static Dictionary<string, GameObject> StorageEntities = new Dictionary<string, GameObject>();

            public static void TrackStorage(string guid, GameObject GO)
            {
                if (string.IsNullOrEmpty(guid))
                {
                    Instance.LoggerInstance.Warning("Attempted to track storage with empty GUID");
                    return;
                }

                if (GO == null)
                {
                    Instance.LoggerInstance.Warning($"Attempted to track null GameObject for GUID: {guid}");
                    return;
                }

                if (!StorageEntities.ContainsKey(guid))
                {
                    StorageEntities.Add(guid, GO);
                }
                else if (StorageEntities[guid] != GO)
                {
                    Instance.LoggerInstance.Warning($"GUID conflict detected: {guid} already tracked to different GameObject");
                }
            }

            public static void UntrackStorage(string guid)
            {
                if (StorageEntities.ContainsKey(guid))
                {
                    StorageEntities.Remove(guid);
                }
                else if (debug)
                {
                    Instance.LoggerInstance.Msg($"Attempted to untrack non-existent GUID: {guid}");
                }
            }

            public static void UntrackAllStorage()
            {
                int count = StorageEntities.Count;
                StorageEntities.Clear();
            }
        }

        public class ModConfig
        {
            public bool AutoFocus { get; set; } = true;
        }

        [Serializable]
        public class LabelData
        {
            public Dictionary<string, string> Labels { get; set; } = new Dictionary<string, string>();
        }
    }

}