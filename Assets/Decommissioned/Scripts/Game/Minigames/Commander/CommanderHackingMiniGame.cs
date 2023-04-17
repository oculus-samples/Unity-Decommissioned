// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using System.Collections;
using Meta.Utilities;
using ScriptableObjectArchitecture;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

namespace Meta.Decommissioned.Game.MiniGames
{
    /// <summary>
    /// Defines the logic for the "hacking" mini game. This mini game is meant for the commander only.
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    public class CommanderHackingMiniGame : NetworkBehaviour
    {
        private const string HACKING_TEXT_PREFIX = "> ";
        private const string HACKING_TITLE_TEXT = "//Hacking to {0} the {1} systems";
        private const string HACKING_TITLE_HEAL_TEXT = "boost";
        private const string HACKING_TITLE_HURT_TEXT = "damage";

        /// <summary>
        /// A reference to the <see cref="MiniGame"/> this mini game logic is related to.
        /// </summary>
        [Tooltip("A reference to the Minigame this mini game logic is related to.")]
        [SerializeField, AutoSet]
        private MiniGame m_miniGame;

        /// <summary>
        /// The text that will display the hacking characters during a hack.
        /// </summary>
        [Tooltip("The text that will display the hacking characters during a hack.")]
        [SerializeField] private TextMeshProUGUI m_hackingText;

        /// <summary>
        /// The text that will display the user-friendly hacking outcome.
        /// </summary>
        [Tooltip("The text that will display the user-friendly hacking outcome.")]
        [SerializeField] private TextMeshProUGUI m_hackingTitleText;

        /// <summary>
        /// The progress bar that will display hacking progress.
        /// </summary>
        [Tooltip("The progress bar that will display hacking progress.")]
        [SerializeField] private TextMeshPro m_progressBar;

        [SerializeField]
        [Tooltip("A reference to the commander station logic component.")]
        private CommanderStation m_stationLogic;

        [SerializeField] private Interactables.InteractableKeyboard m_keyboard;

        private int m_currentProgressBars;
        private string m_currentProgressBarText = "";

        private int m_maxHackingTextLines = 5;

        private bool m_hackingComplete;
        private bool m_isCurrentlyHacking;

        private YieldInstruction m_hackTextInterval = new WaitForSeconds(0.1f);
        private YieldInstruction m_authTextInterval = new WaitForSeconds(0.05f);
        private YieldInstruction m_progressResetInterval = new WaitForSeconds(0.025f);

        private string[] m_currentHackingText;
        private int m_currentHackingTextLine;
        private bool m_isClearingText;

        private float m_lastHackTime;
        private float m_hackTimeDuration = 1f;

        private Coroutine m_hackingTextRoutine;
        private Coroutine m_hackingTitleTextRoutine;

        [SerializeField] private CommanderStationHackingStrings m_hackingStrings;
        private MiniGameRoom m_currentHackingStringStation = MiniGameRoom.None;
        private int m_currentHackingString = -1;
        private int m_currentHackingStringChar;

        [SerializeField] private GameEvent m_onBeginNightPhase;
        [SerializeField] private UnityEvent<MiniGame> m_onHackingComplete;

        private void Awake()
        {
            m_currentHackingText = new string[m_maxHackingTextLines];
            ResetMachine();
        }

        private void OnEnable() => m_onBeginNightPhase.AddListener(ResetMachineForAllClientRpc);


        private void OnDisable() => m_onBeginNightPhase.RemoveListener(ResetMachineForAllClientRpc);


        [ClientRpc]
        private void ResetMachineForAllClientRpc() => ResetMachine();

        private void ResetMachine()
        {
            _ = StartCoroutine(ResetHackingProgress());
            _ = StartCoroutine(ClearHackingText(true));
            ResetHackingString();
            m_currentHackingText[0] = HACKING_TEXT_PREFIX;
            m_hackingComplete = false;
            m_isCurrentlyHacking = false;
        }

        private IEnumerator ResetHackingProgress()
        {
            while (m_currentProgressBars > 0)
            {
                m_currentProgressBarText = m_currentProgressBarText.Remove(--m_currentProgressBars);
                UpdateProgressBar();
                yield return m_progressResetInterval;
            }
        }

        private void UpdateProgressBar() => m_progressBar.text = m_currentProgressBarText;

        private void UpdateHackingText() => m_hackingText.text = string.Join('\n', m_currentHackingText);

        private IEnumerator RunHackingText()
        {
            while (m_isCurrentlyHacking && Time.time < m_lastHackTime + m_hackTimeDuration)
            {
                yield return m_hackTextInterval;

                if (IsAtEndOfHackingLine())
                {
                    IncrementHackingTextLine();
                    m_onHackingComplete.Invoke(m_miniGame);
                }

                else if (!m_isClearingText)
                {
                    var currentChar = GetCharFromHackingString();
                    if (currentChar == '\n')
                    {
                        IncrementHackingTextLine();
                        m_keyboard.PressKeyboardKey("Enter");
                    }
                    else if (currentChar != '\r')
                    {
                        m_currentHackingText[m_currentHackingTextLine] += currentChar;
                        m_keyboard.PressKeyboardKey(currentChar.ToString());
                        UpdateHackingText();
                    }
                }
            }

            m_isCurrentlyHacking = false;
            m_hackingTextRoutine = null;
        }

        private IEnumerator ClearHackingText(bool clearTitle = false)
        {
            if (m_isClearingText) { yield break; }

            m_isClearingText = true;
            var currentLine = 0;
            while (currentLine < m_maxHackingTextLines)
            {
                m_currentHackingText[currentLine++] = string.Empty;
                UpdateHackingText();

                yield return m_hackTextInterval;
            }

            m_currentHackingText[0] = HACKING_TEXT_PREFIX;
            m_currentHackingTextLine = 0;
            m_isClearingText = false;
            UpdateHackingText();
            if (clearTitle)
            {
                UpdateTitleText();
            }
        }

        public void UpdateTitleText()
        {
            if (m_hackingTitleTextRoutine != null)
            {
                StopCoroutine(m_hackingTitleTextRoutine);
                m_hackingTitleTextRoutine = null;
            }

            ClearTitleText();
            m_hackingTitleTextRoutine = StartCoroutine(PrintTitleText());
        }

        private IEnumerator PrintTitleText()
        {
            var outcomeText = m_stationLogic.IsHelping ? HACKING_TITLE_HEAL_TEXT : HACKING_TITLE_HURT_TEXT;
            var currentRoom = LocationManager.Instance.GetFriendlyMiniGameRoomName(m_stationLogic.CurrentlySelectedRoom);
            var currentTitle = string.Format(HACKING_TITLE_TEXT, outcomeText, currentRoom);
            var currentChar = 0;
            while (currentChar < currentTitle.Length)
            {
                yield return m_authTextInterval;
                m_hackingTitleText.text += currentTitle[currentChar++];
            }

            m_hackingTitleTextRoutine = null;
        }

        private void ClearTitleText() => m_hackingTitleText.text = "";

        private void IncrementHackingTextLine()
        {
            if (IsCurrentHackingTextBlockFull())
            {
                _ = StartCoroutine(ClearHackingText());
                ResetHackingString();
                return;
            }
            m_currentHackingText[++m_currentHackingTextLine] = HACKING_TEXT_PREFIX;
        }

        private void ResetHackingString()
        {
            m_currentHackingString = -1;
            m_currentHackingStringChar = 0;
            m_currentHackingStringStation = GetMiniGameRoomForHackingText();
            SetRandomHackingCode(m_currentHackingStringStation);
        }

        private bool IsCurrentHackingTextBlockFull()
        {
            var lineCount = GetCurrentHackingTextLineCount();
            return lineCount >= m_maxHackingTextLines - 1;
        }

        private int GetCurrentHackingTextLineCount() => m_currentHackingTextLine;

        /// <summary>
        /// Informs the mini game logic that the keyboard has begun being typed on.
        /// </summary>
        public void OnKeyboardType()
        {
            if (!m_hackingComplete)
            {
                m_lastHackTime = Time.time;
                if (m_hackingTextRoutine == null)
                {
                    m_isCurrentlyHacking = true;
                    m_hackingTextRoutine = StartCoroutine(RunHackingText());
                }
            }
        }

        private void SetRandomHackingCode(MiniGameRoom room = MiniGameRoom.None)
        {
            var textList = m_stationLogic.IsHelping ? m_hackingStrings.PositiveStrings[room] : m_hackingStrings.NegativeStrings[room];
            var randomText = Random.Range(0, textList.HackingStrings.Length);
            m_currentHackingString = randomText;
            m_currentHackingStringStation = room;
        }
        private MiniGameRoom GetMiniGameRoomForHackingText() => RandomOutcome() > 75 ? m_stationLogic.CurrentlySelectedRoom : MiniGameRoom.None;

        private bool IsAtEndOfHackingLine() => m_stationLogic.IsHelping ?
            m_currentHackingStringChar >= m_hackingStrings.PositiveStrings[m_currentHackingStringStation].HackingStrings[m_currentHackingString].Length :
            m_currentHackingStringChar >= m_hackingStrings.NegativeStrings[m_currentHackingStringStation].HackingStrings[m_currentHackingString].Length;

        private char GetCharFromHackingString() => m_stationLogic.IsHelping ?
        m_hackingStrings.PositiveStrings[m_currentHackingStringStation].HackingStrings[m_currentHackingString][m_currentHackingStringChar++] :
        m_hackingStrings.NegativeStrings[m_currentHackingStringStation].HackingStrings[m_currentHackingString][m_currentHackingStringChar++];

        private int RandomOutcome() => Random.Range(0, 100);
    }
}
