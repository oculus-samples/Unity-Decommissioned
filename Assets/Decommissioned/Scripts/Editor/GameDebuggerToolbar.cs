// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using System.Linq;
using Meta.Decommissioned.Game;
using Meta.Decommissioned.Game.MiniGames;
using Meta.Decommissioned.Interactables;
using Meta.Utilities;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;
using UnityToolbarExtender;

namespace Meta.Decommissioned.Editor
{
    [ExecuteInEditMode]
    public class GameDebuggerToolbar
    {
        [InitializeOnLoadMethod]
        private static void Initialize() => ToolbarExtender.LeftToolbarGUI.Add(OnToolbarGUI);

        private static ulong s_playerId = 0;

        private static void OnToolbarGUI()
        {
            GUILayout.FlexibleSpace();

            if (GUILayout.Button(EditorGUIUtility.IconContent("P4_CheckOutRemote"), new GUIStyle(GUI.skin.button) { margin = ZeroRectOffset }))
            {
                var button = Object.FindObjectOfType<ReadyUpButton>();
                button?.RaiseReadyEvent();
            }

            if (GUILayout.Button(EditorGUIUtility.IconContent("PlayButton On"), new GUIStyle(GUI.skin.button) { margin = ZeroRectOffset }))
            {
                var button = Object.FindObjectOfType<StartGameButton>();
                button?.OnButtonPressed();
            }

            GUILayout.FlexibleSpace();

            EditorPrefsToggle("skip discuss phase", false, "Skip\nDiscussion");
            EditorPrefsToggle("skip vote phase", false, "Skip\nVoting");
            EditorPrefsToggle("skip plan phase", false, "Skip\nPlanning");
            EditorPrefsToggle("skip work phase", false, "Skip\nWork");

            GUILayout.FlexibleSpace();

            if (EditorApplication.isPlaying)
            {
                var locationManager = LocationManager.Instance;
                if (locationManager != null)
                {
                    var tasksByRoom = System.Enum.GetValues(typeof(MiniGameRoom)).
                        Cast<MiniGameRoom>().
                        Select(r => (room: r, name: MiniGameManager.GetRoomName(r))).
                        Where(g => g.name != default).
                        ToList();
                    var names = tasksByRoom.Select(g => g.name).ToArray();

                    if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
                    {
                        var connectedClientsIds = NetworkManager.Singleton.ConnectedClientsIds;
                        var index = EditorGUILayout.Popup(
                            connectedClientsIds.IndexOf(s_playerId) ?? 0,
                            connectedClientsIds.Select(id => id.ToString()).ToArray(),
                            GUILayout.Width(32));
                        s_playerId = connectedClientsIds[index];
                        DrawRoomGUI(NetworkManager.Singleton.ConnectedClients[s_playerId].PlayerObject);
                    }
                    else if (NetworkManager.Singleton != null)
                    {
                        var localClient = NetworkManager.Singleton.LocalClient;
                        if (localClient == null)
                            return;
                        var playerObject = localClient.PlayerObject;
                        if (playerObject == null)
                            return;
                        DrawRoomGUI(playerObject);
                    }

                    void DrawRoomGUI(NetworkObject playerObject)
                    {
                        var currentPosition = locationManager.GetGamePositionByPlayer(playerObject);
                        var currentIndex = tasksByRoom.FindIndex(g => g.room == currentPosition?.MiniGameRoom);

                        var index = EditorGUILayout.Popup(currentIndex, names, GUI.skin.button);
                        if (currentIndex != index)
                        {
                            LocationManager.Instance.TeleportPlayer_ServerRpc(playerObject, tasksByRoom[index].room);
                        }
                    }
                }
            }
        }

        private static RectOffset ZeroRectOffset { get; } = new();

        private static void EditorPrefsToggle(string key, bool defaultValue, string text) =>
            EditorPrefs.SetBool(key, GUILayout.Toggle(EditorPrefs.GetBool(key, defaultValue), text, new GUIStyle(GUI.skin.button)
            {
                fontSize = 6,
                margin = ZeroRectOffset,
                clipping = new()
            }));
    }
}
