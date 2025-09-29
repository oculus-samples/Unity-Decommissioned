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
using System.Collections.Generic;
using System.IO;
using UnityEditor;

namespace Oculus.Avatar2
{
    public class LightingExampleEnvironmentAdder
    {
        public const string MenuItemName = "MetaAvatarsSDK/Lighting Example Scene/Add Environments";
        [MenuItem(MenuItemName)]
        public static void AddEnvironments()
        {
            var currentScenesInBuild = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

            var allScenePaths = Directory.GetFiles("Assets", "*.unity", SearchOption.AllDirectories);
            var affectedScenes = new List<string>();

            foreach (var scenePath in allScenePaths)
            {
                if (OvrAvatarUtility.IsScenePathAnEnvironment(scenePath))
                {
                    bool exists = currentScenesInBuild.Exists(s => s.path.Replace('/', Path.DirectorySeparatorChar) == scenePath);

                    // If it doesn't exist, add it and enable it
                    if (!exists)
                    {
                        EditorBuildSettingsScene newScene = new EditorBuildSettingsScene(scenePath, true);
                        currentScenesInBuild.Add(newScene);
                        affectedScenes.Add(scenePath);
                    }
                    else
                    {
                        // If it exists, make sure it is enabled
                        EditorBuildSettingsScene existingScene = currentScenesInBuild.Find(s => s.path.Replace('/', Path.DirectorySeparatorChar) == scenePath);
                        if (!existingScene.enabled)
                        {
                            existingScene.enabled = true;
                            affectedScenes.Add(scenePath);
                        }
                    }
                }
            }

            // Update build settings with new list
            EditorBuildSettings.scenes = currentScenesInBuild.ToArray();

            if (affectedScenes.Count > 0)
            {
                OvrAvatarLog.LogInfo("Added / Enabled scenes to build:\n" + string.Join(", ", affectedScenes));
            }
            else
            {
                OvrAvatarLog.LogInfo("No scenes were added or enabled.");
            }
        }
    }
}
#endif
