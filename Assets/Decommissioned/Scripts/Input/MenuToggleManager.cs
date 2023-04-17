// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using UnityEngine;

namespace Meta.Decommissioned.Input
{
    /**
     * Class for managing a menu that can be shown and hidden using hand gestures.
     */
    public class MenuToggleManager : MonoBehaviour
    {
        [SerializeField] private GameObject[] m_menus;
        [SerializeField] private bool m_disableInReleaseBuild = true;

        private void OnEnable()
        {
            if (!Debug.isDebugBuild && m_disableInReleaseBuild) { Destroy(gameObject); }
            DisableMenu();
        }

        /**
         * Show or hide the menu(s).
         */
        [ContextMenu("Toggle Menu")]
        public void ToggleMenu()
        {
            foreach (var go in m_menus) { go.SetActive(!go.activeSelf); }
        }

        //Explicitly disables the menus
        private void DisableMenu()
        {
            foreach (var go in m_menus) { go.SetActive(false); }
        }
    }
}
