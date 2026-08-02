using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using MelonLoader;
using MelonLoader.Utils;
using Newtonsoft.Json;
using SimpleLabels.Services;
using SimpleLabels.Utils;

namespace SimpleLabels.Data
{
    public static class LabelDataManager
    {
        private static string _legacyGlobalDir;
        private static string _legacyGlobalFile;
        private const float MigrationDelay = 2f;

        public static void Initialize()
        {
            _legacyGlobalDir = Path.Combine(MelonEnvironment.ModsDirectory, "SimpleLabels");
            _legacyGlobalFile = Path.Combine(_legacyGlobalDir, "Labels.json");

            if (!Directory.Exists(_legacyGlobalDir))
            {
                try { Directory.CreateDirectory(_legacyGlobalDir); }
                catch { /* Ignore */ }
            }

            LabelTracker.Clear();
        }

        public static void Load()
        {
            string savePath = SavePathResolver.GetCurrentSavePath();
            if (string.IsNullOrEmpty(savePath)) return;

            string localFile = SavePathResolver.GetSaveFolderLabelsFilePath();

            if (File.Exists(localFile))
            {
                LabelTracker.Clear();
                LoadFile(localFile);
                LabelService.SyncAll();
                LabelNetworkManager.SyncLabelsToNetwork();
                return;
            }

            if (File.Exists(_legacyGlobalFile))
            {
                LabelTracker.Clear();
                Logger.Warning("Migrating legacy labels to current save...");
                LoadFile(_legacyGlobalFile);
                LabelService.SyncAll();
                LabelNetworkManager.SyncLabelsToNetwork();
                MelonCoroutines.Start(MigrationRoutine());
                return;
            }

            Logger.Msg("No labels found, starting fresh.");
        }

        private static void LoadFile(string path)
        {
            try
            {
                var content = File.ReadAllText(path);
                var data = JsonConvert.DeserializeObject<Dictionary<string, EntityData>>(content);
                
                if (data == null) return;

                foreach (var entry in data.Values)
                {
                    if (string.IsNullOrEmpty(entry?.Guid)) continue;

                    LabelService.CreateLabel(
                        entry.Guid,
                        null,
                        entry.LabelText ?? "",
                        entry.LabelColor,
                        entry.LabelSize,
                        entry.FontSize,
                        entry.FontColor ?? ""
                    );
                }
                
                Logger.Msg($"Loaded {data.Count} labels.");
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to load labels: {ex.Message}");
            }
        }

        public static void Save()
        {
            string savePath = SavePathResolver.GetCurrentSavePath();
            if (string.IsNullOrEmpty(savePath)) return;

            string dir = SavePathResolver.GetSaveFolderSimpleLabelsDirectory(savePath);
            string file = SavePathResolver.GetSaveFolderLabelsFilePath();

            if (string.IsNullOrEmpty(dir) || string.IsNullOrEmpty(file)) return;

            try
            {
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                var toSave = new Dictionary<string, EntityData>();
                foreach (var guid in LabelTracker.GetAllTrackedGuids())
                {
                    var data = LabelTracker.GetEntityData(guid);
                    if (data?.GameObject != null && !string.IsNullOrEmpty(data.LabelText))
                    {
                        toSave[guid] = new EntityData(data.Guid, data.LabelText, data.LabelColor, data.LabelSize, data.FontSize, data.FontColor);
                    }
                }

                File.WriteAllText(file, JsonConvert.SerializeObject(toSave, Formatting.Indented));
            }
            catch (Exception ex)
            {
                Logger.Error($"Save failed: {ex.Message}");
            }
        }

        private static IEnumerator MigrationRoutine()
        {
            yield return new UnityEngine.WaitForSeconds(MigrationDelay);
            MigrateLegacyData();
        }

        private static void MigrateLegacyData()
        {
            var savePath = SavePathResolver.GetCurrentSavePath();
            if (string.IsNullOrEmpty(savePath)) return;

            Save();

            if (!File.Exists(_legacyGlobalFile)) return;

            try
            {
                var globalData = JsonConvert.DeserializeObject<Dictionary<string, EntityData>>(File.ReadAllText(_legacyGlobalFile));
                if (globalData == null) return;

                foreach (var guid in LabelTracker.GetAllTrackedGuids())
                {
                   globalData.Remove(guid);
                }

                if (globalData.Count == 0)
                {
                    File.Delete(_legacyGlobalFile);
                    try { Directory.Delete(_legacyGlobalDir); } catch {}
                    Logger.Warning("Legacy global file cleaned up.");
                }
                else
                {
                    File.WriteAllText(_legacyGlobalFile, JsonConvert.SerializeObject(globalData, Formatting.Indented));
                    Logger.Msg("Legacy file updated (partial migration).");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Migration cleanup failed: {ex.Message}");
            }
        }
    }
}
