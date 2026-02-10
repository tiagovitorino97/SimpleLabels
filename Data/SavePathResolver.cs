using System;
using System.IO;
using System.Reflection;

namespace SimpleLabels.Data
{
    /// <summary>
    /// Resolves the current save folder path from the game's LoadManager (loaded game) or SaveManager (when saving).
    /// Used for per-save label storage: {SavePath}/SimpleLabels/Labels.json
    /// </summary>
    public static class SavePathResolver
    {
        private const string SimpleLabelsSubfolder = "SimpleLabels";

        /// <summary>Gets the current save folder path. Returns null if not in a save (e.g. menu or path not yet set).</summary>
        public static string GetCurrentSavePath()
        {
            // Prefer LoadManager.LoadedGameFolderPath (the currently loaded save)
            try
            {
                Type loadManagerType = Type.GetType("Il2CppScheduleOne.Persistence.LoadManager, Assembly-CSharp")
                    ?? Type.GetType("ScheduleOne.Persistence.LoadManager, Assembly-CSharp");
                if (loadManagerType != null)
                {
                    var instanceProp = loadManagerType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
                    object instance = instanceProp?.GetValue(null);
                    if (instance != null)
                    {
                        var pathProp = loadManagerType.GetProperty("LoadedGameFolderPath", BindingFlags.Public | BindingFlags.Instance);
                        object path = pathProp?.GetValue(instance);
                        if (path != null)
                        {
                            string s = path.ToString();
                            if (!string.IsNullOrEmpty(s)) return s;
                        }
                        var activeProp = loadManagerType.GetProperty("ActiveSaveInfo", BindingFlags.Public | BindingFlags.Instance);
                        object saveInfo = activeProp?.GetValue(instance);
                        if (saveInfo != null)
                        {
                            var savePathField = saveInfo.GetType().GetField("SavePath", BindingFlags.Public | BindingFlags.Instance);
                            if (savePathField != null)
                            {
                                object sp = savePathField.GetValue(saveInfo);
                                if (sp != null) return sp.ToString();
                            }
                        }
                    }
                }
            }
            catch { }

            // Fallback: SaveManager when saving (IndividualSavesContainerPath + SaveName)
            try
            {
                Type saveManagerType = Type.GetType("Il2CppScheduleOne.Persistence.SaveManager, Assembly-CSharp")
                    ?? Type.GetType("ScheduleOne.Persistence.SaveManager, Assembly-CSharp");
                if (saveManagerType != null)
                {
                    var instanceProp = saveManagerType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
                    object instance = instanceProp?.GetValue(null);
                    if (instance != null)
                    {
                        var containerProp = saveManagerType.GetProperty("IndividualSavesContainerPath", BindingFlags.Public | BindingFlags.Instance);
                        var nameProp = saveManagerType.GetProperty("SaveName", BindingFlags.Public | BindingFlags.Instance);
                        object container = containerProp?.GetValue(instance);
                        object name = nameProp?.GetValue(instance);
                        if (container != null && name != null)
                        {
                            string c = container.ToString();
                            string n = name.ToString();
                            if (!string.IsNullOrEmpty(c) && !string.IsNullOrEmpty(n))
                                return Path.Combine(c, n);
                        }
                    }
                }
            }
            catch { }

            return null;
        }

        /// <summary>Gets the path to Labels.json inside the current save folder: {SavePath}/SimpleLabels/Labels.json. Returns null if no save path.</summary>
        public static string GetSaveFolderLabelsFilePath()
        {
            string savePath = GetCurrentSavePath();
            if (string.IsNullOrEmpty(savePath)) return null;
            return Path.Combine(savePath, SimpleLabelsSubfolder, "Labels.json");
        }

        /// <summary>Gets the directory path for SimpleLabels inside the given save folder (for creating the folder).</summary>
        public static string GetSaveFolderSimpleLabelsDirectory(string savePath)
        {
            if (string.IsNullOrEmpty(savePath)) return null;
            return Path.Combine(savePath, SimpleLabelsSubfolder);
        }
    }
}
