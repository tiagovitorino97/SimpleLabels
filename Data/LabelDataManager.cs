using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MelonLoader.Utils;
using Newtonsoft.Json;
using SimpleLabels.Utils;

namespace SimpleLabels.Data
{
    public class LabelDataManager
    {
        private static string _dataDirectory;
        private static string _dataFilePath;
        
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
                {
                    return;
                }
                
                string json = File.ReadAllText(_dataFilePath);
                
                if (json.Contains("\"Labels\":"))
                {
                    Logger.Msg("[DataManager] Detected old format labels file. Converting to new format...");
                    MigrateFromOldFormat(json);
                }
                else
                {
                    var savedData = JsonConvert.DeserializeObject<Dictionary<string, LabelTracker.EntityData>>(json);
                    
                    if (savedData == null)
                    {
                        Logger.Warning("Label data file was empty or corrupted");
                        return;
                    }
                    
                    Logger.Msg($"[DataManager] Loading {savedData.Count} labels from file");
                    
                    foreach (var kvpEntityData in savedData)
                        LabelTracker.TrackEntity(
                            kvpEntityData.Value.Guid,
                            null,
                            kvpEntityData.Value.LabelText,
                            kvpEntityData.Value.LabelColor,
                            kvpEntityData.Value.LabelSize,
                            kvpEntityData.Value.FontSize,
                            kvpEntityData.Value.FontColor
                        );
                }
            }
            catch (Exception e)
            {
                Logger.Error($"Failed to load data from {_dataFilePath}: {e.Message}");
            }
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
                        LabelTracker.TrackEntity(
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
        
        public static void SaveLabelTrackerData()
        {
            try
            {
                var allData = LabelTracker.GetAllEntityData();
                var dataToSave = allData.Where(kvp => !string.IsNullOrEmpty(kvp.Value.LabelText))
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

            // Access the Dictionary via reflection since it's private
            var entityDataDictionary = typeof(LabelTracker)
                .GetField("EntityDataDictionary", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                .GetValue(null) as Dictionary<string, LabelTracker.EntityData>;
    
            if (entityDataDictionary != null)
            {
                entityDataDictionary.Clear();
            }
            else
            {
                Logger.Error("Failed to reset LabelTracker");
            }
    
            // Reset currently managed entity
            LabelTracker.SetCurrentlyManagedEntity(null);
        }
    }
}