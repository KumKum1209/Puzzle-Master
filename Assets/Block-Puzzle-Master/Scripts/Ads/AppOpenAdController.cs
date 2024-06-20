using System;
using System.Collections;
using System.Collections.Generic;
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;
using UnityEditor;
using UnityEngine;

public class AppOpenAdController : MonoBehaviour
{
    public static event Action OnCloseAdEvent;

    /// <summary>
    /// UI element activated when an ad is ready to show.
    /// </summary>
    public GameObject adLoadedStatus;

    // These ad units are configured to always serve test ads.
#if UNITY_ANDROID
    private const string _adUnitId = "ca-app-pub-3940256099942544/9257395921";
#elif UNITY_IPHONE
        private const string _adUnitId = "ca-app-pub-3940256099942544/5575463023";
#else
        private const string _adUnitId = "unused";
#endif

    // App open ads can be preloaded for up to 4 hours.
    private readonly TimeSpan TIMEOUT = TimeSpan.FromHours(4);
    private DateTime _expireTime;
    private AppOpenAd _appOpenAd;

    private bool _isShowingAd = false;

    public bool IsAdAvailable
    {
        get
        {
            return _appOpenAd != null && _appOpenAd.CanShowAd() && DateTime.Now < _expireTime;
        }
    }

    /// <summary>
    /// Loads the ad.
    /// </summary>
    public void LoadAd()
    {
        // Clean up the old ad before loading a new one.
        if (_appOpenAd != null)
        {
            DestroyAd();
        }

        Debug.Log("Loading app open ad.");

        // Create our request used to load the ad.
        var adRequest = new AdRequest();

        // Send the request to load the ad.
        AppOpenAd.Load(_adUnitId, adRequest, (AppOpenAd ad, LoadAdError error) =>
        {
            // If the operation failed with a reason.
            if (error != null)
            {
                Debug.LogError("App open ad failed to load an ad with error : "
                                + error);
                return;
            }

            // If the operation failed for unknown reasons.
            // This is an unexpected error, please report this bug if it happens.
            if (ad == null)
            {
                Debug.LogError("Unexpected error: App open ad load event fired with " +
                                " null ad and null error.");
                return;
            }

            // The operation completed successfully.
            Debug.Log("App open ad loaded with response : " + ad.GetResponseInfo());
            _appOpenAd = ad;

            // App open ads can be preloaded for up to 4 hours.
            _expireTime = DateTime.Now + TIMEOUT;

            // Register to ad events to extend functionality.
            RegisterEventHandlers(ad);

            // Inform the UI that the ad is ready.
            ShowAdLoadedStatus(true);
        });
    }

    /// <summary>
    /// Shows the ad.
    /// </summary>
    public void ShowAd()
    {
        if (_isShowingAd)
        {
            return;
        }

        // App open ads can be preloaded for up to 4 hours.
        if (IsAdAvailable)
        {
            Debug.Log("Showing app open ad.");
            _appOpenAd.Show();
        }
        else
        {
            Debug.LogError("App open ad is not ready yet.");
            LoadAd();
        }

        // Inform the UI that the ad is not ready.
        ShowAdLoadedStatus(false);
    }

    /// <summary>
    /// Destroys the ad.
    /// </summary>
    public void DestroyAd()
    {
        if (_appOpenAd != null)
        {
            Debug.Log("Destroying app open ad.");
            _appOpenAd.Destroy();
            _appOpenAd = null;
        }

        // Inform the UI that the ad is not ready.
        ShowAdLoadedStatus(false);
    }

    /// <summary>
    /// Logs the ResponseInfo.
    /// </summary>
    public void LogResponseInfo()
    {
        if (_appOpenAd != null)
        {
            var responseInfo = _appOpenAd.GetResponseInfo();
            UnityEngine.Debug.Log(responseInfo);
        }
    }

    private void RegisterEventHandlers(AppOpenAd ad)
    {
        // Raised when the ad is estimated to have earned money.
        ad.OnAdPaid += (AdValue adValue) =>
        {
            Debug.Log(String.Format("App open ad paid {0} {1}.",
                adValue.Value,
                adValue.CurrencyCode));
        };
        // Raised when an impression is recorded for an ad.
        ad.OnAdImpressionRecorded += () =>
        {
            Debug.Log("App open ad recorded an impression.");
        };
        // Raised when a click is recorded for an ad.
        ad.OnAdClicked += () =>
        {
            Debug.Log("App open ad was clicked.");
        };
        // Raised when an ad opened full screen content.
        ad.OnAdFullScreenContentOpened += () =>
        {
            Debug.Log("App open ad full screen content opened.");
            _isShowingAd = true;

            // Inform the UI that the ad is consumed and not ready.
            ShowAdLoadedStatus(false);
        };
        // Raised when the ad closed full screen content.
        ad.OnAdFullScreenContentClosed += () =>
        {
            Debug.Log("App open ad full screen content closed.");
            _isShowingAd = false;
            OnCloseAdEvent?.Invoke();

            // It may be useful to load a new ad when the current one is complete.
            LoadAd();
        };
        // Raised when the ad failed to open full screen content.
        ad.OnAdFullScreenContentFailed += (AdError error) =>
        {
            Debug.LogError("App open ad failed to open full screen content with error : "
                            + error);
            _isShowingAd = false;

            // It may be useful to load a new ad when the current one is complete.
            LoadAd();
        };
    }

    public void ShowAdLoadedStatus(bool show = true)
    {
        if (adLoadedStatus != null)
        {
            adLoadedStatus.SetActive(show);
        }
    }


#if UNITY_EDITOR
    [CustomEditor(typeof(AppOpenAdController))]
    public class AppOpenAdController_Inspector : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            AppOpenAdController target = (AppOpenAdController)base.target;

            GUILayout.Space(20f);

            GUIStyle labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold
            };

            GUILayout.Label("Quick Access", labelStyle);

            if (GUILayout.Button("Load Ad"))
            {
                target.LoadAd();
            }

            if (GUILayout.Button("Show Ad"))
            {
                target.ShowAd();
            }

            if (GUILayout.Button("Destroy Ad"))
            {
                target.DestroyAd();
            }

            if (GUILayout.Button("Print ResponseInfo"))
            {
                target.LogResponseInfo();
            }
        }
    }
#endif
}
