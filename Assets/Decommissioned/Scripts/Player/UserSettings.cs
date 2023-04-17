// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-Decommissioned/tree/main/Assets/Decommissioned/LICENSE

using Meta.Utilities;
using UnityEngine;

namespace Meta.Decommissioned.Player
{
    /// <summary>
    /// Stores and manages persistent user settings and their values.
    /// </summary>
    public class UserSettings : Singleton<UserSettings>
    {
        private void OnDestroy() => PlayerPrefs.Save();

        public void SetValue(string settingName, bool settingValue)
        {
            var boolSetting = settingValue ? 1 : 0;
            PlayerPrefs.SetInt(settingName, boolSetting);
        }

        public void SetValue(string settingName, int settingValue) => PlayerPrefs.SetInt(settingName, settingValue);


        public void SetValue(string settingName, float settingValue) => PlayerPrefs.SetFloat(settingName, settingValue);


        public void SetValue(string settingName, string settingValue) => PlayerPrefs.SetString(settingName, settingValue);


        public bool GetPlayerSetting(string settingName, out int value)
        {
            if (!SettingExists(settingName))
            {
                value = default;
                return false;
            }

            value = PlayerPrefs.GetInt(settingName);
            return true;
        }

        public bool GetPlayerSetting(string settingName, out bool value)
        {
            if (!SettingExists(settingName))
            {
                value = default;
                return false;
            }

            value = PlayerPrefs.GetInt(settingName) != 0;
            return true;
        }

        public bool GetPlayerSetting(string settingName, out float value)
        {
            if (!SettingExists(settingName))
            {
                value = default;
                return false;
            }

            value = PlayerPrefs.GetFloat(settingName);
            return true;
        }

        public bool GetPlayerSetting(string settingName, out string value)
        {
            if (!SettingExists(settingName))
            {
                value = default;
                return false;
            }

            value = PlayerPrefs.GetString(settingName);
            return true;
        }

        private bool SettingExists(string settingName) => PlayerPrefs.HasKey(settingName);
    }
}
