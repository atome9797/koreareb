using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Data;

public class Item_Prepare : MonoBehaviour
{
    public MainManager mainManager;

    public ViewPrepare viewPrepare;

    public bool isCheck;

    public Toggle toggle;
    public Button btn_Select;
    public string id;

    public TextMeshProUGUI no;
    public TextMeshProUGUI errorCheck;

    public TextMeshProUGUI aptNum;
    public TextMeshProUGUI aptName;
    public TextMeshProUGUI aptDong;
    public TextMeshProUGUI aptHosu;
    public TextMeshProUGUI userName;
    public TextMeshProUGUI region;
    public TextMeshProUGUI relation;
    public TextMeshProUGUI aptdate;

    public void Init()
    {
        toggle.onValueChanged.AddListener(OnToggleValueChanged);
    }

    private void OnToggleValueChanged(bool value)
    {
        // Debug.Log("Toggle Value: " + value);
        if(errorCheck.text == viewPrepare.dropdown_error.options[4].text)
        {
            return;
        }

        isCheck = value;
        
        if (!value)
        {
            viewPrepare.isAll = false;
            viewPrepare.btn_On.gameObject.SetActive(false);
        }
        else
        {
            viewPrepare.btn_Except.interactable = true;
            viewPrepare.btn_ErrorCheck.interactable = true;
        }

        DataRow[] rows = viewPrepare.prepareTable.Select("NO = " + no.text);

        if (rows.Length > 0)
        {
            // 값 변경
            DataRow row = rows[0];
            //Debug.Log("row : "+ row["Idx"] + " , " + row["isSelect"].ToString());

            row["isSelect"] = value.ToString();

            //Debug.Log("row2 : " + row["Idx"] + " , " + row["isSelect"].ToString());

            // 데이터 테이블 업데이트
            // viewPrepare.prepareTable.AcceptChanges();
        }

        viewPrepare.Check_SelectItem();
    }

}
