// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

namespace Meta.Decommissioned.Game.MiniGames
{
    /// <summary>
    /// The room a MiniGame currently resides in. This is used for filtering the tasks that are in currently disabled rooms
    /// during gameplay.
    /// </summary>
    public enum MiniGameRoom
    {
        None,
        Science,
        Garage,
        Holodeck,
        Hydroponics,
        Habitation,
        Commander,
    }
}
