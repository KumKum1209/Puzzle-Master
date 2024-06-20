using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Rescue : MonoBehaviour 
{
	[SerializeField]
	private Button btnWatchVideo;

	[SerializeField]
	private Text txtRescueReason;


	void OnEnable() 
	{
        //AdManager.OnRewardedFinishedEvent += Instance_OnRewardedFinished;

		switch (GamePlayUI.Instance.currentGameOverReason) {
		case GameOverReason.OUT_OF_MOVES:
			txtRescueReason.SetLocalizedTextForTag ("txt-out-moves");
			break;
		case GameOverReason.BOMB_COUNTER_ZERO:
			txtRescueReason.SetLocalizedTextForTag ("txt-bomb-blast");
			break;
		case GameOverReason.TIME_OVER:
			txtRescueReason.SetLocalizedTextForTag ("txt-time-over");
			break;
		}


		if(btnWatchVideo != null)
		{
			bool isAdsAvailable = false;

			if (GameController.Instance.isInternetAvailable())
				isAdsAvailable = true;

			if (isAdsAvailable &&  GamePlay.Instance.isFreeRescueAvailable())
			{
				btnWatchVideo.interactable = true;
				btnWatchVideo.GetComponent<CanvasGroup>().alpha = 1F;
			} else {
				btnWatchVideo.interactable = false;
				btnWatchVideo.GetComponent<CanvasGroup>().alpha = 0.3F;
			}
		}
    }

 

 //   void OnDisable() {
	//	AdsManager.Instance.appOpenAdController -= Instance_OnRewardedFinished;
	//}

    public void OnRewardedFinished()
    {
		GamePlay.Instance.OnRescueDone(true);
		gameObject.Deactivate();
	}

    public void OnCloseButtonPressed()
	{
		if (InputManager.Instance.canInput ()) {
			AudioManager.Instance.PlayButtonClickSound ();
			GamePlay.Instance.OnGameOver();
			gameObject.Deactivate();
		}
	}

	public void OnRescueUsingWatchVideo()
	{
        //if (InputManager.Instance.canInput())
        //{
			if (AdsManager.Instance.rewardedAdController._rewardedAd.CanShowAd())
			{
				AdsManager.Instance.rewardedAdController.ShowAd(null,OnRewardedFinished,null);
			}
			else
			{
				Debug.Log("Rewarded ad is not available.");
			}
		//}
    }

	public void OnRescueUsingCoins()
	{
		if (InputManager.Instance.canInput ()) {
			bool coinDeduced = CurrencyManager.Instance.deductBalance (200);

			if (coinDeduced) {
				GamePlay.Instance.OnRescueDone (false);
				gameObject.Deactivate();
			} else {
				StackManager.Instance.shopScreen.Activate();
			}
		}
	}
}
