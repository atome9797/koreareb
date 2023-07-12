using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Data;
using SFB;
using System.IO;
using System.Text;
using System;

public class ViewDanji : MonoBehaviour
{
    public string MenuName = "단지 선정";

    public MainManager mainManager;

    public ServerManager serverManager;

    public Item_Danji item_Danji;

    public GameObject item_Empty;

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
    SelectMenu SelectMenu;

    public string printPath;

    // 검색 구분
    public string[] searchMode = { "*", "apt_name", "apt_number", "apt_sub_pos" };

    public int selectSearchMode = 0;

    // 출력
    public Button btn_Print;

    // 초기화
    public Button btn_Reset;

    // 조회
    public Button btn_Search;

    // 엑셀 다운로드
    public Button btn_Excel;

    public DataTable CheckTable;//조회한 데이터 테이블
    public DataTable listTable;//sort 해서 가져오는 테이블

    //사용자 정보
    [SerializeField]
    List<Item_Danji> list_Danji;//단지 스크립트

    bool[] _dropdownType = new bool[4];//검색구분
    bool[] BustType = new bool[3];//적발내용
    bool[] RfxgType = new bool[3];//rfxg 타입
    bool[] PrecentType = new bool[4];//위험도 퍼센트
    bool[] CityMenu = new bool[18];
    float[] percent = { 1, 0.5f, 0.25f, 0.1f }; //세대수 퍼센트
    int index = 0;

    //초기화 모달
    //[SerializeField]
    //GameObject ResetModal;
    //[SerializeField]
    //Button ResetYesButton;
    //[SerializeField]
    //Button ResetNoButton;

    //여기부터가 페이지 기능
    public Button prevButton; // 이전 10 페이지로 이동하는 버튼
    public Button nextButton; // 다음 10 페이지로 이동하는 버튼

    public List<Button> pageButtons; // 페이지 번호를 나타내는 버튼 리스트

    public Button firstPageButton; // 첫 페이지로 이동하는 버튼
    public Button lastPageButton; // 마지막 페이지로 이동하는 버튼

    // DropDown
    public TMP_Dropdown dropdown_inspection;
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

    public SortButton[] sortButtons;


    //조회 건수 
    public int[] listCounts = { 10, 20, 50, 100 };

    public int selectrowCount;

    string[] mapName = { "전국","서울특별시","부산광역시","대구광역시","인천광역시","광주광역시","대전광역시","울산광역시","세종특별자치시",
        "경기도","강원도","충청북도","충청남도","전라북도","전라남도","경상북도","경상남도","제주특별자치도" };

    /*
    public string[] colname = {"주택관리번호", "민영국민", "주택명", "동", "호수", "공급유형", "특별공급", "공급위치_시군구코드", "크기", "일반공급", "특별공급.1", "공급금액", "생년", "성명",
"연령","세대주관계", "행정변경", "변경일자", "배우자", "특이사항", "행정주소", "주소일치여부_부동산원", "주소중복횟수", "특일동시여부", "2년청약건수", "세대원수", "분리세대원수", "폰중복횟수_부동산원",
"IP중복당첨횟수_부동산원", "IP중복신청횟수_부동산원", "폰번호", "IP_1", "IP_2", "IP_3", "IP_4", "ad_IP중복_2자리", "ad_IP중복_3자리", "ad_IP중복_4자리", "접수일자", "접수시간", "접수매체", "신청거주구분",
"당첨거주구분", "거주지역", "신청주소", "상세신청주소", "가점신청여부", "가점당첨여부", "가점합계", "부양가족수", "저축가입기간", "무주택기간", "주택소유구분", "장기복무군인", "I_국민주택무주택기간",
"J_저축종류","청약통장개설일자",  "청약납부회차", "청약납부금액", "청약경과기간", "다자녀_영유아자녀수", "다자녀_무주택기간", "다자녀_해당지역거주기간", "다자녀_입주자저축가입기간", "다자녀_총점", "다자녀_미성년자녀수",
"신혼_공특법", "신혼_개정시행일자", "신혼미공특_소득구분",  "신혼미공특_신청순위", "신혼미공특_미성년자녀수", "신혼미공특_태아수", "신혼미공특_소득비율", "신혼미공특_소득구분명", "ad_신혼미공특소득구분", "신혼공특_월평균소득배점",
"신혼공특_미성년자녀수배점", "신혼공특_미성년자녀수", "신혼공특_해당지역거주기간배점", "신혼공특_해당지역거주기간",  "신혼공특_혼인기간배점","신혼공특_혼인기간",  "신혼공특_한부모가족자녀나이배점",
"신혼공특_한부모가족자녀나이", "신혼공특_입주자저축가입기간배점", "신혼공특_입주자저축가입기간", "신혼공특_총점","기관추천종류", "청약_재당첨제한여부", "청약_혼인및한부모가족", "세대주여부",
"과거5년이내당첨", "무주택세대구성원", "소득기준", "소득(소득세 5개년도 납부)기준", "세대구성", "무주택세대주", "자산기준", "노부모부양", "3명이상미성년자녀", "혼인기간7년이내", "ad_주소일치여부","검사여부",
"부정청약판정", "부정청약유형", "부정_거주지유지", "부정_주소지유지", "부정_위장전입", "부정_자격위조", "부정_입주자저축증서매매", "부정_위장이혼", "부정_기타", "모집공고일", "당첨자발표일", "해당전입제한일",
"1순위요건", "총공급가구수", "분양가상한제여부", "과밀성장권역여부", "1순위인터넷접수일", "2순위인터넷접수일시", "ad_변경일자", "ad_총공급", "ad_성명생년중복", "ad_성명생년전화중복", "ad_행정변경시점",
"ad_접수시간", "ad_신청당첨거주일치여부", "ad_부양가족수", "ad_저축가입기간", "ad_무주택기간", "ad_청약납부회차", "ad_청약납부금액", "ad_청약경과기간", "ad_신청유형", "ad_다자녀영유아자녀수",
"ad_다자녀무주택기간", "ad_다자녀해당지역거주기간", "ad_다자녀입주자저축가입기간", "ad_다자녀총점", "ad_다자녀미성년자녀수", "ad_신혼미공특미성년자녀수", "ad_신혼미공특태아수", "ad_미성년자녀수",
"ad_총점", "ad_변경시점2", "부정청약_rf_prob", "부정청약_rf_0.5",  "부정청약_rf_0.25", "부정청약_rf_0.1", "부정청약_xgb_prob", "부정청약_xgb_0.5", "부정청약_xgb_0.25", "부정청약_xgb_0.1","위장전입_rf_prob",
"위장전입_rf_0.5", "위장전입_rf_0.25", "위장전입_rf_0.1", "위장전입_xgb_prob", "위장전입_xgb_0.5", "위장전입_xgb_0.25", "위장전입_xgb_0.1", "매매_rf_prob", "매매_rf_0.5", "매매_rf_0.25","매매_rf_0.1",
"매매_xgb_prob", "매매_xgb_0.5", "매매_xgb_0.25", "매매_xgb_0.1", "create_date", "change_date", "inspection_status_result"};*/

    string[] colname = { "NO", "점검여부","주택관리번호","민영/국민","단지명","지역","공급세대수","위험도","세대수","모집공고일","당첨자발표일","전입제한","분양가 상한제" };

    // 출력부분
    public GameObject printCanvas;
    public GameObject printCamBox;

    public ScreenShot[] printCamera;

    public Item_DanjiPrint[] printItems;

    public GameObject[] menuTitles;

    private void Awake()
    {
        list_Danji = new List<Item_Danji>(); //사용자 정보
        SelectMenu = gameObject.GetComponent<SelectMenu>();//메뉴 조건

        dropdown_inspection.onValueChanged.AddListener(delegate { InspectionDropdownValueChanged(dropdown_inspection); });
        dropdown_count.onValueChanged.AddListener(delegate { ListCountDropdownValueChanged(dropdown_count); });

        //ResetYesButton.onClick.AddListener(delegate { ResetButton(true); });
        //ResetNoButton.onClick.AddListener(delegate { ResetButton(false); });
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

        for (int i = 0; i < pageButtons.Count; i++)
        {
            int pageNumber = ((currentPage - 1) / 10) * 10 + i + 1;
            pageButtons[i].onClick.AddListener(() => GoToPage(pageNumber));
        }

        Reset();
        dropdown_inspection.value = 0;
        dropdown_count.value = 0;

        // 메뉴 펼치기
        btn_menuOpen.onClick.AddListener(Action_Open);

        // 메뉴 접기
        btn_menuClose.onClick.AddListener(Action_Close);


        // 조회
        btn_Search.onClick.AddListener(Action_Search);

        // 초기화
        btn_Reset.onClick.AddListener(Action_Reset);

        // 출력
        btn_Print.onClick.AddListener(Action_Print);

        // 엑셀 다운로드
        btn_Excel.onClick.AddListener(Action_Excel);

    }


    public void SearchButtonActive(bool check)
    {
        SelectMenu.SeacrhButtonActiveCheck(SelectMenu.cityToggles, btn_Search);
    }

    // 검색 조회
    public async void Action_Search()
    {
        mainManager.viewLoading.Open();

        mainManager.loadMsg.text = "";

        //리스트 조회 초기화
        ClearListView();
        
        CheckTable = null;

        CheckTable = new DataTable();

        SelectMenu.DropdownType(_dropdownType); //검색구분
        SelectMenu.CheckDFType(BustType); //적발내용
        SelectMenu.CheckRfxgType(RfxgType); //rfxg
        SelectMenu.CheckRfxgPerType(PrecentType); //퍼센트
        CityMenu = SelectMenu.CheckCityToggle(SelectMenu.cityToggles);

        CheckTable = await serverManager.GetMarketManageByDanji(SelectMenu.dateText.FromDate,
                                                                SelectMenu.dateText.ToDate,
                                                                _dropdownType,
                                                                SelectMenu.input_search.text,
                                                                CityMenu,
                                                                BustType,
                                                                RfxgType,
                                                                PrecentType);

        for (int i = 0; i < 4; i++) 
        {
            if(PrecentType[i])
            {
                index = i;
            }
        }

        GoToFirstPage();

        mainManager.viewLoading.Close();
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

    public void Action_Excel()
    {
        //string[] cols = new string[CheckTable.Columns.Count];

        string[] fileTypes = new string[] { "CSV Files", "csv" };

        StandaloneFileBrowser.SaveFilePanelAsync("엑셀 다운로드", "", "", "csv", (string path) =>
        {
            if (!string.IsNullOrEmpty(path))
            {
                // 파일 저장 로직을 구현하세요
                // 예를 들어, 파일을 생성하고 내용을 저장할 수 있습니다.
                // File.WriteAllText(path, "Hello, World!");


                SaveDataTableToCsv(CheckTable, path, colname);

                Debug.Log("File saved at: " + path);


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

                string names = Path.GetFileNameWithoutExtension(path);
                mainManager.DownloadLodInsert(MenuName, $"{names} 엑셀다운로드");

                //mainManager.DownloadLodInsert(MenuName, $"{SelectMenu.dateText.Canlendar.text} {localName} 엑셀다운로드");

            }
            else
            {
                Debug.Log("Save file operation cancelled.");
            }
        });
    }

    public void SaveDataTableToCsv(DataTable dataTable, string filePath, string[] columname)
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
            streamWriter.WriteLine(string.Join(",", columname));

            // 데이터 쓰기
            foreach (DataRow row in dataTable.Rows)
            {
                List<string> values = new List<string>();

                foreach (DataColumn column in dataTable.Columns)
                {

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

    // 출력
    public void Action_Print()
    {
        ClearPrintList();

        Debug.Log("Action_Print");

        //for (int i = 0; i < menuTitles.Length; i++)
        //{
        //    Debug.Log($"menuTitles index {i} : " + menuTitles[i].transform.GetSiblingIndex());
        //}

        printCanvas.SetActive(true);
        printCamBox.SetActive(true);

        int requiredCameraCount = Mathf.Clamp((list_Danji.Count - 1) / 17 + 1, 1, 6);
        int maxCameraCount = Mathf.Min(requiredCameraCount, printCamera.Length);

        Debug.Log("maxCameraCount : " + maxCameraCount);

        for (int i = 0; i < maxCameraCount; i++)
        {
            printCamera[i].gameObject.SetActive(true);
            menuTitles[i].gameObject.SetActive(true);
        }

        // 출력 아이템 켜고

        // 출력 아이템 생성하고 
        SetPrintView();

        // StandaloneFileBrowser.SaveFilePanel("엑셀다운로드", "", "", "");

        //string path =  StandaloneFileBrowser.SaveFilePanel("엑셀다운로드", "", "", "");

        //Debug.Log("Select Path : " + path);
        //string basefilename = $"{MenuName}";

        string path = StandaloneFileBrowser.SaveFilePanel("출력 저장", "", MenuName, "");

        if (!string.IsNullOrEmpty(path))
        {
            string currentTime = DateTime.Now.ToString("yyyyMMdd_HHmmss"); // 현재 시간을 yyyyMMdd_HHmmss 형식으로 가져옴
            string originalFileName = Path.GetFileNameWithoutExtension(path); // 기존 파일명 가져오기
            string extension = Path.GetExtension(path); // 확장자 가져오기
            string directoryPath = Path.GetDirectoryName(path); // 새로운 파일 경로

            Debug.Log("Select Path : " + path);
            Debug.Log("Select directoryPath : " + directoryPath);
            Debug.Log("Select currentTime : " + currentTime);
            Debug.Log("Select originalFileName : " + originalFileName);
            Debug.Log("Select extension : " + extension);

            for (int i = 0; i < maxCameraCount; i++)
            {
                printCamera[i].path = directoryPath;
                printCamera[i].filename = currentTime + "_" + MenuName;
                printCamera[i].init();

                printCamera[i].ClickScreenShot();
            }
        }
        /*
        StandaloneFileBrowser.SaveFilePanelAsync("출력 저장", "", basefilename, "", (string path) =>
        {
            if(!string.IsNullOrEmpty(path))
            {
                string currentTime = DateTime.Now.ToString("yyyyMMdd_HHmmss"); // 현재 시간을 yyyyMMdd_HHmmss 형식으로 가져옴
                string originalFileName = Path.GetFileNameWithoutExtension(path); // 기존 파일명 가져오기
                string extension = Path.GetExtension(path); // 확장자 가져오기
                string directoryPath = Path.GetDirectoryName(path); // 새로운 파일 경로

                Debug.Log("Select Path : " + path);
                Debug.Log("Select directoryPath : " + directoryPath);
                Debug.Log("Select currentTime : " + currentTime);
                Debug.Log("Select originalFileName : " + originalFileName);
                Debug.Log("Select extension : " + extension);

                for (int i = 0; i < maxCameraCount; i++)
                {
                    printCamera[i].path = directoryPath;
                    printCamera[i].filename = currentTime + "_" + basefilename;
                    printCamera[i].init();

                    printCamera[i].ClickScreenShot();
                }
            }
        });*/

    }

    public void ClearPrintList()
    {
        Debug.Log("ClearPrintList");

        for (int i = 0; i < printItems.Length; i++)
        {
            printItems[i].Close();
        }

        for (int i = 0; i < menuTitles.Length; i++)
        {
            menuTitles[i].SetActive(false);
            printCamera[i].gameObject.SetActive(false);
        }
    }

    // 출력용 아이템 생성
    public void SetPrintView()
    {
        Debug.Log("SetPrintView");

        Debug.Log("list_Danji.Count : " + list_Danji.Count);

        for (int i = 0; i < list_Danji.Count; i++)
        {
            printItems[i].item_Danji = list_Danji[i];

            printItems[i].Open();

            printItems[i].Init();

        }
        //for (int i = 0; i < list_Check.Count; i++)
        //{
        //    Item_CheckPrint item = Instantiate(item_CheckPrint, printContents);

        //    item.item_Check = list_Check[i];

        //    item.Init();
        //}
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
    /// 리스트 초기화
    /// </summary>
    public void ClearListView()
    {
        var children = contents.transform.GetComponentsInChildren<Item_Danji>(true);

        foreach (var child in children)
        {
            Destroy(child.gameObject);
        }

        list_Danji.Clear();
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

        // 데이터 변경
        //=> 사용시에는 list 초기화 및 재할당이 이루어짐으로 미리 view에서 clearListView함수 호출할것
        if (CheckTable != null)
        {
            UpdateView();
        }

        // 페이지 번호 버튼 업데이트
        int maxPage = Mathf.CeilToInt((float)selectrowCount / itemsPerPage);

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

    public void UpdateView()
    {

        Debug.Log("UPDATE VIEW");

        // 현재 정렬 버튼으로
        CheckTable.DefaultView.Sort = SortTarget;


        listTable = null;
        listTable = CheckTable.DefaultView.ToTable();

        //이부분에 초기화 및 재생성 함수 들어감
        ClearListView();

        //재생성 함수 들어갈곳
        string inspection_status = "";

        switch(listMode)
        {
            case 1 : inspection_status = "점검여부 = '해당없음'"; break;
            case 2: inspection_status = "점검여부 = 'N'"; break;
            case 3: inspection_status = "점검여부 = 'Y'"; break;
        }

        DataRow[] selectedRows = listTable.Select(inspection_status);

        selectrowCount = selectedRows.Length;

        int startIndex = (currentPage - 1) * itemsPerPage;
        int endIndex = Mathf.Min(startIndex + itemsPerPage, selectedRows.Length);
        
        string cellValue = "";

        for (var rowIndex = startIndex; rowIndex < endIndex; rowIndex++)
        {
            var row = selectedRows[rowIndex];

            Item_Danji item = Instantiate(item_Danji, contents);

            //item.no.text = row["NO"].ToString();
            item.no.text = (rowIndex + 1).ToString();


            //item.check.text = row["점검여부"].ToString();
            cellValue = row["점검여부"].ToString();

            if (cellValue == "N")
            {
                cellValue = "<color=#ef8674>" + cellValue + "</color>";
            }
            else if (cellValue == "Y")
            {
                cellValue = "<color=#2dbbbc>" + cellValue + "</color>";

            }
            else
            {
                cellValue = "해당없음";
            }

            item.check.text = cellValue;


            item.aptNum.text = row["주택관리번호"].ToString();
            item.aptType.text = row["민영국민"].ToString();
            item.aptName.text = row["단지명"].ToString();
            item.region.text = row["지역"].ToString();
            item.supportCount.text = int.Parse(row["공급세대수"].ToString()).ToString("N0");
            item.risk.text = (Mathf.Floor(float.Parse(row["위험도"].ToString()) * 10f) / 10f).ToString("N1");//위험도 평균
            item.houseUserCount.text = Mathf.Floor(float.Parse(row["세대수"].ToString())).ToString("N0");//세대수
            item.NoticeDate.text = mainManager.DateFormat(row["모집공고일"].ToString());
            item.winAnnoDate.text = mainManager.DateFormat(row["당첨자발표일"].ToString());
            item.limittransferDate.text = mainManager.DateFormat(row["전입제한일"].ToString());

            cellValue = row["분양가상한제"].ToString();

            if (cellValue == "N")
            {
                cellValue = "<color=#ef8674>" + cellValue + "</color>";
            }
            else if (cellValue == "Y")
            {
                cellValue = "<color=#2dbbbc>" + cellValue + "</color>";
            }

            item.salepriceStatus.text = cellValue;

            list_Danji.Add(item);
        }

        if (list_Danji.Count == 0)
        {
            item_Empty.SetActive(true);
        }
        else
        {
            item_Empty.SetActive(false);
        }

        totalCount = CheckTable.Rows.Count;
        ResetView();
        getCount();

    }

    /// <summary>
    /// 점검여부 토글 선택
    /// </summary>
    /// <param name="change"></param>
    public void InspectionDropdownValueChanged(TMP_Dropdown change)
    {
        Debug.Log("Inspection : " + change.value);

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


    // 재 계산이 필요할때
    public void UpdateListView()
    {
        ClearListView();
        string cellValue = "";

        // NO로 정렬 지정
        Clear_Sort();

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

        if (CheckTable != null)
        {
            UpdateUI();
        }

        Debug.Log(" SORT " + SortTarget);
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

    public void ResetButton(bool check)
    {
        //true일때 초기화 아니면 setActive false
        if (check)
        {
            Reset();
            dropdown_inspection.value = 0;
            dropdown_count.value = 0;

            ClearListView();

            // NO로 정렬 지정
            Clear_Sort();

            // 기본 차순 NO로 오름 차순 정렬한다.
            CheckTable = null;
            listTable = null;

            item_Empty.SetActive(true);

            //selectMenu reset
            SelectMenu.ResetData();
            getCount();
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

        SortTarget = "NO ASC";
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
