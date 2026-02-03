using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MelonLoader.Utils;
using Newtonsoft.Json;
using SimpleLabels.Data;
using SimpleLabels.Services;
using SimpleLabels.Utils;

namespace SimpleLabels.Data
{
    /// <summary>
    /// Handles persistence of labels: reads/writes Labels.json under the mod directory.
    /// </summary>
    /// <remarks>
    /// Initialize creates the data directory, clears LabelTracker, and loads Labels.json into
    /// LabelTracker via LabelService. Supports migration from old <c>Labels</c> object format.
    /// SaveLabelTrackerData serializes all tracked entities with non-empty text to disk.
    /// </remarks>
    public class LabelDataManager
    {
        private static string _dataDirectory;
        private static string _dataFilePath;
        
        /// <summary>
        /// Sets up the data directory, resets LabelTracker, and loads Labels.json into state.
        /// </summary>
        /// <remarks>
        /// Creates SimpleLabels folder under MelonEnvironment.ModsDirectory if missing. If Labels.json
        /// uses the old format (contains "Labels"), migrates in-place and saves. Otherwise deserializes
        /// into a dictionary and creates entities via LabelService; then syncs to network if host.
        /// </remarks>
        public static void Initialize()
        {
            _dataDirectory = Path.Combine(MelonEnvironment.ModsDirectory, "SimpleLabels");
            _dataFilePath = Path.Combine(_dataDirectory, "Labels.json");
            Logger.Msg($"[DataManager] Initializing. Data file: {_dataFilePath}");
            EnsureDataDirectoryExists();
            ResetLabelTracker();
            LoadDataIntoLabelTracker();
        }
        
        private static void EnsureDataDirectoryExists()
        {
            try
            {
                if (!Directory.Exists(_dataDirectory))
                {
                    Directory.CreateDirectory(_dataDirectory);
                }
            }
            catch (Exception e)
            {
                Logger.Error($"Failed to create data directory: {e.Message}");
            }
        }
        
        private static void LoadDataIntoLabelTracker()
        {
            try
            {
                if (!File.Exists(_dataFilePath))
                    return;
                
                string json = File.ReadAllText(_dataFilePath);
                
                if (IsOldFormat(json))
                {
                    Logger.Msg("[DataManager] Detected old format labels file. Converting to new format...");
                    MigrateFromOldFormat(json);
                }
                else
                {
                    LoadNewFormat(json);
                }
            }
            catch (Exception e)
            {
                Logger.Error($"Failed to load data from {_dataFilePath}: {e.Message}");
            }
        }

        private static bool IsOldFormat(string json)
        {
            return json.Contains("\"Labels\":");
        }

        private static void LoadNewFormat(string json)
        {
            var savedData = JsonConvert.DeserializeObject<Dictionary<string, EntityData>>(json);
            
            if (savedData == null)
            {
                Logger.Warning("Label data file was empty or corrupted");
                return;
            }
            
            Logger.Msg($"[DataManager] Loading {savedData.Count} labels from file");
            
            foreach (var entityData in savedData.Values)
            {
                // Use service to create labels from persistence (service handles state + visuals)
                LabelService.CreateLabel(
                    entityData.Guid,
                    null,
                    entityData.LabelText,
                    entityData.LabelColor,
                    entityData.LabelSize,
                    entityData.FontSize,
                    entityData.FontColor
                );
            }
            
            // Sync labels to network if host (so late-joining clients get the loaded labels)
            LabelNetworkManager.SyncLabelsToNetwork();
        }
        
        private static void MigrateFromOldFormat(string jsonString)
        {
            try
            {
                // Use JsonConvert directly to avoid JObject
                var oldFormatDict = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, Dictionary<string, string>>>>(jsonString);
                
                if (oldFormatDict != null && oldFormatDict.ContainsKey("Labels"))
                {
                    var labels = oldFormatDict["Labels"];
                    int migratedCount = 0;
                    
                    foreach (var kvp in labels)
                    {
                        string guid = kvp.Key;
                        var labelData = kvp.Value;
                        
                        string labelText = labelData.ContainsKey("Text") ? labelData["Text"] : "";
                        string labelColor = labelData.ContainsKey("Color") ? labelData["Color"] : "FFFFFFFF";
                        
                        // Ensure color has # prefix for new format
                        if (!labelColor.StartsWith("#") && !string.IsNullOrEmpty(labelColor))
                        {
                            labelColor = "#" + labelColor;
                        }
                        
                        // Add entity with default values for new properties
                        LabelService.CreateLabel(
                            guid,
                            null,
                            labelText,
                            labelColor,
                            1, // Default LabelSize
                            24, // Default FontSize
                            "#000000" // Default FontColor
                        );
                        
                        migratedCount++;
                    }
                    
                    // Save the migrated data in the new format
                    SaveLabelTrackerData();
                }
            }
            catch (Exception e)
            {
                Logger.Error($"Failed to migrate from old format: {e.Message}");
            }
        }
        
        /// <summary>
        /// Serializes all tracked labels with non-empty text to Labels.json.
        /// </summary>
        /// <remarks>
        /// Uses LabelTracker.GetAllEntityData(), filters out empty text, then JsonConvert with
        /// indented formatting. Call after local label changes to persist across sessions.
        /// </remarks>
        public static void SaveLabelTrackerData()
        {
            try
            {
                var allData = LabelTracker.GetAllEntityData();
                var dataToSave = allData
                    .Where(kvp => !string.IsNullOrEmpty(kvp.Value.LabelText))
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                
                Logger.Msg($"[DataManager] Saving {dataToSave.Count} labels to file");
                
                var json = JsonConvert.SerializeObject(dataToSave, Formatting.Indented);
                File.WriteAllText(_dataFilePath, json);
            }
            catch (Exception e)
            {
                Logger.Error($"Failed to save data to {_dataFilePath}: {e.Message}");
            }
        }
        
        private static void ResetLabelTracker()
        {
            LabelTracker.Clear();
        }
    }
}