using ChartUtil;
using SFB;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ViewStatisticLocal : MonoBehaviour
{
    public string MenuName = "통계 지역별";

    [SerializeField]
    MainManager mainManager;

    [SerializeField]
    ServerManager serverManager;

    [SerializeField]
    Item_MapCheck item_Statistic_Local;

    [SerializeField]
    GameObject item_Empty;

    [SerializeField]
    Transform contents;

    [SerializeField]
    Transform parent;
    
    [SerializeField]
    RectTransform menuContent;
    [SerializeField]
    RectTransform searchContent;

    public Vector2 openSize;
    public Vector2 closeSize;

    public bool isMenuOpen;

    public Button btn_menuOpen;
    public Button btn_menuClose;

    //검색 조건
    SelectMenu SelectMenu;

    // 초기화
    public Button btn_Reset;

    // 조회
    public Button btn_Search;

    public DataTable CheckTable;//조회한 데이터 테이블
    public DataTable listTable;//sort 해서 가져오는 테이블

    List<Item_MapCheck> list_Statistic_Local;//단지 스크립트

    
    Series series1;
    Series series2;
    public Chart chart1;
    public Chart chart2;

    public TMP_Text Title_Chart1;
    public TMP_Text Title_Chart2;


    // 전체항목
    public TextMeshProUGUI Text_totalCount;

    public RectTransform pageContents;

    public RectTransform[] viewupdate;

    // 전체항목
    public int totalCount;

    public RectTransform[] rectCapture;
    // 그래프 이력용 시간 저장
    public string captureTime;

    public string logName = "";
	//엑셀 다운로드 버튼
    public Button AccelButton;

    public string[] mapName = { "전국","서울특별시","부산광역시","대구광역시","인천광역시","광주광역시","대전광역시","울산광역시","세종특별자치시",
        "경기도","강원도","충청북도","충청남도","전라북도","전라남도","경상북도","경상남도","제주특별자치도" };

    public string[] colname = { "NO","지역","단지수","세대수","점검단지","점검세대","위험도 (RF + XG)", "전체","위장전입","통장매매","기타"};

    bool[] CityMenu = new bool[18];


    // 긴넘 캡쳐 
    public Item_RankChart[] item_RankChart;

    public CanvasInImagePosition canvasInImage;

    public GameObject GrapthCaptureCamera;
    public GameObject GrapthCaptureCanvas;

    public Transform captureContent;
    public GameObject captureItem;
    public Item_RankChart captureChartItem;

    public ChartPresetLoader captureChartLoader;
    public Chart captureChart;


    private void Awake()
    {
        list_Statistic_Local = new List<Item_MapCheck>(); //단지 정보
        SelectMenu = gameObject.GetComponent<SelectMenu>();//메뉴 조건

    }
    private void Start()
    {
        for (int i = 0; i < SelectMenu.cityToggles.Count; i++)
        {
            SelectMenu.cityToggles[i].toggle.onValueChanged.AddListener(SearchButtonActive);//기간 토글 클릭시 이벤트
        }
        // 메뉴 펼치기
        btn_menuOpen.onClick.AddListener(Action_Open);

        // 메뉴 접기
        btn_menuClose.onClick.AddListener(Action_Close);

        // 조회
        btn_Search.onClick.AddListener(Action_Search);

        // 초기화
        btn_Reset.onClick.AddListener(Action_Reset);

        //엑셀 버튼
        AccelButton.onClick.AddListener(Action_Excel);
    }

    public void SearchButtonActive(bool check)
    {
        SelectMenu.SeacrhButtonActiveCheck(SelectMenu.cityToggles, btn_Search);
    }

    public void Click_GraphDown()
    {
        StartCoroutine(Action_GraphDownload());
    }

    public IEnumerator Action_GraphDownload()
    {
        GrapthCaptureCamera.gameObject.SetActive(true);

        GrapthCaptureCanvas.gameObject.SetActive(true);

        Click_GraphDownload2(0);

        yield return new WaitForSeconds(2);

        GrapthCaptureCamera.gameObject.SetActive(false);

        GrapthCaptureCanvas.gameObject.SetActive(false);
    }

    public void Click_GraphDownload2(int num)
    {
        // 그래프 카피

        foreach (Transform child in captureContent)
        {
            Destroy(child.gameObject);
        }

        Debug.Log("Chart categories Count : " + chart1.chartData.categories.Count);

        captureItem = Instantiate(item_RankChart[0].gameObject, captureContent);

        captureChartItem = captureItem.GetComponent<Item_RankChart>();

        captureChart = captureChartItem.chart;

        captureChart.chartData = chart1.chartData;

        captureChartItem.UI_title.text = Title_Chart1.text;

        float mapy = 490;
        float boxy = 562;

        if (chart1.chartData.categories.Count > 5)
        {
            mapy = chart1.chartData.categories.Count * 98;
            boxy = mapy + 72;
        }

        captureChartItem.Box_RankMap.sizeDelta = new Vector2(boxy, 426);
        captureChartItem.UI_RankMap.sizeDelta = new Vector2(mapy, 354);

        captureChart.UpdateChart();

        // 부모 컨테이너의 중앙 위치를 기준으로 설정
        captureItem.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 0.5f);
        captureItem.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0.5f);

        // RectTransform의 Pivot을 (0.5, 0.5)로 설정하여 중심을 원점으로 이동
        captureItem.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0.5f);

        captureItem.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

        string filename = "";
        string content = "";

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

        if (num == 0)
        {
            // 위험도 평균 점수 
            filename = $"{SelectMenu.dateText.Canlendar.text} {localName} 지역별 위험도 평균 점수 그래프";
        }

        canvasInImage.Image = captureItem.GetComponent<RectTransform>();

        canvasInImage.filename = filename;
        canvasInImage.GetCalcPosition();

        //그래프 캡처 이력 저장
        mainManager.DownloadLodInsert(MenuName, content);
    }

    public void Click_GraphDownload(int num)
    {

        string filename = "";
        string content = "";

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

        mainManager.canvasInImage.Image = rectCapture[num];

        if (num == 0)
        {
            // 위험도 평균 점수 
            filename = $"{SelectMenu.dateText.Canlendar.text} {localName} 지역별 위험도 평균 점수 그래프";
        }
        else if (num == 1)
        {
            // 부정청약 적발 수
            filename = $"{SelectMenu.dateText.Canlendar.text} {localName} 부정청약 적발 수 그래프";
        }

        mainManager.canvasInImage.filename = filename;

        mainManager.canvasInImage.GetCalcPosition();

        //그래프 캡처 이력 저장
        mainManager.DownloadLodInsert(MenuName, filename);

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

                //첫번째 칼럼 삭제
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

    public void SaveDataTableToCsv(DataTable dataTable, string filePath, string[] columname)
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

                    if (column.ColumnName == "지역") 
                    {
                        values.Add(serverManager.LocalName(row[column].ToString()));
                    }
                    else
                    {
                        values.Add(row[column].ToString());
                    }
                }


                streamWriter.WriteLine(string.Join(",", values));
            }
        }
    }
	
	
    

    /// <summary>
    /// 조회
    /// </summary>
    private void Action_Search()
    {
        //리스트 조회 초기화
        ClearListView();

        CheckTable = new DataTable();
        series1 = new Series();
        series2 = new Series();
        chart1.chartData.series.Clear();
        chart1.chartData.categories.Clear();
        chart2.chartData.series.Clear();

        Title_Chart1.text = $"지역별 위험도 평균 점수 <style=sub>[{SelectMenu.dateText.text}]</style>";
        Title_Chart2.text = $"부정청약 적발 수 <style=sub>[{SelectMenu.dateText.text}]</style>";

        CityMenu = SelectMenu.CheckCityToggle(SelectMenu.cityToggles);

        CheckTable = serverManager.GetStatisticsByRegion(SelectMenu.dateText.FromDate, SelectMenu.dateText.ToDate, CityMenu);

        LayoutRebuilder.ForceRebuildLayoutImmediate(pageContents);

        if (CheckTable != null)
        {
            UpdateView();
        }
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
        var children = contents.transform.GetComponentsInChildren<Item_MapCheck>(true);

        foreach (var child in children)
        {
            Destroy(child.gameObject);
        }

        list_Statistic_Local.Clear();
    }

    public void UpdateView()
    {

        Debug.Log("UPDATE VIEW");

        
        listTable = null;
        listTable = CheckTable.DefaultView.ToTable();


        series1.name = "지역별 위험도 평균";
        series2.name = "부정청약 적발 수";

        int CheckAll = 0;
        int CheckCamouflage = 0;
        int CheckBankbook = 0;
        int CheckOther = 0;

        float contentHeight = 0;

        foreach (DataRow row in listTable.Rows)
        {
            contentHeight += 1;
            Item_MapCheck item = Instantiate(item_Statistic_Local, contents);


            string columnAll = int.Parse(row["전체"].ToString()).ToString("N0");
            string columnCamouflage = int.Parse(row["위장전입"].ToString()).ToString("N0");
            string columnBankbook = int.Parse(row["통장매매"].ToString()).ToString("N0");
            string columnOther = int.Parse(row["기타"].ToString()).ToString("N0");

            string columnlocal = serverManager.LocalName(row["지역"].ToString());
            string columnAvg = row["평균"].ToString();
            float columnAvgResult = Mathf.Floor(float.Parse(columnAvg) * 100f) / 100f;//위험도 평균


            item.no.text = row["NO"].ToString();
            item.mapAddr.text = columnlocal;

            item.mapBlockCount.text = int.Parse(row["단지수"].ToString()).ToString("N0");
            item.mapAptCount.text = int.Parse(row["세대수"].ToString()).ToString("N0");
            item.mapCheckBlockCount.text = int.Parse(row["점검단지"].ToString()).ToString("N0");
            item.mapCheckAptCount.text = int.Parse(row["점검세대"].ToString()).ToString("N0");
            
            item.mapCheckCount.text = (Mathf.Floor(float.Parse(columnAvgResult.ToString()) * 10f) / 10f).ToString("N1");
            item.dfTotal.text = columnAll;
            item.dfPaint.text = columnCamouflage;
            item.dfSales.text = columnBankbook;
            item.dfOther.text = columnOther;

            list_Statistic_Local.Add(item);
        }

        listTable.DefaultView.Sort = "평균 DESC";
        DataRow[] selectedRows = listTable.Select();

        //차트 위험도 순위
        foreach (DataRow row in selectedRows)
        {
            string columnAll = row["전체"].ToString();
            string columnCamouflage = row["위장전입"].ToString();
            string columnBankbook = row["통장매매"].ToString();
            string columnOther = row["기타"].ToString();
            string columnlocal = serverManager.LocalName(row["지역"].ToString());
            string columnAvg = row["평균"].ToString();
            float columnAvgResult = Mathf.Floor(float.Parse(columnAvg) * 100f) / 100f;//위험도 평균

            CheckAll += int.Parse(columnAll);
            CheckCamouflage += int.Parse(columnCamouflage);
            CheckBankbook += int.Parse(columnBankbook);
            CheckOther += int.Parse(columnOther);

            series1.data.Add(new Data(columnAvgResult, true));
            chart1.chartData.categories.Add(columnlocal);
        }

        if(selectedRows.Length > 0)
        {
            series2.data.Add(new Data(CheckAll, true));
            series2.data.Add(new Data(CheckCamouflage, true));
            series2.data.Add(new Data(CheckBankbook, true));
            series2.data.Add(new Data(CheckOther, true));

            chart1.transform.parent.GetComponent<RectTransform>().sizeDelta = new Vector2(contentHeight * 120, chart1.transform.parent.GetComponent<RectTransform>().sizeDelta.y);
            chart1.transform.parent.parent.GetComponent<RectTransform>().sizeDelta = new Vector2(contentHeight * 120, chart1.transform.parent.GetComponent<RectTransform>().sizeDelta.y);

            chart1.chartData.series.Add(series1);
            chart2.chartData.series.Add(series2);

            chart1.UpdateChart();
            chart2.UpdateChart();
        }


        if (list_Statistic_Local.Count == 0)
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

            ClearListView();

            // 기본 차순 NO로 오름 차순 정렬한다.
            CheckTable = null;
            listTable = null;

            item_Empty.SetActive(true);


            //기간 초기화 2023.06.11 => 초기화 1년으로
            SelectMenu.periodToggle[1].isOn = true;
            SelectMenu.PriodChange(SelectMenu.periodToggle[1], 1);

            //도시 초기화
            SelectMenu.MenuChangedEvent(true);
        }

        //ResetModal.SetActive(false);
    }

    public void Reset()
    {
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

    public void getCount()
    {

        Text_totalCount.text = "전체항목(" + mainManager.FormatNumberWithSymbol(totalCount) + ")";


        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)Text_totalCount.gameObject.transform.parent);
    }
}
