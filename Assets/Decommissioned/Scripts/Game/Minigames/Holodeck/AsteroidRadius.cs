// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using UnityEngine;

namespace Meta.Decommissioned.Game.MiniGames
{
    public class AsteroidRadius : MonoBehaviour
    {
        [SerializeField] private LineRenderer m_lineRenderer;
        [SerializeField] private LineRenderer m_lineRendererVertical;
        [SerializeField] private Transform m_playerTarget;
        [SerializeField] private Transform m_asteroidTarget;
        [SerializeField] private Transform m_yLevel;
        [SerializeField] private int m_resolution = 32;

        private void Awake()
        {
            m_lineRenderer.positionCount = m_resolution + 1;
            UpdateLineRenderer();
        }
        private void Update()
        {
            UpdateLineRenderer();
        }

        public void ShowRadius()
        {
            m_lineRenderer.gameObject.SetActive(true);
            m_lineRendererVertical.gameObject.SetActive(true);
        }

        public void HideRadius()
        {
            m_lineRenderer.gameObject.SetActive(false);
            m_lineRendererVertical.gameObject.SetActive(false);
        }

        private void UpdateLineRenderer()
        {
            if (m_asteroidTarget == null || m_playerTarget == null)
            {
                return;
            }

            var asteroidPos = m_asteroidTarget.position;
            var targetPos = m_playerTarget.position;

            //flatten positions for accurate radius
            asteroidPos.y = 0;
            targetPos.y = 0;
            var radius = Vector3.Distance(asteroidPos, targetPos);

            //set target position y to yLevel
            targetPos.y = m_yLevel.position.y;
            for (var i = 0; i <= m_resolution; i++)
            {
                var t = (float)i / m_resolution;

                var pointPosition = new Vector3(
                    Mathf.Cos(2 * Mathf.PI * t) * radius,
                    0,
                    Mathf.Sin(2 * Mathf.PI * t) * radius
                );
                m_lineRenderer.SetPosition(i, targetPos + pointPosition);
            }
            var astroidPosBase = asteroidPos;
            astroidPosBase.y = m_yLevel.position.y;
            m_lineRendererVertical.SetPosition(0, astroidPosBase);
            m_lineRendererVertical.SetPosition(1, m_asteroidTarget.position);

        }
    }
}

