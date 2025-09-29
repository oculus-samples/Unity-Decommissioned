/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * Licensed under the Oculus SDK License Agreement (the "License");
 * you may not use the Oculus SDK except in compliance with the License,
 * which is provided at the time of installation or download, or which
 * otherwise accompanies this software in either electronic or hard copy form.
 *
 * You may obtain a copy of the License at
 *
 * https://developer.oculus.com/licenses/oculussdk/
 *
 * Unless required by applicable law or agreed to in writing, the Oculus SDK
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

#nullable enable

#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace Oculus.Avatar2
{
    /// <summary>
    /// A debug visualizer to highlight the bones and joints in a character rig
    /// </summary>
    public class RigVisualizerUtility : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("The root transform of the rig")]
        public Transform? RigRoot;

        [SerializeField]
        [Tooltip("The color to render the skeleton in")]
        public Color Color = Color.red;

        private void OnEnable()
        {
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            if (RigRoot == null)
            {
                return;
            }

            DrawTransform(RigRoot, Color);
        }

        private void DrawTransform(Transform? transform, Color color)
        {
            if (transform == null)
            {
                return;
            }

            if (transform != RigRoot && transform.parent != null)
            {
                Handles.color = color;
                Handles.DrawLine(transform.position, transform.parent.position);
            }

            transform.DebugDrawInEditor(0.03f);

            for (int i = 0; i < transform.childCount; ++i)
            {
                DrawTransform(transform.GetChild(i), color);
            }
        }
    }
}
#endif
