// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using System;
using System.Collections.Generic;
using System.Linq;
using Meta.Decommissioned.Game.MiniGames;
using Meta.Decommissioned.UI;
using NaughtyAttributes;
using Oculus.Interaction;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Events;
using Meta.Utilities;

namespace Meta.Decommissioned.Occlusion
{
    /// <summary>
    /// Defines a zone where all renderers and specific components contained within the zone will be occluded when the room is not occupied, and vice versa.
    /// </summary>
    public class RoomOcclusionZone : Multiton<RoomOcclusionZone>
    {
        /// <summary>
        /// The room this room zone belongs to.
        /// </summary>
        public MiniGameRoom ZoneRoom => m_zoneRoom;

        /// <summary>
        /// Is this room zone occluded or not?
        /// </summary>
        public bool IsOccluded { get; private set; }

        public bool EnableLogging;

        [SerializeField, Tooltip("The position and size of this room zone.")]
        private Bounds m_zoneSize;

        [SerializeField, Tooltip("The room this room zone belongs to.")]
        private MiniGameRoom m_zoneRoom = MiniGameRoom.None;

        [SerializeField, Tooltip("The RoomOcclusionZoneExclusions asset that this room zone will use to exclude renderers from being occluded in this room.")]
        private RoomOcclusionZoneExclusions m_exclusions;

        [SerializeField, ReadOnly, Tooltip("The list of renderers contained within this room zone, excluding objects from the exclusions list.")]

        private List<Renderer> m_renderersInZone = new();

        private readonly Type[] m_occludableComponentTypes =
        {
            typeof(PokeInteractable),
            typeof(PokeInteractableVisual),
            typeof(MainRoomHealthGauge),
            typeof(Grabbable)
        };

        [SerializeField, ReadOnly, Tooltip("The list of occludable components contained within this room zone.")]
        private List<MonoBehaviour> m_occludableComponentsInZone = new();

        [SerializeField, Tooltip("Should this room zone occlude audio sources?")]
        private bool m_occludeAudioSources = true;

        [SerializeField, ReadOnly, HideIf("m_hideAudioSources"), Tooltip("The list of occludable components contained within this room zone.")]

        private List<AudioSource> m_occludableAudioInZone = new();
        [SerializeField] private UnityEvent m_onOcclusion;
        [SerializeField] private UnityEvent m_onUnOcclusion;

#if UNITY_EDITOR
        private void OnDrawGizmosSelected() => Gizmos.DrawWireCube(m_zoneSize.center, m_zoneSize.size);

        private void OnValidate()
        {
            if (UnityEngine.Application.isPlaying)
            {
                return;
            }

            if (m_renderersInZone.Count > 0 || m_occludableComponentsInZone.Count > 0)
            {
                ValidateAllRenderersInZone();
                ValidateAllComponentsInZone();
                return;
            }

            RegrabRenderersInZone();
            EditorUtility.SetDirty(this);
        }

        private void FindAllRenderersInZone()
        {
            var allRenderers = FindObjectsOfType<Renderer>(true);

            foreach (var renderer in allRenderers)
            {
                if (ShouldRendererBeSkipped(renderer))
                {
                    continue;
                }

                if (m_zoneSize.Intersects(renderer.bounds))
                {
                    m_renderersInZone.Add(renderer);
                }
            }

            m_renderersInZone = m_renderersInZone.Distinct().ToList();
        }

        private void FindAllOccludableComponentsInZone()
        {
            var allComponents = FindObjectsOfType<MonoBehaviour>(true);

            foreach (var component in allComponents)
            {
                var typeOfComponent = component.GetType();
                if (!m_occludableComponentTypes.Contains(typeOfComponent))
                {
                    continue;
                }

                if (m_zoneSize.Contains(component.gameObject.transform.position))
                {
                    m_occludableComponentsInZone.Add(component);
                }
            }

            if (m_occludeAudioSources)
            {
                var allAudioSources = FindObjectsOfType<AudioSource>();

                foreach (var audioSource in allAudioSources)
                {
                    if (m_zoneSize.Contains(audioSource.gameObject.transform.position))
                    {
                        m_occludableAudioInZone.Add(audioSource);
                    }
                }
            }

            m_occludableComponentsInZone = m_occludableComponentsInZone.Distinct().ToList();
            m_occludableAudioInZone = m_occludableAudioInZone.Distinct().ToList();
        }

        private void ValidateAllRenderersInZone()
        {
            var allRenderers = FindObjectsOfType<Renderer>(true);

            foreach (var renderer in m_renderersInZone)
            {
                if (renderer == null)
                {
                    Debug.LogWarning($"Found a null renderer in occlusion zone {gameObject.name}. Repopulating list!");
                    RegrabRenderersInZone();
                    return;
                }

                if (allRenderers.Contains(renderer))
                {
                    continue;
                }

                if (ShouldRendererBeSkipped(renderer))
                {
                    continue;
                }

                if (m_zoneSize.Intersects(renderer.bounds))
                {
                    if (EnableLogging)
                    {
                        Debug.Log($"Found a renderer that has not been added to occlusion zone {gameObject.name}. Repopulating list!");
                    }
                    RegrabRenderersInZone();
                    return;
                }
            }

            foreach (var renderer in allRenderers)
            {
                if (m_renderersInZone.Contains(renderer))
                {
                    continue;
                }

                if (ShouldRendererBeSkipped(renderer))
                {
                    continue;
                }

                if (m_zoneSize.Intersects(renderer.bounds))
                {
                    if (EnableLogging)
                    {
                        Debug.Log($"Found a renderer that has not been added to occlusion zone {gameObject.name}. Repopulating list!");
                    }
                    RegrabRenderersInZone();
                    return;
                }
            }
        }

        private void ValidateAllComponentsInZone()
        {
            var allComponents = FindObjectsOfType<MonoBehaviour>(true);
            foreach (var component in m_occludableComponentsInZone)
            {
                if (component == null)
                {
                    Debug.LogWarning($"Found a null component in occlusion zone {gameObject.name}. Repopulating list!");
                    RegrabComponentsInZone();
                    return;
                }

                if (allComponents.Contains(component))
                {
                    continue;
                }

                var typeOfComponent = component.GetType();

                if (!m_occludableComponentTypes.Contains(typeOfComponent))
                {
                    continue;
                }

                if (m_zoneSize.Contains(component.gameObject.transform.position))
                {
                    if (EnableLogging)
                    {
                        Debug.LogWarning($"Found a component that has not been added to occlusion zone {gameObject.name}. Repopulating list!");
                    }
                    RegrabComponentsInZone();
                    return;
                }
            }

            foreach (var component in allComponents)
            {
                if (m_occludableComponentsInZone.Contains(component))
                {
                    continue;
                }

                var typeOfComponent = component.GetType();

                if (!m_occludableComponentTypes.Contains(typeOfComponent))
                {
                    continue;
                }

                if (m_zoneSize.Contains(component.gameObject.transform.position))
                {
                    if (EnableLogging)
                    {
                        Debug.Log($"Found a component that has not been added to occlusion zone {gameObject.name}. Repopulating list!");
                    }
                    RegrabComponentsInZone();
                    return;
                }
            }

            if (m_occludeAudioSources)
            {
                var allAudioSources = FindObjectsOfType<AudioSource>();

                foreach (var audioSource in m_occludableAudioInZone)
                {
                    if (audioSource == null)
                    {
                        Debug.LogWarning($"Found a null audio source in occlusion zone {gameObject.name}. Repopulating list!");
                        RegrabComponentsInZone();
                        return;
                    }

                    if (allAudioSources.Contains(audioSource))
                    {
                        continue;
                    }

                    if (m_zoneSize.Contains(audioSource.gameObject.transform.position))
                    {
                        if (EnableLogging)
                        {
                            Debug.Log($"Found an audio source that has not been added to occlusion zone {gameObject.name}. Repopulating list!");
                        }
                        RegrabComponentsInZone();
                        return;
                    }
                }

                foreach (var audioSource in allAudioSources)
                {
                    if (m_occludableAudioInZone.Contains(audioSource))
                    {
                        continue;
                    }

                    if (m_zoneSize.Contains(audioSource.gameObject.transform.position))
                    {
                        if (EnableLogging)
                        {
                            Debug.Log($"Found an audio source that has not been added to occlusion zone {gameObject.name}. Repopulating list!");
                        }
                        RegrabComponentsInZone();
                        return;
                    }
                }
            }
        }

        [ContextMenu("Reset Zone Objects")]
        private void ResetZoneObjects()
        {
            m_renderersInZone.Clear();
            m_occludableComponentsInZone.Clear();
            m_occludableAudioInZone.Clear();
            OnValidate();
            OnValidate();
            OnValidate();
        }

        private void RegrabRenderersInZone()
        {
            m_renderersInZone.Clear();
            FindAllRenderersInZone();
        }

        private void RegrabComponentsInZone()
        {
            m_occludableComponentsInZone.Clear();
            if (m_occludeAudioSources) { m_occludableAudioInZone.Clear(); }
            FindAllOccludableComponentsInZone();
        }

        private bool ShouldRendererBeSkipped(Renderer renderer)
        {
            if (m_exclusions != null && m_exclusions.OcclusionExclusions.Length > 0)
            {
                foreach (var exclusion in m_exclusions.OcclusionExclusions)
                {
                    var prefabAsset = PrefabUtility.GetCorrespondingObjectFromSource(renderer);
                    if (prefabAsset != null && exclusion != null && prefabAsset.transform.IsChildOf(exclusion.transform))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
#endif

        /// <summary>
        /// Occludes this room zone so that no objects contained within it are visible.
        /// </summary>
        public void OccludeZone()
        {
            if (m_renderersInZone == null || m_renderersInZone.Count == 0)
            {
                Debug.LogWarning($"{gameObject.name} did not have any renderers in the occlusion zone! No occlusion will be applied.");
                return;
            }

            if (IsOccluded)
            {
                return;
            }

            foreach (var renderer in m_renderersInZone)
            {
                if (renderer != null)
                {
                    renderer.enabled = false;
                }
            }

            foreach (var component in m_occludableComponentsInZone)
            {
                if (component != null)
                {
                    component.enabled = false;
                }
            }

            if (m_occludeAudioSources)
            {
                foreach (var audioSource in m_occludableAudioInZone)
                {
                    if (audioSource != null)
                    {
                        audioSource.enabled = false;
                    }
                }
            }

            m_onOcclusion?.Invoke();
            IsOccluded = true;
        }

        /// <summary>
        /// Un-occludes this room zone so that all objects contained within it are visible.
        /// </summary>
        public void UnOccludeZone()
        {
            if (m_renderersInZone.Count == 0)
            {
                Debug.LogWarning($"{gameObject.name} did not have any renderers in the occlusion zone! No occlusion will be applied.");
                return;
            }

            if (!IsOccluded) { return; }

            foreach (var renderer in m_renderersInZone)
            {
                if (renderer != null)
                {
                    renderer.enabled = true;
                }
            }

            foreach (var component in m_occludableComponentsInZone)
            {
                if (component != null)
                {
                    component.enabled = true;
                }
            }

            if (m_occludeAudioSources)
            {
                foreach (var audioSource in m_occludableAudioInZone)
                {
                    if (audioSource != null)
                    {
                        audioSource.enabled = true;
                    }
                }
            }

            m_onUnOcclusion?.Invoke();
            IsOccluded = false;
        }
    }
}
