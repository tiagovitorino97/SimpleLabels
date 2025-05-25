using System.Linq;
using Il2CppScheduleOne.Persistence;
using MelonLoader;
using SimpleLabels.Data;
using SimpleLabels.Settings;
using SimpleLabels.UI;
using UnityEngine;
using UnityEngine.Events;

namespace SimpleLabels;

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
/*
    private const float MESSAGE_CHECK_INTERVAL = 0.2f;
    private float _messageCheckTimer;


    public override void OnUpdate()
    {

        if (Input.GetKeyDown(KeyCode.B))
        {
            LabelSyncManager.SendTestMessage("THIS IS JUST A TEST!");
        }

        _messageCheckTimer += Time.deltaTime;
        if (_messageCheckTimer >= MESSAGE_CHECK_INTERVAL)
        {
            _messageCheckTimer = 0;
            LabelSyncManager.CheckForMessages();
        }
    }
*/

    public override void OnSceneWasInitialized(int buildIndex, string sceneName)
    {
        
    }

    private static void ActivateMod()
    {
        LabelPrefabManager.Initialize();
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