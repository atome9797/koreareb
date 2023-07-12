using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SortButton : MonoBehaviour
{
    [SerializeField]
    public int ID;
    public Image Img_SortMode;
    public int SortStatus = 0; // 0 기본, 1 오름차순(ASC) , 2 내림차순(DESC) 

    public string SortTarget;

}
