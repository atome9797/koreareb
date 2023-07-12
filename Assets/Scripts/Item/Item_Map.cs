using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Item_Map : MonoBehaviour
{
    public TMP_Text[] map;
    public TMP_Text[] mapName;
    public Image[] map_bg;

    public void init()
    {
        int count = gameObject.transform.GetChild(2).childCount;

        for(int i = 0; i < count; i++)
        {
            mapName[i] = gameObject.transform.GetChild(2).GetChild(i).GetChild(0).GetComponent<TMP_Text>();
            map[i] = gameObject.transform.GetChild(2).GetChild(i).GetChild(1).GetComponent<TMP_Text>();
            map[i].text = "0";
        }    
    }
}
