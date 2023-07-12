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

using Newtonsoft.Json;
using SrcTree.Util;
using System.Threading.Tasks;

public class ViewPrepare : BaseView
{
    public string MenuName = "데이터 전처리";
    
    public MainManager mainManager;

    public ServerManager serverManager;

    public ViewErrorPopup viewErrorPopup;

    public ViewDangerous viewDangerous;

    public Item_Prepare item_Prepare;

    public GameObject item_Empty;

    public Transform contents;

    public Transform parent;

    //public Image img_contents;

    public RectTransform[] viewupdate;

    // UI
    public GameObject UI_Fileitem;

    // Text
    public TextMeshProUGUI Text_FileName;

    // 전체항목
    public TextMeshProUGUI Text_totalCount;

    // 미처리
    public TextMeshProUGUI Text_unsolvedCount;

    // 전처리 실행 전
    public TextMeshProUGUI Text_beforeCount;

    // 전처리 완료
    public TextMeshProUGUI Text_endCount;

    // Buttons
    // 찾아보기
    public Button btn_FileLoad;

    // 파일아이템 삭제
    public Button btn_DeleteFile;

    // 전처리 실행
    public Button btn_ActionPrepare;

    // 엑셀 다운로드
    public Button btn_Excel;

    // DropDown
    public TMP_Dropdown dropdown_error;
    public TMP_Dropdown dropdown_count;

    public bool ignoreValueChanged;

    // 제외
    public Button btn_Except;

    // 오류처리
    public Button btn_ErrorCheck;

    // 위험도 산출 전송
    public Button btn_SendFile;

    // 전체선택
    public Button btn_AllSelect;
    public Image btn_On;

    public string _path;
        
    public DataTable prepareTable;    

    DataTable listTable;

    [SerializeField]
    List<Item_Prepare> list_Prepare = new List<Item_Prepare>();

    [SerializeField]
    public List<Toggle> list_Toggles = new List<Toggle>();

    public int listCount;
    public int listMode;

    public int[] listCounts = { 10, 20, 50, 100 };

    public int selectrowCount;

    // 전체항목
    public int totalCount;
    // 미처리
    public int unSolvedCount;
    // 정상
    public int passCount;
    // 제외
    public int exceptCount;
    // 수동처리
    public int manualCount;
    // 전처리 실행전
    public int beforeCount;
    // 전처리 완료
    public int endCount;

    // 전처리 세대주관계 에러
    public int errorRelationCount;
    // 전처리 지역 에러
    public int errorRegionCount;

    public string[] col;

    public string currentTime = "";
    public bool isSelect;
    public bool isAll;

    public bool isErrorRelation;
    public bool isErrorRegion;
    
    [SerializeField]
    public List<FamData> listFamily;

    public string js = "";
    public string savePath;

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

    public string codes;

    // Start is called before the first frame update
    void Start()
    {
        //allData = GetData(); // 데이터 가져오기

        //UpdateUI();

        // 페이지 리스너 등록
        prevButton.onClick.AddListener(Previous10Pages);
        nextButton.onClick.AddListener(Next10Pages);
        firstPageButton.onClick.AddListener(GoToFirstPage);
        lastPageButton.onClick.AddListener(GoToLastPage);

        for (int i = 0; i < pageButtons.Count; i++)
        {
            int pageNumber = ((currentPage - 1) / 10) * 10 + i + 1;            
            pageButtons[i].onClick.AddListener(() => GoToPage(pageNumber));
        }

        // 가족관계 저장 경로
        savePath = Application.streamingAssetsPath + "/famdata.json";

        Init();

        btn_FileLoad.onClick.AddListener(() =>
        {
            // 파일로드
            Action_FileLoad();
        });

        btn_DeleteFile.onClick.AddListener(() =>
        {
            // 파일 경로 삭제
            Action_DeleteFileItem();
        });

        btn_ActionPrepare.onClick.AddListener(() =>
        {
            // 파일 전처리 실행
            Action_Preperation();
        });

        btn_Except.onClick.AddListener(() =>
        {
            Debug.Log("btn_Except Click");

            // 선택한 항목 제외
            //DataRow[] rows = prepareTable.Select("isSelect = 'True'");
            DataRow[] rows = prepareTable.Select("isSelect = True");

            if (rows.Length > 0)
            {
                foreach (DataRow dataRow in rows)
                {
                    // Debug.Log("성명은 :" + dataRow["성명"].ToString());
                    dataRow["Error"] = "4";
                }
            }

            // 전체 선택 해제
            UnSelect();

            if (prepareTable != null)
            {
                UpdateUI();
            }

            rows = prepareTable.Select("Error = 4");
            
            exceptCount = rows.Length;

            getCount();

        });

        // 오류 처리
        btn_ErrorCheck.onClick.AddListener(Load_ErrorProcess);

        // 전체 선택
        btn_AllSelect.onClick.AddListener(All_Select);

        // 전처리 파일 전송
        btn_SendFile.onClick.AddListener(Action_SendPrepare);

        // 엑셀 다운로드
        btn_Excel.onClick.AddListener(Action_Excel);
    }

    // Update is called once per frame
    //void Update()
    //{

    //    if (Input.GetKeyDown(KeyCode.Space))
    //    {
    //        Debug.Log("listTable " + listTable.Rows.Count);
    //        for (int i = 0; i < 10; i++)
    //        {
    //            Debug.Log("listTable : " + listTable.Rows[i]["성명"] + ", " + listTable.Rows[i]["세대주관계"]);
    //        }

    //        Debug.Log("prepareTable " + prepareTable.Rows.Count);
    //        for (int i = 0; i < 10; i++)
    //        {
    //            Debug.Log("prepareTable : " + prepareTable.Rows[i]["성명"] + ", " + prepareTable.Rows[i]["세대주관계"]);
    //        }

    //        Action_CSVLoad();

    //        Save();

    //        Action_Preperation();
    //        SaveFile();
    //        string mapname = getMapName(codes);

    //        Find the FamData object for "친적"

    //       FamData existingFamData = listFamily.Find(f => f.title == "친척");

    //        if (existingFamData != null && Array.IndexOf(existingFamData.member, "고종") == -1)
    //            {
    //                // Add "고종" to the existing FamData's member array
    //                List<string> members = new List<string>(existingFamData.member);
    //                members.Add("고종");
    //                existingFamData.member = members.ToArray();
    //            }


    //        UpdateDuplicateCount();

    //        CopyToDataFrame();
    //    }
    //}

    // 엑셀 다운로드
    public void Action_Excel()
    {
        if (prepareTable == null)
        {
            return;
        }
        else
        {
            if (prepareTable.Rows.Count == 0)
            {
                return;
            }
        }

        //string[] cols = new string[CheckTable.Columns.Count];

        string[] fileTypes = new string[] { "CSV Files", "csv" };

        StandaloneFileBrowser.SaveFilePanelAsync("엑셀 다운로드", "", "", "csv", (string path) =>
        {
            if (!string.IsNullOrEmpty(path))
            {
                // 파일 저장 로직을 구현하세요
                // 예를 들어, 파일을 생성하고 내용을 저장할 수 있습니다.
                // File.WriteAllText(path, "Hello, World!");
                DataTable saveTable = prepareTable.Copy();

                string[] columnsToRemove = { "isSelect", "Error", "NO", "region_status", "relation_status", "region_name" }; // 삭제할 칼럼들의 이름 배열

                foreach (string columnName in columnsToRemove)
                {
                    if (saveTable.Columns.Contains(columnName))
                    {
                        saveTable.Columns.Remove(columnName);
                    }
                }

                SaveDataTableToCsv(saveTable, path);

                Debug.Log("File saved at: " + path);

                if(!string.IsNullOrEmpty(_path))
                {
                    //string originalFileName = Path.GetFileNameWithoutExtension(_path); // 기존 파일명 가져오기
                    //mainManager.DownloadLodInsert(MenuName, originalFileName + " 엑셀다운로드");

                    string names = Path.GetFileNameWithoutExtension(path);
                    mainManager.DownloadLodInsert(MenuName, $"{names} 엑셀다운로드");

                }
            }
            else
            {
                Debug.Log("Save file operation cancelled.");
            }
        });
    }

    // 이력저장


    // 정렬 버튼
    public void Reset_Sort(SortButton sortButton)
    {
        // 정렬 초기화
        for (int i = 0; i < sortButtons.Length; i++)
        {
            if(sortButtons[i] != sortButton)
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

        SortTarget ="NO ASC";
    }

    public void Set_Sort(SortButton sortButton)
    {
        UnSelect();

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

        if(prepareTable != null)
        {
            UpdateUI();
        }

        Debug.Log(" SORT " + SortTarget);
    }


    // 이전 페이지로 이동하는 함수
    void PreviousPage()
    {
        currentPage--;
        if (currentPage < 1)
            currentPage = 1;

        UpdateUI ();
    }

    // 다음 페이지로 이동하는 함수
    void NextPage()
    {
        currentPage++;
        int maxPage = Mathf.CeilToInt((float)prepareTable.Rows.Count / itemsPerPage);
        if (currentPage > maxPage)
            currentPage = maxPage;

        UpdateUI();
    }

    // 이전 10 페이지로 이동하는 함수
    void Previous10Pages()
    {
        if(prepareTable != null)
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
        if (prepareTable != null)
        {
            currentPage += 10;
            int maxPage = Mathf.CeilToInt((float)prepareTable.Rows.Count / itemsPerPage);
            if (currentPage > maxPage)
                currentPage = maxPage;

            UpdateUI();
        }
    }

    // 첫 페이지로 이동하는 함수
    void GoToFirstPage()
    {
        if (prepareTable != null)
        {
            currentPage = 1;
            UpdateUI();
        }
    }

    // 마지막 페이지로 이동하는 함수
    void GoToLastPage()
    {
        if (prepareTable != null)
        {
            int maxPage = Mathf.CeilToInt((float)prepareTable.Rows.Count / itemsPerPage);
            currentPage = maxPage;
            UpdateUI();
        }
    }

    // 특정 페이지로 이동하는 함수
    void GoToPage(int pageNumber)
    {
        if (prepareTable != null)
        {
            Debug.Log(" GOTOPAGE :" + pageNumber);

            int maxPage = Mathf.CeilToInt((float)prepareTable.Rows.Count / itemsPerPage);
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

    void  SetUpdateUI()
    {
        // 페이지 번호 버튼 업데이트
        int maxPage = Mathf.CeilToInt((float)selectrowCount / itemsPerPage);

        //Debug.Log("maxPage :" + maxPage);

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

    // UI 업데이트 함수
    void UpdateUI()
    {
        /*
        Debug.Log("startIndex : " + startIndex);
        Debug.Log("endIndex : " + endIndex);

        for (int i = 0; i < itemUIList.Count; i++)
        {
            GameObject itemUI = itemUIList[i];
            if (i < endIndex - startIndex)
            {
                itemUI.SetActive(true);
                // 데이터 표시
                DataItem dataItem = allData[startIndex + i];
                Text itemText = itemUI.GetComponentInChildren<Text>();
                itemText.text = dataItem.name;
            }
            else
            {
                itemUI.SetActive(false);
            }
        }
        */
        /*
        // 이전 페이지 및 다음 페이지 버튼 활성/비활성 설정
        prevButton.interactable = currentPage > 1;
        nextButton.interactable = currentPage < Mathf.CeilToInt((float)allData.Count / itemsPerPage);
        */

        // 정렬 적용

        // 페이지 번호 버튼 업데이트
        int maxPage = Mathf.CeilToInt((float)prepareTable.Rows.Count / itemsPerPage);

        //Debug.Log("maxPage :" + maxPage);

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
        if (prepareTable != null)
        {
            UpdateView();
        }
    }

    // Init
    public void Init()
    {
        currentPage = 1;

        listCount = 10;
        itemsPerPage = listCount;
        totalCount = 0;
        unSolvedCount = 0;
        passCount = 0;
        exceptCount = 0;
        manualCount = 0;

        dropdown_error.value = 0;
        dropdown_count.value = 0;

        var TempList = Load();

        if (TempList != null)
        {
            listFamily = TempList;
        }
        
        UnSelect();
    }

    public void Reset()
    {
        currentPage = 1;

        listCount = 10;
        itemsPerPage = listCount;
        totalCount = 0;
        unSolvedCount = 0;
        passCount = 0;
        exceptCount = 0;
        manualCount = 0;
        beforeCount = 0;

        dropdown_error.value = 0;
        dropdown_count.value = 0;

        var TempList = Load();

        if (TempList != null)
        {
            listFamily = TempList;
        }

        UnSelect();
    }

    // 전처리 구역

    public void Save()
    {
        string jsonData = JsonConvert.SerializeObject(listFamily);

        // 파일 저장
        File.WriteAllText(savePath, jsonData);    
    }

    public List<FamData> Load()
    {
        if (File.Exists(savePath))
        {
            // 파일에서 JSON 데이터 읽어오기
            string jsonData = File.ReadAllText(savePath);

            // JSON 데이터를 객체로 역직렬화
            List<FamData> data = JsonConvert.DeserializeObject<List<FamData>>(jsonData);

            return data;
        }
        else
        {
            Debug.LogError("Save file not found");

            return null;
        }
    }

    public void getCount()
    {
        passCount = CountRowsWithCondition(prepareTable, "Error", "3");

        exceptCount = CountRowsWithCondition(prepareTable, "Error", "4");

        unSolvedCount = CountRowsWithCondition(prepareTable, "Error", "1");

        manualCount = CountRowsWithCondition(prepareTable, "Error", "2");

        Text_totalCount.text = $"전체항목({totalCount})";
        Text_unsolvedCount.text = $"미처리 (<style=accent>{unSolvedCount}</style>)   " +
                                $"수동처리 ({manualCount})   " +
                                $"정상 ({passCount})  " +
                                $"제외 ({exceptCount})";

        Text_beforeCount.text = $"전처리 실행 전({beforeCount})";
        Text_endCount.text = $"전처리 실행 완료 (<style=complete>{(beforeCount - exceptCount)}</style>)"; 

        if(unSolvedCount == 0)
        {
            btn_SendFile.interactable = true;
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)Text_totalCount.gameObject.transform.parent);
        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)Text_beforeCount.gameObject.transform.parent);
    }    

    public void Action_SendPrepare()
    {
        Debug.Log(" Action_SendPrepare: " + unSolvedCount);

        if(unSolvedCount > 0)
        {
            return;
        }

        CommonPopup commonPopup = Instantiate(mainManager.commonPopup, mainManager.contents);

        commonPopup.ShowPopup_TwoButton(
        "전처리 완료된 파일을 \n" +
        "위험도 산출 메뉴로 전송하시겠습니까?",
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
            commonPopup.CloseDestroy();

            TaskOnPrepare();
        });
    }

    // 전처리 파일 저장
    public async Task<int> CopyToDataFrame()
    {
        /*
        // 전처리용 파일 저장 
        if(File.Exists(mainManager.filePath + "dataframe.csv"))
        {
            // 전처리 파일 있는가?
            Debug.Log("전처리 파일 있다");
            string filePath = Path.Combine(mainManager.filePath, "dataframe.csv");
            
            string copyPath = Path.Combine(Path.GetDirectoryName(_path), Path.GetFileNameWithoutExtension(_path) + "_전처리.csv");

            DataTable saveTable = prepareTable.Copy();

            string[] columnsToRemove = { "isSelect", "Error", "NO" }; // 삭제할 칼럼들의 이름 배열

            foreach (string columnName in columnsToRemove)
            {
                if (saveTable.Columns.Contains(columnName))
                {
                    saveTable.Columns.Remove(columnName);
                }
            }

            SaveDataTableToCsv(saveTable, filePath);

            SaveDataTableToCsv(saveTable, copyPath);
        }
        else
        {
            Debug.Log("전처리 파일 없다");
        }
        */

        string filePath = Path.Combine(mainManager.filePath, "dataframe.csv");

        string currentTime = DateTime.Now.ToString("yyyyMMdd_HHmmss"); // 현재 시간을 yyyyMMdd_HHmmss 형식으로 가져옴
        string originalFileName = Path.GetFileNameWithoutExtension(_path); // 기존 파일명 가져오기
        string extension = Path.GetExtension(_path); // 확장자 가져오기

        //string copyFileName = $"{currentTime}_{originalFileName}_전처리용{extension}"; // 현재 시간을 포함한 새로운 파일명 생성
        //string copyPath = Path.Combine(Path.GetDirectoryName(_path), Path.GetFileNameWithoutExtension(_path) + "_전처리.csv"));
        //string copyPath = Path.Combine(Path.GetDirectoryName(_path), copyFileName); // 새로운 파일 경로 생성

        DataTable saveTable = prepareTable.Clone();

        DataRow[] rows = prepareTable.Select("Error = 2 OR Error = 3");

        foreach(DataRow row in rows)
        {
            saveTable.ImportRow(row);
        }

        string[] columnsToRemove = { "isSelect", "Error", "NO", "region_status", "relation_status", "region_name" }; // 삭제할 칼럼들의 이름 배열

        foreach (string columnName in columnsToRemove)
        {
            if (saveTable.Columns.Contains(columnName))
            {
                saveTable.Columns.Remove(columnName);
            }
        }
        // 전처리
        SaveDataTableToCsv(saveTable, filePath);
        // 백업
        //SaveDataTableToCsv(saveTable, copyPath);


        return 0;
    }

    async void TaskOnPrepare()
    {
        mainManager.viewLoading.Open();

        mainManager.isPython = true;

        int saveresult = await CopyToDataFrame();

        Debug.Log("Processing...");
        mainManager.loadMsg.text = "전처리 Processing... ";

        var result = await mainManager.RunProcessAsync("python", "Preparation.py");

        Debug.Log("Result: " + result.ToString());

        mainManager.viewLoading.Close();

        mainManager.isPython = false;

        string names = Path.GetFileNameWithoutExtension(_path);

        string filePath = Path.Combine(mainManager.filePath, "dataframe_add.csv");

        string currentTime = DateTime.Now.ToString("yyyyMMdd_HHmmss"); // 현재 시간을 yyyyMMdd_HHmmss 형식으로 가져옴
        string originalFileName = Path.GetFileNameWithoutExtension(_path); // 기존 파일명 가져오기
        string extension = Path.GetExtension(_path); // 확장자 가져오기


        string[] files = Directory.GetFiles(Path.GetDirectoryName(_path), "*.csv");
        string baseFileName = $"{originalFileName}_전처리";


        int count = 0;
        foreach (string file in files)
        {
            string fileName = Path.GetFileNameWithoutExtension(file);

            // 파일 이름이 지정된 기본 파일 이름과 유사한 경우 개수 증가
            if (fileName.StartsWith(baseFileName))
            {
                count++;
            }
        }

        Debug.Log(" 비슷한 파일 갯수는 :" + count);
        string copyFileName = $"{originalFileName}_전처리{(count + 1)}{extension}"; // 현재 시간을 포함한 새로운 파일명 생성

        //string copyPath = Path.Combine(Path.GetDirectoryName(_path), Path.GetFileNameWithoutExtension(_path) + "_전처리.csv"));
        string copyPath = Path.Combine(Path.GetDirectoryName(_path), copyFileName); // 새로운 파일 경로 생성

        CopyFile(filePath, copyPath);

        viewDangerous._path = copyPath; 

        mainManager.ChangeLogInsert(MenuName, $"{names} 전처리 실행");

        // 끝나고 
        // 위험도 산출로
        mainManager.gnbViewControl.SelectMenu_01(1);

        viewDangerous.Init();
    }

    // 오류 처리 버튼
    public void Load_ErrorProcess()
    {        
        if(isErrorRelation || isErrorRegion)
        {
            DataRow[] selectedRows = prepareTable.Select("isSelect = 'true' AND Error = 1", "NO ASC", DataViewRowState.CurrentRows);
            /*
            viewErrorPopup.PreErrorTable = new DataTable();

            viewErrorPopup.PreErrorTable = prepareTable.Clone();
            viewErrorPopup.PreErrorTable.Columns.Add(new DataColumn("isSelectError", typeof(string)));
            foreach (DataRow row in selectedRows)
            {                
                viewErrorPopup.PreErrorTable.Rows.Add(row.ItemArray);
            }*/

            viewErrorPopup.PreErrorTable = null;

            viewErrorPopup.PreErrorTable = selectedRows.CopyToDataTable();

            DataColumn dataColumn = new DataColumn("isSelectError", typeof(string));
            dataColumn.DefaultValue = "False";

            viewErrorPopup.PreErrorTable.Columns.Add(dataColumn);

            viewErrorPopup.Open();

            viewErrorPopup.Init();

            //bool isSameReference = object.ReferenceEquals(prepareTable, viewErrorPopup.PreErrorTable);
            //Debug.Log("isSameReference :L " + isSameReference);

            selectedRows = null;
        }        
    }

    public void All_Select()
    {
        if(!isAll)
        {
            isAll = true;

            btn_On.gameObject.SetActive(true); // 전체 이미지 선택 온
        }
        else
        {
            isAll = false;
            btn_On.gameObject.SetActive(false);

            btn_Except.interactable = false;
            btn_ErrorCheck.interactable = false;
        }

        foreach(Item_Prepare item in list_Prepare)
        {
            if(item.toggle.interactable)
            {
                item.toggle.isOn = isAll;
            }
        }
        /*
        foreach (Toggle item in list_Toggles)
        {
            item.isOn = isAll;
        }*/
    }
    public void UnSelect()
    {
        isAll = false;
        btn_On.gameObject.SetActive(false);

        btn_Except.interactable = false;
        btn_ErrorCheck.interactable = false;

        foreach(Item_Prepare item in list_Prepare)
        {
            if(item.toggle.interactable)
            {
                item.toggle.isOn = isAll;
            }
        }
    }

    public string GetCurrentDate()
    {
        return DateTime.Now.ToString(("yyyy.MM.dd HH:mm:ss"));
    }

    public void ErrorDropdownValueChanged(TMP_Dropdown change)
    {
        Debug.Log("Error : " + change.value);
       
        listMode = change.value;

        if (ignoreValueChanged)
            return;
        // 전체 선택 해제
        UnSelect();


        if (prepareTable != null)
        {
            // UpdateListView();
            // 필터 변경시 리스트 처음으로
            GoToFirstPage();
        }
    }
    public void ListCountDropdownValueChanged(TMP_Dropdown change)
    {
        Debug.Log("New Value : " + change.value);

        listCount = listCounts[change.value];

        itemsPerPage = listCount;

        // 전체 선택 해제
        UnSelect();

        // 파일 아이템이 있을 경우에만 새로 그린다
        if (prepareTable != null)
        {
            // 리스트 갯수 변경시 리스트 처음으로
            GoToFirstPage();
            
            //UpdateListView();
        }
    }

    public string getMapName(string mapcode)
    {
        string mapname = "";

        //Debug.Log("serverManager.ds_map.Tables[0].TableName : " + serverManager.ds_map.Tables[0].TableName);

        //var str = serverManager.ds_map.Tables[0].Columns.Cast<DataColumn>()
        //                    .Select(x => x.ColumnName)
        //                    .ToArray();

        // foreach (string ss in str)
        // {
        //     Debug.Log("ss : "+ss + ", " +ss.GetType()); 
        // }
        // Debug.Log("getMapName : " + mapcode);

        try
        {
            if (mapcode.Length > 0 && mapcode != "0")
            {
                serverManager.ds_map.Tables[0].DefaultView.RowFilter = "bjdong_code = '" + mapcode + "'";

                mapname = serverManager.ds_map.Tables[0].DefaultView[0]["bjdong_name"].ToString();

                //Debug.Log("지역코드 정상 ");
                //Debug.Log($"mapcode : {mapcode}, mapname : {mapname}");
            }
            else
            {
                mapname = ""; //미처리로 처리 해주기 위해 1을 전달 한다

                // 예외 처리
                //Debug.Log("필터링 중 오류 발생");
                //Debug.Log($"mapcode : {mapcode}, mapname : {mapname}");

            }
        }
        catch (Exception ex)
        {

            mapname = "1"; //미처리로 처리 해주기 위해 1을 전달 한다

            // 예외 처리
            //Debug.Log("필터링 중 오류 발생: " + ex.Message);
            //Debug.Log($"mapcode : {mapcode}, mapname : {mapname}");

        }

        // Debug.Log("mapname : " + mapname);

        return mapname;
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

    public void Check_SelectItem()
    {
        if(AreAllTogglesOn())
        {
            isAll = true;
            btn_On.gameObject.SetActive(true);
        }

        if(AreAllTogglesOff())
        {
            btn_Except.interactable = false;
            btn_ErrorCheck.interactable = false;
        }
    }

    public void Action_Preperation()
    {
        
        /*
        foreach (EncodingInfo ei in Encoding.GetEncodings())
        {
            Encoding e = ei.GetEncoding();

            Debug.Log("{0,-15}" + ei.CodePage);

            Debug.Log("{0,-25}" + ei.Name);
            Debug.Log("{0,-25}" +  ei.DisplayName);

        }

        var config = new ExcelReaderConfiguration();
        config.FallbackEncoding = Encoding.GetEncoding("euc-kr");*/
        isErrorRegion = false;
        isErrorRelation = false;

        ClearListView();

        // 전처리 할때 시간 저장
        currentTime = GetCurrentDate();
        
        Action_CSVLoad();

        // 전처리 이력 저장
        
    }


    public List<Item_Prepare> ConvertDataTableToPersonList(DataTable table)
    {
        var rows = table.AsEnumerable();
        var people = rows.Select(row => new Item_Prepare
        {
            // Name = row.Field<string>("Name"),
            // Age = row.Field<int>("Age")
        }).ToList();
        return people;
    }

    public void Action_DeleteFileItem()
    {
        Text_FileName.text = "";
        UI_Fileitem.SetActive(false);

        dropdown_error.value = 0;
        dropdown_count.value = 0;

        btn_ActionPrepare.interactable = false;
        btn_ActionPrepare.GetComponentInChildren<TextMeshProUGUI>().color = mainManager.OffTextColor;

        btn_SendFile.interactable = false;

        if (contents.childCount > 0)
        {
            ClearListView();
        }

        if (prepareTable != null)
        {
            prepareTable.Clear();
            prepareTable.Dispose();
            prepareTable = null;
        }

        currentPage = 1;

        listCount = 10;
        itemsPerPage = listCount;
        totalCount = 0;
        unSolvedCount = 0;
        passCount = 0;
        exceptCount = 0;
        manualCount = 0;
        beforeCount = 0;


        Text_totalCount.text = $"전체항목({totalCount})";
        Text_unsolvedCount.text = $"미처리 (<style=accent>{unSolvedCount}</style>)   " +
                                $"수동처리 ({manualCount})   " +
                                $"정상 ({passCount})  " +
                                $"제외 ({exceptCount})";

        Text_beforeCount.text = $"전처리 실행 전({beforeCount})";
        Text_endCount.text = $"전처리 실행 완료 (<style=complete>{(beforeCount - exceptCount)}</style>)";

        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)Text_totalCount.gameObject.transform.parent);
        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)Text_beforeCount.gameObject.transform.parent);

        ResetView();

    }

    public void Action_FileLoad()
    {
        var paths =  StandaloneFileBrowser.OpenFilePanel("Load File", "", "csv", false);
                
        // WriteResult(paths);

        Action_FileItem(paths);

        //Action_CSVLoad();
    }
    
    public void Action_FileItem(string[] paths)
    {
        if (paths.Length == 0)
        {
            return;
        }

        if(contents.childCount > 0)
        {
            ClearListView();
        }

        _path = "";

        _path = paths[0];

        Debug.Log($"[Action_FileItem] [_path] : {_path}");
        Debug.Log($"[Action_FileItem] [_path] : " + Path.GetFileName(_path));
        Debug.Log($"[Action_FileItem] [_path2] : " + Path.GetFileNameWithoutExtension(_path));

        string[] filename = Path.GetFileName(_path).Split('.');

        Text_FileName.text = filename[0] + "."+ filename[1];

        UI_Fileitem.SetActive(true);

        //if (prepareTable != null)
        //{
        //    prepareTable.Clear();
        //    prepareTable.Dispose();
        //    prepareTable = null;            
        //}

        btn_ActionPrepare.interactable = true;

        btn_ActionPrepare.GetComponentInChildren<TextMeshProUGUI>().color = mainManager.OnTextColor;

    }

    // 비동기로 CSV 파일을 읽는 메서드를 추가합니다.
    private async Task<DataTable> ReadCsvAsync(IExcelDataReader reader)
    {
        var dataSet = await Task.Run(() => reader.AsDataSet());
        return dataSet.Tables[0];
    }

    // 파일 로드
    public async void Action_CSVLoad()
    {
        mainManager.viewLoading.Open();
        mainManager.loadMsg.text = "";

        UnSelect();

        // var path = AssetDatabase.GetAssetPath(csv);
        // System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
        //Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        // 한글 깨짐 현상 해결 가능
        var config = new ExcelReaderConfiguration();
        
        //config.FallbackEncoding = System.Text.Encoding.GetEncoding("ks_c_5601-1987");
        // config.FallbackEncoding = Encoding.GetEncoding("51949");

        //config.FallbackEncoding = Encoding.GetEncoding("cp949");
        config.FallbackEncoding = Encoding.GetEncoding("EUC-KR");

        prepareTable = new DataTable();

        using (var streamer = new FileStream(_path, FileMode.Open, FileAccess.Read))
        {
            using (var reader = ExcelReaderFactory.CreateCsvReader(streamer, config))
            {

                // 항상 하나의 시트만 관리된다.

                //prepareTable = reader.AsDataSet().Tables[0];

                prepareTable = await ReadCsvAsync(reader);


                //DataTable data = reader.AsDataSet().Tables[0];

                //prepareTable = data.Copy();


                if (reader.Read())
                {
                    Debug.Log("FieldCount : " + reader.FieldCount);

                    col = new string[reader.FieldCount];

                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        string columnName = reader.GetString(i);
                        //string columnName = reader.AsDataSet().Tables[0].Columns[i].ColumnName;

                        // Debug.Log("columnName : " + columnName);

                        // 칼럼명 저장하기
                        col[i] = columnName;


                        prepareTable.Columns[i].ColumnName = columnName;
                    }
                }

                prepareTable.Rows.RemoveAt(0);

                reader.Dispose();
                reader.Close();

                streamer.Close();

                // 새로운 컬럼 추가        
                // 전처리 선택 구분자( true, false)
                prepareTable.Columns.Add(new DataColumn("isSelect", typeof(string)));


                // "0. 오류없음, 1. 미처리, 2.수동처리, 3.정상, 4.제외 기본값 3"
                prepareTable.Columns.Add(new DataColumn("Error", typeof(int)));

                // 0 => 오류 없음, 1 => 공급 유형 오류, 2 => 세대주관계 오류, 3 => 둘다 오류
                //prepareTable.Columns.Add(new DataColumn("ErrorType", typeof(int)));

                // 0: 에러 없음 (코드가 있고 지역코드가 있는경우),  1: 에러 상태 (빈칸 or 지역코드가 맞는게 없는 경우)
                prepareTable.Columns.Add(new DataColumn("region_status", typeof(int)));

                // 0: 에러 없음, 1: 에러 상태 (빈칸 or #N/A)
                prepareTable.Columns.Add(new DataColumn("relation_status", typeof(int)));

                // no 
                prepareTable.Columns.Add(new DataColumn("NO", typeof(int)));

                // 지역명 넣기 
                prepareTable.Columns.Add(new DataColumn("region_name", typeof(string)));
                /*
                for (int i = 0; i < prepareTable.Rows[0].ItemArray.Length; i++)
                {
                    Debug.Log(prepareTable.Rows[0][i].ToString());
                }
                */

                // Debug.Log("prepareTable.Rows[0] : " + prepareTable.Rows[0].ToString());        


                //UpdateDuplicateCount();

                await UpdateDuplicateCountAsync();
                
                SetListView();

                Debug.Log($"[Action_FileItem] [_path] : {_path}");

            }

        }

        mainManager.viewLoading.Close();

    }

    public static DataTable OrderByTable(DataTable dt, string sortname, string sortTarget)
    {
        DataTable sortDt = dt.Clone();

        //String 형식을 Int 정수 형식으로 변경
        sortDt.Columns[sortname].DataType = Type.GetType("System.Int32");

        foreach (DataRow dr in dt.Rows)
        {
            sortDt.ImportRow(dr);
        }

        sortDt.AcceptChanges();

        DataView dv = sortDt.DefaultView;
        dv.Sort = sortTarget;

        return dv.ToTable();
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
        var children = contents.transform.GetComponentsInChildren<Item_Prepare>(true);

        foreach (var child in children)
        {
            //if (child == contents) continue;
            //child.parent = null;

            Destroy(child.gameObject);
        }

        list_Prepare.Clear();
        list_Toggles.Clear();
    }

    public void UpdateView()
    {
        Debug.Log("UPDATE VIEW");

        // 현재 정렬 버튼으로
        prepareTable.DefaultView.Sort = SortTarget;

        listTable = null;

        listTable = prepareTable.DefaultView.ToTable();

        string cellValue = "";
        string errorValue = "";

        //int startIndex = (currentPage - 1) * itemsPerPage;
        //int endIndex = Mathf.Min(startIndex + itemsPerPage, listTable.Rows.Count);

        //Debug.Log($"<color=yellow>UpdateView startIndex {startIndex}, endIndex {endIndex}</color>");

        ClearListView();

        // 미처리 항목만
        if (listMode == 1)
        {
            // 전체 리스트에서 미처리인 애들 모두

            DataRow[] selectedRows = listTable.Select("Error = 1");

            selectrowCount = selectedRows.Count();

            int startIndex = (currentPage - 1) * itemsPerPage;
            int endIndex = Mathf.Min(startIndex + itemsPerPage, selectedRows.Length);

            Debug.Log($"<color=yellow>UpdateView startIndex {startIndex}, endIndex {endIndex}</color>");

            //for (var rowIndex = 1; rowIndex < listCount + 1; rowIndex++)
            for (var rowIndex = startIndex; rowIndex < endIndex; rowIndex++)
            {
                // 행 가져오기
                var slot = selectedRows[rowIndex];

                isSelect = bool.Parse(slot["isSelect"].ToString());

                // Debug.Log(" 에러코드는 : " + slot["Error"].ToString());
                errorValue = slot["Error"].ToString();

                if (errorValue == "1")
                {
                    Item_Prepare item = Instantiate(item_Prepare, contents);

                    // 그룸 하면 안됨
                    // item.toggle.group = toggleGroups;
                   
                    item.id = slot["NO"].ToString();

                    item.no.text = slot["NO"].ToString();
                    //item.no.text = (rowIndex + 1).ToString();

                    item.aptNum.text = slot["주택관리번호"].ToString();
                    item.aptName.text = slot["주택명"].ToString();
                    item.aptDong.text = slot["동"].ToString();
                    item.aptHosu.text = slot["호수"].ToString();

                    // 지역명 
                    // cellValue = slot["공급위치_시군구코드"].ToString().Trim();

                    item.region.text = slot["region_name"].ToString();

                    item.userName.text = slot["성명"].ToString();

                    item.aptdate.text = currentTime;

                    item.relation.text = slot["세대주관계"].ToString();

                    // 오류 처리
                    // 미처리

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

                    item.btn_Select.targetGraphic.enabled = true;
                    item.errorCheck.text = dropdown_error.options[1].text;

                    item.toggle.isOn = isSelect;
                    item.isCheck = item.toggle.isOn;

                    item.viewPrepare = this;


                    item.Init();

                    list_Toggles.Add(item.toggle);
                    list_Prepare.Add(item);
                }
            }
        }

        // 수동처리
        else if (listMode == 2)
        {
            DataRow[] selectedRows = listTable.Select("Error = 2");

            selectrowCount = selectedRows.Count();

            int startIndex = (currentPage - 1) * itemsPerPage;
            int endIndex = Mathf.Min(startIndex + itemsPerPage, selectedRows.Length);
                        
            for (var rowIndex = startIndex; rowIndex < endIndex; rowIndex++)
            {
                // 행 가져오기
                var slot = selectedRows[rowIndex];

                isSelect = bool.Parse(slot["isSelect"].ToString());

                //if (slot["Error"].ToString().Equals("2"))
                //{

                //}

                Debug.Log(" 에러코드는 : " + slot["Error"].ToString());

                Item_Prepare item = Instantiate(item_Prepare, contents);

                item.id = slot["NO"].ToString();
                item.no.text = slot["NO"].ToString();
                //item.no.text = (rowIndex + 1).ToString();

                item.aptNum.text = slot["주택관리번호"].ToString();
                item.aptName.text = slot["주택명"].ToString();
                item.aptDong.text = slot["동"].ToString();
                item.aptHosu.text = slot["호수"].ToString();


                // 지역명 
                // cellValue = slot["공급위치_시군구코드"].ToString().Trim();

                item.region.text = slot["region_name"].ToString();

                item.userName.text = slot["성명"].ToString();

                item.aptdate.text = currentTime;

                item.relation.text = slot["세대주관계"].ToString();

                // 오류처리
                // 수동처리
                item.errorCheck.text = dropdown_error.options[2].text;

                // 선택한거
                item.toggle.isOn = isSelect;
                item.isCheck = item.toggle.isOn;

                item.viewPrepare = this;

                item.Init();
                list_Toggles.Add(item.toggle);
                list_Prepare.Add(item);
            }
        }

        // 정상
        else if (listMode == 3)
        {
            DataRow[] selectedRows = listTable.Select("Error = 3");

            selectrowCount = selectedRows.Count();

            int startIndex = (currentPage - 1) * itemsPerPage;
            int endIndex = Mathf.Min(startIndex + itemsPerPage, selectedRows.Length);

            for (var rowIndex = startIndex; rowIndex < endIndex; rowIndex++)
            {
                // 행 가져오기
                var slot = selectedRows[rowIndex];

                isSelect = bool.Parse(slot["isSelect"].ToString());

                //if (slot["Error"].ToString().Equals("3"))
                //{
                    
                //}

                Item_Prepare item = Instantiate(item_Prepare, contents);
                
                item.id = slot["NO"].ToString();

                item.no.text = slot["NO"].ToString();
                //item.no.text = (rowIndex + 1).ToString();

                item.aptNum.text = slot["주택관리번호"].ToString();
                item.aptName.text = slot["주택명"].ToString();
                item.aptDong.text = slot["동"].ToString();
                item.aptHosu.text = slot["호수"].ToString();


                // 지역명 
                // cellValue = slot["공급위치_시군구코드"].ToString().Trim();

                item.region.text = slot["region_name"].ToString();

                item.userName.text = slot["성명"].ToString();

                item.aptdate.text = currentTime;

                item.relation.text = slot["세대주관계"].ToString();

                // 오류 처리
                // 정상                    
                item.errorCheck.text = dropdown_error.options[3].text;

                // 선택한거
                item.toggle.isOn = isSelect;

                item.isCheck = item.toggle.isOn;

                item.viewPrepare = this;

                item.Init();
                list_Toggles.Add(item.toggle);
                list_Prepare.Add(item);
            }
        }

        // 제외
        else if (listMode == 4)
        {
            DataRow[] selectedRows = listTable.Select("Error = 4");

            selectrowCount = selectedRows.Count();

            int startIndex = (currentPage - 1) * itemsPerPage;
            int endIndex = Mathf.Min(startIndex + itemsPerPage, selectedRows.Length);

            for (var rowIndex = startIndex; rowIndex < endIndex; rowIndex++)
            {
                // 행 가져오기
                var slot = selectedRows[rowIndex];

                isSelect = bool.Parse(slot["isSelect"].ToString());

                //if (slot["Error"].ToString().Equals("4"))
                //{
                    
                //}

                Item_Prepare item = Instantiate(item_Prepare, contents);

                item.id = slot["NO"].ToString();
                item.no.text = slot["NO"].ToString();
                //item.no.text = (rowIndex + 1).ToString();

                item.aptNum.text = slot["주택관리번호"].ToString();
                item.aptName.text = slot["주택명"].ToString();
                item.aptDong.text = slot["동"].ToString();
                item.aptHosu.text = slot["호수"].ToString();

                // 지역명 
                // cellValue = slot["공급위치_시군구코드"].ToString().Trim();

                item.region.text = slot["region_name"].ToString();

                item.userName.text = slot["성명"].ToString();

                item.aptdate.text = currentTime;

                item.relation.text = slot["세대주관계"].ToString();

                // 오류 처리                    
                // 제외
                item.errorCheck.text = dropdown_error.options[4].text;
                item.toggle.interactable = false;
                item.relation.gameObject.transform.parent.GetComponent<ProceduralImage>().enabled = false;
                item.region.gameObject.transform.parent.GetComponent<ProceduralImage>().enabled = false;

                // 선택한거
                //item.toggle.isOn = isSelect;

                // 제외는 애초에 선택이 안된다.

                item.toggle.isOn = false;

                item.isCheck = item.toggle.isOn;

                item.viewPrepare = this;

                item.Init();
                list_Toggles.Add(item.toggle);
                list_Prepare.Add(item);

            }
        }
        else
        {
            // 오류여부가 0 일때


            selectrowCount = listTable.Rows.Count;

            int startIndex = (currentPage - 1) * itemsPerPage;
            int endIndex = Mathf.Min(startIndex + itemsPerPage, listTable.Rows.Count);

            for (var rowIndex = startIndex; rowIndex < endIndex; rowIndex++)
            {
                // Debug.Log("rowIndex : " + rowIndex);

                // 행 가져오기
                var slot = listTable.Rows[rowIndex];

                Item_Prepare item = Instantiate(item_Prepare, contents);

                // 그룸 하면 안됨
                // item.toggle.group = toggleGroups;

                item.id = slot["NO"].ToString();

                item.no.text = slot["NO"].ToString();
                //item.no.text = (rowIndex + 1).ToString();

                item.aptNum.text = slot["주택관리번호"].ToString();
                item.aptName.text = slot["주택명"].ToString();
                item.aptDong.text = slot["동"].ToString();
                item.aptHosu.text = slot["호수"].ToString();

                // 지역명 
                // cellValue = slot["공급위치_시군구코드"].ToString().Trim();

                item.region.text = slot["region_name"].ToString();

                item.userName.text = slot["성명"].ToString();

                item.aptdate.text = currentTime;
                
                item.relation.text = slot["세대주관계"].ToString();


                errorValue = slot["Error"].ToString();
                // 오류 처리
                if (errorValue == "1")
                {
                    // 에러 표시
                    item.btn_Select.targetGraphic.enabled = true;
                    item.errorCheck.text = dropdown_error.options[1].text;

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
                    item.errorCheck.text = dropdown_error.options[2].text;
                }
                // 정상
                else if (errorValue == "3")
                {
                    item.errorCheck.text = dropdown_error.options[3].text;
                }
                // 제외
                else if (errorValue == "4")
                {
                    item.errorCheck.text = dropdown_error.options[4].text;
                    // 선택 비활성화
                    item.toggle.interactable = false;
                    item.relation.gameObject.transform.parent.GetComponent<ProceduralImage>().enabled = false;
                    item.region.gameObject.transform.parent.GetComponent<ProceduralImage>().enabled = false;

                    item.btn_Select.targetGraphic.enabled = false;

                    slot["isSelect"] = "false";
                }

                item.toggle.isOn = bool.Parse(slot["isSelect"].ToString());
                item.isCheck = item.toggle.isOn;

                item.viewPrepare = this;

                item.Init();
                list_Toggles.Add(item.toggle);
                list_Prepare.Add(item);
            }
        }

        if (list_Prepare.Count == 0)
        {
            item_Empty.SetActive(true);
        }
        else
        {
            item_Empty.SetActive(false);
        }

        // totalCount = prepareTable.Rows.Count;

        // beforeCount = totalCount;

        // passCount = totalCount - exceptCount;

        ResetView();

        // getCount();

        SetUpdateUI();

        if (list_Prepare.Count > 0)
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

    // 재 계산이 필요할때
    public void UpdateListView()
    {
        ClearListView();

        string errorValue = "";

        // NO로 정렬 지정
        Clear_Sort();

        unSolvedCount = 0;

        CheckError();

        // 오름차순
        // prepareTable.Columns[0].DataType = Type.GetType("System.Int32");

        // prepareTable.DefaultView.Sort = "주택관리번호 ASC";

        // 기본 차순 NO로 오름 차순 정렬한다.
        prepareTable.DefaultView.Sort = SortTarget;

        listTable = null;

        listTable = prepareTable.DefaultView.ToTable();
        
        //for (var rowIndex = 1; rowIndex < listCount + 1; rowIndex++)
        for (var rowIndex = 0; rowIndex < listCount; rowIndex++)
        {
            // 행 가져오기
            var slot = listTable.Rows[rowIndex];

            Item_Prepare item = Instantiate(item_Prepare, contents);

            // 그룸 하면 안됨
            // item.toggle.group = toggleGroups;
            item.id = slot["NO"].ToString();

            item.no.text = slot["NO"].ToString();
            //item.no.text = (rowIndex+1).ToString();

            item.aptNum.text = slot["주택관리번호"].ToString();
            item.aptName.text = slot["주택명"].ToString();
            item.aptDong.text = slot["동"].ToString();
            item.aptHosu.text = slot["호수"].ToString();


            // 지역명 
            // cellValue = slot["공급위치_시군구코드"].ToString().Trim();

            item.region.text = slot["region_name"].ToString();
                
            item.userName.text = slot["성명"].ToString();

            item.aptdate.text = currentTime;

            item.relation.text = slot["세대주관계"].ToString();

            // 오류 처리 
            // 미처리
            errorValue = slot["Error"].ToString();

            if (errorValue == "1")
            {
                // 에러 표시
                item.btn_Select.targetGraphic.enabled = true;
                item.errorCheck.text = dropdown_error.options[1].text;

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
                item.errorCheck.text = dropdown_error.options[2].text;
            }
            // 정상
            else if (errorValue == "3")
            {
                item.errorCheck.text = dropdown_error.options[3].text;
            }
            // 제외
            else if (errorValue == "4")
            {
                item.errorCheck.text = dropdown_error.options[4].text;
                // 선택 비활성화
                item.toggle.interactable = false;

                item.relation.gameObject.transform.parent.GetComponent<ProceduralImage>().enabled = false;
                item.region.gameObject.transform.parent.GetComponent<ProceduralImage>().enabled = false;

                item.btn_Select.targetGraphic.enabled = false;

                slot["isSelect"] = "false";
            }

            //DataRow rows = prepareTable.Select("NO = " + item.no.text.ToString()).FirstOrDefault();

            //if(rows != null)
            //{
            //    rows["Error"] = slot["Error"];
            //}

            //prepareTable.AcceptChanges();

            item.toggle.isOn = bool.Parse(slot["isSelect"].ToString());

            item.isCheck = item.toggle.isOn;

            item.viewPrepare = this;

            item.Init();
            list_Toggles.Add(item.toggle);
            list_Prepare.Add(item);
        }

        totalCount = prepareTable.Rows.Count;
        beforeCount = totalCount;

        if (list_Prepare.Count == 0)
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

    }

    // 특정값 칼럼 갯수 구하기
    public int CountRowsWithCondition(DataTable table, string columnName, string value)
    {
        string filterExpression = $"{columnName} = '{value}'";
        DataRow[] filteredRows = table.Select(filterExpression);
        int count = filteredRows.Length;

        return count;
    }
    
    public void UpdateDuplicateCount()
    {
        Debug.Log("UpdateDuplicateCount");

        // 중복된 행의 개수를 구할 컬럼 이름
        string addressColumnName = "행정주소";
        // 주소중복횟수 칼럼 이름
        string duplicateCountColumnName = "주소중복횟수";

        // 중복된 행 찾기
        var duplicatesQuery = from row in prepareTable.AsEnumerable()
                              group row by row.Field<string>(addressColumnName) into g
                              where g.Count() > 1
                              select new
                              {
                                  Address = g.Key,
                                  DuplicateCount = g.Count()
                              };

        // 주소중복횟수 칼럼 추가 및 데이터 넣기
        // prepareTable.Columns.Add(duplicateCountColumnName, typeof(int));

        foreach (var duplicate in duplicatesQuery)
        {
            var rows = prepareTable.AsEnumerable()
                                .Where(row => row.Field<string>(addressColumnName) == duplicate.Address);

            

            foreach (var row in rows)
            {
                //Debug.Log("UpdateDuplicateCount Address : " + duplicate.Address);

                //Debug.Log("UpdateDuplicateCount duplicate : " + duplicate.DuplicateCount);
                //Debug.Log("UpdateDuplicateCount row : " + row["성명"]);

                row[duplicateCountColumnName] = duplicate.DuplicateCount;
            }
        }
    }

    public async Task UpdateDuplicateCountAsync()
    {
        Debug.Log("UpdateDuplicateCount");

        // 중복된 행의 개수를 구할 컬럼 이름
        string addressColumnName = "행정주소";
        // 주소중복횟수 칼럼 이름
        string duplicateCountColumnName = "주소중복횟수";

        // 중복된 행 찾기
        var duplicatesQuery = from row in prepareTable.AsEnumerable()
                              group row by row.Field<string>(addressColumnName) into g
                              where g.Count() > 1
                              select new
                              {
                                  Address = g.Key,
                                  DuplicateCount = g.Count()
                              };

        // 주소중복횟수 칼럼 추가 및 데이터 넣기
        // prepareTable.Columns.Add(duplicateCountColumnName, typeof(int));

        foreach (var duplicate in duplicatesQuery)
        {
            await Task.Delay(1);

            var rows = prepareTable.AsEnumerable()
                                .Where(row => row.Field<string>(addressColumnName) == duplicate.Address);

            foreach (var row in rows)
            {
                //Debug.Log("UpdateDuplicateCount Address : " + duplicate.Address);

                //Debug.Log("UpdateDuplicateCount duplicate : " + duplicate.DuplicateCount);
                //Debug.Log("UpdateDuplicateCount row : " + row["성명"]);

                row[duplicateCountColumnName] = duplicate.DuplicateCount;
            }
        }
    }

    public void SetListView()
    {
        Debug.Log("SetListView");

        ClearListView();
        
        string errorValue = "";        

        // NO로 정렬 지정
        Clear_Sort();

        unSolvedCount = 0;

        for (var rowIndex = 0; rowIndex < prepareTable.Rows.Count; rowIndex++)
        {
            // 행 가져오기
            var slot = prepareTable.Rows[rowIndex];

            slot["isSelect"] = "false";

            slot["Error"] = 3; // 기본은 정상으로 // 1:미처리, 2: 수동처리, 3:정상, 4:제외

            slot["NO"] = rowIndex+1;

            // Debug.Log($"slot : {rowIndex}  = " + slot["Ex"].ToString());

            // Debug.Log($"Slot : {slot["주택관리번호"].GetType()} , {slot["주택관리번호"].ToString()}");

            //if (slot["ad_변경일자"].ToString().Trim().Length == 0)
            //{
            //    slot["ad_변경일자"] = slot["변경일자"];
            //}
        }


        // 저장
        // SaveFile();

        // 에러 채크
        CheckError();

        // 오름차순
        // prepareTable.Columns[0].DataType = Type.GetType("System.Int32");

        // prepareTable.DefaultView.Sort = "주택관리번호 ASC";


        // 기본 차순 NO로 오름 차순 정렬한다.
        prepareTable.DefaultView.Sort = SortTarget;

        listTable = null;

        listTable = prepareTable.DefaultView.ToTable();
        int endIndex = Mathf.Min(listCount, listTable.Rows.Count);
                
        selectrowCount = listTable.Rows.Count;

        for (var rowIndex = 0; rowIndex < endIndex; rowIndex++)
        {
            // 행 가져오기
            var slot = listTable.Rows[rowIndex];

            Item_Prepare item = Instantiate(item_Prepare, contents);

            // 그룸 하면 안됨
            // item.toggle.group = toggleGroups;

            item.no.text = (rowIndex + 1).ToString();

            item.aptNum.text = slot["주택관리번호"].ToString();
            item.aptName.text = slot["주택명"].ToString();
            item.aptDong.text = slot["동"].ToString();
            item.aptHosu.text = slot["호수"].ToString();

            // 지역명 
            // cellValue = slot["공급위치_시군구코드"].ToString().Trim();

            item.region.text = slot["region_name"].ToString();

            item.userName.text = slot["성명"].ToString();

            item.aptdate.text = currentTime;

            item.relation.text = slot["세대주관계"].ToString();

            // 오류 처리 
            // 미처리
            errorValue = slot["Error"].ToString();

            if (errorValue == "1")
            {
                // 에러 표시
                item.btn_Select.targetGraphic.enabled = true;
                item.errorCheck.text = dropdown_error.options[1].text;

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
                item.errorCheck.text = dropdown_error.options[2].text;
            }
            // 정상
            else if (errorValue == "3")
            {
                item.errorCheck.text = dropdown_error.options[3].text;
            }
            // 제외
            else if (errorValue == "4")
            {
                item.errorCheck.text = dropdown_error.options[4].text;
                // 선택 비활성화
                item.toggle.interactable = false;
                item.relation.gameObject.transform.parent.GetComponent<ProceduralImage>().enabled = false;
                item.region.gameObject.transform.parent.GetComponent<ProceduralImage>().enabled = false;

                item.btn_Select.targetGraphic.enabled = false;

                slot["isSelect"] = "false";
            }

            // 이걸 한 이유가 뭐였을까..
            //var rows = prepareTable.Select("NO = " + item.no.text.ToString());

            //rows[0]["Error"] = slot["Error"];

            //item.toggle.isOn = bool.Parse(slot["isSelect"].ToString());
            
            item.toggle.isOn = false;

            item.isCheck = false;

            item.viewPrepare = this;

            item.Init();

            list_Toggles.Add(item.toggle);
            list_Prepare.Add(item);
        }
        
        totalCount = prepareTable.Rows.Count;

        beforeCount = totalCount;

        if (list_Prepare.Count == 0)
        {
            item_Empty.SetActive(true);
        }
        else
        {
            item_Empty.SetActive(false);
        }

        getCount();

        ResetView();

        // 처음으로 세팅
        GoToFirstPage();

        SetUpdateUI();

        if (list_Prepare.Count > 0)
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

        for (var rowIndex = 0; rowIndex < prepareTable.Rows.Count; rowIndex++)
        {           
            var slot = prepareTable.Rows[rowIndex];


            // 시군구 코드 에러 체크
            cellValue = slot["공급위치_시군구코드"].ToString().Trim();

            var res = getMapName(cellValue);

            //if(rowIndex < 11)
            //Debug.Log("RES :" + res);

            if(res.Length > 0)
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
                    slot["Error"] = 4;
                }
                else
                {
                    //var results = listFamily.Where(x => x.title.Contains(slot["세대주관계"].ToString()) || x.member.Any(y => y.Contains(slot["세대주관계"].ToString())));
                    List<string> result = listFamily.Where(famData => famData.member.Any(member => member.ToLower().Contains(cellValue))).Select(famData => famData.title).ToList();

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
                // slot["relation_status"] = 1;                
                slot["Error"] = 4;
            }

            // Debug.Log("<color=yellow>CheckError : " + slot["Error"].ToString() + "</color>");

            //// 행정번경
            cellValue = slot["행정변경"].ToString().Trim();
            // Debug.Log("행정변경 : "+ cellValue);

            // Debug.Log("행정변경 IsNullOrEmpty " + string.IsNullOrEmpty(cellValue));

            if (cellValue == "#N/A" || string.IsNullOrEmpty(cellValue))
            {
                // #N/A일 경우  에러 미처리로 표시
                slot["Error"] = "4";
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
                    if (slot["Error"].ToString() == "2")
                    {
                        slot["Error"] = 2;
                    }
                    else
                    {
                        slot["Error"] = 3;
                    }
                }
            }

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

        errorRelationCount = CountRowsWithCondition(prepareTable, "region_status", "1");

        errorRegionCount = CountRowsWithCondition(prepareTable, "relation_status", "1");

        if(errorRegionCount > 0)
        {
            isErrorRegion = true;
        }
        else
        {
            isErrorRegion = false;
        }
         
        if(errorRelationCount > 0)
        {
            isErrorRelation = true;
        }
        else
        {
            isErrorRelation = false;
        }
    }

    public void SaveFile()
    {
        string filePath = Path.Combine(Application.dataPath, "dataframe2.csv");
        
        SaveDataTableToCsv(prepareTable, filePath);
    }

    private void SaveDataTableToCsv(DataTable dataTable, string filePath)
    {
        // StreamWriter를 이용하여 CSV 파일 생성
        using (StreamWriter streamWriter = new StreamWriter(filePath, false, Encoding.GetEncoding("EUC-KR")))
        {
            /*List<string> headers = new List<string>();
            foreach (DataColumn column in dataTable.Columns)
            {
                headers.Add(column.ColumnName);
            }
            streamWriter.WriteLine(string.Join(",", headers));*/
            
            // 헤더 쓰기
            streamWriter.WriteLine(string.Join(",", col));

            // 데이터 쓰기
            foreach (DataRow row in dataTable.Rows)
            {
                List<string> values = new List<string>();
                foreach (DataColumn column in dataTable.Columns)
                {
                    if (row[column].ToString().Contains(","))
                    {

                        string replace = row[column].ToString();
                        if (replace.Contains("\""))
                        {
                            row[column] = replace.Replace("\"", "");
                        }

                        // Debug.Log("쉼표가 있어" + replace);                       

                        row[column] = $"\"{row[column]}\"";
                    }

                    values.Add(row[column].ToString());
                }
                streamWriter.WriteLine(string.Join(",", values));
            }
        }
    }

    public void AddMemberToFamily(string memberTitle, string memberName)
    {
        FamData existingFamData = listFamily.Find(f => f.title == memberTitle);

        if (existingFamData != null && Array.IndexOf(existingFamData.member, memberName) == -1)
        {
            // Add memberName to the existing FamData's member array
            List<string> members = new List<string>(existingFamData.member);
            members.Add(memberName);
            existingFamData.member = members.ToArray();
        }
    }

    public void RemoveMemberFromFamily(string memberTitle, string memberName)
    {
        FamData existingFamData = listFamily.Find(f => f.title == memberTitle);

        if (existingFamData != null)
        {
            // Find the index of the memberName in the existing FamData's member array
            int memberIndex = Array.IndexOf(existingFamData.member, memberName);

            if (memberIndex != -1)
            {
                // Remove the memberName from the existing FamData's member array
                List<string> members = new List<string>(existingFamData.member);
                members.RemoveAt(memberIndex);
                existingFamData.member = members.ToArray();
            }
        }
    }

    public bool Check_Error(string data)
    {
        return false;    
    }

    public void WriteResult(string[] paths)
    {
        if (paths.Length == 0)
        {
            return;
        }

        _path = "";
        //foreach (var p in paths)
        //{
        //    _path += @""+ p + "\n";
        //}

        _path = paths[0];

        Debug.Log($"[WriteResult] [_path] : {_path}");
        Debug.Log($"[WriteResult] [_path] : " + Path.GetFileName(_path));
    }

    public void WriteResult(string path)
    {
        _path = path;
    }


    private void CopyFile(string sourcePath, string destinationPath)
    {
        try
        {
            // 파일 복사
            File.Copy(sourcePath, destinationPath, true);
            //Debug.Log("파일이 성공적으로 복사되었습니다.");
        }
        catch (IOException e)
        {
            Debug.LogError("파일 복사 중 오류가 발생했습니다: " + e.Message);
        }
    }

}
