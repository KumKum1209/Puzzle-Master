using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Vote : MonoBehaviour
{
    public List<GameObject> liststar = new List<GameObject>();
    int star = 0;
    private void OnEnable()
    {
        star = PlayerPrefs.GetInt("StarRating", 0);
        UpdateStars(star);
    }

    public void btnStar(int index)
    {

        star = index + 1;    
        UpdateStars(star);
    }

    private void UpdateStars(int index)
    {
 
        for (int i = 0; i < liststar.Count; i++)
        {
            liststar[i].SetActive(i < index);
        }
    }
    public void VoteRate()
    {
        if (InputManager.Instance.canInput())
        {
            PlayerPrefs.SetInt("StarRating", star);
            gameObject.Deactivate();
        }
            
    }
    public void OnCloseButtonPressed()
    {
        if (InputManager.Instance.canInput())
        {
            AudioManager.Instance.PlayButtonClickSound();
            gameObject.Deactivate();
        }
    }
}
