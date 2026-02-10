using System.Reflection;
using System.Collections;
using Il2CppScheduleOne.Persistence;
using MelonLoader;
using SimpleLabels.Data;
using SimpleLabels.Settings;
using SimpleLabels.UI;
using UnityEngine;
using UnityEngine.Events;
using Logger = SimpleLabels.Utils.Logger;

[assembly: MelonInfo(typeof(SimpleLabels.LabelMod), "SimpleLabels", "2.2.1", "tiagovito")]
[assembly: MelonGame("TVGS", "Schedule I")]
[assembly: AssemblyMetadata("NexusModID", "680")]

namespace SimpleLabels;

/// <summary>
/// MelonMod entry point for SimpleLabels. Initializes settings, data, network; activates in Main, deactivates in Menu.
/// </summary>
/// <remarks>
/// OnInitializeMelon: ModSettings, LabelDataManager, LabelNetworkManager. OnSceneWasLoaded Main: ActivateMod
/// (prefab pool, input UI, save hook, Mod Manager integration if present). Menu (from Main): DeactivateMod
/// (terminate UI/prefab/applier, re-init data). OnUpdate forwards to LabelNetworkManager.
/// </remarks>
public class LabelMod : MelonMod
{
    private static string _previousSceneName = string.Empty;

    /// <summary>
    /// Sets up settings, loads Labels.json, and initializes Steam network. Runs once at mod load.
    /// </summary>
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


    private static void ActivateMod()
    {
        Logger.Msg("[Mod] Activating mod in Main scene");
        LabelPrefabManager.Initialize();
        InputFieldManager.Initialize();
        // Write labels after the game finishes saving so our file isn't overwritten when the game writes the save folder
        SaveManager.Instance.onSaveStart.AddListener((UnityAction)(() => MelonCoroutines.Start(SaveLabelsAfterGameSave())));

        if (RegisteredMelons.Any(mod => mod.Info.Name == "Mod Manager & Phone App"))
        {
            Logger.Msg("[Mod] Mod Manager & Phone App detected, initializing integration");
            ModManagerIntegration.Initialize();
        }
    }

    private static IEnumerator SaveLabelsAfterGameSave()
    {
        yield return new WaitForSeconds(2f);
        LabelDataManager.SaveLabelTrackerData();
    }

    private static void DeactivateMod()
    {
        Logger.Msg("[Mod] Deactivating mod");
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
    }
}