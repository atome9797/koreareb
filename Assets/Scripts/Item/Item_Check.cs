using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Data;

public class Item_Check : MonoBehaviour
{
    public MainManager mainManager;

    public ViewExecute viewExecute;

    public bool isCheck;

    public Toggle toggle;
    public Button btn_Select;

    public string id;


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

    public void Init()
    {
        toggle.onValueChanged.AddListener(OnToggleValueChanged);
    }


    private void OnToggleValueChanged(bool value)
    {
        // Debug.Log("Toggle Value: " + value);
        //if (errorCheck.text == viewPrepare.dropdown_error.options[4].text)
        //{
        //    return;
        //} 

        isCheck = value;

        if (!value)
        {
            viewExecute.isAll = false;
            viewExecute.btn_On.gameObject.SetActive(false);
        }
        else
        {
            viewExecute.btn_checkProcess.interactable = true;
            //viewExecute.btn_checkResultProcess.interactable = true;
        }


        // data_no → 행 사전으로 바로 찾는다.
        // 이전: resultTable.Select("data_no = '..'") 로 클릭마다 전체 행을 검색 (전체선택 시 페이지 행 수 × 전체 행 수)
        viewExecute.SetRowSelected(id, value);

        viewExecute.Check_SelectItem();
    }


}
