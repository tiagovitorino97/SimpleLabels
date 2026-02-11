using System.Collections;
using System.Collections.Generic;
using MelonLoader;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.SceneManagement;
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
                    onLastLabelChangeReceived: OnLastLabelChangeReceived);
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
                            Logger.Msg($"[Network] New member joined, syncing labels.");
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
                        Logger.Msg($"[Network] Client requested sync, syncing labels.");
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
                        Logger.Msg($"[Network] Client sent label update.");
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

                // Entity may not exist on host (e.g. wiped by ResetLabelTracker when loading from file).
                // Create if missing, update if present.
                var existing = LabelTracker.GetEntityData(labelData.Guid);
                if (existing != null)
                {
                    LabelService.UpdateLabelFromNetwork(
                        labelData.Guid,
                        newLabelText: labelData.LabelText,
                        newLabelColor: labelData.LabelColor,
                        newLabelSize: labelData.LabelSize,
                        newFontSize: labelData.FontSize,
                        newFontColor: labelData.FontColor);
                }
                else
                {
                    LabelService.CreateLabel(labelData.Guid, null,
                        labelText: labelData.LabelText,
                        labelColor: labelData.LabelColor,
                        labelSize: labelData.LabelSize,
                        fontSize: labelData.FontSize,
                        fontColor: labelData.FontColor,
                        fromNetwork: true);
                    // Bind GameObject so the host shows the label visually (entity exists in scene).
                    LabelService.BindGameObjectForGuid(labelData.Guid);
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
            if (!_multiplayerAvailable) return;
            SteamNetworkBridge.Dispose();
        }

        private static void OnLobbyCreated()
        {
            if (SteamNetworkBridge.GetIsHost())
                SyncLabelsToNetwork();
        }

        private static void OnLobbyJoined()
        {
            if (SteamNetworkBridge.GetIsHost()) return;
            LoadSyncedLabels();
        }

        private static void OnLastLabelChangeReceived(EntityData newValue)
        {
            if (SteamNetworkBridge.GetIsHost()) return;
            if (newValue == null || string.IsNullOrEmpty(newValue.Guid)) return;

            var existing = LabelTracker.GetEntityData(newValue.Guid);
            if (existing == null)
            {
                LabelService.CreateLabel(
                    newValue.Guid, null,
                    newValue.LabelText, newValue.LabelColor, newValue.LabelSize,
                    newValue.FontSize, newValue.FontColor,
                    fromNetwork: true);
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
            var applied = new Dictionary<string, EntityData>();
            foreach (var kvp in data)
            {
                if (string.IsNullOrEmpty(kvp.Value?.Guid)) continue;
                applied[kvp.Key] = new EntityData(kvp.Value.Guid, kvp.Value.LabelText ?? "", kvp.Value.LabelColor, kvp.Value.LabelSize, kvp.Value.FontSize, kvp.Value.FontColor);
            }
            var json = JsonConvert.SerializeObject(applied);
            SteamNetworkBridge.SetLobbyLabelsJson(json);
            Logger.Msg($"[Network] Synced {applied.Count} labels to lobby.");
        }

        public static void NotifyLabelChanged(string guid)
        {
            if (!_multiplayerAvailable) return;
            if (string.IsNullOrEmpty(guid)) return;

            var entity = LabelTracker.GetEntityData(guid);
            if (entity == null) return;

            if (SteamNetworkBridge.GetIsHost())
            {
                var payload = new EntityData(guid, entity.LabelText, entity.LabelColor, entity.LabelSize, entity.FontSize, entity.FontColor);
                SteamNetworkBridge.SetLastLabelChangeValue(payload);
            }
            else if (SteamNetworkBridge.GetCurrentLobby() != null)
            {
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

            var data = SteamNetworkBridge.GetLobbyLabelsFromJson();
            var count = data?.Count ?? 0;
            Logger.Msg($"[Network] Loading {count} labels from host.");
            MelonCoroutines.Start(LoadSyncedLabelsRoutine(data ?? new Dictionary<string, EntityData>()));
            var trackedCount = LabelTracker.GetAllTrackedGuids().Count;
            if (count == 0 && trackedCount == 0 && !_clientSyncRetryRunning && IsInMainScene())
                MelonCoroutines.Start(ClientSyncRetryRoutine());
        }

        private static IEnumerator LoadSyncedLabelsRoutine(Dictionary<string, EntityData> data)
        {
            if (data == null) yield break;

            LabelService.ApplyNetworkLabels(data);
            yield return new WaitForSeconds(1f);
            LabelService.BindAllGameObjectsAndApplyLabels();
            Logger.Msg("[Network] Labels loaded.");
        }

        private const int ClientSyncRetryMax = 15;
        private const float ClientSyncRetryIntervalSeconds = 2.5f;

        private static bool IsInMainScene()
        {
            try
            {
                return SceneManager.GetActiveScene().name == "Main";
            }
            catch { return false; }
        }

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

                if (!IsInMainScene())
                    continue;

                var lobbyData = SteamNetworkBridge.GetLobbyLabelsFromJson();
                var count = lobbyData?.Count ?? 0;
                var trackedCount = LabelTracker.GetAllTrackedGuids().Count;
                if (count > 0 || trackedCount > 0)
                {
                    Logger.Msg($"[Network] Labels loaded ({trackedCount} applied).");
                    _clientSyncRetryRunning = false;
                    yield break;
                }

                Logger.Msg($"[Network] Waiting for host labels ({i + 2}/{ClientSyncRetryMax})...");
                RequestLabelSyncFromHost();
                LoadSyncedLabels();
            }

            Logger.Msg("[Network] Could not load labels from host.");
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
