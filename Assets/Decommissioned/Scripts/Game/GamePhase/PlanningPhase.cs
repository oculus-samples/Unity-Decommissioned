// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

namespace Meta.Decommissioned.Game
{
    public class PlanningPhase : GamePhase
    {
        public override Phase Phase => Phase.Planning;

#if UNITY_EDITOR
        protected override float DurationSeconds => UnityEditor.EditorPrefs.GetBool("skip plan phase") ? 0.1f : base.DurationSeconds;
#endif
    }
}
