using System.Collections.Generic;
using SimpleLabels.Data;
using SimpleLabels.Settings;
using SimpleLabels.UI;
using UnityEngine;
using Il2CppScheduleOne.Building;
using Il2CppScheduleOne.EntityFramework;
using Logger = SimpleLabels.Utils.Logger;

namespace SimpleLabels.Services
{
    /// <summary>
    /// Central service for all label operations. This is the single entry point for creating,
    /// updating, and removing labels. It orchestrates state changes, visual updates, and synchronization.
    /// </summary>
    /// <remarks>
    /// Callers (patches, UI, loaders) should use LabelService rather than LabelTracker or LabelApplier
    /// directly. The service ensures state, visuals, and network notification stay in sync.
    /// Use <see cref="UpdateLabelFromNetwork"/> when applying data from multiplayer sync to avoid
    /// broadcast loops.
    /// </remarks>
    public static class LabelService
    {
        /// <summary>
        /// Creates a new label for an entity. This is the primary way to register entities with the label system.
        /// </summary>
        /// <remarks>
        /// Uses mod defaults for color, size, and font when not provided. Applies the physical label
        /// only if <paramref name="gameObject"/> is non-null and <paramref name="labelText"/> is non-empty.
        /// Always notifies the network layer so multiplayer clients receive the new label.
        /// </remarks>
        public static void CreateLabel(string guid, GameObject gameObject, string labelText = "", 
            string labelColor = null, int? labelSize = null, int? fontSize = null, string fontColor = null)
        {
            if (string.IsNullOrEmpty(guid))
            {
                Logger.Error("[LabelService] Cannot create label: GUID is empty");
                return;
            }

            // Use defaults if not provided
            var finalLabelColor = labelColor ?? ModSettings.LabelDefaultColor.Value;
            var finalLabelSize = labelSize ?? ModSettings.LabelDefaultSize.Value;
            var finalFontSize = fontSize ?? ModSettings.DEFAULT_FONT_SIZE;
            var finalFontColor = fontColor ?? ModSettings.FontDefaultColor.Value;

            // Store state
            LabelTracker.StoreEntity(guid, gameObject, labelText, finalLabelColor, finalLabelSize, finalFontSize, finalFontColor);

            // Apply visual representation if we have a GameObject and text
            if (gameObject != null && !string.IsNullOrEmpty(labelText))
            {
                LabelApplier.ApplyOrUpdateLabel(guid);
            }

            // Notify network of change (if this is a local change, not from network)
            LabelNetworkManager.NotifyLabelChanged(guid);
        }

        /// <summary>
        /// Updates an existing label. This is the primary way to modify label properties.
        /// </summary>
        /// <remarks>
        /// Only provided parameters are updated; nulls leave existing values unchanged. Triggers
        /// <see cref="LabelApplier.ApplyOrUpdateLabel"/> and, when <paramref name="fromNetwork"/> is false,
        /// notifies the network so other players see the change. Use <paramref name="fromNetwork"/> = true
        /// when applying sync data to avoid feedback loops.
        /// </remarks>
        public static void UpdateLabel(string guid, string newLabelText = null, string newLabelColor = null,
            int? newLabelSize = null, int? newFontSize = null, string newFontColor = null, bool fromNetwork = false)
        {
            if (string.IsNullOrEmpty(guid))
            {
                Logger.Error("[LabelService] Cannot update label: GUID is empty");
                return;
            }

            var entityData = LabelTracker.GetEntityData(guid);
            if (entityData == null)
            {
                Logger.Warning($"[LabelService] Cannot update label: Entity {guid} not found");
                return;
            }

            // Update state
            LabelTracker.UpdateEntityData(guid, newLabelText, newLabelColor, newLabelSize, newFontSize, newFontColor);

            // Apply visual representation
            LabelApplier.ApplyOrUpdateLabel(guid);

            // Notify network only if this is a local change (not from network sync)
            if (!fromNetwork)
            {
                LabelNetworkManager.NotifyLabelChanged(guid);
            }
        }

        /// <summary>
        /// Removes a label from an entity (clears the text).
        /// </summary>
        /// <remarks>
        /// Implemented as <c>UpdateLabel(guid, newLabelText: "")</c>. The entity remains tracked;
        /// LabelApplier hides the physical label when text is empty.
        /// </remarks>
        public static void RemoveLabel(string guid)
        {
            if (string.IsNullOrEmpty(guid))
                return;

            UpdateLabel(guid, newLabelText: "");
        }

        /// <summary>
        /// Binds a GameObject to an existing label entity. Used when entities are loaded/created.
        /// </summary>
        /// <remarks>
        /// Entity must already exist in LabelTracker (e.g. from persistence or network). If the entity
        /// has label text, the physical label is applied immediately. Call this when builds are placed
        /// or when syncing from network and GameObjects become available.
        /// </remarks>
        public static void BindGameObject(string guid, GameObject gameObject)
        {
            if (string.IsNullOrEmpty(guid) || gameObject == null)
                return;

            var entityData = LabelTracker.GetEntityData(guid);
            if (entityData == null)
            {
                Logger.Warning($"[LabelService] Cannot bind GameObject: Entity {guid} not found");
                return;
            }

            // Update state
            LabelTracker.UpdateGameObjectReference(guid, gameObject);

            // Apply visual if we have text
            if (!string.IsNullOrEmpty(entityData.LabelText))
            {
                LabelApplier.ApplyOrUpdateLabel(guid);
            }
        }

        /// <summary>
        /// Updates label from network synchronization. This bypasses network notification to avoid loops.
        /// </summary>
        /// <remarks>
        /// Forwards to <see cref="UpdateLabel"/> with <c>fromNetwork: true</c>. Use only when applying
        /// data received from the host or other clients; do not use for local player edits.
        /// </remarks>
        public static void UpdateLabelFromNetwork(string guid, string newLabelText = null, string newLabelColor = null,
            int? newLabelSize = null, int? newFontSize = null, string newFontColor = null)
        {
            UpdateLabel(guid, newLabelText, newLabelColor, newLabelSize, newFontSize, newFontColor, fromNetwork: true);
        }

        /// <summary>
        /// Applies all labels from network data during initial sync.
        /// </summary>
        /// <remarks>
        /// Creates entities that do not exist locally and updates existing ones via
        /// <see cref="UpdateLabelFromNetwork"/>. Does not bind GameObjects; call
        /// <see cref="BindAllGameObjectsAndApplyLabels"/> afterward to resolve and apply visuals.
        /// </remarks>
        public static void ApplyNetworkLabels(Dictionary<string, EntityData> networkedData)
        {
            foreach (var kvp in networkedData)
            {
                var guid = kvp.Key;
                var networkedEntityData = kvp.Value;

                var existing = LabelTracker.GetEntityData(guid);
                if (existing != null)
                {
                    UpdateLabelFromNetwork(guid,
                        newLabelText: networkedEntityData.LabelText,
                        newLabelColor: networkedEntityData.LabelColor,
                        newLabelSize: networkedEntityData.LabelSize,
                        newFontSize: networkedEntityData.FontSize,
                        newFontColor: networkedEntityData.FontColor);
                }
                else
                {
                    CreateLabel(guid, null,
                        labelText: networkedEntityData.LabelText,
                        labelColor: networkedEntityData.LabelColor,
                        labelSize: networkedEntityData.LabelSize,
                        fontSize: networkedEntityData.FontSize,
                        fontColor: networkedEntityData.FontColor);
                }
            }

            // Force update all visuals after network sync
            LabelApplier.ForceUpdateAllLabels();
        }

        /// <summary>
        /// Binds GameObjects for all tracked entities and applies their labels.
        /// Used when syncing from network to ensure visuals are applied.
        /// </summary>
        /// <remarks>
        /// Scans <see cref="GridItem"/> and <see cref="SurfaceItem"/> in the scene, builds a
        /// GUID-to-GameObject map, then binds any tracked entity that lacks a GameObject. Also
        /// re-applies labels for entities that already have GameObjects (e.g. after scene load).
        /// </remarks>
        public static void BindAllGameObjectsAndApplyLabels()
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
                    Logger.Msg($"[LabelService] Binding GameObject for synced label: GUID={guid}");
                    BindGameObject(guid, go);
                }
                else if (entityData.GameObject != null && !string.IsNullOrEmpty(entityData.LabelText))
                {
                    LabelApplier.ApplyOrUpdateLabel(guid);
                }
            }
        }
    }
}
