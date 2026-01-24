using MelonLoader;
using SteamNetworkLib;
using SteamNetworkLib.Sync;
using System.Collections;
using UnityEngine;
using Newtonsoft.Json;
using Logger = SimpleLabels.Utils.Logger;
using Il2CppScheduleOne.Building;
using Il2CppScheduleOne.EntityFramework;

namespace SimpleLabels.Data
{
    public class LabelNetworkManager
    {

        private static SteamNetworkClient client;
        private static bool _isInitialized = false;
        public static HostSyncVar<Dictionary<string, LabelTracker.EntityData>> _syncedLabels;
        public static HostSyncVar<int> _syncVarTest;
        public static HostSyncVar<LabelTracker.EntityData> _lastLabelChange;
        
        private static Dictionary<string, string> _lastSeenMemberData = new Dictionary<string, string>();

        public static void Initialize()
        {
            var rules = new SteamNetworkLib.Core.NetworkRules
            {
                EnableRelay = true,
                AcceptOnlyFriends = false
            };

            client = new SteamNetworkClient(rules);
            MelonCoroutines.Start(TryInitializeSteam());
        }


        public static void Update()
        {
            client?.ProcessIncomingMessages();
            
            if (client?.IsHost == true && client?.CurrentLobby != null)
            {
                CheckMemberDataForLabelChanges();
            }
        }
        
        private static void CheckMemberDataForLabelChanges()
        {
            try
            {
                var members = client.GetLobbyMembers();
                foreach (var member in members)
                {
                    if (member.SteamId == client.LocalPlayerId)
                        continue;
                    
                    var lastLabelChange = client.GetPlayerData(member.SteamId, "LastLabelChange");
                    var memberKey = member.SteamId.ToString();
                    
                    if (!string.IsNullOrEmpty(lastLabelChange))
                    {
                        if (!_lastSeenMemberData.TryGetValue(memberKey, out var lastValue) || lastValue != lastLabelChange)
                        {
                            _lastSeenMemberData[memberKey] = lastLabelChange;
                            Logger.Msg($"[Network] Host detected label change from client {member.SteamId}");
                            ProcessClientLabelChange(lastLabelChange, member.SteamId);
                        }
                    }
                }
            }
            catch (System.Exception)
            {
            }
        }
        
        private static void ProcessClientLabelChange(string jsonData, object memberId)
        {
            try
            {
                if (string.IsNullOrEmpty(jsonData))
                    return;

                var labelData = JsonConvert.DeserializeObject<LabelTracker.EntityData>(jsonData);
                if (labelData == null || string.IsNullOrEmpty(labelData.Guid))
                {
                    Logger.Error($"[Network] Failed to deserialize label data from client {memberId}");
                    return;
                }

                Logger.Msg($"[Network] Host processing label change from client {memberId}: GUID={labelData.Guid}, Text='{labelData.LabelText}'");

                var existing = LabelTracker.GetEntityData(labelData.Guid);
                if (existing == null)
                {
                    LabelTracker.TrackEntity(
                        labelData.Guid,
                        null,
                        labelData.LabelText,
                        labelData.LabelColor,
                        labelData.LabelSize,
                        labelData.FontSize,
                        labelData.FontColor
                    );
                }
                else
                {
                    LabelTracker.UpdateLabelFromNetwork(
                        labelData.Guid,
                        newLabelText: labelData.LabelText,
                        newLabelColor: labelData.LabelColor,
                        newLabelSize: labelData.LabelSize,
                        newFontSize: labelData.FontSize,
                        newFontColor: labelData.FontColor
                    );
                }

                NotifyLabelChanged(labelData.Guid);
            }
            catch (System.Exception ex)
            {
                Logger.Error($"[Network] Failed to process label change: {ex.Message}");
            }
        }

        public static void Terminate()
        {
            client?.Dispose();
        }

        private static IEnumerator TryInitializeSteam()
        {
            yield return new WaitForSeconds(2f);

            while (!_isInitialized)
            {
                try
                {
                    Logger.Msg("[Network] Attempting to initialize SteamNetworkClient...");
                    client.Initialize();

                    _isInitialized = true;
                    Logger.Msg("[Network] SteamNetworkClient successfully connected!");
                }
                catch (System.Exception ex)
                {
                    Logger.Warning($"[Network] Steam not ready yet. Retrying in 2s... (Error: {ex.Message})");
                }

                if (!_isInitialized)
                {
                    yield return new WaitForSeconds(2f);
                }

                if (client.Initialize())
                {
                    _syncedLabels = client.CreateHostSyncVar<Dictionary<string, LabelTracker.EntityData>>("SyncedLabels", new Dictionary<string, LabelTracker.EntityData>());
                    _syncVarTest = client.CreateHostSyncVar<int>("SyncVarTest", 0);
                    _lastLabelChange = client.CreateHostSyncVar<LabelTracker.EntityData>("LastLabelChange", new LabelTracker.EntityData());


                    client.OnLobbyCreated += (s, e) =>
                    {
                        if (client.IsHost)
                        {
                            _syncedLabels.Value = LabelTracker.GetAllEntityData();
                            Logger.Msg($"[Network] Host initialized. Synced {_syncedLabels.Value.Count} labels to network.");
                        }
                    };
                    client.OnLobbyJoined += (s, e) =>
                    {
                        if (client.IsHost) return;
                        Logger.Msg($"[Network] Client joined lobby. Synced labels count: {_syncedLabels.Value.Count}");
                    };
                    client.OnMemberJoined += (s, e) =>
                    {
                    };

                    _syncVarTest.OnValueChanged += (oldValue, newValue) =>
                    {
                        if (client.IsHost) return;
                        LoadSyncedLabels();
                    };

                    _lastLabelChange.OnValueChanged += (oldValue, newValue) =>
                    {
                        if (client.IsHost) return;
                        if (newValue == null || string.IsNullOrEmpty(newValue.Guid)) return;

                        Logger.Msg($"[Network] Client received label change from host: GUID={newValue.Guid}, Text='{newValue.LabelText}'");

                        var existing = LabelTracker.GetEntityData(newValue.Guid);
                        if (existing == null)
                        {
                            LabelTracker.TrackEntity(
                                newValue.Guid,
                                null,
                                newValue.LabelText,
                                newValue.LabelColor,
                                newValue.LabelSize,
                                newValue.FontSize,
                                newValue.FontColor
                            );
                            BindGameObjectsAndApplyLabels();
                        }
                        else
                        {
                            LabelTracker.UpdateLabelFromNetwork(
                                newValue.Guid,
                                newLabelText: newValue.LabelText,
                                newLabelColor: newValue.LabelColor,
                                newLabelSize: newValue.LabelSize,
                                newFontSize: newValue.FontSize,
                                newFontColor: newValue.FontColor
                            );
                        }
                    };

                }
            }
        }

        public static void SyncLabelsToNetwork()
        {
            if (client?.IsHost == true && _syncedLabels != null)
            {
                _syncedLabels.Value = LabelTracker.GetAllEntityData();
                Logger.Msg($"[Network] Host synced {_syncedLabels.Value.Count} labels to network.");
            }
        }

        public static void NotifyLabelChanged(string guid)
        {
            if (string.IsNullOrEmpty(guid))
                return;

            if (client?.IsHost == true && _lastLabelChange != null)
            {
                var entity = LabelTracker.GetEntityData(guid);
                if (entity == null)
                    return;

                Logger.Msg($"[Network] Host broadcasting label change: GUID={guid}, Text='{entity.LabelText}'");

                var payload = new LabelTracker.EntityData(
                    guid: entity.Guid,
                    labelText: entity.LabelText,
                    labelColor: entity.LabelColor,
                    labelSize: entity.LabelSize,
                    fontSize: entity.FontSize,
                    fontColor: entity.FontColor
                );

                _lastLabelChange.Value = payload;
            }
            else if (client?.IsHost == false && client?.CurrentLobby != null)
            {
                var entity = LabelTracker.GetEntityData(guid);
                if (entity == null)
                    return;

                Logger.Msg($"[Network] Client sending label change to host: GUID={guid}, Text='{entity.LabelText}'");

                var labelDataJson = JsonConvert.SerializeObject(new LabelTracker.EntityData(
                    guid: entity.Guid,
                    labelText: entity.LabelText,
                    labelColor: entity.LabelColor,
                    labelSize: entity.LabelSize,
                    fontSize: entity.FontSize,
                    fontColor: entity.FontColor
                ));

                try
                {
                    client.SetMyData("LastLabelChange", labelDataJson);
                }
                catch (System.Exception ex)
                {
                    Logger.Error($"[Network] Failed to set member data: {ex.Message}");
                }
            }
        }

        public static void LoadSyncedLabels()
        {
            if (client?.IsHost == false && _syncedLabels != null)
            {
                Logger.Msg($"[Network] Client loading {_syncedLabels.Value.Count} synced labels from network.");
                MelonCoroutines.Start(LoadSyncedLabelsRoutine());
            }
        }

        private static IEnumerator LoadSyncedLabelsRoutine()
        {
            LabelTracker.UpdateLocalLabelsFromNetwork(_syncedLabels.Value);
            yield return new WaitForSeconds(1f);
            BindGameObjectsAndApplyLabels();
            Logger.Msg("[Network] Client finished loading synced labels.");
        }

        public static void BindGameObjectsAndApplyLabels()
        {
            var gridItems = UnityEngine.Object.FindObjectsOfType<GridItem>();
            var surfaceItems = UnityEngine.Object.FindObjectsOfType<SurfaceItem>();

            var guidToGo = new Dictionary<string, GameObject>();

            foreach (var item in gridItems)
            {
                if (item == null) continue;
                var guid = item.GUID.ToString();
                if (string.IsNullOrEmpty(guid)) continue;
                if (!guidToGo.ContainsKey(guid))
                    guidToGo.Add(guid, item.gameObject);
            }

            foreach (var item in surfaceItems)
            {
                if (item == null) continue;
                var guid = item.GUID.ToString();
                if (string.IsNullOrEmpty(guid)) continue;
                if (!guidToGo.ContainsKey(guid))
                    guidToGo.Add(guid, item.gameObject);
            }

            foreach (var guid in LabelTracker.GetAllTrackedGuids())
            {
                var entityData = LabelTracker.GetEntityData(guid);
                if (entityData == null) continue;

                if (entityData.GameObject == null && guidToGo.TryGetValue(guid, out var go))
                {
                    Logger.Msg($"[Network] Binding GameObject for synced label: GUID={guid}");
                    LabelTracker.UpdateGameObjectReference(guid, go);
                    entityData = LabelTracker.GetEntityData(guid);
                }

                if (entityData != null && entityData.GameObject != null && !string.IsNullOrEmpty(entityData.LabelText))
                {
                    UI.LabelApplier.ApplyOrUpdateLabel(guid);
                }
            }
        }

    }
}
