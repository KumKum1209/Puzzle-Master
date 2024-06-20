using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;
#if UNITY_PURCHASING
using UnityEngine.Purchasing;
#endif

public class PurchaseRewardsManager : Singleton<PurchaseRewardsManager> 
{

	public void ProcessRewardForProduct(Product product)
	{
		switch (product.definition.id) {
		case "1" :
			CurrencyManager.Instance.AddCoinBalance (500);
			break;
		case "2":
			CurrencyManager.Instance.AddCoinBalance (1650);
			break;
		case "3":
			CurrencyManager.Instance.AddCoinBalance (3000);
			break;
		case "4":
			CurrencyManager.Instance.AddCoinBalance (6250);
			break;
		}
	}
	
}
