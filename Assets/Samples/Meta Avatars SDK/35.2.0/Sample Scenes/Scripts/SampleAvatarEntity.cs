/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * Licensed under the Oculus SDK License Agreement (the "License");
 * you may not use the Oculus SDK except in compliance with the License,
 * which is provided at the time of installation or download, or which
 * otherwise accompanies this software in either electronic or hard copy form.
 *
 * You may obtain a copy of the License at
 *
 * https://developer.oculus.com/licenses/oculussdk/
 *
 * Unless required by applicable law or agreed to in writing, the Oculus SDK
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

#nullable enable

#if USING_XR_MANAGEMENT && (USING_XR_SDK_OCULUS || USING_XR_SDK_OPENXR) && !OVRPLUGIN_UNSUPPORTED_PLATFORM
#define USING_XR_SDK
#endif

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

using Oculus.Avatar2;
#if USING_XR_SDK
using Oculus.Platform;

#endif
using UnityEngine;

using CAPI = Oculus.Avatar2.CAPI;

public class SampleAvatarEntity : OvrAvatarEntity
{
    private const string logScope = "sampleAvatar";

    [System.Serializable]
    protected struct AssetData
    {
        public AssetSource source;
        public string path;

        public AssetData(AssetSource source, string path)
        {
            this.source = source;
            this.path = path;
        }
    }

    [Header("Sample Avatar Entity")]
    [Tooltip("Attempt to load the Avatar model file from the Content Delivery Network (CDN) based on a userID, as opposed to loading from disk.")]
    [SerializeField]
    private bool _loadUserFromCdn = true;

    [Tooltip("If set to 0, attempt to load the currently logged in user avatar. Can also be set to an oculus user ID. Fallback to preset (on disk) avatar if fetch fails.")]
    [SerializeField]
    private UInt64 _cdnUserID = 0;

    [HideInInspector]
    [SerializeField]
#pragma warning disable CS0414 // disable "not used" warning
    private int _style2DropdownIndex = 0;
#pragma warning restore CS0414

    [Tooltip("Make initial requests for avatar and then defer loading until other avatars can make their requests.")]
    [SerializeField]
    private bool _deferLoading = false;

    [Header("Assets")]
    [Tooltip("Asset paths to load, and whether each asset comes from a preloaded zip file or directly from StreamingAssets. See Preset Asset settings on OvrAvatarManager for how this maps to the real file name.")]
    [SerializeField]
    protected List<AssetData> _assets = new List<AssetData>();

    [Tooltip("Adds an underscore between the path and the postfix.")]
    [SerializeField]
    private bool _underscorePostfix = true;

    [Tooltip("Filename Postfix (WARNING: Typically the postfix is Platform specific, such as \"_rift.glb\")")]
    [SerializeField]
    private string _overridePostfix = String.Empty;

    [Header("CDN")]
    [Tooltip("Automatically retry LoadUser download request on failure")]
    [SerializeField]
    protected bool _autoCdnRetry = true;

    [Tooltip("During the initial load, the user may be valid but have no avatar. If checked, do a background poll and load the avatar if the user creates one, replacing the fallback avatar.")]
    [SerializeField]
    protected bool _enablePollingForAvatarCreation = true;

    [Tooltip("How frequently to check for avatar changes")]
    [SerializeField]
    [Range(4.0f, 320.0f)]
    private float _changeCheckInterval = 8.0f;


    protected bool HasLocalAvatarConfigured => _assets.Count > 0;

    private Stopwatch _loadTime = new Stopwatch();

    private bool _isPollingAvatarChanges = false;

    private bool _stopCurrentAvatarChangePoll = false;

    private bool _isPollingAvatarCreation = false;

    private bool _stopCurrentAvatarCreationPoll = false;

    protected virtual IEnumerator Start()
    {
        // If we have not passed in a local glb path, set the assets loaded to be the default path
        if (_assets.Count == 0)
        {
            OvrAvatarLog.LogWarning("No local assets have been specified. Setting fallback avatar to default preset.");
            SetDefaultAssets();
        }

        if (!_deferLoading)
        {
            if (_loadUserFromCdn)
            {
                _userId = _cdnUserID;
                yield return LoadCdnAvatar();
            }
            else
            {
                LoadLocalAvatar();
            }
        }
    }

    protected virtual void SetDefaultAssets()
    {
        // TODO: T195612718 Update to Style 2 default path after full deprecation for Style 1
        _assets = new List<AssetData> { new(source: AssetSource.Zip, path: "0") }; // Style 1 preset path
    }

    #region Loading
    private IEnumerator LoadCdnAvatar()
    {
#if USING_XR_SDK
        // Ensure OvrPlatform is Initialized
        if (OvrPlatformInit.status == OvrPlatformInitStatus.NotStarted)
        {
            OvrPlatformInit.InitializeOvrPlatform();
        }

        while (OvrPlatformInit.status != OvrPlatformInitStatus.Succeeded)
        {
            if (OvrPlatformInit.status == OvrPlatformInitStatus.Failed)
            {
                OvrAvatarLog.LogError($"Error initializing OvrPlatform. Falling back to local avatar", logScope);
                LoadLocalAvatar();
                yield break;
            }

            yield return null;
        }

        // user ID == 0 means we want to load logged in user avatar from CDN
        if (_userId == 0)
        {
            // Get User ID
            bool getUserIdComplete = false;
            Users.GetLoggedInUser().OnComplete(message =>
            {
                if (!message.IsError)
                {
                    _userId = message.Data.ID;
                }
                else
                {
                    var e = message.GetError();
                    OvrAvatarLog.LogError($"Error loading CDN avatar: {e.Message}. Falling back to local avatar", logScope);
                }

                getUserIdComplete = true;
            });

            while (!getUserIdComplete) { yield return null; }
        }
#endif
        yield return LoadUserAvatar();
    }

    public void LoadRemoteUserCdnAvatar(ulong userId)
    {
        StartLoadTimeCounter();
        _userId = userId;
        StartCoroutine(LoadCdnAvatar());
    }

    private IEnumerator LoadUserAvatar()
    {
        if (_userId == 0)
        {
            LoadLocalAvatar();
            yield break;
        }

        yield return Retry_HasAvatarRequest();
    }

    private bool IsFromZip(AssetData asset)
    {
        return asset.source == AssetSource.Zip;
    }

    protected virtual void LoadLocalAvatar()
    {
        if (!HasLocalAvatarConfigured)
        {
            OvrAvatarLog.LogInfo("No local avatar asset configured", logScope, this);
            return;
        }

        // Zip asset paths are relative to the inside of the zip.
        // Zips can be loaded from the OvrAvatarManager at startup or by calling OvrAvatarManager.Instance.AddZipSource
        // Assets can also be loaded individually from Streaming assets
        foreach (var asset in _assets)
        {
            string assetPostfix = GetAssetPostfix(IsFromZip(asset));

            var assetPath = $"{asset.path}{assetPostfix}";
            LoadAssets(new[] { assetPath }, asset.source);
        }
    }

    public void ReloadAvatarManually(string newAssetPaths, AssetSource newAssetSource)
    {
        ReloadAvatarManually(new[] { newAssetPaths }, newAssetSource);
    }

    private string GetAssetPostfix(bool isFromZip)
    {
        string assetPostfix = (_underscorePostfix ? "_" : "")
                              + OvrAvatarManager.Instance.GetPlatformGLBPostfix(_creationInfo.renderFilters.quality, isFromZip)
                              + OvrAvatarManager.Instance.GetPlatformGLBVersion(_creationInfo.renderFilters.quality, isFromZip)
                              + OvrAvatarManager.Instance.GetPlatformGLBExtension(isFromZip);
        if (!String.IsNullOrEmpty(_overridePostfix))
        {
            assetPostfix = _overridePostfix;
        }

        return assetPostfix;
    }

    public void ReloadAvatarManually(string[] newAssetPaths, AssetSource newAssetSource)
    {
        Teardown();
        CreateEntity();

        bool isFromZip = (newAssetSource == AssetSource.Zip);
        string assetPostfix = GetAssetPostfix(isFromZip);

        string[] combinedPaths = new string[newAssetPaths.Length];
        for (var index = 0; index < newAssetPaths.Length; index++)
        {
            combinedPaths[index] = $"{newAssetPaths[index]}{assetPostfix}";
        }

        LoadAssets(combinedPaths, newAssetSource);
    }

    public bool LoadPreset(int preset, string namePrefix = "")
    {
        StartLoadTimeCounter();
        const bool isFromZip = true;
        string assetPostfix = GetAssetPostfix(isFromZip);

        var assetPath = $"{namePrefix}{preset}{assetPostfix}";
        return LoadAssets(new[] { assetPath }, AssetSource.Zip);
    }
    #endregion // Loading

    #region Retry
    protected void UserHasNoAvatarFallback()
    {
        OvrAvatarLog.LogError(
            $"Unable to find user avatar with userId {_userId}. Falling back to local avatar.", logScope, this);

        LoadLocalAvatar();
    }

    protected virtual IEnumerator Retry_HasAvatarRequest()
    {
        const float HAS_AVATAR_RETRY_WAIT_TIME = 4.0f;
        const int HAS_AVATAR_RETRY_ATTEMPTS = 12;

        int totalAttempts = _autoCdnRetry ? HAS_AVATAR_RETRY_ATTEMPTS : 1;
        bool continueRetries = _autoCdnRetry;
        int retriesRemaining = totalAttempts;
        bool hasFoundAvatar = false;
        bool requestComplete = false;
        bool shouldPollForCustomAvatar = false;
        do
        {
            var hasAvatarRequest = OvrAvatarManager.Instance.UserHasAvatarAsync(_userId);
            while (!hasAvatarRequest.IsCompleted) { yield return null; }

            switch (hasAvatarRequest.Result)
            {
                case OvrAvatarManager.HasAvatarRequestResultCode.HasAvatar:
                    hasFoundAvatar = true;
                    requestComplete = true;
                    continueRetries = false;

                    // Now attempt download
                    yield return AutoRetry_LoadUser(true);
                    // End coroutine - do not load default
                    break;

                case OvrAvatarManager.HasAvatarRequestResultCode.HasNoAvatar:
                    requestComplete = true;
                    continueRetries = false;
                    shouldPollForCustomAvatar = true;
                    OvrAvatarLog.LogDebug("User has no avatar. Falling back to local avatar.", logScope, this);
                    break;

                case OvrAvatarManager.HasAvatarRequestResultCode.SendFailed:
                    OvrAvatarLog.LogError("Unable to send avatar status request.", logScope, this);
                    break;

                case OvrAvatarManager.HasAvatarRequestResultCode.RequestFailed:
                    OvrAvatarLog.LogError("An error occurred while querying avatar status.", logScope, this);
                    break;

                case OvrAvatarManager.HasAvatarRequestResultCode.BadParameter:
                    continueRetries = false;
                    OvrAvatarLog.LogError("Attempted to load invalid userId.", logScope, this);
                    break;

                case OvrAvatarManager.HasAvatarRequestResultCode.RequestCancelled:
                    continueRetries = false;
                    OvrAvatarLog.LogInfo("HasAvatar request cancelled.", logScope, this);
                    break;

                case OvrAvatarManager.HasAvatarRequestResultCode.UnknownError:
                default:
                    OvrAvatarLog.LogError(
                        $"An unknown error occurred {hasAvatarRequest.Result}. Falling back to local avatar."
                        , logScope, this);
                    break;
            }

            continueRetries &= --retriesRemaining > 0;
            if (continueRetries)
            {
                yield return new WaitForSecondsRealtime(HAS_AVATAR_RETRY_WAIT_TIME);
            }
        } while (continueRetries);

        if (!requestComplete)
        {
            OvrAvatarLog.LogError($"Unable to query UserHasAvatar {totalAttempts} attempts", logScope, this);
        }

        if (!hasFoundAvatar)
        {
            // We cannot find an avatar, use local fallback
            UserHasNoAvatarFallback();

            if (shouldPollForCustomAvatar && _enablePollingForAvatarCreation)
            {
                yield return PollForAvatarCreation();
            }
        }

        // Check for changes unless a local asset is configured, user could create one later
        // If a local asset is loaded, it will currently conflict w/ the CDN asset
        if (hasFoundAvatar || !HasLocalAvatarConfigured)
        {
            yield return PollForAvatarChange();
        }
    }

    protected virtual IEnumerator AutoRetry_LoadUser(bool loadFallbackOnFailure)
    {
        const float LOAD_USER_POLLING_INTERVAL = 4.0f;
        const float LOAD_USER_BACKOFF_FACTOR = 1.618033988f;
        const int CDN_RETRY_ATTEMPTS = 13;

        int totalAttempts = _autoCdnRetry ? CDN_RETRY_ATTEMPTS : 1;
        int remainingAttempts = totalAttempts;
        bool didLoadAvatar = false;
        var currentPollingInterval = LOAD_USER_POLLING_INTERVAL;
        do
        {
            // Initiate user spec load (ie: CDN Avatar)
            LoadUser();

            CAPI.ovrAvatar2Result status;
            do
            {
                // Wait for retry interval before taking any action
                yield return new WaitForSecondsRealtime(currentPollingInterval);

                // Check current `entity` status
                status = this.entityStatus;
                if (status.IsSuccess() || HasNonDefaultAvatar)
                {
                    didLoadAvatar = true;
                    // Finished downloading - no more retries
                    remainingAttempts = 0;

                    OvrAvatarLog.LogVerbose(
                        "Load user retry check found successful download, ending retry routine"
                        , logScope, this);
                    break;
                }

                // Increase backoff interval
                currentPollingInterval *= LOAD_USER_BACKOFF_FACTOR;

                // `while` status is still pending, keep polling the current attempt
                // Do not start a new request - do not decrement retry attempts
            } while (status == CAPI.ovrAvatar2Result.Pending);
            // Decrement retry attempts now that load failure has been confirmed (status != Pending)
        } while (--remainingAttempts > 0);

        if (loadFallbackOnFailure && !didLoadAvatar)
        {
            OvrAvatarLog.LogError(
                $"Unable to download user after {totalAttempts} retry attempts",
                logScope, this);

            // We cannot download an avatar, use local fallback (ie: Preset Avatar)
            UserHasNoAvatarFallback();
        }
    }

    private void StartLoadTimeCounter()
    {
        _loadTime.Start();

        OnUserAvatarLoadedEvent.AddListener((OvrAvatarEntity entity) =>
        {
            _loadTime.Stop();
        });
    }

    public long GetLoadTimeMs()
    {
        return _loadTime.ElapsedMilliseconds;
    }

    #endregion // Retry

    #region Avatar Creation Polling
    protected virtual IEnumerator PollForAvatarCreation()
    {
        var waitForPollInterval = new WaitForSecondsRealtime(_changeCheckInterval);

        _isPollingAvatarCreation = true;
        bool continuePolling = _enablePollingForAvatarCreation;
        do
        {
            if (_stopCurrentAvatarCreationPoll)
            {
                _stopCurrentAvatarCreationPoll = false;
                _isPollingAvatarCreation = false;
                break;
            }
            var hasAvatarRequest = OvrAvatarManager.Instance.UserHasAvatarAsync(_userId);
            while (!hasAvatarRequest.IsCompleted) { yield return null; }

            switch (hasAvatarRequest.Result)
            {
                case OvrAvatarManager.HasAvatarRequestResultCode.HasAvatar:
                    continuePolling = false;

                    // Reload the avatar
                    ApplyConfig(GetAvatarConfig(), true);
                    yield break;

                case OvrAvatarManager.HasAvatarRequestResultCode.HasNoAvatar:
                    OvrAvatarLog.LogDebug("User has no avatar.  Continue polling in background.", logScope, this);
                    break;

                case OvrAvatarManager.HasAvatarRequestResultCode.SendFailed:
                    OvrAvatarLog.LogError("Unable to send avatar status request.", logScope, this);
                    break;

                case OvrAvatarManager.HasAvatarRequestResultCode.RequestFailed:
                    OvrAvatarLog.LogError("An error occurred while querying avatar status.", logScope, this);
                    break;

                case OvrAvatarManager.HasAvatarRequestResultCode.BadParameter:
                    continuePolling = false;
                    OvrAvatarLog.LogError("Attempted to load invalid userId.", logScope, this);
                    break;

                case OvrAvatarManager.HasAvatarRequestResultCode.RequestCancelled:
                    continuePolling = false;
                    OvrAvatarLog.LogInfo("HasAvatar request cancelled.", logScope, this);
                    break;

                case OvrAvatarManager.HasAvatarRequestResultCode.UnknownError:
                default:
                    OvrAvatarLog.LogError(
                        $"An unknown error occurred {hasAvatarRequest.Result}. Falling back to local avatar."
                        , logScope, this);
                    break;
            }

            if (continuePolling)
            {
                yield return waitForPollInterval;
            }
        } while (continuePolling);
        _isPollingAvatarCreation = false;
    }
    #endregion // Avatar Creation Polling

    #region Change Check

    protected IEnumerator PollForAvatarChange()
    {
        var waitForPollInterval = new WaitForSecondsRealtime(_changeCheckInterval);

        _isPollingAvatarChanges = true;
        bool continueChecking = true;
        do
        {
            if (_stopCurrentAvatarChangePoll)
            {
                _stopCurrentAvatarChangePoll = false;
                _isPollingAvatarChanges = false;
                break;
            }
            var checkTask = HasAvatarChangedAsync();
            while (!checkTask.IsCompleted) { yield return null; }

            switch (checkTask.Result)
            {
                case OvrAvatarManager.HasAvatarChangedRequestResultCode.UnknownError:
                    OvrAvatarLog.LogError("Check avatar changed unknown error, aborting.", logScope, this);
                    continueChecking = false; // Stop retrying or we'll just spam this error
                    break;

                case OvrAvatarManager.HasAvatarChangedRequestResultCode.BadParameter:
                    OvrAvatarLog.LogError("Check avatar changed invalid parameter, aborting.", logScope, this);
                    continueChecking = false; // Stop retrying or we'll just spam this error
                    break;

                case OvrAvatarManager.HasAvatarChangedRequestResultCode.SendFailed:
                    OvrAvatarLog.LogWarning("Check avatar changed send failed.", logScope, this);
                    break;

                case OvrAvatarManager.HasAvatarChangedRequestResultCode.RequestFailed:
                    OvrAvatarLog.LogError("Check avatar changed request failed.", logScope, this);
                    break;

                case OvrAvatarManager.HasAvatarChangedRequestResultCode.RequestCancelled:
                    OvrAvatarLog.LogInfo("Check avatar changed request cancelled.", logScope, this);
                    continueChecking = false; // Stop retrying, this entity has likely been destroyed
                    break;

                case OvrAvatarManager.HasAvatarChangedRequestResultCode.AvatarHasNotChanged:
                    OvrAvatarLog.LogVerbose("Avatar has not changed.", logScope, this);
                    break;

                case OvrAvatarManager.HasAvatarChangedRequestResultCode.AvatarHasChanged:
                    OvrAvatarLog.LogInfo("Avatar has changed, loading new spec.", logScope, this);
                    yield return AutoRetry_LoadUser(false); // Load new avatar!
                    break;
            }

            if (continueChecking)
            {
                yield return waitForPollInterval;
            }

        } while (continueChecking);
        _isPollingAvatarChanges = false;
    }

    #endregion // Change Check

    #region SDK UI

    public SampleAvatarConfig GetAvatarConfig()
    {
        var assetDataList = new List<SampleAvatarConfig.AssetData>();
        foreach (var asset in _assets)
        {
            assetDataList.Add(new SampleAvatarConfig.AssetData { source = asset.source, path = asset.path });
        }

        SampleAvatarConfig outConfig = new SampleAvatarConfig
        {
            CreationInfo = new CAPI.ovrAvatar2EntityCreateInfo()
            {
                features = _creationInfo.features,
                renderFilters = _creationInfo.renderFilters,
                lodFlags = _creationInfo.lodFlags,
            },
            ActiveView = GetActiveView(),
            ActiveManifestation = GetActiveManifestation(),
            LoadUserFromCdn = _loadUserFromCdn,
            Assets = assetDataList,
        };

        return outConfig;
    }

    public void ApplyConfig(SampleAvatarConfig config, bool requiresTeardown = false)
    {
        _loadUserFromCdn = config.LoadUserFromCdn;

        SetActiveView(config.ActiveView);
        SetActiveManifestation(config.ActiveManifestation);

        if (!requiresTeardown)
        {
            OvrAvatarLog.LogInfo($"Applied the following settings to Avatar \"{name}\" :\n{config}", logScope);
            return;
        }

        _creationInfo = new CAPI.ovrAvatar2EntityCreateInfo()
        {
            features = config.CreationInfo.features,
            renderFilters = config.CreationInfo.renderFilters,
            lodFlags = config.CreationInfo.lodFlags,
        };

        if (config.Assets != null)
        {
            _assets.Clear();
            foreach (var asset in config.Assets)
            {
                _assets.Add(new AssetData { source = asset.source, path = asset.path });
            }
        }

        if (_isPollingAvatarChanges)
        {
            _stopCurrentAvatarChangePoll = true;
        }
        if (_isPollingAvatarCreation)
        {
            _stopCurrentAvatarCreationPoll = true;
        }
        Teardown();
        CreateEntity();


        if (config.LoadUserFromCdn)
        {
            StartCoroutine(LoadCdnAvatar());
        }
        else
        {
            LoadLocalAvatar();
        }

        OvrAvatarLog.LogInfo($"Applied the following settings to Avatar \"{name}\" :\n{config}", logScope);
    }

    #endregion // SDK UI
}
