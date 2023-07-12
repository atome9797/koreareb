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

public class ViewConnectHistory : MonoBehaviour
{
    public string MenuName = "접속 이력";

    public MainManager mainManager;

    public ServerManager serverManager;

    public Item_ConnectLog item_connection_log;

    public GameObject item_Empty;

    public Transform contents;

    public Transform parent;

    public RectTransform menuContent;
    public RectTransform searchContent;



    //검색 조건
    SelectMenu SelectMenu;

    // 초기화
    public Button btn_Reset;

    // 조회
    public Button btn_Search;

    public DataTable CheckTable;//조회한 데이터 테이블
    public DataTable listTable;//sort 해서 가져오는 테이블


    //사용자 정보
    List<Item_ConnectLog> list_connection_log;//단지 스크립트

    bool[] _dropdownType = new bool[4];//검색구분

    //여기부터가 페이지 기능
    public Button prevButton; // 이전 10 페이지로 이동하는 버튼
    public Button nextButton; // 다음 10 페이지로 이동하는 버튼

    public List<Button> pageButtons; // 페이지 번호를 나타내는 버튼 리스트

    public Button firstPageButton; // 첫 페이지로 이동하는 버튼
    public Button lastPageButton; // 마지막 페이지로 이동하는 버튼

    // DropDown
    public TMP_Dropdown dropdown_count;

    // 전체항목
    public TextMeshProUGUI Text_totalCount;


    public int itemsPerPage = 10;


    public int currentPage = 1; // 현재 페이지
    public RectTransform pageContents;

    public RectTransform[] viewupdate;

    public int listCount;
    public int listMode;
    // 전체항목
    public int totalCount;


    //조회 건수 
    public int[] listCounts = { 10, 20, 50, 100 };

    //엑셀 다운로드 버튼
    public Button AccelButton;

    
    public string[] colname = { "NO", "ID", "성명","부서","직책/직급","접속일시" };

    void Awake()
    {
        list_connection_log = new List<Item_ConnectLog>(); //사용자 정보
        SelectMenu = gameObject.GetComponent<SelectMenu>();//메뉴 조건
        dropdown_count.onValueChanged.AddListener(delegate { ListCountDropdownValueChanged(dropdown_count); });
    }


    private void Start()
    {
        // 페이지 리스너 등록
        prevButton.onClick.AddListener(Previous10Pages);
        nextButton.onClick.AddListener(Next10Pages);
        firstPageButton.onClick.AddListener(GoToFirstPage);
        lastPageButton.onClick.AddListener(GoToLastPage);

        //엑셀 버튼
        AccelButton.onClick.AddListener(Action_Excel);


        for (int i = 0; i < pageButtons.Count; i++)
        {
            int pageNumber = ((currentPage - 1) / 10) * 10 + i + 1;
            pageButtons[i].onClick.AddListener(() => GoToPage(pageNumber));
        }

        Reset();
        dropdown_count.value = 0;


        // 조회
        btn_Search.onClick.AddListener(Action_Search);

        // 초기화
        btn_Reset.onClick.AddListener(Action_Reset);

    }

    /// <summary>
    /// 엑셀 파일 로드
    /// </summary>
    public void Action_Excel()
    {
        string[] fileTypes = new string[] { "CSV Files", "csv" };

        StandaloneFileBrowser.SaveFilePanelAsync("엑셀 다운로드", "", "", "csv", (string path) =>
        {
            if (!string.IsNullOrEmpty(path))
            {
                // 파일 저장 로직을 구현하세요
                // 예를 들어, 파일을 생성하고 내용을 저장할 수 있습니다.
                // File.WriteAllText(path, "Hello, World!");

                //CheckTable.Columns.RemoveAt(0);

                SaveDataTableToCsv(CheckTable, path, colname);

                //mainManager.DownloadLodInsert(MenuName, $"{SelectMenu.dateText.Canlendar.text} 엑셀다운로드");

                string names = Path.GetFileNameWithoutExtension(path);
                mainManager.DownloadLodInsert(MenuName, $"{names} 엑셀다운로드");

                Debug.Log("File saved at: " + path);
            }
            else
            {
                Debug.Log("Save file operation cancelled.");
            }
        });

    }

    public void SaveDataTableToCsv(DataTable dataTable, string filePath, string [] columname)
    {
        // StreamWriter를 이용하여 CSV 파일 생성
        using (StreamWriter streamWriter = new StreamWriter(filePath, false, Encoding.GetEncoding("EUC-KR")))
        {
            streamWriter.WriteLine(string.Join(",", columname));

            // 데이터 쓰기
            foreach (DataRow row in dataTable.Rows)
            {
                List<string> values = new List<string>();

                foreach (DataColumn column in dataTable.Columns)
                {
                    if (row[column].ToString().Contains(","))
                    {
                        row[column] = $"\"{row[column]}\"";
                    }
                }

                //row
                values.Add(row["NO"].ToString());
                values.Add(row["mem_userid"].ToString());
                values.Add(row["mem_name"].ToString());
                values.Add(row["mem_depart"].ToString());
                values.Add(row["mem_title"].ToString());
                values.Add(row["날짜"].ToString());

                streamWriter.WriteLine(string.Join(",", values));
            }

        }
    }

    // 검색 조회
    public void Action_Search()
    {

        //리스트 조회 초기화
        ClearListView();

        CheckTable = new DataTable();

        SelectMenu.DropdownType(_dropdownType); //검색구분

        CheckTable = serverManager.GetLog("tb_connect_log", SelectMenu.dateText.FromDate, SelectMenu.dateText.ToDate, _dropdownType, SelectMenu.input_search.text);


        GoToFirstPage();
    }

    // 초기화
    public void Action_Reset()
    {
        //초기화 모달 띄워주기
        // ResetModal.SetActive(true);

        CommonPopup commonPopup = Instantiate(mainManager.commonPopup, mainManager.contents);

        commonPopup.ShowPopup_TwoButton(
            "모든 조회설정이 초기화 됩니다.\n" +
            "진행하시겠습니까 ? ",
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
                ResetButton(true);

                commonPopup.CloseDestroy();
            });


    }

    private void ResetButton(bool check)
    {
        //true일때 초기화 아니면 setActive false
        if (check)
        {
            Reset();
            dropdown_count.value = 0;

            ClearListView();

            // 기본 차순 NO로 오름 차순 정렬한다.
            CheckTable = null;
            listTable = null;

            item_Empty.SetActive(true);

            //selectMenu reset
            SelectMenu.ResetData();
        }

        //ResetModal.SetActive(false);
    }

    public void Reset()
    {
        currentPage = 1;

        listCount = 10;
        itemsPerPage = listCount;
        totalCount = 0;
    }

    /// <summary>
    /// 건수 토글 선택
    /// </summary>
    /// <param name="change"></param>
    public void ListCountDropdownValueChanged(TMP_Dropdown change)
    {
        Debug.Log("New Value : " + change.value);

        listCount = listCounts[change.value];

        itemsPerPage = listCount;


        // 파일 아이템이 있을 경우에만 새로 그린다
        if (CheckTable != null)
        {
            UpdateListView();
        }
    }

    // 재 계산이 필요할때
    public void UpdateListView()
    {
        ClearListView();

        //CheckError();

        listTable = null;
        listTable = CheckTable.DefaultView.ToTable();

        totalCount = CheckTable.Rows.Count;

        getCount();

        ResetView();

        GoToFirstPage();

    }

    /// <summary>
    /// 리스트 초기화
    /// </summary>
    public void ClearListView()
    {
        var children = contents.transform.GetComponentsInChildren<Item_ConnectLog>(true);

        foreach (var child in children)
        {
            Destroy(child.gameObject);
        }

        list_connection_log.Clear();
    }

    void Previous10Pages()
    {
        if (CheckTable != null)
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
        if (CheckTable != null)
        {
            currentPage += 10;
            int maxPage = Mathf.CeilToInt((float)CheckTable.Rows.Count / itemsPerPage);
            if (currentPage > maxPage)
                currentPage = maxPage;

            UpdateUI();
        }
    }

    // 첫 페이지로 이동하는 함수
    public void GoToFirstPage()
    {
        if (CheckTable != null)
        {
            currentPage = 1;
            UpdateUI();
        }
    }

    // 마지막 페이지로 이동하는 함수
    void GoToLastPage()
    {
        if (CheckTable != null)
        {
            int maxPage = Mathf.CeilToInt((float)CheckTable.Rows.Count / itemsPerPage);
            currentPage = maxPage;
            UpdateUI();
        }
    }

    // 특정 페이지로 이동하는 함수
    void GoToPage(int pageNumber)
    {
        if (CheckTable != null)
        {
            Debug.Log(" GOTOPAGE :" + pageNumber);

            int maxPage = Mathf.CeilToInt((float)CheckTable.Rows.Count / itemsPerPage);
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

    // UI 업데이트 함수
    void UpdateUI()
    {
        // 페이지 번호 버튼 업데이트
        int maxPage = Mathf.CeilToInt((float)CheckTable.Rows.Count / itemsPerPage);

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
        //=> 사용시에는 list 초기화 및 재할당이 이루어짐으로 미리 view에서 clearListView함수 호출할것
        if (CheckTable != null)
        {
            UpdateView();
        }
    }

    public void UpdateView()
    {


        listTable = null;
        listTable = CheckTable.DefaultView.ToTable();

        //이부분에 초기화 및 재생성 함수 들어감
        ClearListView();


        DataRow[] selectedRows = listTable.Select();

        int startIndex = (currentPage - 1) * itemsPerPage;
        int endIndex = Mathf.Min(startIndex + itemsPerPage, selectedRows.Length);

        for (var rowIndex = startIndex; rowIndex < endIndex; rowIndex++)
        {
            var row = selectedRows[rowIndex];

            Item_ConnectLog item = Instantiate(item_connection_log, contents);

            item.no.text = row["NO"].ToString();
            item.user_id.text = row["mem_userid"].ToString();
            item.user_name.text = row["mem_name"].ToString();
            item.part.text = row["mem_depart"].ToString();
            item.position.text = row["mem_title"].ToString();
            item.connection_log_date.text = row["날짜"].ToString();

            list_connection_log.Add(item);
        }

        if (list_connection_log.Count == 0)
        {
            item_Empty.SetActive(true);
            AccelButton.interactable = false;
        }
        else
        {
            item_Empty.SetActive(false);
            AccelButton.interactable = true;
        }

        totalCount = CheckTable.Rows.Count;
        ResetView();
        getCount();

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

    public void getCount()
    {

        Text_totalCount.text = "전체항목(" + mainManager.FormatNumberWithSymbol(totalCount) + ")";


        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)Text_totalCount.gameObject.transform.parent);
    }


}
