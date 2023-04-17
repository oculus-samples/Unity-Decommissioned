// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using System.Collections;
using System.Linq;
using Meta.Decommissioned.Game;
using Meta.Decommissioned.Game.MiniGames;
using Meta.Multiplayer.Avatar;
using Oculus.Avatar2;
using TMPro;
using UnityEngine;

namespace Meta.Decommissioned.UI
{
    /**
     * Class for managing a bit of floating text that is shown for a short time during a specific phase before being destroyed.
     */
    public class FloatingPhaseInfo : MonoBehaviour
    {
        [SerializeField] private TextMeshPro m_textComponent;
        [SerializeField] private Phase m_trackedPhase = Phase.Night;
        [TextArea(2, 2)][SerializeField] private string m_tutorialText;
        [SerializeField] private float m_verticalOffset = 0.1f;
        [SerializeField] private float m_duration = 5f;
        [SerializeField] private bool m_useRightAnchor;
        private Transform m_textAnchor;

        private void Update()
        {
            if (m_textAnchor == null) { return; }
            var newPosition = m_textAnchor.position;
            newPosition.y += m_verticalOffset;
            transform.position = newPosition;
        }

        private void OnEnable()
        {
            m_textComponent.gameObject.SetActive(false);
            GamePhaseManager.Instance.OnPhaseChanged += OnPhaseChanged;
        }

        private void OnDestroy() => GamePhaseManager.Instance.OnPhaseChanged -= OnPhaseChanged;

        private void OnPhaseChanged(Phase newPhase)
        {
            if (newPhase != m_trackedPhase) { return; }
            var isHandTracking = (OVRInput.GetConnectedControllers() & OVRInput.Controller.Hands) != 0;
            if (isHandTracking) { return; }
            ShowTutorialText();
            _ = StartCoroutine(TetherToAnchor());
        }

        private IEnumerator TetherToAnchor()
        {
            yield return new WaitUntil(() => LocationManager.Instance.GetPlayersInRoom(MiniGameRoom.Holodeck).FirstOrDefault() != null);
            var activePlayer = LocationManager.Instance.GetPlayersInRoom(MiniGameRoom.Holodeck).FirstOrDefault();

            AvatarEntity player = null;
            yield return new WaitUntil(() => activePlayer.TryGetComponent(out player));

            m_textAnchor = m_useRightAnchor ?
                player.GetJointTransform(CAPI.ovrAvatar2JointType.RightHandWrist) :
                player.GetJointTransform(CAPI.ovrAvatar2JointType.LeftHandWrist);
        }

        [ContextMenu("Show Tutorial Text")]
        public void ShowTutorialText()
        {
            m_textComponent.gameObject.SetActive(true);
            _ = StartCoroutine(ShowTutorialForDuration());
        }

        private IEnumerator ShowTutorialForDuration()
        {
            m_textComponent.text = m_tutorialText;
            yield return new WaitForSeconds(m_duration);
            m_textComponent.gameObject.SetActive(false);
        }
    }
}
