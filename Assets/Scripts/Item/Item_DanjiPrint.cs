using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Data;

public class Item_DanjiPrint : BaseView
{
    public Item_Danji item_Danji;

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

    // Start is called before the first frame update
    void Start()
    {
        Init();
    }

    public void Init()
    {
        no.text = item_Danji.no.text;
        check.text = item_Danji.check.text;
        aptNum.text = item_Danji.aptNum.text;
        aptType.text = item_Danji.aptType.text;
        aptName.text = item_Danji.aptName.text;
        region.text = item_Danji.region.text;
        supportCount.text = item_Danji.supportCount.text;
        risk.text = item_Danji.risk.text;
        houseUserCount.text = item_Danji.houseUserCount.text;
        NoticeDate.text = item_Danji.NoticeDate.text;
        winAnnoDate.text = item_Danji.winAnnoDate.text;
        limittransferDate.text = item_Danji.limittransferDate.text;
        salepriceStatus.text = item_Danji.salepriceStatus.text;
    }
}
