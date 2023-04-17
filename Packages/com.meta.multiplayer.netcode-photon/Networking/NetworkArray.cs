// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Netcode;

namespace Meta.Multiplayer.Networking
{
    /// <summary>
    /// Event based NetworkVariable container for syncing Arrays
    /// </summary>
    /// <typeparam name="T">The type for the array</typeparam>
    public class NetworkArray<T> : NetworkVariableBase where T : unmanaged, IEquatable<T>
    {
        private NativeList<T> m_list = new(0, Allocator.Persistent);

        /// <summary>
        /// Delegate type for array changed event
        /// </summary>
        public delegate void OnValuesChangedDelegate(NativeArray<T> newValue);

        /// <summary>
        /// The callback to be invoked when the array gets changed
        /// </summary>
        public OnValuesChangedDelegate OnValuesChanged;

        public NetworkArray() { }

        public NetworkArray(
            NetworkVariableReadPermission readPerm = DefaultReadPerm,
            NetworkVariableWritePermission writePerm = DefaultWritePerm)
            : base(readPerm, writePerm)
        {
        }

        /// <inheritdoc />
        public override void WriteDelta(FastBufferWriter writer)
        {
            if (IsDirty())
            {
                WriteField(writer);
            }
        }

        /// <inheritdoc />
        public override unsafe void WriteField(FastBufferWriter writer)
        {
            var length = (ushort)m_list.Length;
            writer.WriteValueSafe(length);

            if (length != 0)
            {
                var pointer = m_list.GetUnsafeReadOnlyPtr();
                writer.WriteBytesSafe((byte*)pointer, length * sizeof(T));
            }
        }

        /// <inheritdoc />
        public override unsafe void ReadField(FastBufferReader reader)
        {
            reader.ReadValueSafe(out ushort length);
            m_list.Clear();
            m_list.ResizeUninitialized(length);

            if (length != 0)
            {
                var pointer = m_list.GetUnsafeReadOnlyPtr();
                reader.ReadBytesSafe((byte*)pointer, length * sizeof(T));
            }
        }

        /// <inheritdoc />
        public override unsafe void ReadDelta(FastBufferReader reader, bool keepDirtyDelta)
        {
            ReadField(reader);

            if (keepDirtyDelta)
            {
                SetDirty(true);
            }

            OnValuesChanged?.Invoke(m_list);
        }

        public int LastModifiedTick =>
                // todo: implement proper network tick for NetworkArray
                NetworkTickSystem.NoTick;

        public unsafe void ModifyValues(Action<NativeList<T>> modifyValues)
        {
            if (NetworkManager.Singleton != null && !CanClientWrite(NetworkManager.Singleton.LocalClientId))
            {
                throw new InvalidOperationException("Client is not allowed to write to this NetworkVariable");
            }

            SetDirty(true);

            modifyValues(m_list);

            OnValuesChanged?.Invoke(m_list);
        }

        public override void Dispose()
        {
            m_list.Dispose();
            base.Dispose();
        }

        public NativeArray<T> Values => m_list;
    }
}
