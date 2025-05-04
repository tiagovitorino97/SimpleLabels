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
            EnsureDataDirectoryExists();
            LoadDataIntoLabelTracker();
            Logger.Msg("Data directory initialized");
        }
        
        private static void EnsureDataDirectoryExists()
        {
            try
            {
                if (!Directory.Exists(_dataDirectory))
                {
                    Directory.CreateDirectory(_dataDirectory);
                    Logger.Msg($"Data directory: {_dataDirectory} created");
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
                    Logger.Msg($"Data file: {_dataFilePath} not found.");
                    return;
                }
                
                string json = File.ReadAllText(_dataFilePath);
                
                // Try to detect format based on JSON structure
                if (json.Contains("\"Labels\":"))
                {
                    Logger.Msg("Detected old format labels file. Converting to new format...");
                    MigrateFromOldFormat(json);
                }
                else
                {
                    // It's new format, proceed normally
                    var savedData = JsonConvert.DeserializeObject<Dictionary<string, LabelTracker.EntityData>>(json);
                    
                    if (savedData == null)
                    {
                        Logger.Warning("Label data file was empty or corrupted");
                        return;
                    }
                    
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
                    
                    Logger.Msg($"Loaded {savedData.Count} entities from {_dataFilePath}");
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
                    Logger.Msg($"Migration complete. Converted {migratedCount} labels to new format.");
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
                
                // Filter out items with empty LabelText
                var dataToSave = allData.Where(kvp => !string.IsNullOrEmpty(kvp.Value.LabelText))
                                       .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                
                var json = JsonConvert.SerializeObject(dataToSave, Formatting.Indented);
                File.WriteAllText(_dataFilePath, json);
                
                int removedCount = allData.Count - dataToSave.Count;
                if (removedCount > 0)
                {
                    Logger.Msg($"Removed {removedCount} entities with empty labels");
                }
                
                Logger.Msg($"Saved {dataToSave.Count} entities to {_dataFilePath}");
            }
            catch (Exception e)
            {
                Logger.Error($"Failed to save data to {_dataFilePath}: {e.Message}");
            }
        }
    }
}