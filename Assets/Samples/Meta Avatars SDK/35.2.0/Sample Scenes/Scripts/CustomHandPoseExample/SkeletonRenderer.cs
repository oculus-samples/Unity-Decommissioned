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

using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class SkeletonRenderer : MonoBehaviour
{
    public Color color;

    public bool drawAxes;
    public float axisSize;

#if UNITY_EDITOR
    private void OnEnable()
    {
        SceneView.duringSceneGui += Draw;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= Draw;
    }
#endif

#if UNITY_EDITOR
    private void Draw(SceneView sceneView)
    {
        Handles.matrix = Matrix4x4.identity;

        foreach (var xform in GetComponentsInChildren<Transform>())
        {
            var parent = xform.parent;
            var position = xform.position;
            if (parent)
            {
                Handles.color = color;
                Handles.DrawLine(position, parent.position);
            }

            if (drawAxes)
            {
                var r = xform.rotation;
                var xAxis = position + r * Vector3.right * axisSize ;
                var yAxis = position + r * Vector3.up * axisSize ;
                var zAxis = position + r * Vector3.forward * axisSize;

                Handles.color = Color.blue;
                Handles.DrawLine(position, zAxis);

                Handles.color = Color.green;
                Handles.DrawLine(position, yAxis);

                Handles.color = Color.red;
                Handles.DrawLine(position, xAxis);
            }
        }
    }
#endif
}
