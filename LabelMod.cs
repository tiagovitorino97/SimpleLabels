using Il2CppScheduleOne.Persistence;
using MelonLoader;
using SimpleLabels.Data;
using SimpleLabels.Settings;
using SimpleLabels.UI;
using UnityEngine;
using UnityEngine.Events;
using Logger = SimpleLabels.Utils.Logger;

namespace SimpleLabels;

public class LabelMod : MelonMod
{
    private static string _previousSceneName = string.Empty;

    public override void OnInitializeMelon()
    {
        base.OnInitializeMelon();
        MelonLogger.Msg("[Mod] Initializing SimpleLabels mod...");
        ModSettings.Initialize();
        LabelDataManager.Initialize();
        LabelNetworkManager.Initialize();
        Logger.Msg("[Mod] SimpleLabels mod initialized successfully");
    }

    public override void OnDeinitializeMelon()
    {
        LabelNetworkManager.Terminate();
    }



    public override void OnSceneWasLoaded(int buildIndex, string sceneName)
    {
        switch (sceneName)
        {
            case "Main":
                ActivateMod();
                break;
            case "Menu":
                // Only reset if we're transitioning FROM Main scene (not if already in Menu)
                if (_previousSceneName == "Main")
                {
                    DeactivateMod();
                }
                break;
        }
        
        _previousSceneName = sceneName;
    }

    public override void OnSceneWasInitialized(int buildIndex, string sceneName)
    {
        
    }

    private static void ActivateMod()
    {
        Logger.Msg("[Mod] Activating mod in Main scene");
        LabelPrefabManager.Initialize();
        InputFieldManager.Initialize();
        SaveManager.Instance.onSaveStart.AddListener((UnityAction)(() => LabelDataManager.SaveLabelTrackerData()));

        if (RegisteredMelons.Any(mod => mod.Info.Name == "Mod Manager & Phone App"))
        {
            Logger.Msg("[Mod] Mod Manager & Phone App detected, initializing integration");
            ModManagerIntegration.Initialize();
        }
    }

    private static void DeactivateMod()
    {
        if (ModSettings.ShowDebug != null && ModSettings.ShowDebug.Value)
            MelonLogger.Msg("[Mod] Deactivating mod");
        InputFieldManager.Terminate();
        LabelPrefabManager.Terminate();
        LabelApplier.Terminate();
        
        // Reset and reload label data when returning to menu from Main scene
        // This ensures unsaved labels from the previous session are discarded
        Logger.Msg("[Mod] Resetting label data for new game session");
        LabelDataManager.Initialize();
    }

    public override void OnUpdate()
    {

        LabelNetworkManager.Update();

        if (Input.GetKeyDown(KeyCode.G))
        {
           LabelNetworkManager._syncVarTest.Value += 1;
        }
    }
}