// Copyright (c) Meta Platforms, Inc. and affiliates.

using Unity.Netcode;
using UnityEngine;

namespace Meta.Multiplayer.Networking
{
    public class NetworkSingleton<T> : NetworkBehaviour where T : NetworkSingleton<T>
    {
        public static T Instance { get; private set; }

        private static System.Action<T> s_onAwake;
        private static System.Action<T> s_onDestroy;

        public static void WhenInstantiated(System.Action<T> action)
        {
            if (Instance != null)
                action(Instance);
            else
                s_onAwake += action;
        }

        public static void WhenDestroyed(System.Action<T> action)
        {
            s_onDestroy += action;
        }

        protected void Awake()
        {
            if (!enabled)
                return;

            Debug.Assert(Instance == null, $"Singleton {typeof(T).Name} has been instantiated more than once.", this);
            Instance = (T)this;

            s_onAwake?.Invoke(Instance);
            s_onAwake = null;
        }

        protected void OnEnable()
        {
            if (Instance != this)
                Awake();
        }

        public override void OnDestroy()
        {
            s_onDestroy?.Invoke(Instance);
            s_onDestroy = null;
            base.OnDestroy();
        }
    }
}
