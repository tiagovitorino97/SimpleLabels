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
using Il2CppFishNet.Object;
using Il2CppScheduleOne.Persistence.Datas;
using Il2CppScheduleOne.EntityFramework;
using System.Runtime.CompilerServices;
using Il2CppScheduleOne.ObjectScripts;
using System.Security.AccessControl;
using static Il2CppVLB.Consts;
using System.Linq;


namespace SimpleLabels
{

    public class LabelMod : MelonMod
    {
        public static LabelMod Instance { get; private set; }
        private static readonly bool debug = true;
        private TMP_InputField customInputField;
        private bool wasStorageRackOpen = false; // Track if the storage rack was open
        GameObject openEntityGameObject;
        private string openEntityGameObjectGUID;
        private string openEntityName;
        private string escWasAlreadyPressed = "false"; // Track if ESC was already pressed

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
                InitializeLabelPrefab(); // Initialize the label prefab
                
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

                escWasAlreadyPressed = "false"; // Reset ESC key state

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


        private System.Collections.IEnumerator WaitAndHook() //Hook into SaveManager
        {

            while (SaveManager.Instance == null && LoadManager.Instance == null)
                yield return null;


            UnityEvent onSaveStart = SaveManager.Instance.onSaveStart;
            onSaveStart.AddListener((UnityAction)OnSaveStart);

            //UnityEvent saveCompleteEvent = SaveManager.Instance.onSaveComplete;  --> Probably not needed, but could be useful in the future
            //saveCompleteEvent.AddListener((UnityAction)OnSaveCompleted);        




        }

        private void OnSaveStart()
        {
            if (debug) MelonLogger.Msg("Save Start.");

            
            foreach (var kvp in unsavedLabelData.Labels)
            {
                labelData.Labels[kvp.Key] = kvp.Value;
            }

            
            foreach (var guid in unsavedLabelData.Labels.Keys)// Remove any empty strings 
            {
                if (string.IsNullOrEmpty(unsavedLabelData.Labels[guid]))
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

                    //Update label prefabs

                    StorageTracker.TrackStorage(Instance.openEntityGameObjectGUID, entity.gameObject);

                }
            }
        }

        private System.Collections.IEnumerator WaitAndSubscribe() //Subscribe to StorageMenu close event
        {
            while (StorageMenu.Instance == null)
                yield return null;

            StorageMenu.Instance.onClosed.AddListener((UnityAction)OnStorageMenuClosed);
        }

        private void OnStorageMenuClosed()
        {
            StorageMenu.Instance.gameObject.transform.Find("CustomInputField").gameObject.SetActive(false); //Hide input field


        }

        [HarmonyPatch(typeof(StorageMenu))]
        class StorageMenu_Close_Patches
        {

            [HarmonyPrefix]
            [HarmonyPatch(nameof(StorageMenu.Close))]
            [HarmonyPatch(nameof(StorageMenu.Exit))]
            static void HandleMenuClose(StorageMenu __instance)
            {
                // Block exit if triggered by right-click
                if (Input.GetMouseButtonDown(1)) // Right mouse button
                {
                    if (debug) MelonLogger.Msg("Blocked StorageMenu exit: Right-click detected!");
                    return;
                }

                // Block exit if triggered by ESC key the first time
                if (Input.GetKeyDown(KeyCode.Escape) && Instance.escWasAlreadyPressed == "false")
                {
                    Instance.escWasAlreadyPressed = "true";
                    if (debug) MelonLogger.Msg("Blocked StorageMenu exit: ESC key detected!");
                    return;
                }

                

                try
                {

                    var inputField = __instance.gameObject.transform.Find("CustomInputField");
                    if (inputField != null)
                    {


                        if (Instance.wasStorageRackOpen)
                        {

                            string labelText = Instance.customInputField.text;
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
                }
                catch (Exception ex)
                {
                    if (debug) MelonLogger.Msg($"Error during menu close: {ex.Message}");
                }
            }

            
        }

        private void CreateInputField(GameObject parentUI)
        {
            GameObject inputFieldGameObject = new GameObject("CustomInputField");
            inputFieldGameObject.layer = 5; // UI layer
            inputFieldGameObject.transform.SetParent(parentUI.transform, false);


            RectTransform rectTransform = inputFieldGameObject.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = new Vector2(500, 50); // 5x larger than before

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
            customInputField.characterLimit = 25;

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
                    if (debug) MelonLogger.Msg("Config loaded successfully");
                }
                else
                {
                    config = new ModConfig();
                    SaveConfig();
                    if (debug) MelonLogger.Msg("Created new config file");
                }
            }
            catch (Exception ex)
            {
                config = new ModConfig();
                MelonLogger.Error($"Failed to load config: {ex.Message}");
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
                    if (debug) MelonLogger.Msg("Label data loaded successfully");
                }
                else
                {
                    labelData = new LabelData();
                    if (debug) MelonLogger.Msg("Created new label data file");
                }
            }
            catch (Exception ex)
            {
                labelData = new LabelData();
                MelonLogger.Error($"Failed to load label data: {ex.Message}");
            }
        }

        private void SaveLabelData()
        {
            try
            {
                string json = JsonConvert.SerializeObject(labelData, Formatting.Indented);
                File.WriteAllText(labelDataFilePath, json);
                if (debug) MelonLogger.Msg("Label data saved successfully");
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Failed to save label data: {ex.Message}");
            }
        }

        public void InitializeLabelPrefab()
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
                MelonLogger.Error("Couldn't find pillow material to reuse!");
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
            textMesh.overflowMode = TextOverflowModes.Truncate;
            textMesh.margin = new Vector4(0.1f, 0.1f, 0.1f, 0.1f);

            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.sizeDelta = new Vector2(1.8f, 0.5f);
            textRect.anchorMin = new Vector2(0.5f, 0.5f);
            textRect.anchorMax = new Vector2(0.5f, 0.5f);
            textRect.pivot = new Vector2(0.5f, 0.5f);

            if (debug) MelonLogger.Msg("Label prefab initialized");
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
                        //MelonLogger.Msg($"[DEBUG] Loaded GridItem: {objectName} (GUID: {objectGuid})");

                        //ADD label prefab for this gameobject

                        Instance.AddLabelPrefabToGameObject(GO, objectName, objectGuid);
                        StorageTracker.TrackStorage(objectGuid, GO);

                    }






                }
                else
                {
                    MelonLogger.Warning($"[DEBUG] GridItem from '{mainPath}' is null.");
                }
            }
        }

        public void AddLabelPrefabToGameObject(GameObject parentGO, string parentOBName, string parentOBGUID)
        {
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
            for (int i = 0; i < 4; i++)
            {
                // Instantiate the label prefab
                GameObject labelInstance = GameObject.Instantiate(Instance.labelPrefab, parentGO.transform);
                
                
                
                // Set position and rotation
                labelInstance.transform.localPosition = positions[i];
                labelInstance.transform.localRotation = rotations[i];

                // Set a unique name for each label
                labelInstance.name = $"Label_{i}";

                // Store reference to the text component for later updates
                var textMesh = labelInstance.GetComponentInChildren<TextMeshPro>();

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
            }

        }

        public void UpdateLabelPrefabInGameObject(string GUID, string labelText, GameObject parentGO, string parentName)
        {
            if (StorageTracker.StorageEntities.ContainsKey(GUID))
            {
                // Check if the parentGO already contains a child GameObject called "LabelText"
                bool hasLabelPrefabs = parentGO.GetComponentsInChildren<Transform>(true)
                    .Any(child => child.gameObject.name == "LabelText");

                if (!hasLabelPrefabs)
                {
                    // Add label prefabs if they don't exist
                    AddLabelPrefabToGameObject(parentGO, parentName, GUID);
                }

                // Update the text of existing label prefabs
                foreach (var labelObject in parentGO.GetComponentsInChildren<Transform>(true))
                {
                    if (labelObject.gameObject.name.StartsWith("Label_"))
                    {
                        if (string.IsNullOrEmpty(labelText))
                        {
                            labelObject.gameObject.SetActive(false);
                        }
                        else
                        {
                            labelObject.gameObject.SetActive(true);
                            var textMesh = labelObject.GetComponentInChildren<TextMeshPro>(true);
                            if (textMesh != null)
                            {
                                textMesh.text = labelText;
                            }
                        }
                    }
                }



            }
        }

        public static class StorageTracker
        {
            public static Dictionary<string, GameObject> StorageEntities = new Dictionary<string, GameObject>();

            public static void TrackStorage(string guid, GameObject GO)
            {
                if (!StorageEntities.ContainsKey(guid))
                {
                    StorageEntities.Add(guid, GO);

                }
            }

            public static void UntrackStorage(string guid)
            {
                if (StorageEntities.ContainsKey(guid))
                {
                    StorageEntities.Remove(guid);
                }
            }

            public static void UntrackAllStorage()
            {
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