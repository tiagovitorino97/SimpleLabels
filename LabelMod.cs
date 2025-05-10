using System.Linq;
using Il2CppScheduleOne.Persistence;
using MelonLoader;
using SimpleLabels.Data;
using SimpleLabels.Settings;
using SimpleLabels.UI;
using UnityEngine.Events;

namespace SimpleLabels
{
    public class LabelMod : MelonMod
    {
        public override void OnInitializeMelon()
        {
            base.OnInitializeMelon();
            ModSettings.Initialize();
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            switch (sceneName)
            {
                case "Main":
                    ActivateMod();
                    break;
                case "Menu":
                    DeactivateMod();
                    break;
            }
        }
        

        public override void OnSceneWasInitialized(int buildIndex, string sceneName)
        {
            LabelPrefabManager.Initialize();
        }

        private static void ActivateMod()
        {
            LabelDataManager.Initialize();
            InputFieldManager.Initialize();
            SaveManager.Instance.onSaveStart.AddListener((UnityAction)(() => LabelDataManager.SaveLabelTrackerData()));
            ;

            //Try kook into Mod Manager & Phone App
            if (RegisteredMelons.Any(mod => mod.Info.Name == "Mod Manager & Phone App"))
                ModManagerIntegration.Initialize();
        }

        private static void DeactivateMod()
        {
            InputFieldManager.Terminate();
            LabelPrefabManager.Terminate();
            LabelApplier.Terminate();
        }
    }
}