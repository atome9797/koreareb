using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Data;

public class Item_Member : MonoBehaviour
{
    public MainManager mainManager;
    public ViewAccountManager viewAccountManager;

    public Toggle toggle;

    public TextMeshProUGUI no;
    public TextMeshProUGUI errorCheck;

    public TextMeshProUGUI user_id;
    public TextMeshProUGUI user_name;
    public TextMeshProUGUI part;
    public TextMeshProUGUI position;
    public TextMeshProUGUI status;
    public TextMeshProUGUI authId;
    public TextMeshProUGUI createDate;

    private void Awake()
    {
        toggle.onValueChanged.AddListener(OnToggleValueChanged);   
    }

    private void OnToggleValueChanged(bool value)
    {
        

        if (!value)
        {
            viewAccountManager.isAll = false;
            viewAccountManager.btn_On.gameObject.SetActive(false);
        }
        else
        {
            viewAccountManager.passWordReset.interactable = true;
            viewAccountManager.accountLock.interactable = true;
        }

        DataRow[] rows = viewAccountManager.listTable.Select("NO = " + no.text.ToString());

        if (rows.Length > 0)
        {
            // 값 변경
            DataRow row = rows[0];

            if(value)
            {
                row["isSelect"] = 1;
            }
            else
            {
                row["isSelect"] = 0;
            }
            
        }

        viewAccountManager.Check_SelectItem();
    }


}
