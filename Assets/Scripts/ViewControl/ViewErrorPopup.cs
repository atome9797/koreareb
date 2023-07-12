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

public class ViewErrorPopup : BaseView
{
    public MainManager mainManager;

    public ViewPrepare viewPrepare;
    public ServerManager serverManager;

    public ViewMapSearch viewMapSearch;

    public Transform contents;

    public Transform parent;

    public RectTransform[] viewupdate;

    public GameObject Box_Relation;
    public GameObject Box_Region;

    public DataTable listTable;
    public DataTable PreErrorTable;

    [SerializeField]
    public List<Item_PreError> list_error = new List<Item_PreError>();

    [SerializeField]
    public List<Toggle> list_Toggles = new List<Toggle>();

    public Item_PreError item_PreError;

    public GameObject item_Empty;

    public TMP_Dropdown dropdown_count;

    // 세대주 선택
    public string[] listrelations = { "","부모","본인","배우자","자녀","친형제","손자","조부모","친척","기타" };

    // 세대주 리스트
    public TMP_Dropdown dropdown_relation;

    // 지역명 등록
    public TMP_InputField input_region;

    // 지역명 검색
    public Button btn_search_region;

    // 그룹명 처리
    public Button btn_groupProcess;

    // 지역명 등록
    public Button btn_regionSet;

    // 전체선택
    public Button btn_AllSelect;  
    public Image btn_On;

    public bool isSelect = false;
    public bool isAll = false;

    public bool isErrorRelation;
    public bool isErrorRegion;

    public int[] listCounts = { 10, 20, 50, 100 };
    public int listCount;

    // 미처리 
    public int unSolvedCount = 0;

    // 수동처리
    public int manualCount = 0;

    public string selectRelation;

    public string selectRegion;

    public string selectRegionCode;

    public List<FamData> errorFamily;

    // 미처리 텍스트
    public TextMeshProUGUI Text_unsolvedCount;

    // 에러 처리 완료
    public Button Btn_Submit;
    public Button Btn_Cancel;

    // 전처리 세대주관계 에러
    public int errorRelationCount;
    // 전처리 지역 에러
    public int errorRegionCount;

    // 정렬 버튼 
    public SortButton[] sortButtons;

    public Sprite Img_ASC; // 오름차순 이미지
    public Sprite Img_DESC; // 내림 차순 이미지

    public string SortTarget; // 정렬 기준

    int intValue = 0;
    double doubleValue = 0;

    // 페이지

    // 페이지별 아이템 수
    public int itemsPerPage = 10;
    public List<GameObject> itemUIList; // 아이템을 표시할 UI 요소 리스트

    public Button prevButton; // 이전 10 페이지로 이동하는 버튼
    public Button nextButton; // 다음 10 페이지로 이동하는 버튼

    public List<Button> pageButtons; // 페이지 번호를 나타내는 버튼 리스트

    public Button firstPageButton; // 첫 페이지로 이동하는 버튼
    public Button lastPageButton; // 마지막 페이지로 이동하는 버튼

    public int currentPage = 1; // 현재 페이지
    public RectTransform pageContents;
    private bool isSelectError;

    private void Start()
    {
        Debug.Log("[ViewErrorPopup][Start]");

        prevButton.onClick.AddListener(Previous10Pages);
        nextButton.onClick.AddListener(Next10Pages);
        firstPageButton.onClick.AddListener(GoToFirstPage);
        lastPageButton.onClick.AddListener(GoToLastPage);

        for (int i = 0; i < pageButtons.Count; i++)
        {
            int pageNumber = ((currentPage - 1) / 10) * 10 + i + 1;
            pageButtons[i].onClick.AddListener(() => GoToPage(pageNumber));
        }

        input_region.onSubmit.AddListener(delegate { CheckInputRegion(input_region); });

        btn_search_region.onClick.AddListener(delegate { CheckInputRegion(input_region); });

        // 그룹명 처리
        btn_groupProcess.onClick.AddListener(GroupProcess);

        // 지역명 등록
        btn_regionSet.onClick.AddListener(RegionSet);

        // 전체 선택
        btn_AllSelect.onClick.AddListener(All_Select);

        // 에러 처리 완료
        Btn_Submit.onClick.AddListener(Submit);

        // 에러 처리 취소
        Btn_Cancel.onClick.AddListener(ClosePopup);

        // Init();
    }

    public void Update()
    {
        if(Input.GetKeyDown(KeyCode.W))        
        {
            UnSelect();
        }

    }

    public void Init()
    {
        currentPage = 1;

        selectRelation = "";
        selectRegion = "";
        selectRegionCode = "";

        dropdown_count.SetValueWithoutNotify(viewPrepare.dropdown_count.value);

        listCount = listCounts[dropdown_count.value] ;        
        itemsPerPage = listCount;

        btn_On.gameObject.SetActive(false);
        isAll = false;

        btn_groupProcess.interactable = false;
        btn_regionSet.interactable = false;
        Btn_Submit.interactable = false;

        input_region.text = "";
        dropdown_relation.value = 0;

        errorFamily.Clear();

        errorFamily = viewPrepare.Load();

        CheckError();

        /*PreErrorTable = null;

        PreErrorTable = new DataTable();

        PreErrorTable = viewPrepare.prepareTable.Copy();
*/
        //foreach (DataRow item in PreErrorTable.Rows)
        //{
        //    Debug.Log("ROWS Error : " + item["Error"].ToString());
        //}

        if (isErrorRelation)
        {
            Box_Relation.SetActive(true);
        }
        else
        {
            Box_Relation.SetActive(false);
        }

        if (isErrorRegion)
        {
            Box_Region.SetActive(true);
        }
        else
        {
            Box_Region.SetActive(false);
        }

        SetListView();
    }

    // 정렬 버튼
    public void Reset_Sort(SortButton sortButton)
    {
        // 정렬 초기화
        for (int i = 0; i < sortButtons.Length; i++)
        {
            if (sortButtons[i] != sortButton)
            {
                // sortButtons[i].Img_SortMode.gameObject.SetActive(false);
                // sortButtons[i].Img_SortMode.sprite = Img_DESC;

                sortButtons[i].SortStatus = 0;
            }
        }
    }

    public void Clear_Sort()
    {
        // NO로 정렬 초기화
        for (int i = 0; i < sortButtons.Length; i++)
        {
            // sortButtons[i].Img_SortMode.gameObject.SetActive(false);
            // sortButtons[i].Img_SortMode.sprite = Img_DESC;

            sortButtons[i].SortStatus = 0;
        }

        sortButtons[0].SortStatus = 1;

        SortTarget = "Error ASC";
    }

    public void Set_Sort(SortButton sortButton)
    {
        Reset_Sort(sortButton);

        if (sortButton.SortStatus == 0)
        {
            // 오름 차순으로
            // sortButton.Img_SortMode.gameObject.SetActive(true);

            // sortButton.Img_SortMode.sprite = Img_ASC;

            sortButton.SortStatus = 1;

            SortTarget = sortButton.SortTarget + " ASC";
        }
        else if (sortButton.SortStatus == 1)
        {
            // 내림 차순으로

            // sortButton.Img_SortMode.gameObject.SetActive(true);

            // sortButton.Img_SortMode.sprite = Img_DESC;

            sortButton.SortStatus = 2;

            SortTarget = sortButton.SortTarget + " DESC";
        }
        else
        {
            // 2일땐

            // 오름 차순으로
            // sortButton.Img_SortMode.gameObject.SetActive(true);

            // sortButton.Img_SortMode.sprite = Img_ASC;
            sortButton.SortStatus = 1;

            SortTarget = sortButton.SortTarget + " ASC";
        }

        // 기본으로
        //sortButton.Img_SortMode.gameObject.SetActive(false);

        //sortButton.SortStatus = 0;

        //SortTarget = "NO ASC";
        // sortButton.Img_SortMode.sprite = Img_ASC;

        if (PreErrorTable != null)
        {
            UpdateUI();
        }

    }

    public void GroupProcess()
    {
        Debug.Log("GroupProcess Start" );

        btn_groupProcess.interactable = false;

        if (selectRelation.Length > 0)
        {

            CommonPopup commonPopup = Instantiate(mainManager.commonPopup, mainManager.contents);

            commonPopup.ShowPopup_TwoButton(
            "선택하신 항목으로 그룹핑 하시겠습니까?",
            "아니오",
            "네",
            () =>
            {
                // 아니오 삭제
                commonPopup.CloseDestroy();

                btn_groupProcess.interactable = true;
            },

            () =>
            {
                CheckError();

                DataRow[] selectList = PreErrorTable.Select("isSelectError = true");

                //Debug.Log("SelectList : " + selectList.Length);

                foreach(DataRow row in selectList)
                {                    
                    //Debug.Log("region_status : " + row["region_status"].ToString());
                    //Debug.Log("relation_status : " + row["relation_status"].ToString());
                    //Debug.Log("NO : " + row["NO"].ToString());

                    // 선택한 리스트에서 세대주 관계가 에러면 변경 
                    if (row["relation_status"].ToString() == "1")
                    {
                        string memberName = row["세대주관계"].ToString();
                        //Debug.Log("멤버 네임 : " + memberName + "은" + selectRelation + "으로 분류됩니다.");
                        AddMemberToFamily(selectRelation, memberName);

                        row["세대주관계"] = selectRelation;
                        row["relation_status"] = 0;
                    }
                }

                // 네
                //var selectList = list_error.FindAll(i => i.toggle.isOn == true);

                /*                
                foreach (Item_PreError item in selectList)
                {
                    // Debug.Log("item name : " + item.userName.text);
                    // Debug.Log("dropdown_relation.captionText " + dropdown_relation.captionText.text);

                    // 선택한 리스트에서 세대주 관계가 에러면 변경 
                    if (item.relation.gameObject.transform.parent.GetComponent<ProceduralImage>().enabled)
                    {
                        DataRow row = PreErrorTable.Select("NO = " + item.no.text.ToString()).FirstOrDefault();

                        
                        if (row != null)
                        {
                            string memberName = row["세대주관계"].ToString();
                            Debug.Log("멤버 네임 : " + memberName + "은" + selectRelation + "으로 분류됩니다.");

                            AddMemberToFamily(selectRelation, memberName);

                            //row["세대주관계"] = dropdown_relation.captionText.text;
                            row["세대주관계"] = selectRelation;
                            item.relation.text = selectRelation;

                            item.relation.gameObject.transform.parent.GetComponent<ProceduralImage>().enabled = false;

                            row["relation_status"] = 0;

                            Debug.Log("region_status : " + row["region_status"].ToString());
                            Debug.Log("relation_status : " + row["relation_status"].ToString());

                            if (row["region_status"].ToString() == "0")
                            {
                                item.btn_Select.targetGraphic.enabled = false;

                                row["Error"] = "2";

                                item.errorCheck.text = viewPrepare.dropdown_error.options[2].text;
                            }
                            else
                            {
                                item.btn_Select.targetGraphic.enabled = true;
                            }
                        }
                    }
                }
                */

                // UnSelect();
                CheckError();

                getCount();

                PreErrorTable.AcceptChanges();

                UpdateView();

                btn_groupProcess.interactable = true;

                commonPopup.CloseDestroy();
            });
        }
    }

    public void RegionSet()
    {
        btn_regionSet.interactable = false;

        if (selectRegionCode.Length > 0)
        {
            CommonPopup commonPopup = Instantiate(mainManager.commonPopup, mainManager.contents);

            commonPopup.ShowPopup_TwoButton(
            "지역명 등록을 완료하시겠습니까?",
            "아니오",
            "네",
            () =>
            {
                // 아니오 삭제
                commonPopup.CloseDestroy();

                btn_regionSet.interactable = true;

            },

            () =>
            {                
                CheckError();

                DataRow[] selectList = PreErrorTable.Select("isSelectError = true");
                //Debug.Log("SelectList : " + selectList.Length);

                foreach (DataRow row in selectList)
                {
                    //Debug.Log("region_status : " + row["region_status"].ToString());
                    //Debug.Log("relation_status : " + row["relation_status"].ToString());
                    //Debug.Log("NO : " + row["NO"].ToString());

                    // 선택한 리스트에서 지역코드가 에러면 변경 
                    if (row["region_status"].ToString() == "1")
                    {
                        row["공급위치_시군구코드"] = selectRegionCode;
                        row["region_status"] = 0;
                    }
                }

                /*
                // 네
                var selectList = list_error.FindAll(i => i.toggle.isOn == true);

                foreach (Item_PreError item in selectList)
                {
                    Debug.Log("selectRegionCode : " + selectRegionCode + ", selectRegion : " + selectRegion );

                    // 선택한 리스트에서 
                    if (item.region.gameObject.transform.parent.GetComponent<ProceduralImage>().enabled)
                    {
                        DataRow row = PreErrorTable.Select("NO = " + item.no.text.ToString()).FirstOrDefault();

                        if (row != null)
                        {
                            row["공급위치_시군구코드"] = selectRegionCode;
                            item.region.text = viewPrepare.getMapName(selectRegionCode);

                            item.region.gameObject.transform.parent.GetComponent<ProceduralImage>().enabled = false;

                            row["region_status"] = 0;

                            Debug.Log("region_status : " + row["region_status"].ToString());
                            Debug.Log("relation_status : " + row["relation_status"].ToString());

                            if (row["relation_status"].ToString() == "0")
                            {
                                item.btn_Select.targetGraphic.enabled = false;

                                row["Error"] = "2";

                                item.errorCheck.text = viewPrepare.dropdown_error.options[2].text;
                            }
                            else
                            {
                                item.btn_Select.targetGraphic.enabled = true;
                            }                               

                        }
                    }

                }
                */

                // UnSelect();
                CheckError();

                getCount();

                PreErrorTable.AcceptChanges();

                UpdateView();

                btn_groupProcess.interactable = true;

                commonPopup.CloseDestroy();

            });

        }
    }

     public void ClosePopup()
   {
        CommonPopup commonPopup = Instantiate(mainManager.commonPopup, mainManager.contents);

        commonPopup.ShowPopup_TwoButton(
            "입력하신 정보가 저장되지않습니다. 나가시겠습니까?",
            "아니오",
            "네",
            () =>
            {
                // 아니오 삭제
                commonPopup.CloseDestroy();
            },

            () =>
            {

                ClearListView();

                // 네
                commonPopup.CloseDestroy();

                this.Close();
            });
    }

    public void Submit()
    {
        CommonPopup commonPopup = Instantiate(mainManager.commonPopup, mainManager.contents);

        commonPopup.ShowPopup_TwoButton(
            "오류 처리를 완료하시겠습니까?", 
            "아니오",
            "네",
            () => 
            {
                // 아니오 삭제
                commonPopup.CloseDestroy();
            },
            
            () =>
            {
                // 네
                foreach (DataRow row in PreErrorTable.Rows)
                {
                    string noValue = row["NO"].ToString(); // NO 열 값 가져오기

                    // NO 열과 동일한 값을 가진 행을 찾기
                    DataRow[] foundRows = viewPrepare.prepareTable.Select("NO = '" + noValue + "'");

                    if (foundRows.Length > 0)
                    {
                        DataRow foundRow = foundRows[0];
                        // 동일한 값을 가진 행의 ColumnName 열 값을 변경
                        // Debug.Log("row[Error] : " + row["Error"].ToString());
                        // Debug.Log("foundRow[Error] : " + foundRow["Error"].ToString());

                        foundRow["Error"] = row["Error"];
                        foundRow["세대주관계"] = row["세대주관계"];
                        foundRow["공급위치_시군구코드"] = row["공급위치_시군구코드"];
                        foundRow["isSelect"] = "false";
                    }
                }

                viewPrepare.listFamily = errorFamily.ToList();

                viewPrepare.prepareTable.AcceptChanges();

                viewPrepare.ignoreValueChanged = true;

                viewPrepare.dropdown_error.value = 0;

                viewPrepare.UnSelect();

                viewPrepare.UpdateListView();

                viewPrepare.ignoreValueChanged = false;

                commonPopup.CloseDestroy();

                this.Close();
            });
    }

    public void CheckInputRegion(TMP_InputField _input)
    {
        if (_input.text.Length > 0)
        {
            viewMapSearch.viewErrorPopup = this;

            viewMapSearch.Input_map.text = _input.text;

            viewMapSearch.Open();

            viewMapSearch.Init();
        }
        else
        {
            viewMapSearch.viewErrorPopup = this;

            // viewMapSearch.Input_map.text = _input.text;

            viewMapSearch.Open();
        }
    }

    public void getCount()
    {
        unSolvedCount = viewPrepare.CountRowsWithCondition(PreErrorTable, "Error", "1");
        manualCount = viewPrepare.CountRowsWithCondition(PreErrorTable, "Error", "2");

        Text_unsolvedCount.text = $"미처리 (<style=accent>{unSolvedCount}</style>)" + 
            $"수동처리 ({manualCount})";

        if(unSolvedCount == 0)
        {
            // 모든 처리가 완료 되었다고 보고 완료 버튼 활성화
            Btn_Submit.interactable = true;
        }
        else
        {
            Btn_Submit.interactable = false;
        }

    }

    public void ResetView()
    {
        for (int i = 0; i < viewupdate.Length; i++)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(viewupdate[i]);
        }

        parent.gameObject.SetActive(false);

        parent.gameObject.SetActive(true);
    }

    public void ClearListView()
    {
        var children = contents.transform.GetComponentsInChildren<Item_PreError>(true);

        foreach (var child in children)
        {
            //if (child == contents) continue;
            //child.parent = null;

            Destroy(child.gameObject);
        }


        list_error.Clear();
        list_Toggles.Clear();
    }

    public void UpdateListView()
    {
        ClearListView();

        string cellValue = "";
        string errorValue = "";


        // NO로 정렬 지정
        Clear_Sort();

        CheckError();

        // 기본 차순 NO로 오름 차순 정렬한다.
        PreErrorTable.DefaultView.Sort = SortTarget;

        listTable = null;

        listTable = PreErrorTable.DefaultView.ToTable();

        // listCount와 테이블의 행 수 중 작은 값을 선택
        int rowCount = Math.Min(listCount, listTable.Rows.Count);

        for (var rowIndex = 0; rowIndex < rowCount; rowIndex++)
        {
            // 행 가져오기
            var slot = listTable.Rows[rowIndex];

            isSelectError = bool.Parse(slot["isSelectError"].ToString());

            Debug.Log("isSelectError : " + slot["isSelectError"].ToString());

            Item_PreError item = Instantiate(item_PreError, contents);

            // 그룸 하면 안됨
            // item.toggle.group = toggleGroups;           

            item.no.text = slot["NO"].ToString();

            item.aptNum.text = slot["주택관리번호"].ToString();
            item.aptName.text = slot["주택명"].ToString();
            item.aptDong.text = slot["동"].ToString();
            item.aptHosu.text = slot["호수"].ToString();

            // 지역명 
            // cellValue = slot["공급위치_시군구코드"].ToString().Trim();

            item.region.text = slot["region_name"].ToString();

            item.userName.text = slot["성명"].ToString();

            item.aptdate.text = viewPrepare.currentTime;
            
            item.relation.text = slot["세대주관계"].ToString();

            // 오류 처리 
            // 미처리

            errorValue = slot["Error"].ToString();

            if (errorValue == "1")
            {
                // 에러 표시
                item.btn_Select.targetGraphic.enabled = true;

                item.errorCheck.text = viewPrepare.dropdown_error.options[1].text;

                if (slot["region_status"].ToString() == "1")
                {
                    // 지역명 상태코드가 1인경우엔 에러로 판단하여 에러 코드 입력
                    item.region.gameObject.transform.parent.GetComponent<ProceduralImage>().enabled = true;

                }

                if (slot["relation_status"].ToString() == "1")
                {
                    // 세대주 관곅 에러일경우 
                    item.relation.gameObject.transform.parent.GetComponent<ProceduralImage>().enabled = true;
                }
            }
            // 수동처리
            else if (errorValue == "2")
            {
                item.errorCheck.text = viewPrepare.dropdown_error.options[2].text;
            }
            // 정상
            else if (errorValue == "3")
            {
                item.errorCheck.text = viewPrepare.dropdown_error.options[3].text;
            }
            // 제외
            else if (errorValue == "4")
            {
                item.errorCheck.text = viewPrepare.dropdown_error.options[4].text;

                item.toggle.interactable = false;

                item.relation.gameObject.transform.parent.GetComponent<ProceduralImage>().enabled = false;
                item.region.gameObject.transform.parent.GetComponent<ProceduralImage>().enabled = false;

                item.btn_Select.targetGraphic.enabled = false;

                slot["isSelect"] = "false";
                slot["isSelectError"] = "false";

            }

            item.toggle.isOn = isSelectError;

            item.isCheck = item.toggle.isOn;

            item.viewErrorPopup = this;

            item.Init();

            list_Toggles.Add(item.toggle);
            list_error.Add(item);
        }

        if (list_error.Count == 0)
        {
            item_Empty.SetActive(true);
        }
        else
        {
            item_Empty.SetActive(false);
        }

        ResetView();

        getCount();

        SetUpdateUI();

        if (list_error.Count > 0)
        {
            if (AreAllTogglesOn())
            {
                isAll = true;
                btn_On.gameObject.SetActive(true);
            }
            else
            {
                isAll = false;
                btn_On.gameObject.SetActive(false);
            }
        }
    }

    public void SetListView()
    {
        ClearListView();

        string errorValue = "";

        // NO로 정렬 지정
        Clear_Sort();

        unSolvedCount = 0;
        manualCount = 0;

        CheckError();

        // 기본 차순 NO로 오름 차순 정렬한다.
        PreErrorTable.DefaultView.Sort = SortTarget;

        listTable = null;

        listTable = PreErrorTable.DefaultView.ToTable();
        
        // listCount와 테이블의 행 수 중 작은 값을 선택
        int rowCount = Math.Min(listCount, listTable.Rows.Count);  
        
        for (var rowIndex = 0; rowIndex < rowCount; rowIndex++)
        {            
            // 행 가져오기
            var slot = listTable.Rows[rowIndex];

            Item_PreError item = Instantiate(item_PreError, contents);

            // 그룸 하면 안됨
            // item.toggle.group = toggleGroups;           

            item.no.text = slot["NO"].ToString();

            item.aptNum.text = slot["주택관리번호"].ToString();
            item.aptName.text = slot["주택명"].ToString();
            item.aptDong.text = slot["동"].ToString();
            item.aptHosu.text = slot["호수"].ToString();

            // 지역명 
            // cellValue = slot["공급위치_시군구코드"].ToString().Trim();

            item.region.text = slot["region_name"].ToString();

            item.userName.text = slot["성명"].ToString();

            item.aptdate.text = viewPrepare.currentTime;

            item.relation.text = slot["세대주관계"].ToString();

            // 오류 처리 
            // 미처리
            errorValue = slot["Error"].ToString();

            if (errorValue == "1")
            {
                // 에러 표시
                item.btn_Select.targetGraphic.enabled = true;
                item.errorCheck.text = viewPrepare.dropdown_error.options[1].text;

                if (slot["region_status"].ToString() == "1")
                {
                    // 지역명 상태코드가 1인경우엔 에러로 판단하여 에러 코드 입력
                    item.region.gameObject.transform.parent.GetComponent<ProceduralImage>().enabled = true;

                }

                if (slot["relation_status"].ToString() == "1")
                {
                    // 세대주 관곅 에러일경우 
                    item.relation.gameObject.transform.parent.GetComponent<ProceduralImage>().enabled = true;
                }
            }
            // 수동처리
            else if (errorValue == "2")
            {
                item.errorCheck.text = viewPrepare.dropdown_error.options[2].text;
            }
            // 정상
            else if (errorValue == "3")
            {
                item.errorCheck.text = viewPrepare.dropdown_error.options[3].text;
            }
            // 제외
            else if (errorValue == "4")
            {
                item.errorCheck.text = viewPrepare.dropdown_error.options[4].text;

                // 선택 비활성화
                item.toggle.interactable = false;
                item.relation.gameObject.transform.parent.GetComponent<ProceduralImage>().enabled = false;
                item.region.gameObject.transform.parent.GetComponent<ProceduralImage>().enabled = false;

                item.btn_Select.targetGraphic.enabled = false;

                slot["isSelect"] = "false";
                slot["isSelectError"] = "false";
            }

            item.toggle.isOn = false;

            item.isCheck = item.toggle.isOn;

            item.viewErrorPopup = this;

            item.Init();

            list_Toggles.Add(item.toggle);
            list_error.Add(item);
        }
        
        if (list_error.Count == 0)
        {
            item_Empty.SetActive(true);
        }
        else
        {
            item_Empty.SetActive(false);
        }

        getCount();

        ResetView();

        GoToFirstPage();
                
        if (list_error.Count > 0)
        {
            if (AreAllTogglesOn())
            {
                isAll = true;
                btn_On.gameObject.SetActive(true);
            }
            else
            {
                isAll = false;
                btn_On.gameObject.SetActive(false);
            }
        }
    }

    public void CheckError()
    {
        string cellValue = "";

        bool isRelation = false;
        bool isRegion = false;

        for (var rowIndex = 0; rowIndex < PreErrorTable.Rows.Count; rowIndex++)
        {
            var slot = PreErrorTable.Rows[rowIndex];

            // 시군구 코드 에러 체크
            cellValue = slot["공급위치_시군구코드"].ToString().Trim();

            var res = viewPrepare.getMapName(cellValue);

            if (res.Length > 0)
            {
                if (res == "1")
                {
                    //에러 상태인경우
                    slot["region_status"] = 1;
                    slot["region_name"] = cellValue;
                }

                else
                {
                    // 에러가 아닌경우
                    slot["region_status"] = 0;
                    slot["region_name"] = res;
                }
            }
            else
            {
                //에러 상태인경우
                slot["region_status"] = 1;
                slot["region_name"] = cellValue;

            }

            // 세대주 에러 체크
            cellValue = slot["세대주관계"].ToString().Trim();

            // 세대주 관계 코드 판별
            if (cellValue.Length > 0)
            {
                if (cellValue == "#N/A")
                {
                    // Debug.Log("이상치 입니다.");
                    // 제외로 처리
                    slot["Error"] = "4";
                }
                else
                {
                    //var results = listFamily.Where(x => x.title.Contains(slot["세대주관계"].ToString()) || x.member.Any(y => y.Contains(slot["세대주관계"].ToString())));
                    List<string> result = errorFamily.Where(famData => famData.member.Any(member => member.ToLower().Contains(cellValue))).Select(famData => famData.title).ToList();

                    // 자동으로 분류
                    if (result.Count > 0)
                    {
                        // 세대주 관계가 맞으면
                        slot["세대주관계"] = result[0];
                        slot["relation_status"] = 0;

                    }
                    else
                    {
                        // Debug.Log("Results : " + slot["세대주관계"].ToString() + "는 어디에도 없습니다");
                        // 에러 미처리로 표시
                        slot["relation_status"] = 1;
                    }
                }
            }
            else
            {
                // 세대주관계 빈칸일경우 
                // 에러 미처리로 표시
                slot["relation_status"] = 1;
            }

            if (slot["Error"].ToString() == "4")
            {
                // 무조건 제외로 처리 하믄 된다~                
            }
            else
            {
                //  에러 상태 최종 체크
                if (slot["region_status"].ToString() == "1" || slot["relation_status"].ToString() == "1")
                {
                    // 둘중 하나라도 에러믄 미처리
                    slot["Error"] = 1;
                }
                else if (slot["region_status"].ToString() == "0" && slot["relation_status"].ToString() == "0")
                {
                    // 둘다 0인경우엔 정상 처리
                    slot["Error"] = 2;
                }
            }


            errorRelationCount = CountRowsWithCondition(PreErrorTable, "relation_status", "1");

            errorRegionCount = CountRowsWithCondition(PreErrorTable, "region_status", "1");

            if (errorRegionCount > 0)
            {
                isRegion = true;
            }
            else
            {
                isRegion = false;
            }

            if (errorRelationCount > 0)
            {
                isRelation = true;
            }
            else
            {
                isRelation = false;
            }


            //if (isRegion || isRelation)
            //{
            //    slot["Error"] = 1;
            //}
            //else
            //{
            //    slot["Error"] = 2;
            //}

            isErrorRegion = isRegion;
            isErrorRelation = isRelation;

            //Debug.Log("isErrorRegion  : " + isErrorRegion + ", isErrorRelation : " + isErrorRelation);

            // Debug.Log("<color=yellow>CheckError : " + slot["Error"].ToString() + "</color>");

            //// 행정번경
            //if (slot["행정변경"].ToString().Trim() == "#N/A")
            //{
            //    // #N/A일 경우  에러 미처리로 표시
            //    slot["Error"] = "1";
            //}

            //// 변경일자
            //if (slot["변경일자"].ToString().Trim() == "#N/A")
            //{
            //    // #N/A일 경우  에러 미처리로 표시
            //    slot["Error"] = "1";
            //}

            //// 배우자
            //if (slot["변경일자"].ToString().Trim() == "#N/A")
            //{
            //    // #N/A일 경우  에러 미처리로 표시
            //    slot["Error"] = "1";
            //}

            //// 행정주소
            //if (slot["변경일자"].ToString().Trim() == "#N/A")
            //{
            //    // #N/A일 경우  에러 미처리로 표시
            //    slot["Error"] = "1";
            //}

            //// 주소일치여부_부동산원
            //if (slot["주소일치여부_부동산원"].ToString().Trim() == "#N/A")
            //{
            //    // #N/A일 경우  에러 미처리로 표시
            //    slot["Error"] = "1";
            //}

            //if (slot["Error"].ToString() == "1")
            //{
            //    // 미처리 카운트 증가
            //    unSolvedCount += 1;
            //}
            //else if (slot["Error"].ToString() == "4")
            //{
            //    // 제외가 필요하면 제외 카운트 나중에 추가

            //    // 제외 카운트 추가
            //    exceptCount += 1;
            //}

        }

    }


    public void ListRelationDropdownValueChanged(TMP_Dropdown change)
    {
        Debug.Log("New Value : " + change.value);

        if(change.value != 0)
        {
            btn_groupProcess.interactable = true;
            selectRelation = listrelations[change.value];
        }
        else
        {
            btn_groupProcess.interactable = false;
            selectRelation = "";
        }
    }

    public void ListCountDropdownValueChanged(TMP_Dropdown change)
    {
        Debug.Log("New Value : " + change.value);

        listCount = listCounts[change.value];

        itemsPerPage = listCount;

        // 전체 선택 해제
        UnSelect();

        if (PreErrorTable != null)
        {
            // 리스트 갯수 변경시 리스트 처음으로
            GoToFirstPage();

            // UpdateListView();
        }
    }

    public void All_Select()
    {
        if (!isAll)
        {
            isAll = true;

            btn_On.gameObject.SetActive(true);
        }
        else
        {
            isAll = false;
            btn_On.gameObject.SetActive(false);

        }

        foreach (Item_PreError item in list_error)
        {
            if (item.toggle.interactable)
            {
                item.toggle.isOn = isAll;
            }
        }

        //foreach (Toggle item in list_Toggles)
        //{
        //    item.isOn = isAll;
        //}
    }

    public void UnSelect()
    {
        isAll = false;
        btn_On.gameObject.SetActive(false);

        foreach (Item_PreError item in list_error)
        {
            if (item.toggle.interactable)
            {
                item.toggle.isOn = isAll;
            }
        }
    }

    // 선택한 아이템이 있는지 체크
    public void Check_SelectItem()
    {
        if (AreAllTogglesOn())
        {
            isAll = true;
            btn_On.gameObject.SetActive(true);

            if(selectRegionCode.Length > 0)
            {
                btn_regionSet.interactable = true;
            }

            if(dropdown_relation.value != 0)
            {
                btn_groupProcess.interactable = true;
            }
        }

        if (AreAllTogglesOff())
        {
            btn_regionSet.interactable = false;
            btn_groupProcess.interactable = false;
        }
    }

    public bool AreAllTogglesOn()
    {
        bool allTogglesOn = true;

        foreach (Toggle toggle in list_Toggles)
        {
            if (!toggle.isOn)
            {
                allTogglesOn = false;
                break;
            }
        }
        return allTogglesOn;
    }

    public bool AreAllTogglesOff()
    {
        bool allTogglesOff = true;

        foreach (Toggle toggle in list_Toggles)
        {
            if (toggle.isOn)
            {
                allTogglesOff = false;
                break;
            }
        }

        return allTogglesOff;
    }
    void SetUpdateUI()
    {
        // 정렬 적용

        // 페이지 번호 버튼 업데이트
        int maxPage = Mathf.CeilToInt((float)listTable.Rows.Count / itemsPerPage);

        Debug.Log("maxPage :" + maxPage);

        int buttonOffset = (currentPage - 1) / 10 * 10; // Calculate the offset for the page buttons

        for (int i = 0; i < pageButtons.Count; i++)
        {
            Button pageButton = pageButtons[i];
            int pageNumber = buttonOffset + i + 1; // Apply the offset to the page number

            if (pageNumber <= maxPage)
            {
                pageButton.gameObject.SetActive(true);
                TextMeshProUGUI buttonText = pageButton.GetComponentInChildren<TextMeshProUGUI>();
                buttonText.text = mainManager.FormatNumberWithSymbol(pageNumber);

                LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)pageButton.gameObject.transform);

                // 현재 페이지 버튼 강조 표시
                if (pageNumber == currentPage)
                {
                    ColorBlock colors = pageButton.colors;
                    colors.normalColor = mainManager.PageOnColor;
                    // colors.highlightedColor = Color.blue;
                    pageButton.colors = colors;
                    buttonText.color = mainManager.PageTextOnColor;
                }
                else
                {
                    ColorBlock colors = pageButton.colors;
                    colors.normalColor = mainManager.PageOffColor;
                    // colors.highlightedColor = Color.white;
                    pageButton.colors = colors;
                    buttonText.color = mainManager.PageTextOffColor;
                }
            }
            else
            {
                pageButton.gameObject.SetActive(false);
            }
        }

        // 이전 10 페이지 및 다음 10 페이지 버튼 활성/비활성 설정   
        prevButton.interactable = currentPage > 10;
        nextButton.interactable = buttonOffset + 10 <= maxPage;

        // 첫 페이지 및 마지막 페이지 버튼 활성/비활성 설정
        firstPageButton.interactable = currentPage > 1;
        lastPageButton.interactable = currentPage < maxPage;

        LayoutRebuilder.ForceRebuildLayoutImmediate(pageContents);

    }

    void UpdateUI()
    {        
        // 정렬 적용

        // 페이지 번호 버튼 업데이트
        int maxPage = Mathf.CeilToInt((float)listTable.Rows.Count / itemsPerPage);

        Debug.Log("maxPage :" + maxPage);

        int buttonOffset = (currentPage - 1) / 10 * 10; // Calculate the offset for the page buttons

        for (int i = 0; i < pageButtons.Count; i++)
        {
            Button pageButton = pageButtons[i];
            int pageNumber = buttonOffset + i + 1; // Apply the offset to the page number

            if (pageNumber <= maxPage)
            {
                pageButton.gameObject.SetActive(true);
                TextMeshProUGUI buttonText = pageButton.GetComponentInChildren<TextMeshProUGUI>();
                buttonText.text = mainManager.FormatNumberWithSymbol(pageNumber);

                LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)pageButton.gameObject.transform);

                // 현재 페이지 버튼 강조 표시
                if (pageNumber == currentPage)
                {
                    ColorBlock colors = pageButton.colors;
                    colors.normalColor = mainManager.PageOnColor;
                    // colors.highlightedColor = Color.blue;
                    pageButton.colors = colors;
                    buttonText.color = mainManager.PageTextOnColor;
                }
                else
                {
                    ColorBlock colors = pageButton.colors;
                    colors.normalColor = mainManager.PageOffColor;
                    // colors.highlightedColor = Color.white;
                    pageButton.colors = colors;
                    buttonText.color = mainManager.PageTextOffColor;
                }
            }
            else
            {
                pageButton.gameObject.SetActive(false);
            }
        }

        // 이전 10 페이지 및 다음 10 페이지 버튼 활성/비활성 설정   
        prevButton.interactable = currentPage > 10;
        nextButton.interactable = buttonOffset + 10 <= maxPage;

        // 첫 페이지 및 마지막 페이지 버튼 활성/비활성 설정
        firstPageButton.interactable = currentPage > 1;
        lastPageButton.interactable = currentPage < maxPage;

        LayoutRebuilder.ForceRebuildLayoutImmediate(pageContents);

        // 데이터 변경
        if (PreErrorTable != null)
        {
            UpdateView();
        }

    }

    public void UpdateView()
    {
        Debug.Log("UPDATE VIEW");

        ClearListView();

        string cellValue = "";
        string errorValue = "";

        // 현재 정렬 버튼으로
        PreErrorTable.DefaultView.Sort = SortTarget;

        listTable = null;

        listTable = PreErrorTable.DefaultView.ToTable();

        int startIndex = (currentPage - 1) * itemsPerPage;
        int endIndex = Mathf.Min(startIndex + itemsPerPage, listTable.Rows.Count);

        for (var rowIndex = startIndex; rowIndex < endIndex; rowIndex++)
        {
            // Debug.Log("rowIndex : " + rowIndex);

            // 행 가져오기
            var slot = listTable.Rows[rowIndex];

            isSelectError = bool.Parse(slot["isSelectError"].ToString());

            Item_PreError item = Instantiate(item_PreError, contents);

            // 그룸 하면 안됨
            // item.toggle.group = toggleGroups;

            item.no.text = slot["NO"].ToString();

            item.aptNum.text = slot["주택관리번호"].ToString();
            item.aptName.text = slot["주택명"].ToString();
            item.aptDong.text = slot["동"].ToString();
            item.aptHosu.text = slot["호수"].ToString();

            // 지역명 
            // cellValue = slot["공급위치_시군구코드"].ToString().Trim();

            item.region.text = slot["region_name"].ToString();

            item.userName.text = slot["성명"].ToString();

            item.aptdate.text = viewPrepare.currentTime;

            item.relation.text = slot["세대주관계"].ToString();

            errorValue = slot["Error"].ToString();

            // 오류 처리
            if (errorValue == "1")
            {
                // 에러 표시
                item.btn_Select.targetGraphic.enabled = true;
                item.errorCheck.text = viewPrepare.dropdown_error.options[1].text;

                if (slot["region_status"].ToString() == "1")
                {
                    // 지역명 상태코드가 1인경우엔 에러로 판단하여 에러 코드 입력
                    item.region.gameObject.transform.parent.GetComponent<ProceduralImage>().enabled = true;

                }

                if (slot["relation_status"].ToString() == "1")
                {
                    // 세대주 관곅 에러일경우 
                    item.relation.gameObject.transform.parent.GetComponent<ProceduralImage>().enabled = true;
                }
            }
            // 수동처리
            else if (errorValue == "2")
            {
                item.errorCheck.text = viewPrepare.dropdown_error.options[2].text;
            }
            // 정상
            else if (errorValue == "3")
            {
                item.errorCheck.text = viewPrepare.dropdown_error.options[3].text;
            }
            // 제외
            else if (errorValue == "4")
            {
                item.errorCheck.text = viewPrepare.dropdown_error.options[4].text;
                // 선택 비활성화
                item.toggle.interactable = false;
                item.relation.gameObject.transform.parent.GetComponent<ProceduralImage>().enabled = false;
                item.region.gameObject.transform.parent.GetComponent<ProceduralImage>().enabled = false;

                item.btn_Select.targetGraphic.enabled = false;

                slot["isSelect"] = "false";
                slot["isSelectError"] = "false";
            }

            item.toggle.isOn = isSelectError;

            item.isCheck = item.toggle.isOn;

            item.viewErrorPopup = this;

            item.Init();
            list_Toggles.Add(item.toggle);
            list_error.Add(item);

        }

        ResetView();

        SetUpdateUI();
        

        if (AreAllTogglesOn())
        {
            isAll = true;
            btn_On.gameObject.SetActive(true);
        }
        else
        {
            isAll = false;
            btn_On.gameObject.SetActive(false);
        }
    }

    // 이전 10 페이지로 이동하는 함수
    void Previous10Pages()
    {
        if (listTable != null)
        {                     
            currentPage -= 10;
            if (currentPage < 1)
                currentPage = 1;

            UpdateUI();
        }
    }

    // 다음 10 페이지로 이동하는 함수
    void Next10Pages()
    {
        if (PreErrorTable != null)
        {
            currentPage += 10;
            int maxPage = Mathf.CeilToInt((float)PreErrorTable.Rows.Count / itemsPerPage);
            if (currentPage > maxPage)
                currentPage = maxPage;

            UpdateUI();
        }
    }

    // 첫 페이지로 이동하는 함수
    void GoToFirstPage()
    {
        if (PreErrorTable != null)
        {
            currentPage = 1;
            UpdateUI();
        }
    }

    // 마지막 페이지로 이동하는 함수
    void GoToLastPage()
    {
        if (PreErrorTable != null)
        {
            int maxPage = Mathf.CeilToInt((float)PreErrorTable.Rows.Count / itemsPerPage);
            currentPage = maxPage;
            UpdateUI();
        }
    }

    // 특정 페이지로 이동하는 함수
    void GoToPage(int pageNumber)
    {
        if (PreErrorTable != null)
        {
            Debug.Log(" GOTOPAGE :" + pageNumber);

            int maxPage = Mathf.CeilToInt((float)PreErrorTable.Rows.Count / itemsPerPage);
            int buttonOffset = (currentPage - 1) / 10 * 10; // Calculate the offset for the page buttons

            int targetPage = buttonOffset + pageNumber; // Calculate the target page based on the current offset

            if (targetPage < 1)
                targetPage = 1;

            if (targetPage > maxPage)
                targetPage = maxPage;

            currentPage = targetPage;

            UpdateUI();
        }
    }

    // 특정값 칼럼 갯수 구하기
    public int CountRowsWithCondition(DataTable table, string columnName, string value)
    {
        string filterExpression = $"{columnName} = '{value}'";
        DataRow[] filteredRows = table.Select(filterExpression);
        int count = filteredRows.Length;

        return count;
    }


    public void AddMemberToFamily(string memberTitle, string memberName)
    {
        FamData existingFamData = errorFamily.Find(f => f.title == memberTitle);

        if (existingFamData != null && Array.IndexOf(existingFamData.member, memberName) == -1)
        {
            // Add memberName to the existing FamData's member array
            List<string> members = new List<string>(existingFamData.member);
            members.Add(memberName);
            existingFamData.member = members.ToArray();
        }
    }
}
