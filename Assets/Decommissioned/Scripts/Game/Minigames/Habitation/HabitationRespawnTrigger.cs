// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using UnityEngine;

namespace Meta.Decommissioned.Game.MiniGames
{
    /// <summary>
    /// Small class that controls the respawning of a <see cref="HabitationObject"/> when it touches a specific trigger.
    /// </summary>
    public class HabitationRespawnTrigger : MonoBehaviour
    {
        private void OnCollisionEnter(Collision coll)
        {
            if (coll.gameObject.TryGetComponent(out HabitationObject placeableObject))
            {
                placeableObject.UngrabItem();
                placeableObject.RespawnItem();
            }
        }
    }
}
