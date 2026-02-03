using System;
using System.Collections;
using System.Collections.Generic;
using MelonLoader;
using SteamNetworkLib;
using SteamNetworkLib.Sync;
using UnityEngine;
using Logger = SimpleLabels.Utils.Logger;

namespace SimpleLabels.Data
{
    /// <summary>
    /// Isolates all SteamNetworkLib usage so that the main mod never references it.
    /// When SteamNetworkLib is missing, only this class triggers the load (when its methods are first called).
    /// LabelNetworkManager calls the bridge only after catching the missing-DLL case in Initialize().
    /// </summary>
    internal static class SteamNetworkBridge
    {
        private static SteamNetworkClient _client;
        private static HostSyncVar<Dictionary<string, EntityData>> _syncedLabels;
        private static HostSyncVar<int> _syncVarTest;
        private static HostSyncVar<EntityData> _lastLabelChange;

        private static bool _initialized;

        /// <summary>
        /// Tries to load SteamNetworkLib and create the client. Call from LabelNetworkManager.Initialize() inside try/catch.
        /// Returns true if multiplayer is available, false otherwise. On success, runs the init coroutine.
        /// </summary>
        public static bool TryLoadAndInitialize(
            Action onLobbyCreated,
            Action onLobbyJoined,
            Action<Dictionary<string, EntityData>> onSyncedLabelsChanged,
            Action<EntityData> onLastLabelChangeReceived,
            Action onSyncVarTestChanged = null)
        {
            try
            {
                var rules = new SteamNetworkLib.Core.NetworkRules
                {
                    EnableRelay = true,
                    AcceptOnlyFriends = false
                };

                _client = new SteamNetworkClient(rules);
                MelonCoroutines.Start(TryInitializeSteamCoroutine(onLobbyCreated, onLobbyJoined, onSyncedLabelsChanged, onLastLabelChangeReceived, onSyncVarTestChanged));
                return true;
            }
            catch (Exception ex)
            {
                Logger.Msg($"[Mod] SteamNetworkLib could not be loaded: {ex.Message}. Multiplayer label sync disabled.");
                return false;
            }
        }

        private static IEnumerator TryInitializeSteamCoroutine(
            Action onLobbyCreated,
            Action onLobbyJoined,
            Action<Dictionary<string, EntityData>> onSyncedLabelsChanged,
            Action<EntityData> onLastLabelChangeReceived,
            Action onSyncVarTestChanged)
        {
            yield return new WaitForSeconds(2f);

            while (!_initialized)
            {
                try
                {
                    Logger.Msg("[Network] Attempting to initialize SteamNetworkClient...");
                    _client.Initialize();
                    _initialized = true;
                    Logger.Msg("[Network] SteamNetworkClient successfully connected!");

                    InitializeSyncVars();
                    SetupEventHandlers(onLobbyCreated, onLobbyJoined, onSyncedLabelsChanged, onLastLabelChangeReceived, onSyncVarTestChanged);
                }
                catch (Exception ex)
                {
                    Logger.Warning($"[Network] Steam not ready yet. Retrying in 2s... (Error: {ex.Message})");
                }

                if (!_initialized)
                    yield return new WaitForSeconds(2f);
            }
        }

        private static void InitializeSyncVars()
        {
            _syncedLabels = _client.CreateHostSyncVar<Dictionary<string, EntityData>>("SyncedLabels", new Dictionary<string, EntityData>());
            _syncVarTest = _client.CreateHostSyncVar<int>("SyncVarTest", 0);
            _lastLabelChange = _client.CreateHostSyncVar<EntityData>("LastLabelChange", new EntityData());
        }

        private static void SetupEventHandlers(
            Action onLobbyCreated,
            Action onLobbyJoined,
            Action<Dictionary<string, EntityData>> onSyncedLabelsChanged,
            Action<EntityData> onLastLabelChangeReceived,
            Action onSyncVarTestChanged)
        {
            _client.OnLobbyCreated += (_, __) => onLobbyCreated?.Invoke();
            _client.OnLobbyJoined += (_, __) => onLobbyJoined?.Invoke();
            _syncedLabels.OnValueChanged += (_, newVal) => onSyncedLabelsChanged?.Invoke(newVal);
            _lastLabelChange.OnValueChanged += (_, newVal) => onLastLabelChangeReceived?.Invoke(newVal);
            if (onSyncVarTestChanged != null)
                _syncVarTest.OnValueChanged += (_, __) => onSyncVarTestChanged?.Invoke();
        }

        public static void ProcessIncomingMessages() => _client?.ProcessIncomingMessages();

        public static bool GetIsHost() => _client?.IsHost == true;

        public static object GetCurrentLobby() => _client?.CurrentLobby;

        /// <summary>Returns SteamId of each lobby member so callers never reference SteamNetworkLib types.</summary>
        public static List<object> GetLobbyMemberSteamIds()
        {
            var list = new List<object>();
            if (_client == null) return list;
            foreach (var m in _client.GetLobbyMembers())
                list.Add(m.SteamId);
            return list;
        }

        public static object GetLocalPlayerId() => _client?.LocalPlayerId;

        public static string GetPlayerData(object steamId, string key)
        {
            if (_client == null) return null;
            try { return _client.GetPlayerData((dynamic)steamId, key); } catch { return null; }
        }

        public static void SetMyData(string key, string value)
        {
            try { _client?.SetMyData(key, value); } catch { }
        }

        public static void Dispose() => _client?.Dispose();

        public static Dictionary<string, EntityData> GetSyncedLabelsValue() => _syncedLabels?.Value;

        public static void SetSyncedLabelsValue(Dictionary<string, EntityData> value)
        {
            if (_syncedLabels != null) _syncedLabels.Value = value;
        }

        public static void SetLastLabelChangeValue(EntityData value)
        {
            if (_lastLabelChange != null) _lastLabelChange.Value = value;
        }

        public static int GetSyncedLabelsCount() => _syncedLabels?.Value?.Count ?? 0;
    }
}
