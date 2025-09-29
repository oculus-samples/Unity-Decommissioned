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

using System;
using Oculus.Avatar2;
using Unity.Collections;
using UnityEngine;
using StreamLOD = Oculus.Avatar2.OvrAvatarEntity.StreamLOD;

/// <summary>
/// This class is an example of how to use the Streaming functions of the avatar to send and receive data over the network
/// </summary>
public class BasicSampleRemoteLoopbackManager : RemoteLoopbackManagerBase
{
    protected class SamplePacketData : PacketData, IDisposable
    {
        public NativeArray<byte> data;
        public UInt32 dataByteCount;

        ~SamplePacketData()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (data.IsCreated)
            {
                data.Dispose();
            }
            data = default;
        }
    };

    protected override PacketData GeneratePacketData(OvrAvatarEntity entity, StreamLOD lod)
    {
        SamplePacketData packet = FetchPacketFromPool() as SamplePacketData ?? new SamplePacketData();
        packet.Retain();

        packet.dataByteCount = entity.RecordStreamData_AutoBuffer(lod, ref packet.data);
        Debug.Assert(packet.dataByteCount > 0);

        return packet;
    }

    protected override void ProcessPacketData(OvrAvatarEntity entity, PacketData packet)
    {
        var samplePacket = packet as SamplePacketData;
        if (samplePacket != null)
        {
            var dataSlice = samplePacket.data.Slice(0, (int)samplePacket.dataByteCount);
            entity.ApplyStreamData(in dataSlice);
        }
        else
        {
            Debug.LogError("Invalid packet format");
        }
    }
}
