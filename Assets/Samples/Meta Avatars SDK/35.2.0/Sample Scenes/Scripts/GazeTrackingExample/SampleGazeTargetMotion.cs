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

public class SampleGazeTargetMotion : MonoBehaviour
{
    [SerializeField]
    private float _magnitudeX = 0f;
    [SerializeField]
    private float _magnitudeY = 0f;
    [SerializeField]
    private float _magnitudeZ = 0f;

    [SerializeField]
    private float _speedX = 1f;
    [SerializeField]
    private float _speedY = 1f;
    [SerializeField]
    private float _speedZ = 1f;

    private Vector3 _startPos;

    void Awake()
    {
        _startPos = transform.localPosition;
    }

    void Update()
    {
        var t = transform;
        float radians = Time.time * Mathf.PI;

        // Only update axis that are actually moving - so that we can drag in the editor when its stationary
        Vector3 newPos = t.localPosition;
        newPos.x = _magnitudeX > 0f ? _startPos.x + Mathf.Sin(radians * _speedX) * _magnitudeX : newPos.x;
        newPos.y = _magnitudeY > 0f ? _startPos.y + Mathf.Sin(radians * _speedY) * _magnitudeY : newPos.y;
        newPos.z = _magnitudeZ > 0f ? _startPos.z + Mathf.Sin(radians * _speedZ) * _magnitudeZ : newPos.z;

        t.localPosition = newPos;
    }
}
