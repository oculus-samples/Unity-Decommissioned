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
using UnityEngine.Rendering;

public class ToggleMaterialsForRendering : MonoBehaviour
{
    [SerializeField] private Material? builtInMaterial;
    [SerializeField] private Material? urpMaterial;

    private void Start()
    {
        // Get the current rendering pipeline
        bool isBuiltInPipeline = GraphicsSettings.defaultRenderPipeline == null;
        bool isUrpPipeline = GraphicsSettings.defaultRenderPipeline != null && GraphicsSettings.defaultRenderPipeline.GetType().Name == "UniversalRenderPipelineAsset";

        if (isBuiltInPipeline && builtInMaterial != null)
        {
            GetComponent<Renderer>().material = builtInMaterial;
        }
        else if (isUrpPipeline && urpMaterial != null)
        {
            GetComponent<Renderer>().material = urpMaterial;
        }
        else
        {
            Debug.LogWarning($"Skipping setting materials for {gameObject.name} based on rendering pipeline, material is not set..");
        }
    }
}
