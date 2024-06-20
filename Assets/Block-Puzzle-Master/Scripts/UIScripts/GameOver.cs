using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using Unity.VisualScripting;

public class GameOver : MonoBehaviour {

	[SerializeField] Text txtScore;
	[SerializeField] private Text txtBestScore;
	[SerializeField] private Text txtCoinReward;
	[SerializeField] private GameObject Newbest;
	[SerializeField] private GameObject confetti1;
	[SerializeField] private GameObject confetti2;


	public void SetLevelScore(int score, int coinReward)
	{
		int bestScore = PlayerPrefs.GetInt("BestScore_" + GameController.gameMode.ToString(), score);
		StartCoroutine(SetScore(score));
		if (score >= bestScore)
		{
			PlayerPrefs.SetInt("BestScore_" + GameController.gameMode.ToString(), score);
			bestScore = score;
			txtBestScore.transform.parent.gameObject.SetActive(false);
			Newbest.SetActive(true);
            Confetti(true);

        }
		else
		{
			Newbest.SetActive(false);
		}

		txtScore.text = string.Format("{0:#,#.}", score.ToString("0"));
		txtBestScore.text = string.Format("{0:#,#.}", bestScore.ToString("0"));
		txtCoinReward.text = string.Format("{0:#,#.}", coinReward.ToString("0"));

		CurrencyManager.Instance.AddCoinBalance(coinReward);
	}
	IEnumerator SetScore(int coinBalance)
	{
		int oldBalance = 0;
		int.TryParse(txtScore.text.Replace(",", ""), out oldBalance);

		int IterationSize = (coinBalance - oldBalance) / 50;

		for (int index = 1; index < 50; index++)
		{
			oldBalance += IterationSize;
			txtScore.text = string.Format("{0:#,#.}", oldBalance);
			yield return new WaitForEndOfFrame();
		}
		txtScore.text = string.Format("{0:#,#.}", coinBalance);
	}
	public void OnHomeButtonPressed()
	{
		if (InputManager.Instance.canInput()) {
			AudioManager.Instance.PlayButtonClickSound();
			StackManager.Instance.mainMenu.Activate();
			gameObject.Deactivate();
			Confetti(false);
		}
	}

	public void OnReplayButtonPressed()
	{
		if (InputManager.Instance.canInput()) {
			AudioManager.Instance.PlayButtonClickSound();
			StackManager.Instance.ActivateGamePlay();
			gameObject.Deactivate();
			Confetti(false);
		}
	}

	private void OnEnable()
	{
		//You can adjust ad show delay based on your requirement.
		ShowInterstitial();
	
	}
	void Confetti(bool result)
	{
        confetti1.SetActive(result);
        confetti2.SetActive(result);
    }
    void ShowInterstitial()
    {
        //if(AdManager.Instance.isAdsAllowed())
        //{
        //    AdManager.Instance.ShowInterstitial();
        //}
    }
}
