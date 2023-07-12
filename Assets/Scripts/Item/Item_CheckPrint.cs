using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Data;

public class Item_CheckPrint : BaseView
{
    public Item_Check item_Check;

    public TextMeshProUGUI no;
    public TextMeshProUGUI check;
    public TextMeshProUGUI checkResult;
    public TextMeshProUGUI risk;
    public TextMeshProUGUI aptNum;
    public TextMeshProUGUI aptType;
    public TextMeshProUGUI aptName;
    public TextMeshProUGUI aptDong;
    public TextMeshProUGUI aptHosu;
    public TextMeshProUGUI userName;
    public TextMeshProUGUI userBirth;
    public TextMeshProUGUI userAge;
    // 공급유형
    public TextMeshProUGUI supType;
    // 세부유형
    public TextMeshProUGUI detailType;
    // 세대주관계
    public TextMeshProUGUI relation;
    // 변경일자
    public TextMeshProUGUI userchangeDate;
    // 배우자
    public TextMeshProUGUI userspouse;
    // 세대원수
    public TextMeshProUGUI housemember;
    // 분리세대
    public TextMeshProUGUI hp_housememeber;
    // 폰중복
    public TextMeshProUGUI phonedup;
    // ip중복당첨
    public TextMeshProUGUI ipdupwin;
    // ip중복신청
    public TextMeshProUGUI ipdupapp;
    // 거주구분_신청
    public TextMeshProUGUI appclass;
    // 거주구분_당첨
    public TextMeshProUGUI winclass;
    // 주소일치
    public TextMeshProUGUI addressmatch;
    // 등본주소 - 행정주소
    public TextMeshProUGUI adminaddress;
     // 입력주소 - 신청주소+상세주소
    public TextMeshProUGUI inputaddress;
    // 기관추천
    public TextMeshProUGUI recomType;
    // 업로드일시 (조회일시)
    public TextMeshProUGUI uploadDate;

    // Start is called before the first frame update
    void Start()
    {
        //Init();
    }

    public void Init()
    {
        no.text = item_Check.no.text;
        check.text = item_Check.check.text;

        checkResult.text = item_Check.checkResult.text;
        risk.text = item_Check.risk.text;
        aptNum.text = item_Check.aptNum.text;
        aptType.text = item_Check.aptType.text;

        aptName.text = item_Check.aptName.text;
        aptDong.text = item_Check.aptDong.text;
        aptHosu.text = item_Check.aptHosu.text;
        userName.text = item_Check.userName.text;
        userBirth.text = item_Check.userBirth.text;
        userAge.text = item_Check.userAge.text;
        
        supType.text = item_Check.supType.text;

        detailType.text = item_Check.detailType.text;

        relation.text = item_Check.relation.text;

        userchangeDate.text = item_Check.userchangeDate.text;

        userspouse.text = item_Check.userspouse.text;

        housemember.text = item_Check.housemember.text;

        hp_housememeber.text = item_Check.hp_housememeber.text;

        phonedup.text = item_Check.phonedup.text;

        ipdupwin.text = item_Check.ipdupwin.text;

        ipdupapp.text = item_Check.ipdupapp.text;

        appclass.text = item_Check.appclass.text;

        winclass.text = item_Check.winclass.text;

        addressmatch.text = item_Check.addressmatch.text;

        adminaddress.text = item_Check.adminaddress.text;

        inputaddress.text = item_Check.inputaddress.text;

        recomType.text = item_Check.recomType.text;

        uploadDate.text = item_Check.uploadDate.text;

    }

}
