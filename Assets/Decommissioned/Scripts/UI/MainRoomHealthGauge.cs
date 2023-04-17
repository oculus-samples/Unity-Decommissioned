// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using Meta.Decommissioned.Game;
using Meta.Decommissioned.Game.MiniGames;
using Meta.Utilities;
using TMPro;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

namespace Meta.Decommissioned.UI
{
    public class MainRoomHealthGauge : Multiton<MainRoomHealthGauge>
    {
        [Tooltip("The task room that this gauge will keep track of.")]
        [field: SerializeField] public MiniGameRoom MiniGameRoom { get; private set; } = MiniGameRoom.None;

        [Tooltip("A reference to the needle on the gauge that will move to display the health.")]
        [SerializeField] private Transform m_displayNeedle;

        [SerializeField] private TextMeshPro m_healthDisplay;
        [SerializeField] private bool m_updateHealthChangeDisplay;
        [SerializeField] private MeshRenderer m_gaugeFlashingMesh;
        [SerializeField] private MeshRenderer m_gaugeBaseMesh;

        [SerializeField, AutoSetFromChildren] private StationHealthChangeDisplay m_healthChangeDisplay;
        [SerializeField] private UnityEvent<int> m_onGaugeReadoutChanged;

        [SerializeField] private EnumDictionary<MiniGameRoom, float> m_textVerticalOffsets = new();
        [SerializeField] private float m_textHorizontalOffset = 0.915f;
        [SerializeField] private float m_textHorizontalScale = 13f;

        public bool ShouldFlashForCallout => m_gaugeFlashingMesh != null;

        private int m_healthAtPhaseStart = 100;
        private float m_minNeedlePosition = 0.06f;
        private float m_maxNeedlePosition = 0;

        private float m_currentNeedlePosition;

        private MaterialPropertyBlock m_materialProperties;
        private readonly int m_textOffsetName = Shader.PropertyToID("_DetailAlbedoMap_ST");

        internal MiniGame m_miniGame;

        private new void Awake()
        {
            m_materialProperties = new();

            m_materialProperties.SetVector(m_textOffsetName, new Vector4(m_textHorizontalScale, 12, m_textHorizontalOffset, m_textVerticalOffsets[MiniGameRoom]));
            m_gaugeBaseMesh.SetPropertyBlock(m_materialProperties);

            if (MiniGameRoom == MiniGameRoom.None)
            {
                Debug.LogError("A MainRoomHealthGauge was not assigned to a minigame room! It will be disabled this session.");
                gameObject.SetActive(false);
            }
        }

        private void Start()
        {
            m_healthChangeDisplay.ResetDisplay();
            InitializeMiniGame();
        }

        private new void OnEnable()
        {
            base.OnEnable();

            if (GamePhaseManager.Instance != null)
            {
                GamePhaseManager.Instance.OnPhaseChanged += OnPhaseChanged;
            }

            if (GameManager.Instance != null)
            {
                GameManager.OnGameStateChanged += OnGameStateChanged;
            }

            UpdateGaugeText();
            if (!m_miniGame) { return; }
            m_miniGame.OnHealthChanged += OnMiniGameHealthChanged;
            m_miniGame.SpawnPoint.OnOccupyingPlayerChanged += OnPlayerChanged;
        }

        private new void OnDisable()
        {
            if (GamePhaseManager.Instance != null)
            {
                GamePhaseManager.Instance.OnPhaseChanged -= OnPhaseChanged;
            }

            if (m_miniGame != null)
            {
                m_miniGame.OnHealthChanged -= OnMiniGameHealthChanged;
            }

            base.OnDisable();
        }

        private void InitializeMiniGame()
        {
            m_miniGame = MiniGame.GetByMiniGameRoom(MiniGameRoom);
            if (m_miniGame == null)
            {
                Debug.LogError("A MainRoomHealthGauge was unable to find a minigame in the assigned " +
                               "MiniGameRoom. It will be disabled this session.");
                gameObject.SetActive(false);
                return;
            }

            m_miniGame.OnHealthChanged += OnMiniGameHealthChanged;
            OnMiniGameHealthChanged();
        }

        private void OnGameStateChanged(GameState state)
        {
            if (state != GameState.Gameplay)
            {
                return;
            }
            m_healthChangeDisplay.ResetDisplay();
        }

        private void OnPlayerChanged(NetworkObject prevPlayer, NetworkObject newPlayer) => OnMiniGameHealthChanged();

        private void OnPhaseChanged(Phase phase)
        {
            switch (phase)
            {
                case Phase.Night:
                    m_healthAtPhaseStart = m_miniGame.CurrentHealth;
                    m_healthChangeDisplay.ResetDisplay();
                    break;
                case Phase.Discussion:
                    m_healthChangeDisplay.UpdateDisplay(m_miniGame.GetHealthChange(),
                        m_miniGame.CurrentHealth > m_healthAtPhaseStart);
                    UpdateGaugeText();
                    break;
                case Phase.Voting:
                    break;
                case Phase.Invalid:
                    break;
                case Phase.Planning:
                    break;
                default:
                    break;
            }
        }

        private void OnMiniGameHealthChanged()
        {
            UpdateGaugeText();
            m_onGaugeReadoutChanged?.Invoke(m_miniGame.CurrentHealth);
        }

        private void UpdateGaugeText()
        {
            if (m_miniGame == null)
            {
                return;
            }

            m_healthDisplay.text = m_miniGame.GetMiniGameHealthRatio() * 100 + "%";

            if (m_updateHealthChangeDisplay)
            {
                m_healthChangeDisplay.UpdateDisplay(m_miniGame.GetHealthChange(),
                    m_miniGame.CurrentHealth > m_healthAtPhaseStart);
            }
        }

        public void SetGaugeDangerFlash(bool on) => m_gaugeFlashingMesh.gameObject.SetActive(on);

        private void FixedUpdate()
        {
            var xPos = GetNeedlePosition();
            m_currentNeedlePosition = Mathf.MoveTowards(m_currentNeedlePosition, xPos, .001f);
            var localNeedlePosition = m_displayNeedle.localPosition;
            m_displayNeedle.localPosition = new Vector3(m_currentNeedlePosition, localNeedlePosition.y, localNeedlePosition.z);
        }

        private float GetNeedlePosition()
        {
            if (m_miniGame == null)
            {
                return 1;
            }

            var healthValue = (float)m_miniGame.CurrentHealth / m_miniGame.Config.MaxHealth; //Gives a 0-1 float
            var position = math.remap(0, 1, m_minNeedlePosition, m_maxNeedlePosition, healthValue);
            return position;
        }
    }
}
