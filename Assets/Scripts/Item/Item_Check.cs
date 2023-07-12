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

                        
        //DataRow[] rows = viewExecute.resultTable.Select("NO = " + id);
        DataRow[] rows = viewExecute.resultTable.Select("data_no = '" + id+ "'");

        if (rows.Length > 0)
        {
            // 값 변경
            DataRow row = rows[0];
            
            //Debug.Log("row : "+ row["data_no"] + " , id : " + id + ", isSelect : " + row["isSelect"].ToString());
            
            row["isSelect"] = value.ToString();

            //Debug.Log("row2 : " + row["data_no"] + " , id : " + id + row["isSelect"].ToString());

            // 데이터 테이블 업데이트
            // viewPrepare.prepareTable.AcceptChanges();
        }

        viewExecute.Check_SelectItem();
    }


}
