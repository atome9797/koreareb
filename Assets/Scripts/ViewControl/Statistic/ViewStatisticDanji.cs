using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Data;
using SFB;
using System.IO;
using System.Text;

public class ViewStatisticDanji : MonoBehaviour
{
    public string MenuName = "통계 단지별";

    [SerializeField]
    MainManager mainManager;

    [SerializeField]
    ServerManager serverManager;

    [SerializeField]
    Item_BlockCheck item_Statistic_Danji;

    [SerializeField]
    GameObject item_Empty;

    public Transform contents;

    public Transform parent;

    public RectTransform menuContent;
    public RectTransform searchContent;

    public Vector2 openSize;
    public Vector2 closeSize;

    //public GameObject[] UI_Search;

    public bool isMenuOpen;

    public Button btn_menuOpen;
    public Button btn_menuClose;

    //검색 조건
    public SelectMenu SelectMenu;

    // 초기화
    public Button btn_Reset;

    // 조회
    public Button btn_Search;

    public DataTable CheckTable;//조회한 데이터 테이블
    public DataTable listTable;//sort 해서 가져오는 테이블


    List<Item_BlockCheck> list_Statistic_Danji;//단지 스크립트

    bool[] BustType = new bool[3];//적발내용
    bool[] RfxgType = new bool[3];//rfxg 타입
    bool[] PrecentType = new bool[4];//위험도 퍼센트

    //여기부터가 페이지 기능
    public Button prevButton; // 이전 10 페이지로 이동하는 버튼
    public Button nextButton; // 다음 10 페이지로 이동하는 버튼

    public List<Button> pageButtons; // 페이지 번호를 나타내는 버튼 리스트

    public Button firstPageButton; // 첫 페이지로 이동하는 버튼
    public Button lastPageButton; // 마지막 페이지로 이동하는 버튼

    // DropDown
    public TMP_Dropdown dropdown_sort;
    public TMP_Dropdown dropdown_count;


    // 전체항목
    public TextMeshProUGUI Text_totalCount;

    public string SortTarget; // 정렬 기준

    public int itemsPerPage = 10;


    public int currentPage = 1; // 현재 페이지
    public RectTransform pageContents;

    public RectTransform[] viewupdate;

    public int listCount;
    public int listMode;
    // 전체항목
    public int totalCount;

    //그래프 활성화
    public Button GraphButton;
    public GameObject GraphPopup;


    //조회 건수 
    public int[] listCounts = { 10, 20, 50, 100 };

    public string[] mapName = { "전국","서울특별시","부산광역시","대구광역시","인천광역시","광주광역시","대전광역시","울산광역시","세종특별자치시",
        "경기도","강원도","충청북도","충청남도","전라북도","전라남도","경상북도","경상남도","제주특별자치도" };

    public string[] colname = {"NO", "단지명", "지역", "위험도 (RF+XG)" ,"전체","위장전입","통장매매","기타", "모집공고일" };

    //엑셀 다운로드 버튼
    public Button AccelButton;

    bool[] CityMenu = new bool[18];

    
    private void Awake()
    {
        list_Statistic_Danji = new List<Item_BlockCheck>(); //사용자 정보
        SelectMenu = gameObject.GetComponent<SelectMenu>();//메뉴 조건

        dropdown_sort.onValueChanged.AddListener(delegate { SortDropdownValueChanged(dropdown_sort); });
        dropdown_count.onValueChanged.AddListener(delegate { ListCountDropdownValueChanged(dropdown_count); });

        GraphButton.onClick.AddListener(delegate {
            GraphPopup.SetActive(true);
            CitySetup(GraphPopup.GetComponent<ViewMapGraphPopup>().CityMenu, GraphPopup.GetComponent<ViewMapGraphPopup>().CityMenuBackup);
            GraphPopup.GetComponent<ViewMapGraphPopup>().Init();
        });

    }

    // Start is called before the first frame update
    void Start()
    {
        for (int i = 0; i < SelectMenu.cityToggles.Count; i++)
        {
            SelectMenu.cityToggles[i].toggle.onValueChanged.AddListener(SearchButtonActive);//기간 토글 클릭시 이벤트
        }
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
        dropdown_sort.value = 0;
        dropdown_count.value = 0;


        // 메뉴 펼치기
        btn_menuOpen.onClick.AddListener(Action_Open);

        // 메뉴 접기
        btn_menuClose.onClick.AddListener(Action_Close);


        // 조회
        btn_Search.onClick.AddListener(Action_Search);

        // 초기화
        btn_Reset.onClick.AddListener(Action_Reset);

    }

    public void SearchButtonActive(bool check)
    {
        SelectMenu.SeacrhButtonActiveCheck(SelectMenu.cityToggles, btn_Search);
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

                string localName = "";

                //전국으로 조회 안했을때
                if (CityMenu[0] == false)
                {
                    int count = 0;
                    string title = "";
                    for (int j = 0; j < CityMenu.Length; j++)
                    {
                        if (CityMenu[j])
                        {
                            if (count == 0)
                            {
                                title += mapName[j];
                            }
                            count++;
                        }
                    }

                    if (count > 1)
                    {
                        title += $" 외 {count - 1}지역";
                    }

                    localName = title;
                }
                else
                {
                    localName = "전국";
                }


                //mainManager.DownloadLodInsert(MenuName, $"{SelectMenu.dateText.Canlendar.text} {localName} 엑셀다운로드");

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
                    //지역코드를 변경해주기

                    if (row[column].ToString().Contains(","))
                    {
                        //Debug.Log("쉼표가 있어" + row[column].ToString());

                        row[column] = $"\"{row[column]}\"";
                    }

                    values.Add(row[column].ToString());
                    
                }


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

        BustType[0] = true; //부정청약 true

        SelectMenu.CheckRfxgType(RfxgType); //rfxg
        SelectMenu.CheckRfxgPerType(PrecentType); //퍼센트

        //검색 기간 
        GraphPopup.GetComponent<ViewMapGraphPopup>().Date = SelectMenu.dateText.Canlendar.text;

        CityMenu = SelectMenu.CheckCityToggle(SelectMenu.cityToggles);

        CheckTable = serverManager.GetStatisticsByDanji(SelectMenu.dateText.FromDate, SelectMenu.dateText.ToDate, CityMenu, BustType, RfxgType, PrecentType);

        //그래프는 조회 했을때의 토글을 기억하도록 함
        GraphPopup.GetComponent<ViewMapGraphPopup>().CheckCityToggle(GraphPopup.GetComponent<ViewMapGraphPopup>().CityMenu, SelectMenu.cityToggles);
        GraphPopup.GetComponent<ViewMapGraphPopup>().CheckCityToggle(GraphPopup.GetComponent<ViewMapGraphPopup>().CityMenuBackup, SelectMenu.cityToggles);


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
            dropdown_sort.value = 0;
            dropdown_count.value = 0;

            ClearListView();

            // 기본 차순 NO로 오름 차순 정렬한다.
            CheckTable = null;
            listTable = null;

            item_Empty.SetActive(true);

            GraphButton.interactable = false;

            //selectMenu reset
            //기간 초기화 2023.06.11 => 초기화 1년으로
            SelectMenu.periodToggle[1].isOn = true;
            SelectMenu.PriodChange(SelectMenu.periodToggle[1], 1);

            //도시 초기화
            SelectMenu.MenuChangedEvent(true);

            //적발내용 초기화
            SelectMenu.toggle_checkType[0].isOn = true;
            SelectMenu.toggle_prob[0].isOn = true;
            SelectMenu.toggle_rank[0].isOn = true;

        }

        //ResetModal.SetActive(false);
    }


    // 메뉴 펼치기
    public void Action_Open()
    {
        //for (int i = 0; i < UI_Search.Length; i++)
        //{
        //    UI_Search[i].SetActive(true);
        //}

        searchContent.sizeDelta = openSize;

        btn_menuOpen.gameObject.SetActive(false);
        btn_menuClose.gameObject.SetActive(true);

        isMenuOpen = true;

        LayoutRebuilder.ForceRebuildLayoutImmediate(menuContent);
        LayoutRebuilder.ForceRebuildLayoutImmediate(searchContent);

        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)parent);

    }

    // 메뉴 접기
    public void Action_Close()
    {
        //for (int i = 0; i < UI_Search.Length; i++)
        //{
        //    UI_Search[i].SetActive(false);
        //}

        searchContent.sizeDelta = closeSize;

        btn_menuOpen.gameObject.SetActive(true);
        btn_menuClose.gameObject.SetActive(false);

        isMenuOpen = false;

        LayoutRebuilder.ForceRebuildLayoutImmediate(menuContent);
        LayoutRebuilder.ForceRebuildLayoutImmediate(searchContent);

        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)parent);

    }

    /// <summary>
    /// 최신순 토글 선택
    /// </summary>
    /// <param name="change"></param>
    public void SortDropdownValueChanged(TMP_Dropdown change)
    {
        Debug.Log("sort : " + change.value);

        listMode = change.value;


        if (CheckTable != null)
        {
            UpdateListView();
        }
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


    /// <summary>
    /// 리스트 초기화
    /// </summary>
    public void ClearListView()
    {
        var children = contents.transform.GetComponentsInChildren<Item_BlockCheck>(true);

        foreach (var child in children)
        {
            Destroy(child.gameObject);
        }

        list_Statistic_Danji.Clear();
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

        //재생성 함수 들어갈곳

        
        switch (listMode)
        {
            case 0: SortTarget = "모집공고일 DESC, 지역 ASC"; break;
            case 1: SortTarget = "모집공고일 ASC, 지역 DESC"; break;
        }

        listTable.DefaultView.Sort = SortTarget;

        DataRow[] selectedRows = listTable.Select();


        int startIndex = (currentPage - 1) * itemsPerPage;
        int endIndex = Mathf.Min(startIndex + itemsPerPage, selectedRows.Length);

        for (var rowIndex = startIndex; rowIndex < endIndex; rowIndex++)
        {
            var row = selectedRows[rowIndex];

            Item_BlockCheck item = Instantiate(item_Statistic_Danji, contents);

            item.no.text = row["NO"].ToString();
            // item.no.text = rowIndex.ToString();

            item.blockName.text = row["단지명"].ToString();
            item.mapName.text = row["지역"].ToString();
            item.dfPoint.text = (Mathf.Floor(float.Parse(row["위험도"].ToString()) * 10f) / 10f).ToString("N1");//위험도 평균
            item.dfTotal.text = int.Parse(row["전체"].ToString()).ToString("N0");
            item.dfPaint.text = int.Parse(row["위장전입"].ToString()).ToString("N0");
            item.dfSales.text = int.Parse(row["통장매매"].ToString()).ToString("N0");
            item.dfOther.text = int.Parse(row["기타"].ToString()).ToString("N0");

            item.recannodate.text = mainManager.DateFormat(row["모집공고일"].ToString());

            list_Statistic_Danji.Add(item);
        }

        if (list_Statistic_Danji.Count == 0)
        {
            item_Empty.SetActive(true);
            GraphButton.interactable = false;
            AccelButton.interactable = false;
        }
        else
        {
            item_Empty.SetActive(false);
            GraphButton.interactable = true;
            AccelButton.interactable = true;
        }

        totalCount = CheckTable.Rows.Count;
        ResetView();
        getCount();

    }

    // 재 계산이 필요할때
    public void UpdateListView()
    {
        ClearListView();
        string cellValue = "";

        //CheckError();

        // 기본 차순 NO로 오름 차순 정렬한다.
        CheckTable.DefaultView.Sort = SortTarget;

        listTable = null;
        listTable = CheckTable.DefaultView.ToTable();

        totalCount = CheckTable.Rows.Count;

        getCount();

        ResetView();

        GoToFirstPage();

    }

    public void getCount()
    {

        Text_totalCount.text = "전체항목(" + mainManager.FormatNumberWithSymbol(totalCount) + ")";


        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)Text_totalCount.gameObject.transform.parent);
    }

    public void Reset()
    {
        currentPage = 1;

        listCount = 10;
        itemsPerPage = listCount;
        totalCount = 0;
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

  
    private void CitySetup(bool[] cityMenu, bool[] cityMenuBackup)
    {
        for (int i = 0; i < cityMenuBackup.Length; i++)
        {
            cityMenu[i] = cityMenuBackup[i];
        }
    }
}
