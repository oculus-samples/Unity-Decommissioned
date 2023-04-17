// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections.Generic;
using Unity.Netcode;

namespace Meta.Multiplayer.Networking
{
    public class NetworkMultiton<T> : NetworkBehaviour where T : NetworkMultiton<T>
    {
        private static HashSet<T> s_instances = new();
        public static IReadOnlyCollection<T> Instances => s_instances;

        protected void Awake()
        {
            if (!enabled)
                return;
            _ = s_instances.Add((T)this);
        }

        protected void OnEnable()
        {
            _ = s_instances.Add((T)this);
        }

        protected void OnDisable()
        {
            _ = s_instances.Remove((T)this);
        }

        public override void OnDestroy()
        {
            _ = s_instances.Remove((T)this);
            base.OnDestroy();
        }
    }
}
