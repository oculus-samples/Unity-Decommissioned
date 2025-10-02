// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Meta.Multiplayer.Networking;
using Meta.Multiplayer.PlayerManagement;
using Meta.Utilities;
using Meta.XR.Samples;
using Oculus.Interaction;
using ScriptableObjectArchitecture;
using TMPro;
using Unity.Netcode;
using UnityEngine;

//reset input string on new phase
namespace Meta.Decommissioned.Game.MiniGames
{
    /// <summary>
    /// Defines behaviour for the "Habitation" mini game. 
    /// </summary>
    /// TODO: Eliminate ClientRPCs
    [MetaCodeSample("Decommissioned")]
    public class HabitationMiniGame : NetworkBehaviour
    {
        [SerializeField] private HabitationPlacementPoint[] m_placementPoints;
        [SerializeField] private Transform[] m_itemBoxPoints;
        [SerializeField] private NetworkVariable<byte> m_maxInputs = new(3);
        [SerializeField] private TextMeshPro m_textDisplay;
        [SerializeField] private MeshRenderer[] m_itemDisplays = new MeshRenderer[6];
        [SerializeField, AutoSet] private MiniGame m_miniGame;
        [SerializeField] private GameObject m_itemPoolContainer;
        [SerializeField] private AudioTrigger m_itemPlacementAudio;
        [SerializeField] private HabitationMiniGame m_sisterMiniGame;

        /// <summary>
        /// Should the max sequence length increase with each round?
        /// </summary>
        [Tooltip("Should the max sequence length increase with each round?")]
        [SerializeField] private bool m_increaseMaxLengthEachRound = true;

        /// <summary>
        /// Is this MiniGame only supposed to display the sister sequence and nothing else?
        /// </summary>
        [Tooltip("Is this MiniGame only supposed to display the sister sequence and nothing else?")]
        [SerializeField] private bool m_onlyDisplaySequence;

        [SerializeField] private GameEvent m_onBeginNightPhase;
        [SerializeField] private GameEvent m_onEndNightPhase;

        private List<HabitationObject> m_itemPool = new();
        private int m_minItemsToGrab = 7;
        private int m_maxItemsToGrab = 10;
        private int m_minItemsFromPool = 20;
        private bool m_hasBeenReset;
        private bool m_hasBeenSubmitted;

        private MaterialPropertyBlock[] m_snapshotProperties;
        private readonly int m_snapshotCoordsProperty = Shader.PropertyToID("_BaseMap_ST");
        private readonly int m_snapshotColorProperty = Shader.PropertyToID("_BaseColor");
        private YieldInstruction m_resetDelay = new WaitForSeconds(5);

        private HabitationObject[] m_correctInput;
        private HabitationObject[] m_currentInput;
        private HabitationObject[] m_sisterCorrectInput;

        private Color[] m_availableColors = { Color.white, Color.yellow, Color.blue, Color.cyan, Color.green, Color.red, Color.magenta };

        private const string SISTER_CODE_TEXT =
            "The order for\nthe other terminal\nis displayed below";

        private const string COMPLETION_TEXT =
            "<color=#00FF00>Systems have been\ncalibrated successfully!";

        private const string FAILURE_TEXT =
            "<color=#FF0000>Calibration failure.\nResetting system...";

        private const string SOLUTION_INCOMPLETE_TEXT =
            "<color=#FF0000>Calibration failure.\nEnsure all objects are inserted before resubmitting.";

        private const string SUBMIT_SIDE_TEXT =
            "Insert the items\nin the order displayed\non the other terminal";

        private void Awake()
        {
            var poolTransform = m_itemPoolContainer.transform;
            for (var i = 0; i < poolTransform.childCount; i++)
            {
                var item = poolTransform.GetChild(i);
                if (item && item.TryGetComponent(out HabitationObject poolItem))
                {
                    m_itemPool.Add(poolItem);
                }
            }

            if (m_itemPool.Count != m_itemPoolContainer.transform.childCount)
            {
                Debug.LogError("A game object in the item pool didn't have a HabitationObject script attached to it! This may cause problems. Please make sure that all objects in the pool are set up correctly.");
            }

            foreach (var go in m_placementPoints) { go.MiniGameLogic = this; }
            m_snapshotProperties = new MaterialPropertyBlock[m_itemDisplays.Length];
            for (var i = 0; i < m_snapshotProperties.Length; i++) { m_snapshotProperties[i] = new(); }

            LocationManager.Instance.OnPlayerJoinedRoom += OnPlayerJoinedRoom;
        }

        private void Start()
        {
            if (m_onlyDisplaySequence) { DisplaySisterCodeText(); }
            else { DisplaySubmitCodeText(); }

            if (IsServer) { GrabItemsFromPool(); }
            m_onBeginNightPhase.AddListener(OnBeginNightPhase);
            m_onEndNightPhase.AddListener(OnEndNightPhase);
        }

        private new void OnDestroy()
        {
            m_onBeginNightPhase.RemoveListener(OnBeginNightPhase);
            m_onEndNightPhase.RemoveListener(OnEndNightPhase);
            base.OnDestroy();
        }

        private void OnEnable()
        {
            m_miniGame.MiniGameInit = () =>
            {
                OnBeginNightPhase();
                RequestDisplayItemSnapshotsClientRpc();
            };
        }

        private void OnPlayerJoinedRoom(NetworkObject player, MiniGameRoom room)
        {
            if (room != MiniGameRoom.Habitation) { return; }

            var relevantSpawn = LocationManager.Instance.GetGamePositionByIndex(MiniGameRoom.Habitation, 0);
            if (relevantSpawn.OccupyingPlayer != player) { return; }

            var thisPlayerId = player.GetOwnerPlayerId();
            if (IsServer && thisPlayerId.HasValue && NetworkObject.GetOwnerPlayerId() != thisPlayerId.Value)
            {
                foreach (var item in m_itemPool) { item.NetworkObject.ChangeOwnership(thisPlayerId.Value); }
                UnOccupyAllPlacementPointsClientRpc(NetcodeHelpers.CreateSendRpcParams(thisPlayerId.Value));
            }
        }

        public void SubmitAnswer() => SubmitAnswerServerRpc();

        [ServerRpc(RequireOwnership = false)]
        private void SubmitAnswerServerRpc()
        {
            if (m_hasBeenSubmitted || !HasAllInputsInserted())
            {
                if (HasAllInputsInserted()) { return; }
                var viewers = GetMiniGameViewers();
                DisplaySolutionIncompleteTextClientRpc(NetcodeHelpers.CreateSendRpcParams(viewers));
                return;
            }

            m_hasBeenSubmitted = true;

            var grade = GradeAnswer();

            if (HasAllInputsInserted() && grade == m_maxInputs.Value) { Success(); }
            else if (HasAllInputsInserted())
            {
                FailureServerRpc(grade);
                var viewers = GetMiniGameViewers();
                DisplayFailureTextClientRpc(NetcodeHelpers.CreateSendRpcParams(viewers));
                NotifyLinkedMiniGameOfCompletion(false);
            }

            _ = StartCoroutine(RerollNewSequenceAfterTime());
        }

        public void OnItemInserted(HabitationObject item, int placementIndex)
        {
            if (!item.IsOwner || m_placementPoints[placementIndex].PlacedItem != null || item.IsBeingGrabbed()) { return; }

            InsertItemIntoPosition(item, placementIndex);
        }

        public void OnItemRemoved(HabitationObject item, int placementIndex)
        {
            if (!item.IsOwner) { return; }

            var isItemInPosition = m_placementPoints[placementIndex].PlacedItem == item;

            if (isItemInPosition) { RemoveItemFromPosition(item, placementIndex); }
        }

        private void OnBeginNightPhase()
        {
            if (m_onlyDisplaySequence) { DisplaySisterCodeText(); }
            else { DisplaySubmitCodeText(); }

            if (!IsServer) { return; }

            var shouldIncreaseInputs = GameManager.Instance.CurrentRoundCount > 1 && m_increaseMaxLengthEachRound;

            if (shouldIncreaseInputs) { IncrementInputCount(); }

            ResetMiniGame();
            GrabItemsFromPool();

            if (m_onlyDisplaySequence)
            {
                SubmitSisterCorrectInputClientRpc();
                _ = StartCoroutine(WaitForOccupant());
            }
            else { SetRandomInput(); }
        }

        private void OnEndNightPhase()
        {
            m_hasBeenReset = false;
            ResetAllActivePedestalColors();
        }

        [ClientRpc]
        private void UnOccupyAllPlacementPointsClientRpc(ClientRpcParams clientRpcParams = default)
        {
            foreach (var placementPoint in m_placementPoints)
            {
                var placedItem = placementPoint.PlacedItem;
                if (placedItem == null)
                {
                    continue;
                }

                OnItemRemoved(placedItem, placementPoint.PlacementIndex);
                placedItem.RespawnItem();
            }
        }

        private IEnumerator WaitForOccupant()
        {
            yield return new WaitUntil(() => m_miniGame.SpawnPoint.OccupyingPlayer != null && m_sisterCorrectInput != default);

            var ownerPlayerId = m_miniGame.SpawnPoint.OccupyingPlayer.GetOwnerPlayerId();
            if (!ownerPlayerId.HasValue)
            {
                yield break;
            }
            var playerInMiniGame = ownerPlayerId.Value;
            var commander = LocationManager.Instance.GetPlayersInRoom(MiniGameRoom.Commander).FirstOrDefault();
            if (commander == null)
            {
                yield break;
            }
            var commanderPlayerId = commander.GetOwnerPlayerId();
            if (!commanderPlayerId.HasValue)
            {
                yield break;
            }
            var viewers = new[] { playerInMiniGame, commanderPlayerId.Value };
            RequestDisplayItemSnapshotsClientRpc(NetcodeHelpers.CreateSendRpcParams(viewers));
        }

        private void ResetMiniGame()
        {
            m_hasBeenReset = true;
            ClearInputsServerRpc();
            m_miniGame.ResetMiniGame(false);
            if (m_sisterMiniGame != null && m_sisterMiniGame.m_hasBeenReset)
            {
                //The other MiniGame was responsible for resetting the item pool, so this one will skip it.
                return;
            }
            ResetItemPoolServerRpc();
        }

        private IEnumerator RerollNewSequenceAfterTime()
        {
            yield return m_resetDelay;

            var viewers = GetMiniGameViewers();
            var clientRpcParams = NetcodeHelpers.CreateSendRpcParams(viewers);

            if (m_onlyDisplaySequence)
            {
                m_sisterMiniGame.DisplaySubmitCodeTextClientRpc(clientRpcParams);
                DisplaySisterCodeTextClientRpc(clientRpcParams);
            }
            else
            {
                m_sisterMiniGame.DisplaySisterCodeTextClientRpc(clientRpcParams);
                DisplaySubmitCodeTextClientRpc(clientRpcParams);
            }
            ResetAllActivePedestalColors();
            RerollNewSequence();
            m_hasBeenSubmitted = false;
        }

        private void RerollNewSequence()
        {
            ResetItemPoolClientRpc(NetcodeHelpers.CreateSendRpcParams(m_itemPool[0].OwnerClientId));
            SetRandomInput();

            if (m_onlyDisplaySequence) { _ = StartCoroutine(WaitForOccupant()); }
            else
            {
                m_sisterMiniGame.SubmitSisterCorrectInputClientRpc();
                _ = StartCoroutine(m_sisterMiniGame.WaitForOccupant());
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void ClearInputsServerRpc()
        {
            m_correctInput = new HabitationObject[m_maxInputs.Value];
            m_currentInput = new HabitationObject[m_maxInputs.Value];
            m_sisterCorrectInput = default;
        }

        private void SetRandomInput()
        {
            var availableItemsInPool = m_itemPool.Where(item => !item.m_isPickedForAnswer);
            var availableItemsInPoolCount = availableItemsInPool.Count();
            if (availableItemsInPoolCount < m_maxInputs.Value)
            {
                if (availableItemsInPoolCount == 0)
                {
                    Debug.LogError("No items in the item pool were available to assign for the Habitation MiniGame!");
                }
                else
                {
                    Debug.LogError($"Not enough items in the item pool were available to assign for the Habitation MiniGame! Please make sure there are more than {m_maxInputs.Value} items available in total.");
                }
                return;
            }

            ClearInputsServerRpc();
            var newInput = new HabitationObject[m_maxInputs.Value];

            for (var i = 0; i < Mathf.Min(m_maxInputs.Value, newInput.Length); i++)
            {
                var randomInt = Random.Range(0, m_itemPool.Count);
                while (m_itemPool[randomInt].m_isPickedForAnswer) { randomInt = Random.Range(0, m_itemPool.Count); }
                newInput[i] = m_itemPool[randomInt];
                m_itemPool[randomInt].m_isPickedForAnswer = true;
            }

            m_correctInput = newInput;
            SubmitNewInputClientRpc(newInput.Select(o => (NetworkReference<HabitationObject>)o).ToArray());
        }

        [ClientRpc]
        private void SubmitSisterCorrectInputClientRpc() =>
            StartCoroutine(AssignSisterCorrectInput());

        private IEnumerator AssignSisterCorrectInput()
        {
            yield return new WaitUntil(() => m_sisterMiniGame.m_correctInput != default);
            if (m_sisterMiniGame == null)
            {
                Debug.LogError("A Habitation MiniGame did not have it's MiniGameLogic component attached to it! Please make sure the MiniGameLogic component is attached to the same object as the Minigame itself.", m_miniGame.gameObject);
            }
            m_sisterCorrectInput = m_sisterMiniGame.GetCorrectInput();
        }

        private void GrabItemsFromPool()
        {
            if (!IsServer || m_onlyDisplaySequence) { return; }

            //If the item pool contains m_minItemsFromPool or less items, assign them all to this station
            if (m_itemPool.Count(item => !item.m_isAssignedToStation) <= m_minItemsFromPool)
            {
                foreach (var item in m_itemPool.Where(item => !item.m_isAssignedToStation))
                {
                    InitializeItem(item);
                }
                return;
            }

            var itemAmount = Random.Range(m_minItemsToGrab, m_maxItemsToGrab);
            var randomItem = -1;
            for (var i = 0; i < itemAmount; i++)
            {
                randomItem = Random.Range(0, m_itemPool.Count);

                if (m_itemPool[randomItem].m_isAssignedToStation)
                {
                    i--;
                    continue;
                }

                InitializeItem(m_itemPool[randomItem]);
            }
        }

        private void InitializeItem(HabitationObject item)
        {
            if (!item.NetworkObject.IsOwner) { item.NetworkObject.ChangeOwnership(PlayerId.ServerPlayerId()); }

            var colorForItem = GetRandomColorForItem(item);
            var spawnPointIndex = Random.Range(0, m_itemBoxPoints.Length);

            item.m_netTransform.Teleport(m_itemBoxPoints[spawnPointIndex].position, Quaternion.identity, Vector3.one);
            item.m_isAssignedToStation = true;
            item.m_respawnPoint.Value = m_itemBoxPoints[spawnPointIndex].position;
            item.SetItemColor(colorForItem, true);
        }

        private void ResetItemPool()
        {
            foreach (var item in m_itemPool)
            {
                ResetItemPoolVariablesServerRpc();

                if (item.m_position != -1)
                {
                    OnItemRemoved(item, item.m_position);
                }
            }
        }
        [ServerRpc(RequireOwnership = false)]
        private void ResetItemPoolVariablesServerRpc()
        {
            foreach (var item in m_itemPool)
            {
                item.m_isAssignedToStation = false;
                item.m_isPickedForAnswer = false;
            }
        }

        [ClientRpc]
        private void ResetItemPoolClientRpc(ClientRpcParams clientRpcParams = default)
        {
            ResetItemPool();
        }
        [ServerRpc(RequireOwnership = false)]
        private void ResetItemPoolServerRpc()
        {
            foreach (var item in m_itemPool)
            {
                if (!item.IsOwnedByServer)
                {
                    item.NetworkObject.ChangeOwnership(NetworkManager.ServerClientId);
                }
            }
            ResetItemPool();
        }

        [ClientRpc]
        private void SubmitNewInputClientRpc(NetworkReference<HabitationObject>[] newInput) => m_correctInput = newInput.Select(i => i.Value).ToArray();

        private void IncrementInputCount()
        {
            if (!IsServer) { return; }
            if (m_maxInputs.Value <= 6) { m_maxInputs.Value++; }
        }

        private HabitationObject[] GetCorrectInput() => m_correctInput;

        private int GradeAnswer()
        {
            var correct = 0;
            for (var i = 0; i < m_maxInputs.Value; i++)
            {
                if (m_currentInput[i].Equals(m_correctInput[i])) { correct++; }
            }

            return correct;
        }

        private bool IsSubmissionCorrect(int submitIndex) => m_currentInput[submitIndex].Equals(m_correctInput[submitIndex]);

        private bool HasAllInputsInserted()
        {
            for (var i = 0; i < m_maxInputs.Value; i++)
            {
                if (m_currentInput[i] == default) { return false; }
            }

            return true;
        }

        private void Success()
        {
            m_miniGame.IncreaseHealth();

            var viewers = GetMiniGameViewers();

            NotifyLinkedMiniGameOfCompletion(true);
            DisplayCompletionTextClientRpc(NetcodeHelpers.CreateSendRpcParams(viewers));
            ChangeAllActivePedestalColors(Color.green);
        }

        [ServerRpc(RequireOwnership = false)]
        private void FailureServerRpc(int grade = 0)
        {
            switch (grade)
            {
                case 1:
                    m_miniGame.DecreaseHealth((int)(m_miniGame.Config.HealthChangeOnAction * .66f));
                    break;
                case 2:
                    m_miniGame.DecreaseHealth((int)(m_miniGame.Config.HealthChangeOnAction * .33f));
                    break;
                default:
                    ChangeAllActivePedestalColors(Color.red);
                    m_miniGame.DecreaseHealth();
                    return;
            }

            //We get here if our grade is above 0
            for (var i = 0; i < m_maxInputs.Value; i++)
            {
                var correct = IsSubmissionCorrect(i);
                if (correct) { ChangePedestalColorClientRpc(i, Color.green); }
                else { ChangePedestalColorClientRpc(i, Color.red); }
            }
        }

        private void NotifyLinkedMiniGameOfCompletion(bool success)
        {
            if (m_sisterMiniGame != null)
            {
                if (success)
                {
                    m_sisterMiniGame.DisplayCompletionTextClientRpc();
                    m_sisterMiniGame.m_miniGame.IncreaseHealth();
                }
                else
                {
                    m_sisterMiniGame.DisplayFailureTextClientRpc();
                    m_sisterMiniGame.m_miniGame.DecreaseHealth();
                }
            }
            else
            {
                Debug.LogError("A Habitation MiniGame did not have it's MiniGameLogic component attached to it! Please make sure the MiniGameLogic component is attached to the same object as the Minigame itself.", m_miniGame.gameObject);
            }
        }

        private void DisplaySisterCodeText() => m_textDisplay.text = SISTER_CODE_TEXT;

        private void DisplaySubmitCodeText() => m_textDisplay.text = SUBMIT_SIDE_TEXT;


        [ClientRpc]
        private void RequestDisplayItemSnapshotsClientRpc(ClientRpcParams clientRpcParams = default) =>
            _ = StartCoroutine(DisplayItemIcons());

        private IEnumerator DisplayItemIcons()
        {
            yield return new WaitUntil(() => m_sisterCorrectInput != default);

            for (var i = 0; i < m_placementPoints.Length; i++)
            {
                if (i >= m_maxInputs.Value)
                {
                    m_snapshotProperties[i].SetVector(m_snapshotCoordsProperty, Vector4.zero);
                    m_snapshotProperties[i].SetColor(m_snapshotColorProperty, Color.black);
                    m_itemDisplays[i].SetPropertyBlock(m_snapshotProperties[i]);
                    continue;
                }

                if (m_sisterCorrectInput[i] != default)
                {
                    m_snapshotProperties[i].SetVector(m_snapshotCoordsProperty, m_sisterCorrectInput[i].TextureCoords);
                    m_snapshotProperties[i].SetColor(m_snapshotColorProperty, m_sisterCorrectInput[i].CurrentColor);
                    m_itemDisplays[i].SetPropertyBlock(m_snapshotProperties[i]);
                }
            }
        }

        [ClientRpc]
        private void DisplayCompletionTextClientRpc(ClientRpcParams clientRpcParams = default) =>
            m_textDisplay.text = COMPLETION_TEXT;

        [ClientRpc]
        private void DisplayFailureTextClientRpc(ClientRpcParams clientRpcParams = default) =>
            m_textDisplay.text = FAILURE_TEXT;

        [ClientRpc]
        private void DisplaySolutionIncompleteTextClientRpc(ClientRpcParams clientRpcParams = default) =>
            m_textDisplay.text = SOLUTION_INCOMPLETE_TEXT;

        [ClientRpc]
        private void DisplaySisterCodeTextClientRpc(ClientRpcParams clientRpcParams = default) => DisplaySisterCodeText();

        [ClientRpc]
        private void DisplaySubmitCodeTextClientRpc(ClientRpcParams clientRpcParams = default) => DisplaySubmitCodeText();

        private void InsertItemIntoPosition(HabitationObject placeableObject, int placementIndex)
        {
            if (m_placementPoints[placementIndex].IsOccupied)
            {
                Debug.LogWarning("Tried to insert an item into a placement point that was already occupied in the Habitation MiniGame!");
                return;
            }

            if (!placeableObject)
            {
                Debug.LogError($"A MiniGame item was attempted to be inserted at position {placementIndex} without a HabitationObject script attached! Please attach this script to the object.");
                return;
            }

            if (!placeableObject.IsBeingGrabbed())
            {
                if (m_itemPlacementAudio != null) { m_itemPlacementAudio.PlayAudio(); }
            }

            placeableObject.m_isInPosition.Value = true;
            placeableObject.m_position = placementIndex;

            // Physics setup
            placeableObject.m_spring.connectedBody = m_placementPoints[placementIndex].m_placementPointBody;
            placeableObject.m_spring.spring = 50;
            placeableObject.m_rb.useGravity = false;

            // Light up the pedestal
            m_placementPoints[placementIndex].PlacedItem = placeableObject;

            //The owner colors their own pedestal to avoid latency
            m_placementPoints[placementIndex].ChangePedestalColor(Color.white);
            ChangePedestalColorForAllServerRpc(placementIndex, Color.white);
            InsertItemAsInputServerRpc(placementIndex, placeableObject);
        }

        [ServerRpc(RequireOwnership = false)]
        private void InsertItemAsInputServerRpc(int placementIndex, NetworkReference<HabitationObject> miniGameItem)
        {
            if (m_currentInput == null) { return; }
            m_currentInput[placementIndex] = miniGameItem;
        }

        [ServerRpc(RequireOwnership = false)]
        private void ClearInputServerRpc(int placementIndex)
        {
            m_currentInput[placementIndex] = default;
        }

        private void RemoveItemFromPosition(HabitationObject habitationObject, int placementIndex)
        {
            if (!m_placementPoints[placementIndex].IsOccupied)
            {
                Debug.LogError("Tried to remove an item into a placement point that was not occupied in the Habitation MiniGame!");
                return;
            }

            if (!habitationObject)
            {
                Debug.LogError($"Tried to remove item at position {placementIndex} without a HabitationObject script attached!");
                return;
            }

            habitationObject.m_isInPosition.Value = false;
            habitationObject.m_position = -1;
            m_placementPoints[placementIndex].PlacedItem = null;

            // Physics reset
            habitationObject.m_spring.connectedBody = null;
            habitationObject.m_spring.spring = 0;
            habitationObject.m_rb.useGravity = true;

            //Un-light up the pedestal
            //The owner colors their own pedestal to avoid latency
            m_placementPoints[placementIndex].ChangePedestalColor(Color.black);
            ChangePedestalColorForAllServerRpc(placementIndex, Color.black);
            ClearInputServerRpc(placementIndex);
        }

        [ServerRpc(RequireOwnership = false)]
        private void ChangePedestalColorForAllServerRpc(int pedestalIndex, Color color) => ChangePedestalColorClientRpc(pedestalIndex, color);

        [ClientRpc]
        private void ChangePedestalColorClientRpc(int pedestalIndex, Color newColor) =>
            m_placementPoints[pedestalIndex].ChangePedestalColor(newColor);

        private void ChangeAllActivePedestalColors(Color newColor)
        {
            for (var i = 0; i < m_maxInputs.Value; i++) { ChangePedestalColorClientRpc(i, newColor); }
        }
        private void ResetAllActivePedestalColors()
        {
            for (var i = 0; i < m_maxInputs.Value; i++)
            {
                ChangePedestalColorClientRpc(i, m_placementPoints[i].IsOccupied ? Color.white : Color.black);
            }
        }

        private Color GetRandomColorForItem(HabitationObject item)
        {
            var randomColor = Random.Range(0, m_availableColors.Length);
            var colorForItem = m_availableColors[randomColor];
            var similarItemsWithSameColor = m_itemPool.Count(items => items.ItemName == item.ItemName && items.CurrentColor == colorForItem);

            //If there is a similar item with the same color, reroll the color up to 5 times.
            for (var c = 0; c < 5; c++)
            {
                if (similarItemsWithSameColor == 0) { break; }

                randomColor = Random.Range(0, m_availableColors.Length);
                colorForItem = m_availableColors[randomColor];
                similarItemsWithSameColor = m_itemPool.Count(items => items.ItemName == item.ItemName && items.CurrentColor == colorForItem);
            }

            return colorForItem;
        }

        private PlayerId[] GetMiniGameViewers()
        {
            var playersInMiniGame =
                LocationManager.Instance.GetPlayersInRoom(MiniGameRoom.Habitation)?
                    .Select(player => player.GetOwnerPlayerId() ?? PlayerId.New()).ToArray();

            var commander = LocationManager.Instance.GetPlayersInRoom(MiniGameRoom.Commander)?
                .Select(player => player.GetOwnerPlayerId() ?? PlayerId.New()).ToArray();

            var viewers = new PlayerId[playersInMiniGame.Length + commander.Length];
            playersInMiniGame.CopyTo(viewers, 0);
            commander.CopyTo(viewers, playersInMiniGame.Length);

            return viewers;
        }
    }
}
