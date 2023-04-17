// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using System.Collections;
using Meta.Multiplayer.Core;
using Meta.Multiplayer.Networking;
using Meta.Utilities;
using NaughtyAttributes;
using Oculus.Interaction;
using ScriptableObjectArchitecture;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;

namespace Meta.Decommissioned.Game.MiniGames
{
    /// <summary>
    /// Class for managing objects spawned for the Conveyor Belt station.
    /// <seealso cref="ConveyorObjectSpawner"/>
    /// <seealso cref="ConveyorMiniGame"/>
    /// </summary>
    public class ConveyorObject : NetworkBehaviour
    {
        [SerializeField, AutoSet] private ClientNetworkTransform m_networkTransform;
        [SerializeField, AutoSet] private InteractableGroupView m_objectInteractableView;
        [SerializeField, AutoSetFromChildren] private MeshRenderer m_meshRenderer;

        [SerializeField] private Receptacles m_requiredReceptacle = Receptacles.ReceptacleA;
        [SerializeField] private BoolGameEvent m_destinationReachedEvent;
        [SerializeField] private Vector3 m_spawnRotation = new(0, 0, 30);

        private Vector3 m_despawnedPosition;

        internal readonly NetworkVariable<bool> m_isSpawned = new();
        private readonly NetworkVariable<bool> m_hasBeenGrabbed = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        private readonly NetworkVariable<NetworkReference<ConveyorBelt>> m_activeBelt = new(writePerm: NetworkVariableWritePermission.Owner);

        private readonly NetworkVariable<bool> m_transformSyncEnabled = new(writePerm: NetworkVariableWritePermission.Owner);

        private YieldInstruction m_waitForPhysicsFrame = new WaitForFixedUpdate();

        private bool m_isCollidingThisFrame = false;

        [ShowNativeProperty]
        private ConveyorBelt ActiveBelt
        {
            get => m_activeBelt.Value.Value;
            set => m_activeBelt.Value = value;
        }

        [field: SerializeField]
        private Transform SpawnPosition { get; set; }

        [ShowNativeProperty]
        public bool IsOnConveyorBelt => ActiveBelt != null;

        private float m_movementSpeed = 0.5f;
        [SerializeField, AutoSet] private Rigidbody m_rigidbody;

        [ShowNativeProperty]
        public Receptacles RequiredReceptacle => m_requiredReceptacle;

        [ShowNativeProperty]
        public Transform Destination
        {
            private get;
            set;
        }

        private void Start()
        {
            GamePhaseManager.Instance.OnPhaseChanged += OnPhaseChanged;
            LocationManager.Instance.OnPlayerJoinedRoom += OnPlayerJoinedRoom;
            m_isSpawned.OnValueChanged += OnSpawnStateChanged;
            m_activeBelt.OnValueChanged += (oldBelt, newBelt) => OnActiveBeltChanged(oldBelt, newBelt);
            m_objectInteractableView.WhenStateChanged += OnInteractableStateChanged;
            m_transformSyncEnabled.OnValueChanged += OnTransformSyncEnabledChanged;
            m_despawnedPosition = transform.position;
            m_meshRenderer.enabled = false;
            m_rigidbody.isKinematic = true;
        }

        private void OnTransformSyncEnabledChanged(bool previousValue, bool newValue)
        {
            m_networkTransform.enabled = !IsOwner || newValue;
            m_networkTransform.IgnoreUpdates = !newValue;
        }

        private void OnInteractableStateChanged(InteractableStateChangeArgs stateArgs)
        {
            if (IsOwner && stateArgs.NewState == InteractableState.Select)
            {
                m_hasBeenGrabbed.Value = true;
            }
        }

        private void OnPlayerJoinedRoom(NetworkObject player, MiniGameRoom room)
        {
            if (room != MiniGameRoom.Garage)
            {
                return;
            }

            var thisPlayerId = player.GetOwnerPlayerId();

            if (IsServer && thisPlayerId.HasValue && NetworkObject.GetOwnerPlayerId() != thisPlayerId.Value)
            {
                NetworkObject.ChangeOwnership(thisPlayerId.Value);
            }

            SetSyncStatus();
        }

        private void SetSyncStatus()
        {
            // By default, transform syncing is disabled to save on network resources. It is managed by the object now according to what actions the owner is performing with it.
            if (IsServer && IsOwner)
            {
                m_networkTransform.enabled = false;
            }
        }

        private void OnActiveBeltChanged(ConveyorBelt oldBelt, ConveyorBelt newBelt)
        {
            var isOnBelt = newBelt != null;

            if (!isOnBelt || m_objectInteractableView.State == InteractableState.Select)
            {
                return;
            }

            Destination = newBelt.ConveyorBeltDestination;
            m_movementSpeed = newBelt.ObjectMovementSpeed;

            if (IsOwner)
            {
                m_hasBeenGrabbed.Value = false;
                _ = StartCoroutine(WaitForPhysicsSync());
            }
        }

        private IEnumerator WaitForPhysicsSync()
        {
            // Wait until FixedUpdate in order to allow collision detection to run before syncing transforms
            yield return m_waitForPhysicsFrame;

            // This RPC is required to sync the transform for clients because the network transform isn't always synced exactly to this position on the belt when it lands
            m_networkTransform.Teleport(transform.position, transform.rotation, transform.lossyScale);
            Teleport_ServerRpc(transform.position, transform.rotation, m_rigidbody.velocity, m_rigidbody.angularVelocity);
        }

        [ServerRpc]
        private void Teleport_ServerRpc(Vector3 position, Quaternion rotation, Vector3 velocity, Vector3 angularVelocity)
        {
            Teleport_ClientRpc(position, rotation, velocity, angularVelocity);
        }

        [ClientRpc]
        private void Teleport_ClientRpc(Vector3 position, Quaternion rotation, Vector3 velocity, Vector3 angularVelocity)
        {
            if (IsOwner)
            {
                return;
            }

            transform.SetPositionAndRotation(position, rotation);
            m_rigidbody.velocity = velocity;
            m_rigidbody.angularVelocity = angularVelocity;
        }

        private void OnCollisionStay(Collision collision)
        {
            if (collision.gameObject.tag == tag)
            {
                m_isCollidingThisFrame = true;
            }
        }

        private void FixedUpdate()
        {
            if (IsOwner)
            {
                m_transformSyncEnabled.Value = m_isCollidingThisFrame || m_hasBeenGrabbed.Value;
            }
            m_isCollidingThisFrame = false;

            if (!m_isSpawned.Value || !IsOnConveyorBelt || m_objectInteractableView.State == InteractableState.Select)
            {
                return;
            }
            Move();
        }

        public override void OnDestroy()
        {
            GamePhaseManager.Instance.OnPhaseChanged -= OnPhaseChanged;
            base.OnDestroy();
        }

        private void OnPhaseChanged(Phase newPhase)
        {
            if (!IsServer)
            {
                return;
            }

            if (newPhase == Phase.Discussion)
            {
                Despawn(true);
            }
        }

        private void OnSpawnStateChanged(bool oldValue, bool newValue)
        {
            if (newValue)
            {
                Spawn(false);
            }
            else
            {
                Despawn(false);
            }
        }

        public void OnLeftConveyorBelt()
        {
            if (IsOwner)
            {
                ActiveBelt = null;
            }
        }

        /// <summary>
        /// Behavior upon entering the proximity of/touching a possibly conveyor belt object (trigger).
        /// </summary>
        /// <param name="other">The object being checked; if it is a conveyor belt, the object will start moving based
        /// on its assigned destination and speed.</param>
        public void CheckForConveyorBelt(GameObject other)
        {
            if (!IsOwner)
            {
                return;
            }

            if (other.TryGetComponent(out ConveyorBelt conveyorBelt))
            {
                ActiveBelt = conveyorBelt;
            }
        }

        /// <summary>
        /// Behavior upon reaching a possible destination object (trigger).
        /// </summary>
        /// <param name="destinationObject">The object being checked. If it has a Conveyor_Receptacle object on it,
        /// the object will notify the relevant components.</param>
        public void OnReachedDestination(GameObject destinationObject)
        {
            if (destinationObject.TryGetComponent(out ConveyorReceptacle receptacle))
            {
                receptacle.OnObjectReceived(this);
                m_destinationReachedEvent.Raise(receptacle.ReceptacleType == m_requiredReceptacle);
            }

            Despawn(true);
        }

        public void OnObjectDropped()
        {
            if (!IsOwner)
            {
                return;
            }

            OnObjectDroppedServerRpc();
        }

        [ServerRpc(RequireOwnership = true)]
        private void OnObjectDroppedServerRpc()
        {
            m_destinationReachedEvent.Raise(RequiredReceptacle == Receptacles.NoReceptacle);
            Despawn(true);
        }

        /// <summary>
        /// When this is called, the object's spawn state will be set to "true", which will trigger
        /// spawning/physics driven behavior.
        /// </summary>
        /// <param name="setSpawnOnServer">If true, sets the spawn state on the server rather than just on the client.</param>
        public void Spawn(bool setSpawnOnServer)
        {
            transform.position = SpawnPosition.position;
            transform.rotation = Quaternion.Euler(m_spawnRotation);
            m_rigidbody.velocity = Vector3.zero;
            m_rigidbody.angularVelocity = Vector3.zero;
            m_rigidbody.isKinematic = false;
            m_meshRenderer.enabled = true;

            if (IsServer && setSpawnOnServer)
            {
                m_isSpawned.Value = true;
            }
        }

        /// <summary>
        /// When this is called, the object's spawn state will be set to "true", which will trigger
        /// spawning/physics driven behavior.
        /// </summary>
        /// <param name="setDespawnOnServer">If true, sets the spawn state on the server rather than just on the client.</param>
        public void Despawn(bool setDespawnOnServer)
        {
            if (setDespawnOnServer)
            {
                if (IsServer)
                {
                    m_isSpawned.Value = false;
                }
                else
                {
                    DespawnServerRpc();
                }
            }

            if (IsOwner)
            {
                ActiveBelt = null;
            }

            m_rigidbody.velocity = m_rigidbody.angularVelocity = Vector3.zero;
            m_rigidbody.isKinematic = true;
            m_meshRenderer.enabled = false;
            transform.position = m_despawnedPosition;
            transform.rotation = quaternion.identity;
        }

        [ServerRpc(RequireOwnership = false)]
        private void DespawnServerRpc()
        {
            Despawn(true);
        }

        private void Move()
        {
            var step = m_movementSpeed * Time.fixedDeltaTime;
            var targetPosition = Vector3.MoveTowards(transform.position, Destination.position, step);
            m_rigidbody.MovePosition(targetPosition);
            transform.SetPositionAndRotation(targetPosition, transform.rotation);
        }
    }
}
