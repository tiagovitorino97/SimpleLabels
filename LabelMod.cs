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

[assembly: MelonInfo(typeof(SimpleLabels.LabelMod), "SimpleLabels", "2.2.2", "tiagovito")]
[assembly: MelonGame("TVGS", "Schedule I")]
[assembly: MelonOptionalDependencies("SteamNetworkLib")]
[assembly: AssemblyMetadata("NexusModID", "680")]

namespace SimpleLabels;

public class LabelMod : MelonMod
{
    private string _lastScene;
    private UnityAction _onSaveStart;

    public override void OnInitializeMelon()
    {
        ModSettings.Initialize();
        LabelDataManager.Initialize();
        LabelNetworkManager.Initialize();
        Logger.Msg("SimpleLabels initialized.");
    }

    public override void OnDeinitializeMelon() => LabelNetworkManager.Terminate();

    public override void OnSceneWasLoaded(int buildIndex, string sceneName)
    {
        if (sceneName == "Main")
        {
            EnableMod();
        }
        else if (sceneName == "Menu" && _lastScene == "Main")
        {
            DisableMod();
        }
        
        _lastScene = sceneName;
    }

    private void EnableMod()
    {
        Logger.Msg("Mod activated.");
        LabelPrefabManager.Initialize();
        InputFieldManager.Initialize();
        
        // Delay saving to avoid conflict with game save
        _onSaveStart = (UnityAction)(() => MelonCoroutines.Start(SaveRoutine()));
        SaveManager.Instance.onSaveStart.AddListener(_onSaveStart);

        if (RegisteredMelons.Any(m => m.Info.Name == "Mod Manager & Phone App"))
        {
            ModManagerIntegration.Initialize();
        }
    }

    private IEnumerator SaveRoutine()
    {
        yield return new WaitForSeconds(2f);
        LabelDataManager.Save();
    }

    private void DisableMod()
    {
        if (_onSaveStart != null)
        {
            SaveManager.Instance?.onSaveStart.RemoveListener(_onSaveStart);
            _onSaveStart = null;
        }

        InputFieldManager.Terminate();
        LabelPrefabManager.Terminate();
        LabelApplier.Terminate();
        
        LabelDataManager.Initialize(); // Reset data
    }

    public override void OnUpdate()
    {
        LabelNetworkManager.Update();
    }
}
