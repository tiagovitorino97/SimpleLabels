using System;
using System.IO;
using System.Reflection;

namespace SimpleLabels.Data
{
    public static class SavePathResolver
    {
        private const string SimpleLabelsSubfolder = "SimpleLabels";

        public static string GetCurrentSavePath()
        {
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

        public static string GetSaveFolderLabelsFilePath()
        {
            string savePath = GetCurrentSavePath();
            if (string.IsNullOrEmpty(savePath)) return null;
            return Path.Combine(savePath, SimpleLabelsSubfolder, "Labels.json");
        }

        public static string GetSaveFolderSimpleLabelsDirectory(string savePath)
        {
            if (string.IsNullOrEmpty(savePath)) return null;
            return Path.Combine(savePath, SimpleLabelsSubfolder);
        }
    }
}
