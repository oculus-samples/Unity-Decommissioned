// Copyright (c) Meta Platforms, Inc. and affiliates.

using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Meta.Multiplayer.Core
{
    /// <summary>
    /// Handle scenes loading and keeps tracks of the current loaded scene and loading scenes through the NetCode
    /// NetworkManager.
    /// </summary>
    public class SceneLoader
    {
        private static string s_currentScene = null;

#if UNITY_EDITOR
        [UnityEditor.InitializeOnEnterPlayMode]
        private static void Reset()
        {
            s_currentScene = null;
        }
#endif

        public bool SceneLoaded { get; private set; } = false;

        public bool AllowSceneReload { get; set; } = false;

        public string CurrentScene => s_currentScene;

        public SceneLoader() => SceneManager.sceneLoaded += OnSceneLoaded;

        ~SceneLoader()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            SceneLoaded = true;
            s_currentScene = scene.name;
            _ = SceneManager.SetActiveScene(scene);
        }

        public void LoadScene(string scene, bool useNetManager = true)
        {
            if (!AllowSceneReload && !useNetManager && scene == s_currentScene) return;

            SceneLoaded = false;

            if (useNetManager && NetworkManager.Singleton.IsClient)
            {
                _ = NetworkManager.Singleton.SceneManager.LoadScene(scene, LoadSceneMode.Single);
                return;
            }

            _ = SceneManager.LoadSceneAsync(scene);
        }
    }
}
