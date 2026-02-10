using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MelonLoader;
using MelonLoader.Utils;
using Newtonsoft.Json;
using SimpleLabels.Services;
using SimpleLabels.Utils;

namespace SimpleLabels.Data
{
    /// <summary>
    /// Handles persistence of labels per save: reads/writes Labels.json in the current save folder.
    /// Flow: check save folder first -> if no file, check global (legacy) -> load; if loaded from global, migrate applied labels to save folder and remove from global.
    /// </summary>
    public class LabelDataManager
    {
        private static string _globalDataDirectory;
        private static string _globalDataFilePath;
        private const float MigrationDelaySeconds = 2f;

        /// <summary>
        /// Ensures global directory exists (for migration source only), clears LabelTracker. Does not load any labels.
        /// </summary>
        public static void Initialize()
        {
            _globalDataDirectory = Path.Combine(MelonEnvironment.ModsDirectory, "SimpleLabels");
            _globalDataFilePath = Path.Combine(_globalDataDirectory, "Labels.json");
            Logger.Msg("[DataManager] Initializing (per-save labels).");
            EnsureGlobalDataDirectoryExists();
            ResetLabelTracker();
        }

        private static void EnsureGlobalDataDirectoryExists()
        {
            try
            {
                if (!Directory.Exists(_globalDataDirectory))
                    Directory.CreateDirectory(_globalDataDirectory);
            }
            catch (Exception e)
            {
                Logger.Error($"Failed to create global data directory: {e.Message}");
            }
        }

        /// <summary>
        /// Loads labels for the current save. Call when a save has finished loading (e.g. LoadManager.onLoadComplete).
        /// Flow: if save folder has Labels.json -> load from there; else if global has Labels.json -> load from global and schedule migration; else new save (no load).
        /// </summary>
        public static void LoadLabelsForCurrentSave()
        {
            ResetLabelTracker();
            string savePath = SavePathResolver.GetCurrentSavePath();
            if (string.IsNullOrEmpty(savePath))
            {
                Logger.Msg("[DataManager] No save path; skipping load.");
                return;
            }

            string saveFilePath = SavePathResolver.GetSaveFolderLabelsFilePath();
            string globalPath = _globalDataFilePath;

            // 1) Prefer save folder
            if (!string.IsNullOrEmpty(saveFilePath) && File.Exists(saveFilePath))
            {
                LoadFromFile(saveFilePath);
                LabelService.BindAllGameObjectsAndApplyLabels();
                LabelNetworkManager.SyncLabelsToNetwork();
                return;
            }

            // 2) Fallback: global (legacy) then migrate
            if (File.Exists(globalPath))
            {
                Logger.Warning($"[SimpleLabels] One-time migration: moving labels from {_globalDataFilePath} into this save's folder. Normal, happens once.");
                LoadFromFile(globalPath);
                LabelService.BindAllGameObjectsAndApplyLabels();
                LabelNetworkManager.SyncLabelsToNetwork();
                MelonCoroutines.Start(MigrationDelayedRoutine());
                return;
            }

            // 3) No files: new save, nothing to load
            Logger.Msg("[DataManager] No labels file in save folder or global; starting empty.");
        }

        private static void LoadFromFile(string filePath)
        {
            try
            {
                string json = File.ReadAllText(filePath);
                var savedData = JsonConvert.DeserializeObject<Dictionary<string, EntityData>>(json);
                if (savedData == null || savedData.Count == 0)
                {
                    if (savedData != null) Logger.Msg("[DataManager] Labels file empty.");
                    return;
                }
                Logger.Msg($"[DataManager] Loading {savedData.Count} labels from {filePath}");
                foreach (var entityData in savedData.Values)
                {
                    if (string.IsNullOrEmpty(entityData?.Guid)) continue;
                    LabelService.CreateLabel(
                        entityData.Guid,
                        null,
                        entityData.LabelText ?? "",
                        entityData.LabelColor,
                        entityData.LabelSize,
                        entityData.FontSize,
                        entityData.FontColor ?? ""
                    );
                }
            }
            catch (Exception e)
            {
                Logger.Error($"Failed to load labels from {filePath}: {e.Message}");
            }
        }

        private static IEnumerator MigrationDelayedRoutine()
        {
            yield return new UnityEngine.WaitForSeconds(MigrationDelaySeconds);
            MigrateAppliedLabelsToSaveFolderAndRemoveFromGlobal();
        }

        /// <summary>
        /// Copies all labels that are "applied" (have a bound GameObject) to the current save folder, then removes them from the global file. If global becomes empty, deletes the global file and folder.
        /// </summary>
        private static void MigrateAppliedLabelsToSaveFolderAndRemoveFromGlobal()
        {
            string savePath = SavePathResolver.GetCurrentSavePath();
            if (string.IsNullOrEmpty(savePath))
                return;

            string saveDir = SavePathResolver.GetSaveFolderSimpleLabelsDirectory(savePath);
            string saveFilePath = SavePathResolver.GetSaveFolderLabelsFilePath();
            if (string.IsNullOrEmpty(saveDir) || string.IsNullOrEmpty(saveFilePath))
                return;

            // Build dict of applied labels (entity has GameObject bound and non-empty text)
            var applied = new Dictionary<string, EntityData>();
            foreach (string guid in LabelTracker.GetAllTrackedGuids())
            {
                var data = LabelTracker.GetEntityData(guid);
                if (data == null || data.GameObject == null || string.IsNullOrEmpty(data.LabelText))
                    continue;
                applied[guid] = new EntityData(data.Guid, data.LabelText, data.LabelColor, data.LabelSize, data.FontSize, data.FontColor);
            }

            if (applied.Count > 0)
            {
                try
                {
                    if (!Directory.Exists(saveDir))
                        Directory.CreateDirectory(saveDir);
                    var json = JsonConvert.SerializeObject(applied, Formatting.Indented);
                    File.WriteAllText(saveFilePath, json);
                    Logger.Warning($"[SimpleLabels] Migration done: {applied.Count} label(s) moved to {saveDir}. One-time only.");
                }
                catch (Exception e)
                {
                    Logger.Error($"[DataManager] Failed to write save folder labels: {e.Message}");
                    return;
                }
            }
            else
            {
                // No applied labels: still create empty file so next load uses save folder
                try
                {
                    if (!Directory.Exists(saveDir))
                        Directory.CreateDirectory(saveDir);
                    File.WriteAllText(saveFilePath, "{}");
                    Logger.Warning($"[SimpleLabels] Labels now stored per save. New location: {saveDir}. One-time only.");
                }
                catch (Exception e)
                {
                    Logger.Error($"[DataManager] Failed to create save folder labels file: {e.Message}");
                }
            }

            // Remove migrated keys from global file
            if (!File.Exists(_globalDataFilePath))
                return;

            try
            {
                string globalJson = File.ReadAllText(_globalDataFilePath);
                var globalData = JsonConvert.DeserializeObject<Dictionary<string, EntityData>>(globalJson);
                if (globalData == null) return;
                foreach (string guid in applied.Keys)
                    globalData.Remove(guid);
                if (globalData.Count == 0)
                {
                    File.Delete(_globalDataFilePath);
                    if (Directory.Exists(_globalDataDirectory))
                    {
                        try { Directory.Delete(_globalDataDirectory); } catch { }
                    }
                    Logger.Warning($"[SimpleLabels] Old file removed (was empty). Labels are now in each save folder (e.g. {saveDir}).");
                }
                else
                {
                    globalJson = JsonConvert.SerializeObject(globalData, Formatting.Indented);
                    File.WriteAllText(_globalDataFilePath, globalJson);
                    Logger.Msg($"[DataManager] Removed {applied.Count} migrated labels from global file.");
                }
            }
            catch (Exception e)
            {
                Logger.Error($"[DataManager] Failed to update global file: {e.Message}");
            }
        }

        /// <summary>
        /// Saves only "applied" labels (those with a bound GameObject in this save) to the current save folder.
        /// </summary>
        public static void SaveLabelTrackerData()
        {
            string savePath = SavePathResolver.GetCurrentSavePath();
            if (string.IsNullOrEmpty(savePath))
            {
                Logger.Msg("[DataManager] No save path; skipping save.");
                return;
            }

            string saveDir = SavePathResolver.GetSaveFolderSimpleLabelsDirectory(savePath);
            string saveFilePath = SavePathResolver.GetSaveFolderLabelsFilePath();
            if (string.IsNullOrEmpty(saveDir) || string.IsNullOrEmpty(saveFilePath))
                return;

            try
            {
                if (!Directory.Exists(saveDir))
                    Directory.CreateDirectory(saveDir);

                // Only save labels that are applied in this save (have a bound GameObject)
                var dataToSave = new Dictionary<string, EntityData>();
                foreach (string guid in LabelTracker.GetAllTrackedGuids())
                {
                    var data = LabelTracker.GetEntityData(guid);
                    if (data == null || data.GameObject == null || string.IsNullOrEmpty(data.LabelText))
                        continue;
                    dataToSave[guid] = new EntityData(data.Guid, data.LabelText, data.LabelColor, data.LabelSize, data.FontSize, data.FontColor);
                }

                Logger.Msg($"[DataManager] Saving {dataToSave.Count} labels to save folder.");
                var json = JsonConvert.SerializeObject(dataToSave, Formatting.Indented);
                File.WriteAllText(saveFilePath, json);
            }
            catch (Exception e)
            {
                Logger.Error($"[DataManager] Failed to save labels: {e.Message}");
            }
        }

        private static void ResetLabelTracker()
        {
            LabelTracker.Clear();
        }
    }
}
