using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.ProceduralImage;

public class ManagerMapColor : MonoBehaviour
{
    public MainManager mainManager;

    public GameObject DarkTheme;
    public GameObject LightTheme;

    public List<MapColor> DarkColor;
    public List<MapColor> LightColor;

    public ProceduralImage[] ThemeDarkColor;
    public ProceduralImage[] ThemeLightColor;

    public Toggle[] Darktoggles;
    public Toggle[] Lighttoggles;

    public bool[] isColorNum;


    // Start is called before the first frame update
    void Start()
    {
        //for (int i = 0; i < Darktoggles.Length; i++)
        //{
        //    int toggleIndex = i; // 이벤트 핸들러에서 사용할 인덱스 저장

        //    Darktoggles[i].onValueChanged.AddListener(delegate { ToggleValueChanged(toggleIndex); });

        //    Lighttoggles[i].onValueChanged.AddListener(delegate { ToggleValueChanged(toggleIndex); });
        //}
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Load_ColorBox()
    {
        if (mainManager.thememode == MainManager.Thememode.dark)
        {
            DarkTheme.SetActive(true);
        }
        else
        {
            LightTheme.SetActive(true);
        }
    }

    void ToggleValueChanged(int toggleIndex)
    {
        Debug.Log("Toggle " + toggleIndex + " is on.");

        ChangeMapColor(toggleIndex);
    }

    public void ChangeMapColor(int num)
    {
        // Debug.Log("ChangeMapColor : " + num);

        if(mainManager.thememode == MainManager.Thememode.dark)
        {
            for (int i = 0; i < ThemeDarkColor.Length; i++)
            {
                ThemeDarkColor[i].color = DarkColor[num].colors[i];
            }
        }
        else
        {
            for (int i = 0; i < ThemeLightColor.Length; i++)
            {
                ThemeLightColor[i].color = LightColor[num].colors[i];
            }
        }
    }

    
}

[Serializable]
public class MapColor
{
    public Color[] colors;
}
