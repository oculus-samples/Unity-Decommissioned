// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using Meta.Decommissioned.Lobby;
using UnityEngine;

namespace Meta.Decommissioned.Utils
{
    /// <summary>
    /// Child class of <see cref="CollisionEnterBehavior"/> that only executes collision behavior for the player occupying a
    /// specific <see cref="GamePosition"/>.
    /// </summary>
    public class PositionSensitiveCollisionEnterBehavior : CollisionEnterBehavior
    {
        [SerializeField]
        private GamePosition m_trackedGamePosition;

        protected override void OnCollision(GameObject collidedObject)
        {
            if (m_trackedGamePosition == null)
            {
                Debug.LogError($"{gameObject.name}: Object does not have a tracked game position!", collidedObject);
                return;
            }

            if (!m_trackedGamePosition.IsOccupied || !m_trackedGamePosition.OccupyingPlayer.IsLocalPlayer) { return; }

            base.OnCollision(collidedObject);
        }

        protected override void OnExitCollision(GameObject collidedObject)
        {
            if (m_trackedGamePosition == null)
            {
                Debug.LogError($"{gameObject.name}: Object does not have a tracked game position!", collidedObject);
                return;
            }

            if (!m_trackedGamePosition.IsOccupied || !m_trackedGamePosition.OccupyingPlayer.IsLocalPlayer) { return; }

            base.OnExitCollision(collidedObject);
        }
    }
}
