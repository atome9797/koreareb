using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Data;

public class Item_PreError : MonoBehaviour
{
    public MainManager mainManager;

    public ViewErrorPopup viewErrorPopup;

    public bool isCheck;

    public PrepareData prepareData;

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
        //Debug.Log("Toggle Value: " + value);

        isCheck = value;
        
        if (!value)
        {
            viewErrorPopup.isAll = false;
            viewErrorPopup.btn_On.gameObject.SetActive(false);
        }
        else
        {
            // viewErrorPopup.btn_groupProcess.interactable = true;
            // viewErrorPopup.btn_regionSet.interactable = true;
        }

        
        viewErrorPopup.list_error.Find(i => i.no.text == no.text).toggle.isOn = value;

        DataRow[] rows = viewErrorPopup.PreErrorTable.Select("NO = '" + no.text.ToString() + "'");
        
        if (rows.Length > 0)
        {
            // 값 변경
            DataRow row = rows[0];
            //Debug.Log("row : "+ row["Idx"] + " , " + row["isSelect"].ToString());

            row["isSelectError"] = value.ToString();

            //Debug.Log("row2 : " + row["Idx"] + " , " + row["isSelect"].ToString());

            // 데이터 테이블 업데이트
            // viewPrepare.prepareTable.AcceptChanges();
        }

        viewErrorPopup.Check_SelectItem();
    }

}
