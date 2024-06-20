using GoogleMobileAds.Api;
using GoogleMobileAds.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AdsManager : Singleton<AdsManager>
{
 
    public AppOpenAdController appOpenAdController;
    public RewardedAdController rewardedAdController;
    public void Start()
    {
        // Initialize the Google Mobile Ads SDK.
        MobileAds.Initialize(initStatus => {
            appOpenAdController.LoadAd();
            rewardedAdController.LoadAd();
        });
        
        AppStateEventNotifier.AppStateChanged += OnAppStateChanged;

        Debug.Log("test");

    }

    private void OnDestroy()
    {
        AppStateEventNotifier.AppStateChanged -= OnAppStateChanged;
    }


    private void OnAppStateChanged(AppState state)
    {
        Debug.Log("App State changed to : " + state);

        // If the app is Foregrounded and the ad is available, show it.
        if (state == AppState.Foreground)
        {
            appOpenAdController.ShowAd();
        }
    }

    
}
