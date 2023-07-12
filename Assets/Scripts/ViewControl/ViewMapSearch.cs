using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SFB;
using System.IO;
using System.Text.RegularExpressions;
using ExcelDataReader;
using System.Text;
using System.Data;
using System.Linq;
using System;
using UnityEngine.UI.ProceduralImage;

public class ViewMapSearch : BaseView
{
    // 전처리 지역 검색

    public ServerManager serverManager;

    public ViewErrorPopup viewErrorPopup;
    
    public Transform contents;

    public Item_Region item_Region;
    public TMP_InputField Input_map;


    public TextMeshProUGUI Text_listCount;

    public Button btn_Search;

    public DataTable mapTable;

    public List<Item_Region> list_regions = new List<Item_Region>();

    public DataRow[] rows;

    // Start is called before the first frame update
    void Start()
    {
        btn_Search.onClick.AddListener(SearchMap);

        Input_map.onSubmit.AddListener(delegate { SearchMap(); });
    }

    public void Init()
    {
        if(Input_map.text.Trim().Length > 0)
        {
            SearchMap();
        }        
    }

    public void SearchMap()
    {
        if(Input_map.text.Trim().Length > 0)
        {
            Debug.Log("SearchMap" + Input_map.text);

            rows = serverManager.ds_map.Tables[0].Select("bjdong_name LIKE '%" + Input_map.text.Trim() + "%'");

            Debug.Log("rows : " + rows.Length);

            Text_listCount.text = "검색결과 : " + rows.Length + "건";

            ClearMapList();

            for (int i = 0; i < rows.Length; i++)
            {
                Item_Region item = Instantiate(item_Region, contents);

                item.mapname.text = rows[i]["bjdong_name"].ToString();
                item.mapcode = rows[i]["bjdong_code"].ToString();

                item.btn_select.onClick.AddListener(() =>
                {
                    SelectItem(item.mapname.text, item.mapcode, i);
                });

                list_regions.Add(item);
            }
        }
    }

    public void SelectItem(string _mapname, string _mapcode, int i)
    {
        Debug.Log("SelectItem :  " + i + ", "  + _mapname + ", " + _mapcode);

        viewErrorPopup.input_region.text = _mapname;

        viewErrorPopup.selectRegion = _mapname;
        viewErrorPopup.selectRegionCode = _mapcode;

        viewErrorPopup.btn_regionSet.interactable = true;

        this.Close();
    }

    public void ClearMapList()
    {
        var children = contents.transform.GetComponentsInChildren<Item_Region>(true);

        foreach (var child in children)
        {
            //if (child == contents) continue;
            //child.parent = null;

            Destroy(child.gameObject);
        }

        list_regions.Clear();        
    }

    public void SetMapList()
    {

    }

}
