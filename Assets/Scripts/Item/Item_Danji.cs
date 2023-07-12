using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Data;

public class Item_Danji : MonoBehaviour
{
    public MainManager mainManager;

    public ViewDanji viewDanji;

    //public bool isCheck;

    //public Toggle toggle;
    //public Button btn_Select;

    public TextMeshProUGUI no;
    public TextMeshProUGUI check;
    public TextMeshProUGUI aptNum;
    public TextMeshProUGUI aptType;
    public TextMeshProUGUI aptName;
    public TextMeshProUGUI region;
    public TextMeshProUGUI supportCount;

    // 위험도
    public TextMeshProUGUI risk;

    // 세대수
    public TextMeshProUGUI houseUserCount;

    // 모집공고일
    public TextMeshProUGUI NoticeDate;

    // 당첨자 발표일
    public TextMeshProUGUI winAnnoDate;

    // 전입제한
    public TextMeshProUGUI limittransferDate;

    // 분양가 상한제 여부 
    public TextMeshProUGUI salepriceStatus;

}
