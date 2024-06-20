using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UILoading : MonoBehaviour
{

    int loading;
    public Animator animator;
    private void OnEnable()
    {
        loading = 0;
        StartCoroutine(LoadingCoroutine());
    }

    private IEnumerator LoadingCoroutine()
    {
       
        while (loading < 1000)
        {
            loading += Random.Range(0, 4);
          
            yield return new WaitForEndOfFrame();
        }
        AdsManager.Instance.appOpenAdController.ShowAd();
        gameObject.SetActive(false);    
    }

}
