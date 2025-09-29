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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Oculus.Avatar2
{
    public class LightingExampleEnvironmentAdderBuildStep : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        private static bool IsLightingSceneInBuild()
        {
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                if (scene.enabled && scene.path.EndsWith($"LightingExample.unity"))
                {
                    return true;
                }
            }

            return false;
        }

        private bool AreAllEnvironmentScenesInBuild()
        {
            var currentScenesInBuild = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

            var allScenePaths = Directory.GetFiles("Assets", "*.unity", SearchOption.AllDirectories);
            foreach (var scenePath in allScenePaths)
            {
                if (OvrAvatarUtility.IsScenePathAnEnvironment(scenePath))
                {
                    bool exists = currentScenesInBuild.Exists(s => s.path.Replace('/', Path.DirectorySeparatorChar) == scenePath);
                    if (!exists)
                    {
                        return false;
                    }
                    EditorBuildSettingsScene environmentScene = currentScenesInBuild.Find(s => s.path.Replace('/', Path.DirectorySeparatorChar) == scenePath);
                    if (!environmentScene.enabled)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public void OnPreprocessBuild(BuildReport report)
        {
            if (IsLightingSceneInBuild() && !AreAllEnvironmentScenesInBuild())
            {
                // Unfortunately, we can't auto-add these at Build time (they don't get included until the next build),
                // but at least this detection happens early in the build process
                throw new BuildFailedException(
                    $"Unable to build a project with LightingExample, because additional environments scenes are required. Run {LightingExampleEnvironmentAdder.MenuItemName} to include these environments in your build.");
            }
        }
    }
}

#endif
