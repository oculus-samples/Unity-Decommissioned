// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using System.Collections;
using Meta.Decommissioned.Game;
using Meta.Decommissioned.Lobby;
using Meta.Decommissioned.Player;
using Meta.Decommissioned.Utils;
using Meta.Multiplayer.Networking;
using Meta.Multiplayer.PlayerManagement;
using Meta.Utilities;
using NaughtyAttributes;
using TMPro;
using Unity.Netcode;
using UnityEngine;

namespace Meta.Decommissioned.UI
{
    /// <summary>
    /// Class for managing the text and appearance of a nameplate. Displays the name, role, color,
    /// and current status of players in the relevant <see cref="GamePosition"/> to relevant clients.
    /// </summary>
    public class PlayerNameplate : NetworkBehaviour
    {
        [SerializeField, Required] private GamePosition m_associatedPlayerPosition;

        [SerializeField, Required] private MeshRenderer m_nameplateBaseMesh;
        [SerializeField, Required] private MeshRenderer m_nameplateFlashingMesh;
        [SerializeField, Required] private MeshRenderer m_nameplateLightMesh;

        [SerializeField, Required] private TMP_Text m_playerName;
        [SerializeField, Required] private TMP_Text m_backPlayerName;
        [SerializeField, Required] private Material m_winnerIndicatorMaterial;
        [SerializeField, Required] private Material m_candidateIndicatorMaterial;
        [SerializeField, Required] private Material m_commanderIndicatorMaterial;

        [SerializeField] private EnumDictionary<NameplateTitle, float> m_textOffsets = new();

        [SerializeField] private bool m_displayCommanderStatus = true;
        [SerializeField] private bool m_lightsAlwaysOn;
        [SerializeField] private bool m_doesNotFlash;

        private MaterialPropertyBlock m_nameplateProperties;
        private MaterialPropertyBlock m_nameplateLightProperties;
        private readonly int m_textOffsetProperty = Shader.PropertyToID("_DetailAlbedoMap_ST");
        private Color m_nameplateLightColor = Color.green;

        private NetworkObject AssignedPlayer => m_associatedPlayerPosition == null ? null : m_associatedPlayerPosition.OccupyingPlayer;

        private readonly int m_emissionColorProperty = Shader.PropertyToID("_EmissionColor");

        private Coroutine m_playerDataCoroutine;
        private Coroutine m_gameEndCoroutine;
        private bool m_listenersInitialized;

        private void Start()
        {
            m_nameplateProperties = new();
            m_nameplateLightProperties = new();

            if (m_associatedPlayerPosition == null)
            {
                Debug.LogError($"Nameplate {gameObject.name}'s game position is not assigned!", this);
            }

            if (m_lightsAlwaysOn)
            {
                SetSideLightColor(Color.green);
            }
            else
            {
                ToggleSideLights(false);
            }
        }

        private void SetUpListeners()
        {
            if (m_listenersInitialized)
            {
                return;
            }

            GameManager.Instance.GameEnd.OnGameEnd += OnGameEnd;
            GameManager.OnGameStateChanged += OnGameStateChanged;
            CommanderCandidateManager.Instance.OnNewCommander += OnNewCommander;
            CommanderCandidateManager.Instance.OnNewCandidates += OnNewCandidates;
            GamePhaseManager.Instance.OnPhaseChanged += OnPhaseChanged;
            NetworkManager.Singleton.OnClientConnectedCallback += OnPlayerConnected;

            if (m_associatedPlayerPosition != null)
            {
                m_associatedPlayerPosition.OnOccupyingPlayerChanged += OnOccupyingPlayerChanged;
            }

            var localRole = PlayerRole.GetByPlayerId(PlayerManager.LocalPlayerId);

            if (localRole)
            {
                localRole.OnCurrentRoleChanged += OnRoleChanged;
            }

            m_listenersInitialized = true;
        }

        private void RemoveListeners()
        {
            if (!m_listenersInitialized)
            {
                return;
            }

            if (GameManager.Instance)
            {
                GameManager.Instance.GameEnd.OnGameEnd -= OnGameEnd;
            }

            if (CommanderCandidateManager.Instance)
            {
                CommanderCandidateManager.Instance.OnNewCommander -= OnNewCommander;
                CommanderCandidateManager.Instance.OnNewCandidates -= OnNewCandidates;
            }

            if (GamePhaseManager.Instance)
            {
                GamePhaseManager.Instance.OnPhaseChanged -= OnPhaseChanged;
            }

            if (m_associatedPlayerPosition != null && m_associatedPlayerPosition.OccupyingPlayer != null)
            {
                m_associatedPlayerPosition.OnOccupyingPlayerChanged -= OnOccupyingPlayerChanged;
            }

            if (NetworkManager.Singleton == null)
            {
                return;
            }

            NetworkManager.Singleton.OnClientConnectedCallback -= OnPlayerConnected;

            if (NetworkManager.Singleton.SpawnManager == null)
            {
                return;
            }

            var localRole = PlayerRole.GetByPlayerId(PlayerManager.LocalPlayerId);

            if (localRole)
            {
                localRole.OnCurrentRoleChanged -= OnRoleChanged;
            }

            m_listenersInitialized = false;
        }

        public override void OnDestroy()
        {
            RemoveListeners();
            base.OnDestroy();
        }

        private void GetOccupyingPlayer()
        {
            if (m_associatedPlayerPosition == null) { return; }
            _ = StartCoroutine(WaitForOccupyingPlayer());
        }

        private void ResetNameplate()
        {
            m_playerName.text = "";
            m_backPlayerName.text = "";
            SetNameplateTitle(NameplateTitle.None);
            SetFlashingMaterial(null, false);
        }

        private void UpdateNameplate()
        {
            if (AssignedPlayer == null || !m_associatedPlayerPosition || !m_associatedPlayerPosition.IsOccupied) { return; }

            var playerState = AssignedPlayer.GetOwnerPlayerId().Value.PlayerObject;
            Debug.Assert(playerState != null, $"{gameObject.name}: PlayerName is null!", this);
            Debug.Assert(playerState.Username != null, $"{gameObject.name}: PlayerObject.username is null!", this);

            var username = playerState.Username;
            username = NameTruncator.TruncateName(username);

            var ownerPlayerId = AssignedPlayer.GetOwnerPlayerId();
            Color playerColor = default;

            if (ownerPlayerId.HasValue)
            {
                playerColor = PlayerColor.GetByPlayerId(ownerPlayerId.Value).Color;
            }

            SetName(username, playerColor);

            if (GameManager.Instance.State == GameState.Gameplay)
            {
                UpdateRoleState();
            }
        }

        #region Text Behavior

        private void UpdateRoleState()
        {
            if (AssignedPlayer == null)
            {
                return;
            }

            var role = PlayerRole.GetByPlayerObject(AssignedPlayer).CurrentRole;
            var localPlayerRole = PlayerRole.GetByPlayerId(PlayerManager.LocalPlayerId).CurrentRole;

            var sideLightColor = role == Role.Mole && localPlayerRole == Role.Mole ? Color.red : Color.green;
            var assignedPlayerId = AssignedPlayer.GetOwnerPlayerId();
            var currentCommander = CommanderCandidateManager.Instance.GetCommander();
            var playerIsCommander = assignedPlayerId.HasValue && currentCommander == assignedPlayerId.Value &&
                                            GamePhaseManager.CurrentPhase != Phase.Night;

            var roleTitle = localPlayerRole == Role.Mole && role == Role.Mole ? NameplateTitle.Mole
                : AssignedPlayer.IsLocalPlayer ? role switch
                {
                    Role.Crewmate => NameplateTitle.Crewmate,
                    Role.Mole => NameplateTitle.Mole,
                    Role.Unknown => NameplateTitle.None,
                    _ => NameplateTitle.None,
                } : NameplateTitle.Crewmate;

            if (playerIsCommander && m_displayCommanderStatus)
            {
                roleTitle = NameplateTitle.Commander;
            }

            if (GameManager.Instance.State == GameState.GameEnd)
            {
                roleTitle = role == Role.Mole ? NameplateTitle.Mole : NameplateTitle.Crewmate;
            }

            SetNameplateTitle(roleTitle);
            SetSideLightColor(sideLightColor);
            ToggleSideLights(m_lightsAlwaysOn);

            if (!assignedPlayerId.HasValue)
            {
                return;
            }

            var (commanderCandidate1, commanderCandidate2) = CommanderCandidateManager.Instance.GetCommanderCandidates();
            var candidatesSelected = GamePhaseManager.CurrentPhase == Phase.Voting &&
                                 (assignedPlayerId.Value == commanderCandidate1 ||
                                  assignedPlayerId.Value == commanderCandidate2);

            if (currentCommander == assignedPlayerId.Value)
            {
                SetFlashingMaterial(m_commanderIndicatorMaterial);
            }
            else if (candidatesSelected)
            {
                SetFlashingMaterial(m_candidateIndicatorMaterial);
            }
        }

        private void SetName(string newText, Color color = default)
        {
            var nameColor = color != default ? color : Color.white;
            m_playerName.text = newText;
            m_playerName.color = nameColor;
            m_backPlayerName.text = newText;
            m_backPlayerName.color = nameColor;
        }

        private void SetNameplateTitle(NameplateTitle title)
        {
            m_nameplateProperties.SetVector(m_textOffsetProperty, new Vector4(25, 15, m_textOffsets[title], 0.42f));
            m_nameplateBaseMesh.SetPropertyBlock(m_nameplateProperties);
        }

        #endregion

        #region Lights and Colors

        private void SetFlashingMaterial(Material flashingMaterial = null, bool turnFlashingOn = true)
        {
            if (m_doesNotFlash)
            {
                return;
            }

            m_nameplateFlashingMesh.gameObject.SetActive(turnFlashingOn);

            if (!turnFlashingOn || !flashingMaterial)
            {
                return;
            }

            m_nameplateFlashingMesh.material = flashingMaterial;
        }

        private void SetSideLightColor(Color color)
        {
            m_nameplateLightColor = color;
            m_nameplateLightProperties.SetColor(m_emissionColorProperty, color);
            m_nameplateLightMesh.SetPropertyBlock(m_nameplateLightProperties);
        }

        private void ToggleSideLights(bool willActivateEmission)
        {
            m_nameplateLightProperties ??= new();

            if (!willActivateEmission)
            {
                m_nameplateLightProperties.SetColor(m_emissionColorProperty, Color.black);
                m_nameplateLightMesh.SetPropertyBlock(m_nameplateLightProperties);
                return;
            }

            m_nameplateLightProperties.SetColor(m_emissionColorProperty, m_nameplateLightColor);
            m_nameplateLightMesh.SetPropertyBlock(m_nameplateLightProperties);
        }

        #endregion

        #region Callbacks

        public override void OnNetworkSpawn()
        {
            _ = StartCoroutine(Initialization());

            IEnumerator Initialization()
            {
                yield return new WaitUntil(() => m_associatedPlayerPosition != null && GameManager.Instance != null &&
                                                 GameManager.Instance.GameEnd != null);
                GetOccupyingPlayer();
                SetUpListeners();
            }
        }

        private void OnPlayerConnected(ulong id) => GetOccupyingPlayer();

        private void OnNewCandidates(PlayerId candidateA, PlayerId candidateB)
        {
            if (!m_associatedPlayerPosition.IsOccupied)
            {
                return;
            }

            SetFlashingMaterial(null, false);

            var isCandidate = m_associatedPlayerPosition.OccupyingPlayer.GetOwnerPlayerId() == candidateA ||
                              m_associatedPlayerPosition.OccupyingPlayer.GetOwnerPlayerId() == candidateB;

            _ = StartCoroutine(ToggleLight());

            IEnumerator ToggleLight()
            {
                yield return new WaitForSeconds(0.1f);
                SetFlashingMaterial(m_candidateIndicatorMaterial, isCandidate);
            }
        }

        private void OnNewCommander(PlayerId player)
        {
            if (!m_associatedPlayerPosition.IsOccupied || GameManager.Instance.State == GameState.GameEnd)
            {
                return;
            }

            if (AssignedPlayer.GetOwnerPlayerId() == player)
            {
                SetNameplateTitle(NameplateTitle.Commander);
            }

            SetFlashingMaterial(m_commanderIndicatorMaterial, AssignedPlayer.GetOwnerPlayerId() == player);
        }

        private void OnOccupyingPlayerChanged(NetworkObject prevPlayer, NetworkObject player)
        {
            if (!m_associatedPlayerPosition.IsOccupied || player == default)
            {
                if (m_playerDataCoroutine != null)
                {
                    StopCoroutine(m_playerDataCoroutine);
                }

                ResetNameplate();
                SetFlashingMaterial(null, false);
                ToggleSideLights(m_lightsAlwaysOn);
                return;
            }

            _ = StartCoroutine(WaitForOccupyingPlayer());
        }

        private void OnGameEnd(GameEnd.GameWinner winners)
        {
            SetFlashingMaterial(null, false);
            m_gameEndCoroutine = StartCoroutine(UpdateNameplateForGameEnd(winners));
        }

        private void OnRoleChanged(Role newRole)
        {
            if (GameManager.Instance.State != GameState.Gameplay)
            {
                return;
            }
            UpdateRoleState();
        }

        private void OnPhaseChanged(Phase newPhase)
        {
            m_playerDataCoroutine = StartCoroutine(WaitForPlayerData());

            if (!m_lightsAlwaysOn)
            {
                ToggleSideLights(false);
            }
        }

        private void OnGameStateChanged(GameState state)
        {
            if (state != GameState.GameEnd && m_gameEndCoroutine != null)
            {
                StopCoroutine(m_gameEndCoroutine);
                SetFlashingMaterial(null, false);
            }

            m_playerDataCoroutine = StartCoroutine(WaitForPlayerData());
        }

        public void OnReadyUpEvent(ReadyUp.ReadyStatus readyStatus)
        {
            var playerInvalid = m_associatedPlayerPosition == null
                                || !m_associatedPlayerPosition.OccupyingPlayer
                                || !m_associatedPlayerPosition.OccupyingPlayerId.HasValue
                                || readyStatus.PlayerId != m_associatedPlayerPosition.OccupyingPlayerId;

            if (playerInvalid)
            {
                return;
            }

            ToggleSideLights(m_lightsAlwaysOn || readyStatus.IsPlayerReady);
        }

        #endregion

        #region Coroutines

        /// <summary>
        /// Behavior that executes when the game ends; change the visuals based on player roles and the determined victor.
        /// </summary>
        /// <param name="winners">The specified winners of the game (moles or crew).</param>
        private IEnumerator UpdateNameplateForGameEnd(GameEnd.GameWinner winners)
        {
            yield return new WaitUntil(() => m_associatedPlayerPosition.IsOccupied);

            if (m_associatedPlayerPosition.OccupyingPlayer == null || GameManager.Instance.State != GameState.GameEnd)
            {
                yield break;
            }

            SetFlashingMaterial(null, false);
            var role = PlayerRole.GetByPlayerObject(m_associatedPlayerPosition.OccupyingPlayer);
            var gameEndTitle = NameplateTitle.None;

            switch (winners, role.CurrentRole)
            {
                case (GameEnd.GameWinner.Crewmates, Role.Crewmate):
                    SetFlashingMaterial(m_winnerIndicatorMaterial);
                    gameEndTitle = NameplateTitle.Crewmate;
                    break;

                case (GameEnd.GameWinner.Crewmates, Role.Mole):
                    gameEndTitle = NameplateTitle.Mole;
                    break;

                case (GameEnd.GameWinner.Moles, Role.Mole):
                    SetFlashingMaterial(m_winnerIndicatorMaterial);
                    gameEndTitle = NameplateTitle.Mole;
                    break;

                case (GameEnd.GameWinner.Moles, Role.Crewmate):
                    gameEndTitle = NameplateTitle.Crewmate;
                    break;

                default:
                    gameEndTitle = NameplateTitle.None;
                    break;
            }

            SetNameplateTitle(gameEndTitle);
        }

        private IEnumerator WaitForOccupyingPlayer()
        {
            yield return new WaitUntil(() => m_associatedPlayerPosition.IsOccupied
                                             && GameManager.Instance != null
                                             && GameManager.Instance.GameEnd != null);

            m_playerDataCoroutine = StartCoroutine(WaitForPlayerData());
        }

        private IEnumerator WaitForPlayerData()
        {
            if (!AssignedPlayer)
            {
                yield break;
            }

            var playerState = AssignedPlayer.GetOwnerPlayerId().Value.PlayerObject;

            yield return new WaitUntil(() => !playerState.Username.IsNullOrEmpty() &&
                NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject() != null);

            UpdateNameplate();
        }

        #endregion

        private enum NameplateTitle
        {
            None,
            Crewmate,
            Mole,
            Commander
        }
    }
}
