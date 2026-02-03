using System.Collections;
using System.Collections.Generic;
using MelonLoader;
using Newtonsoft.Json;
using UnityEngine;
using Il2CppScheduleOne.Building;
using Il2CppScheduleOne.EntityFramework;
using SimpleLabels.Services;
using Logger = SimpleLabels.Utils.Logger;

namespace SimpleLabels.Data
{
    /// <summary>
    /// Handles multiplayer sync of labels via SteamNetworkLib. Host owns synced state; clients request and receive updates.
    /// All SteamNetworkLib usage is isolated in <see cref="SteamNetworkBridge"/> so the mod runs without the DLL (single-player only).
    /// </summary>
    public class LabelNetworkManager
    {
        private static bool _multiplayerAvailable = false;

        private static Dictionary<string, string> _lastSeenMemberData = new Dictionary<string, string>();
        private static Dictionary<string, string> _lastSeenSyncRequest = new Dictionary<string, string>();
        private static float _lastSyncForRequestTime;
        private const float MinSyncRequestIntervalSeconds = 2f;
        private static bool _clientSyncRetryRunning;
        private static HashSet<object> _knownMembers = new HashSet<object>();

        /// <summary>Whether multiplayer label sync is available (SteamNetworkLib present in UserLibs).</summary>
        public static bool IsMultiplayerAvailable => _multiplayerAvailable;

        public static void Initialize()
        {
            try
            {
                _multiplayerAvailable = SteamNetworkBridge.TryLoadAndInitialize(
                    onLobbyCreated: OnLobbyCreated,
                    onLobbyJoined: OnLobbyJoined,
                    onSyncedLabelsChanged: OnSyncedLabelsChanged,
                    onLastLabelChangeReceived: OnLastLabelChangeReceived,
                    onSyncVarTestChanged: () => LoadSyncedLabels());
            }
            catch (System.IO.FileNotFoundException)
            {
                Logger.Msg("[Mod] SteamNetworkLib not found (UserLibs). Multiplayer label sync disabled. Mod works in single-player only.");
                _multiplayerAvailable = false;
            }
            catch (System.Exception ex)
            {
                Logger.Msg($"[Mod] SteamNetworkLib could not be loaded: {ex.Message}. Multiplayer label sync disabled.");
                _multiplayerAvailable = false;
            }

            if (_multiplayerAvailable)
            {
                _knownMembers.Clear();
                _lastSeenSyncRequest.Clear();
                if (SteamNetworkBridge.GetIsHost())
                    _knownMembers.Add(SteamNetworkBridge.GetLocalPlayerId());
            }
        }

        public static void Update()
        {
            if (!_multiplayerAvailable) return;

            SteamNetworkBridge.ProcessIncomingMessages();

            if (SteamNetworkBridge.GetIsHost() && SteamNetworkBridge.GetCurrentLobby() != null)
            {
                CheckMemberDataForLabelChanges();
                CheckForNewMembers();
                CheckForSyncRequests();
            }
        }

        private static void CheckForNewMembers()
        {
            try
            {
                var steamIds = SteamNetworkBridge.GetLobbyMemberSteamIds();
                foreach (var memberId in steamIds)
                {
                    if (!_knownMembers.Contains(memberId))
                    {
                        _knownMembers.Add(memberId);
                        if (!Equals(memberId, SteamNetworkBridge.GetLocalPlayerId()))
                        {
                            Logger.Msg($"[Network] New member detected: {memberId}. Syncing labels...");
                            SyncLabelsToNetwork();
                        }
                    }
                }
            }
            catch (System.Exception) { }
        }

        private static void CheckForSyncRequests()
        {
            try
            {
                var now = UnityEngine.Time.realtimeSinceStartup;
                if (now - _lastSyncForRequestTime < MinSyncRequestIntervalSeconds)
                    return;

                var steamIds = SteamNetworkBridge.GetLobbyMemberSteamIds();
                var localId = SteamNetworkBridge.GetLocalPlayerId();

                foreach (var steamId in steamIds)
                {
                    if (Equals(steamId, localId)) continue;

                    var req = SteamNetworkBridge.GetPlayerData(steamId, "RequestLabelSync");
                    var key = steamId.ToString();
                    if (string.IsNullOrEmpty(req)) continue;

                    if (!_lastSeenSyncRequest.TryGetValue(key, out var last) || last != req)
                    {
                        _lastSeenSyncRequest[key] = req;
                        _lastSyncForRequestTime = now;
                        Logger.Msg($"[Network] Host saw sync request from {steamId}. Syncing labels...");
                        SyncLabelsToNetwork();
                        break;
                    }
                }
            }
            catch (System.Exception) { }
        }

        private static void CheckMemberDataForLabelChanges()
        {
            try
            {
                var steamIds = SteamNetworkBridge.GetLobbyMemberSteamIds();
                var localId = SteamNetworkBridge.GetLocalPlayerId();

                foreach (var steamId in steamIds)
                {
                    if (Equals(steamId, localId)) continue;

                    var lastLabelChange = SteamNetworkBridge.GetPlayerData(steamId, "LastLabelChange");
                    var memberKey = steamId.ToString();

                    if (!string.IsNullOrEmpty(lastLabelChange) &&
                        (!_lastSeenMemberData.TryGetValue(memberKey, out var lastValue) || lastValue != lastLabelChange))
                    {
                        _lastSeenMemberData[memberKey] = lastLabelChange;
                        Logger.Msg($"[Network] Host detected label change from client {steamId}");
                        ProcessClientLabelChange(lastLabelChange, steamId);
                    }
                }
            }
            catch (System.Exception) { }
        }

        private static void ProcessClientLabelChange(string jsonData, object memberId)
        {
            try
            {
                if (string.IsNullOrEmpty(jsonData)) return;

                var labelData = JsonConvert.DeserializeObject<EntityData>(jsonData);
                if (labelData == null || string.IsNullOrEmpty(labelData.Guid))
                {
                    Logger.Error($"[Network] Failed to deserialize label data from client {memberId}");
                    return;
                }

                Logger.Msg($"[Network] Host processing label change from client {memberId}: GUID={labelData.Guid}, Text='{labelData.LabelText}'");

                LabelService.UpdateLabelFromNetwork(
                    labelData.Guid,
                    newLabelText: labelData.LabelText,
                    newLabelColor: labelData.LabelColor,
                    newLabelSize: labelData.LabelSize,
                    newFontSize: labelData.FontSize,
                    newFontColor: labelData.FontColor);
                NotifyLabelChanged(labelData.Guid);
            }
            catch (System.Exception ex)
            {
                Logger.Error($"[Network] Failed to process label change: {ex.Message}");
            }
        }

        public static void Terminate()
        {
            if (!_multiplayerAvailable) return;
            SteamNetworkBridge.Dispose();
        }

        private static void OnSyncedLabelsChanged(Dictionary<string, EntityData> newValue)
        {
            if (SteamNetworkBridge.GetIsHost()) return;
            if (newValue != null && newValue.Count > 0)
            {
                Logger.Msg($"[Network] Client received updated synced labels from host ({newValue.Count} labels). Loading...");
                LoadSyncedLabels();
            }
        }

        private static void OnLobbyCreated()
        {
            if (SteamNetworkBridge.GetIsHost())
                SyncLabelsToNetwork();
        }

        private static void OnLobbyJoined()
        {
            if (SteamNetworkBridge.GetIsHost()) return;
            Logger.Msg($"[Network] Client joined lobby. Synced labels count: {SteamNetworkBridge.GetSyncedLabelsCount()}");
            LoadSyncedLabels();
        }

        private static void OnLastLabelChangeReceived(EntityData newValue)
        {
            if (SteamNetworkBridge.GetIsHost()) return;
            if (newValue == null || string.IsNullOrEmpty(newValue.Guid)) return;

            Logger.Msg($"[Network] Client received label change from host: GUID={newValue.Guid}, Text='{newValue.LabelText}'");

            var existing = LabelTracker.GetEntityData(newValue.Guid);
            if (existing == null)
            {
                LabelService.CreateLabel(
                    newValue.Guid, null,
                    newValue.LabelText, newValue.LabelColor, newValue.LabelSize,
                    newValue.FontSize, newValue.FontColor);
                LabelService.BindAllGameObjectsAndApplyLabels();
            }
            else
            {
                LabelService.UpdateLabelFromNetwork(
                    newValue.Guid,
                    newLabelText: newValue.LabelText,
                    newLabelColor: newValue.LabelColor,
                    newLabelSize: newValue.LabelSize,
                    newFontSize: newValue.FontSize,
                    newFontColor: newValue.FontColor);
            }
        }

        public static void SyncLabelsToNetwork()
        {
            if (!_multiplayerAvailable) return;
            if (!SteamNetworkBridge.GetIsHost()) return;

            var data = LabelTracker.GetAllEntityData();
            SteamNetworkBridge.SetSyncedLabelsValue(data);
            Logger.Msg($"[Network] Host synced {data.Count} labels to network.");
        }

        public static void OnNewMemberJoined()
        {
            if (!_multiplayerAvailable) return;
            if (SteamNetworkBridge.GetIsHost())
            {
                SyncLabelsToNetwork();
                Logger.Msg("[Network] Host synced labels for new member.");
            }
        }

        public static void NotifyLabelChanged(string guid)
        {
            if (!_multiplayerAvailable) return;
            if (string.IsNullOrEmpty(guid)) return;

            var entity = LabelTracker.GetEntityData(guid);
            if (entity == null) return;

            if (SteamNetworkBridge.GetIsHost())
            {
                Logger.Msg($"[Network] Host broadcasting label change: GUID={guid}, Text='{entity.LabelText}'");
                var payload = new EntityData(guid, entity.LabelText, entity.LabelColor, entity.LabelSize, entity.FontSize, entity.FontColor);
                SteamNetworkBridge.SetLastLabelChangeValue(payload);
            }
            else if (SteamNetworkBridge.GetCurrentLobby() != null)
            {
                Logger.Msg($"[Network] Client sending label change to host: GUID={guid}, Text='{entity.LabelText}'");
                var payload = new EntityData(guid, entity.LabelText, entity.LabelColor, entity.LabelSize, entity.FontSize, entity.FontColor);
                try
                {
                    SteamNetworkBridge.SetMyData("LastLabelChange", JsonConvert.SerializeObject(payload));
                }
                catch (System.Exception ex)
                {
                    Logger.Error($"[Network] Failed to set member data: {ex.Message}");
                }
            }
        }

        public static void LoadSyncedLabels()
        {
            if (!_multiplayerAvailable) return;
            if (SteamNetworkBridge.GetIsHost() || SteamNetworkBridge.GetCurrentLobby() == null) return;

            var data = SteamNetworkBridge.GetSyncedLabelsValue();
            var count = data?.Count ?? 0;
            Logger.Msg($"[Network] Client loading {count} synced labels from network.");
            MelonCoroutines.Start(LoadSyncedLabelsRoutine(data ?? new Dictionary<string, EntityData>()));
            if (count == 0 && !_clientSyncRetryRunning)
                MelonCoroutines.Start(ClientSyncRetryRoutine());
        }

        private static IEnumerator LoadSyncedLabelsRoutine(Dictionary<string, EntityData> data)
        {
            if (data == null) yield break;

            LabelService.ApplyNetworkLabels(data);
            yield return new WaitForSeconds(1f);
            LabelService.BindAllGameObjectsAndApplyLabels();
            Logger.Msg("[Network] Client finished loading synced labels.");
        }

        private const int ClientSyncRetryMax = 15;
        private const float ClientSyncRetryIntervalSeconds = 2.5f;

        private static IEnumerator ClientSyncRetryRoutine()
        {
            _clientSyncRetryRunning = true;

            for (var i = 0; i < ClientSyncRetryMax; i++)
            {
                yield return new WaitForSeconds(ClientSyncRetryIntervalSeconds);

                if (SteamNetworkBridge.GetIsHost())
                {
                    _clientSyncRetryRunning = false;
                    yield break;
                }

                var count = SteamNetworkBridge.GetSyncedLabelsCount();
                if (count > 0)
                {
                    Logger.Msg("[Network] Client received labels, stopping sync retry.");
                    _clientSyncRetryRunning = false;
                    yield break;
                }

                Logger.Msg($"[Network] Client retrying label sync (attempt {i + 2}/{ClientSyncRetryMax})...");
                RequestLabelSyncFromHost();
                LoadSyncedLabels();
            }

            Logger.Msg("[Network] Client gave up label sync retries.");
            _clientSyncRetryRunning = false;
        }

        public static void RequestLabelSyncFromHost()
        {
            if (!_multiplayerAvailable) return;
            if (SteamNetworkBridge.GetIsHost() || SteamNetworkBridge.GetCurrentLobby() == null) return;
            try
            {
                SteamNetworkBridge.SetMyData("RequestLabelSync", System.DateTime.UtcNow.Ticks.ToString());
            }
            catch (System.Exception ex)
            {
                Logger.Error($"[Network] Failed to request label sync: {ex.Message}");
            }
        }
    }
}
