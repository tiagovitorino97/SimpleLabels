using System;
using System.Collections;
using System.Collections.Generic;
using MelonLoader;
using Newtonsoft.Json;
using SteamNetworkLib;
using SteamNetworkLib.Sync;
using UnityEngine;
using Logger = SimpleLabels.Utils.Logger;

namespace SimpleLabels.Data
{
    /// <summary>
    /// Isolates all SteamNetworkLib usage so that the main mod never references it.
    /// SyncVars work poorly with dictionaries - use Lobby Data (host sets JSON string) for full sync.
    /// Keep HostSyncVar&lt;EntityData&gt; for real-time single-label updates.
    /// </summary>
    internal static class SteamNetworkBridge
    {
        private const string LobbyDataKeyLabels = "SimpleLabels_Labels";

        private static SteamNetworkClient _client;
        private static HostSyncVar<EntityData> _lastLabelChange;

        private static bool _initialized;

        public static bool TryLoadAndInitialize(
            Action onLobbyCreated,
            Action onLobbyJoined,
            Action<EntityData> onLastLabelChangeReceived)
        {
            try
            {
                var rules = new SteamNetworkLib.Core.NetworkRules
                {
                    EnableRelay = true,
                    AcceptOnlyFriends = false
                };

                _client = new SteamNetworkClient(rules);
                MelonCoroutines.Start(TryInitializeSteamCoroutine(onLobbyCreated, onLobbyJoined, onLastLabelChangeReceived));
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
            Action<EntityData> onLastLabelChangeReceived)
        {
            yield return new WaitForSeconds(2f);

            while (!_initialized)
            {
                try
                {
                    _client.Initialize();
                    _initialized = true;
                    Logger.Msg("[Network] Connected.");

                    _lastLabelChange = _client.CreateHostSyncVar<EntityData>("LastLabelChange", new EntityData());
                    _client.OnLobbyCreated += (_, __) => onLobbyCreated?.Invoke();
                    _client.OnLobbyJoined += (_, __) => onLobbyJoined?.Invoke();
                    _lastLabelChange.OnValueChanged += (_, newVal) => onLastLabelChangeReceived?.Invoke(newVal);
                }
                catch (Exception ex)
                {
                    Logger.Warning($"[Network] Steam not ready, retrying... ({ex.Message})");
                }

                if (!_initialized)
                    yield return new WaitForSeconds(2f);
            }
        }

        public static void ProcessIncomingMessages() => _client?.ProcessIncomingMessages();

        public static bool GetIsHost() => _client?.IsHost == true;

        public static object GetCurrentLobby() => _client?.CurrentLobby;

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

        public static void SetLastLabelChangeValue(EntityData value)
        {
            if (_lastLabelChange != null) _lastLabelChange.Value = value;
        }

        /// <summary>Host sets lobby data with JSON of labels. Lobby Data works for strings; SyncVars fail with dictionaries.</summary>
        public static void SetLobbyLabelsJson(string json)
        {
            try
            {
                if (_client != null && _client.IsHost)
                    _client.SetLobbyData(LobbyDataKeyLabels, json);
            }
            catch (Exception ex) { Logger.Error($"[Network] Failed to set lobby labels: {ex.Message}"); }
        }

        /// <summary>Client reads labels from lobby data (host sets it). Returns null if not found or empty.</summary>
        public static Dictionary<string, EntityData> GetLobbyLabelsFromJson()
        {
            try
            {
                if (_client == null) return null;
                var json = _client.GetLobbyData(LobbyDataKeyLabels);
                if (string.IsNullOrEmpty(json)) return null;
                var data = JsonConvert.DeserializeObject<Dictionary<string, EntityData>>(json);
                return data;
            }
            catch (Exception ex)
            {
                Logger.Error($"[Network] Failed to get lobby labels: {ex.Message}");
                return null;
            }
        }
    }
}
