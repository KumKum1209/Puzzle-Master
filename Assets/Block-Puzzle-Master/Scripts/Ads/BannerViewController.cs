using System;
using System.Collections;
using System.Collections.Generic;
using GoogleMobileAds.Api;
using UnityEditor;
using UnityEngine;

public class BannerViewController : MonoBehaviour
{
    /// <summary>
    /// UI element activated when an ad is ready to show.
    /// </summary>
    public GameObject adLoadedStatus;

    // These ad units are configured to always serve test ads.
#if UNITY_ANDROID
    private const string _adUnitId = "ca-app-pub-3940256099942544/6300978111";
#elif UNITY_IPHONE
        private const string _adUnitId = "ca-app-pub-3940256099942544/2934735716";
#else
        private const string _adUnitId = "unused";
#endif

    private BannerView _bannerView;

    /// <summary>
    /// Creates a adaptive banner at top of the screen.
    /// </summary>
    public void CreateBannerView()
    {
        Debug.Log("Creating banner view.");

        // If we already have a banner, destroy the old one.
        if (_bannerView != null)
        {
            DestroyAd();
        }

        AdSize adaptiveSize =
            AdSize.GetCurrentOrientationAnchoredAdaptiveBannerAdSizeWithWidth(AdSize.FullWidth);

        _bannerView = new BannerView(_adUnitId, adaptiveSize, AdPosition.Bottom);

        // Listen to events the banner may raise.
        ListenToAdEvents();

        Debug.Log("Banner view created.");
    }

    /// <summary>
    /// Creates the banner view and loads a banner ad.
    /// </summary>
    public void LoadAd()
    {
        // Create an instance of a banner view first.
        if (_bannerView == null)
        {
            CreateBannerView();
        }

        // Create our request used to load the ad.
        var adRequest = new AdRequest();

        // Send the request to load the ad.
        Debug.Log("Loading banner ad.");
        _bannerView.LoadAd(adRequest);
        HideAd();
    }

    /// <summary>
    /// Shows the ad.
    /// </summary>
    public void ShowAd()
    {
        if (_bannerView != null)
        {
            Debug.Log("Showing banner view.");
            _bannerView.Show();
        }
    }

    /// <summary>
    /// Hides the ad.
    /// </summary>
    public void HideAd()
    {
        if (_bannerView != null)
        {
            Debug.Log("Hiding banner view.");
            _bannerView.Hide();
        }
    }

    /// <summary>
    /// Destroys the ad.
    /// When you are finished with a BannerView, make sure to call
    /// the Destroy() method before dropping your reference to it.
    /// </summary>
    public void DestroyAd()
    {
        if (_bannerView != null)
        {
            Debug.Log("Destroying banner view.");
            _bannerView.Destroy();
            _bannerView = null;
        }

        // Inform the UI that the ad is not ready.
        ShowAdLoadedStatus(false);
    }

    /// <summary>
    /// Logs the ResponseInfo.
    /// </summary>
    public void LogResponseInfo()
    {
        if (_bannerView != null)
        {
            var responseInfo = _bannerView.GetResponseInfo();
            if (responseInfo != null)
            {
                UnityEngine.Debug.Log(responseInfo);
            }
        }
    }

    /// <summary>
    /// Listen to events the banner may raise.
    /// </summary>
    private void ListenToAdEvents()
    {
        // Raised when an ad is loaded into the banner view.
        _bannerView.OnBannerAdLoaded += () =>
        {
            Debug.Log("Banner view loaded an ad with response : "
                + _bannerView.GetResponseInfo());

            // Inform the UI that the ad is ready.
            ShowAdLoadedStatus(true);
        };
        // Raised when an ad fails to load into the banner view.
        _bannerView.OnBannerAdLoadFailed += (LoadAdError error) =>
        {
            Debug.LogError("Banner view failed to load an ad with error : " + error);

            LoadAd();
        };
        // Raised when the ad is estimated to have earned money.
        _bannerView.OnAdPaid += (AdValue adValue) =>
        {
            Debug.Log(String.Format("Banner view paid {0} {1}.",
                adValue.Value,
                adValue.CurrencyCode));
        };
        // Raised when an impression is recorded for an ad.
        _bannerView.OnAdImpressionRecorded += () =>
        {
            Debug.Log("Banner view recorded an impression.");
        };
        // Raised when a click is recorded for an ad.
        _bannerView.OnAdClicked += () =>
        {
            Debug.Log("Banner view was clicked.");
        };
        // Raised when an ad opened full screen content.
        _bannerView.OnAdFullScreenContentOpened += () =>
        {
            Debug.Log("Banner view full screen content opened.");
        };
        // Raised when the ad closed full screen content.
        _bannerView.OnAdFullScreenContentClosed += () =>
        {
            Debug.Log("Banner view full screen content closed.");

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
    [CustomEditor(typeof(BannerViewController))]
    public class BannerViewController_Inspector : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            BannerViewController target = (BannerViewController)base.target;

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

            if (GUILayout.Button("Hide Ad"))
            {
                target.HideAd();
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
