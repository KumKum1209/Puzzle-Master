using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ComboAnim : MonoBehaviour
{
    public List<GameObject> listnumber;
    public List<GameObject> listtext;
    private void OnEnable()
    {
        foreach (var item in listnumber) { item.SetActive(false); }
        foreach (var item in listtext) { item.SetActive(false); }
    }
    public void ShowCombo(int index, bool unbelieve)
    {
        if(index >= listnumber.Count)
            listnumber[listnumber.Count - 1].SetActive(true);
        else
            listnumber[index - 1].SetActive(true);
        if (unbelieve)
        {
            Unbelieveable();
        }
        else
        {
            int indextext = Random.Range(0, listtext.Count - 1);
            listtext[indextext].SetActive(true);
        }
       

        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0;
        transform.position = mousePos;   

    }
    public void HideCombo()
    {
        gameObject.SetActive(false);
    }
    public void Unbelieveable()
    {
        listtext[listtext.Count - 1].SetActive(true);
    }
}
