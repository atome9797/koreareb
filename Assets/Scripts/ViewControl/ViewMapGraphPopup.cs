using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ChartUtil;
using System;
using System.Data;
using System.Text.RegularExpressions;
using PygmyMonkey.ColorPalette;

public class ViewMapGraphPopup : MonoBehaviour
{
    public MainManager mainManager;

    public ViewDashBoard viewDashBoard;

    public ViewStatisticDanji viewStatisticDanji;

    public string MenuName = "단지별";

    public ServerManager serverManager;

    public GameObject[] UI_Maps;

    // 기간 
    public TextMeshProUGUI text_dateRange;


    // 지역 타이틀 
    public TMP_Text[] mapTitle;

    public TMP_Dropdown dropdown_rank;
    public TMP_Dropdown dropdown_rank_w; //부정청약 위험도 순위 토글

    // 차트 정렬 모드
    public int listMode;

    // 선택 지역명
    public string[] mapName = { "전국","서울특별시","부산광역시","대구광역시","인천광역시","광주광역시","대전광역시","울산광역시","세종특별자치시",
        "경기도","강원도","충청북도","충청남도","전라북도","전라남도","경상북도","경상남도","제주특별자치도" };


    public string selectMapname = "";



    public Button btn_Close;

    public Button btn_Confirm;

    public ScrollRect scrollRect;

    public ChartPresetLoader PerChartLoader_d;
    public ChartPresetLoader PerChartLoader_w;

    public ChartPresetLoader RankChartLoader_d;
    public ChartPresetLoader RankChartLoader_w;

    public ChartPresetLoader CountChartLoader_d;
    public ChartPresetLoader CountChartLoader_w;

    // 부정청약 위험도
    public Chart DFPercentChart;

    // 부정청약 적발수
    public Chart DFCountChart;

    // 부정청약 위험도 순위
    public Chart DFRankChart;


    // 캡쳐 영역

    public RectTransform[] rectCapture;

    // 그래프 이력용 시간 저장
    public string captureTime;

    public string logName = "";

    public DataTable CheckTable;//조회한 데이터 테이블
    public DataTable listMapTable;//지도 테이블
    public DataTable listSortTable;//sort 해서 가져오는 테이블
    public DataTable listSortTable2;//sort 해서 가져오는 테이블
    public DataTable listPercentTable;//퍼센트 막대 그래프 테이블
    public DataTable listDFCountTable;//적발수 막대 그래프 테이블

    public DataTable listMapTableBackup;
    public DataTable listSortTable2Backup;
    public DataTable listPercentTableBackup;
    public DataTable listDFCountTableBackup;

    public string SortTarget; // 정렬 기준

    Series series1;
    Series series2;
    Series series3;


    Queue<float> que = new Queue<float>();

    public Button ResetButton;

    Dictionary<int, string> localItem = new Dictionary<int, string>();

    public DataTable PercentPersonRate;//퍼센트 사람 비율

    bool typeReset;
    string localType = "All";
    public bool[] CityMenu = new bool[18];
    public bool[] CityMenuBackup = new bool[18];
    public string Date = "";

    public TextMeshProUGUI DangerScore;
    public int currentNum = 0;


    public Color colorMapDefault_d;
    public Color colorMapHover_d;
    public Color colorMapSelect_d;
    public Color colorMapDisabled_d;

    public Color colorTextDeFault_d;
    public Color colorTextHover_d;
    public Color colorTextSelect_d;
    public Color colorTextDisabled_d;

    public Color colorBubbleSelect_d;
    public Color colorBubbleSelect_w;

    public Color colorOutline_d;
    public Color colorOutline_w;

    public Color colorMapDefault_w;
    public Color colorMapHover_w;
    public Color colorMapSelect_w;
    public Color colorMapDisabled_w;

    public Color colorTextDeFault_w;
    public Color colorTextHover_w;
    public Color colorTextSelect_w;
    public Color colorTextDisabled_w;

    // 긴넘 캡쳐 
    public CanvasInImagePosition canvasInImage;

    public GameObject GrapthCaptureCamera;
    public GameObject GrapthCaptureCanvas;

    public Transform captureContent;
    public GameObject captureItem;

    public ChartPresetLoader captureChartLoader;
    public Chart captureChart;

    /// <summary>
    ///  
    /// </summary>
    private void Awake()
    {

        localItem.Add(4111, "경기도 수원시");
        localItem.Add(4113, "경기도 성남시");
        localItem.Add(4117, "경기도 안양시");
        localItem.Add(4127, "경기도 안산시");
        localItem.Add(4128, "경기도 고양시");
        localItem.Add(4146, "경기도 용인시");
        localItem.Add(4311, "충청북도 청주시");
        localItem.Add(4413, "충청남도 천안시");
        localItem.Add(4511, "전라북도 전주시");
        localItem.Add(4711, "경상북도 포항시");
        localItem.Add(4812, "경상남도 창원시");

        dropdown_rank.onValueChanged.AddListener(delegate { RankDropdownValueChanged(dropdown_rank); });
        dropdown_rank_w.onValueChanged.AddListener(delegate { RankDropdownValueChanged(dropdown_rank_w); });

        // 닫기 버튼
        btn_Close.onClick.AddListener(Action_Close);

        // 확인 버튼
        btn_Confirm.onClick.AddListener(Action_Close);

        //ResetButton.onClick.AddListener(Init);
    }

    public void ChangeTheme()
    {
        Debug.Log("ChangeTheme : " + mainManager.thememode);

        if (mainManager.thememode == MainManager.Thememode.dark)
        {
            ColorPaletteData.Singleton.setCurrentPalette(2);

            PerChartLoader_d.chartOptions = DFPercentChart.chartOptions;
            PerChartLoader_d.LoadPresets();

            DFPercentChart.UpdateChart();

            RankChartLoader_d.chartOptions = DFRankChart.chartOptions;
            RankChartLoader_d.LoadPresets();

            DFRankChart.UpdateChart();

            CountChartLoader_d.chartOptions = DFCountChart.chartOptions;
            CountChartLoader_d.LoadPresets();

            DFCountChart.UpdateChart();

            dropdown_rank_w.gameObject.SetActive(false);

            dropdown_rank.value = listMode;

            dropdown_rank.gameObject.SetActive(true);
        }
        else
        {
            ColorPaletteData.Singleton.setCurrentPalette(3);

            PerChartLoader_w.chartOptions = DFPercentChart.chartOptions;
            PerChartLoader_w.LoadPresets();

            DFPercentChart.UpdateChart();

            RankChartLoader_w.chartOptions = DFRankChart.chartOptions;
            RankChartLoader_w.LoadPresets();

            DFRankChart.UpdateChart();

            CountChartLoader_w.chartOptions = DFCountChart.chartOptions;
            CountChartLoader_w.LoadPresets();

            DFCountChart.UpdateChart();

            dropdown_rank.gameObject.SetActive(false);

            dropdown_rank_w.value = listMode;

            dropdown_rank_w.gameObject.SetActive(true);
        }

        UpdateMapView(UI_Maps[currentNum]);

    }

    public void Action_Close()
    {

        Reset();
        gameObject.SetActive(false);
    }


    public void Init()
    {
        Select_Maps(0);
        currentNum = 0;

        dropdown_rank.value = 0;

        dropdown_rank_w.value = 0;
    }

    public void Reset()
    {
        //Select_Maps(0);
        ScrollToTop();
        currentNum = 0;

        dropdown_rank.value = 0;

        dropdown_rank_w.value = 0;
    }

    private void ScrollToTop()
    {
        // Content 영역의 위치를 맨 위로 설정
        scrollRect.content.anchoredPosition = new Vector2(0, 0f);
        scrollRect.velocity = Vector2.zero;
    }


    public void unLoad_Maps()
    {
        for (int i = 0; i < UI_Maps.Length; i++)
        {
            UI_Maps[i].SetActive(false);
        }
    }

    public void Select_Maps(int selectNum)
    {
        currentNum = selectNum;

        unLoad_Maps();

        UI_Maps[selectNum].SetActive(true);//아이템 활성화
        UI_Maps[selectNum].GetComponent<Item_Map>().init(); //아이템 초기화

        CheckCityToggle(selectNum, CityMenu);//도시 선택 메뉴 0일때는 조회 안함

        if (selectNum != 0)
        { 
            CityTitleSetting(CityMenu); //그래프 별 타이틀 설정
        }
        else
        {
            CityTitleSetting(CityMenuBackup); //그래프 별 타이틀 설정
        }


        if (!typeReset)
        {

            listMapTable = null;
            listSortTable2 = null;
            listPercentTable = null;
            listDFCountTable = null;

            PercentPersonRate = new DataTable();//테이블 초기화

            PercentPersonRate = serverManager.GetPercentPerson(viewStatisticDanji.SelectMenu.dateText.FromDate.ToString(), viewStatisticDanji.SelectMenu.dateText.ToDate.ToString(), CityMenu, "StatisticMapSort");


            string PercentAll = "";
            string Percent5 = "";
            string Percent10 = "";
            string Percent20 = "";
            foreach (DataRow row in PercentPersonRate.Rows)
            {
                PercentAll = Mathf.Floor(float.Parse(row["total"].ToString())).ToString();
                Percent5 = Mathf.Floor(float.Parse(row["high"].ToString())).ToString();
                Percent10 = Mathf.Floor(float.Parse(row["middle"].ToString())).ToString();
                Percent20 = Mathf.Floor(float.Parse(row["low"].ToString())).ToString();
            }

            if (selectNum != 0)
            {
                localType = "Local";
                listMapTable = serverManager.GetStatisticMapByDangerLocal(CityMenu, viewStatisticDanji.SelectMenu.dateText.FromDate.ToString(), viewStatisticDanji.SelectMenu.dateText.ToDate.ToString(), "map"); //지역
                listSortTable2 = serverManager.GetStatisticMapByDangerLocal(CityMenu, viewStatisticDanji.SelectMenu.dateText.FromDate.ToString(), viewStatisticDanji.SelectMenu.dateText.ToDate.ToString(), "score");
                listDFCountTable = serverManager.GetStatisticMapByDangerLocal(CityMenu, viewStatisticDanji.SelectMenu.dateText.FromDate.ToString(), viewStatisticDanji.SelectMenu.dateText.ToDate.ToString(), "map");
                listPercentTable = serverManager.GetStatisticMapByDangerPercent(CityMenu, viewStatisticDanji.SelectMenu.dateText.FromDate.ToString(), viewStatisticDanji.SelectMenu.dateText.ToDate.ToString(), PercentAll, Percent5, Percent10, Percent20);
            }
            else
            {
                localType = "All";
                listMapTable = serverManager.GetStatisticMapByDanger(CityMenu, viewStatisticDanji.SelectMenu.dateText.FromDate.ToString(), viewStatisticDanji.SelectMenu.dateText.ToDate.ToString(), "map"); //전국
                listSortTable2 = serverManager.GetStatisticMapByDanger(CityMenu, viewStatisticDanji.SelectMenu.dateText.FromDate.ToString(), viewStatisticDanji.SelectMenu.dateText.ToDate.ToString(), "score");
                listDFCountTable = serverManager.GetStatisticMapByDanger(CityMenu, viewStatisticDanji.SelectMenu.dateText.FromDate.ToString(), viewStatisticDanji.SelectMenu.dateText.ToDate.ToString(), "map");
                listPercentTable = serverManager.GetStatisticMapByDangerPercent(CityMenu, viewStatisticDanji.SelectMenu.dateText.FromDate.ToString(), viewStatisticDanji.SelectMenu.dateText.ToDate.ToString(), PercentAll, Percent5, Percent10, Percent20);
                listMapTableBackup = listMapTable;
                listSortTable2Backup = listSortTable2;
                listDFCountTableBackup = listDFCountTable;
                listPercentTableBackup = listPercentTable;
            }

        }

        UpdateMapView(UI_Maps[selectNum]);
        UpdateSortView();
        UpdatePercentView();
        UpdateDFCountView();
        typeReset = false;

    }

    public void Click_Home()
    {
        typeReset = true;
        listMapTable = listMapTableBackup;
        listSortTable2 = listSortTable2Backup;
        listPercentTable = listPercentTableBackup;
        listDFCountTable = listDFCountTableBackup;
        localType = "All";
        Init();
    }

    public void Click_GraphDown()
    {
        StartCoroutine(Action_GraphDownload());
    }

    public IEnumerator Action_GraphDownload()
    {
        GrapthCaptureCamera.gameObject.SetActive(true);

        GrapthCaptureCanvas.gameObject.SetActive(true);

        Click_GraphDownload2(3);

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

        captureItem = Instantiate(rectCapture[num].gameObject, captureContent);

        // 부모 컨테이너의 중앙 위치를 기준으로 설정
        captureItem.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 0.5f);
        captureItem.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0.5f);

        // RectTransform의 Pivot을 (0.5, 0.5)로 설정하여 중심을 원점으로 이동
        captureItem.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0.5f);

        captureItem.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

        string currentTime = DateTime.Now.ToString("yyyyMMdd_HHmmss"); // 현재 시간을 yyyyMMdd_HHmmss 형식으로 가져옴
        string content = "";

        string filename = "";

        canvasInImage.Image = captureItem.GetComponent<RectTransform>();

        if (num == 0)
        {
            // 지도 그래프 
            filename = $"{currentTime}_위험도 지도(최근1년 평균)";
            content = $"{Date} {mapTitle[num].text} 위험도 지도(모집공고일) 그래프";
        }
        else if (num == 1)
        {
            // 부정청약 위험도
            filename = $"{currentTime}_부정청약 위험도(%)";
            content = $"{Date} {mapTitle[num].text} 부정청약 위험도(%) 그래프";
        }
        else if (num == 2)
        {
            // 부정청약 적발 수
            filename = $"{currentTime}_부정청약 적발 수";
            content = $"{Date} {mapTitle[num].text} 부정청약 적발수 그래프";
        }
        else if (num == 3)
        {
            // 부정청약 위험도 순위
            filename = $"{currentTime}_부정청약 위험도 순위";
            content = $"{Date} {mapTitle[num].text} 부정청약 위험도 순위 그래프";
        }

        canvasInImage.filename = filename;

        canvasInImage.GetCalcPosition();

        //그래프 캡처 이력 저장
        mainManager.DownloadLodInsert(MenuName, content);

    }

    public void Click_GraphDownload(int num)
    {
        string currentTime = DateTime.Now.ToString("yyyyMMdd_HHmmss"); // 현재 시간을 yyyyMMdd_HHmmss 형식으로 가져옴
        string content = "";

        string filename = "";

        mainManager.canvasInImage.Image = rectCapture[num];

        if (num == 0)
        {
            // 지도 그래프 
            filename = $"{currentTime}_위험도 지도(최근1년 평균)";
            content = $"{Date} {mapTitle[num].text} 위험도 지도(모집공고일) 그래프";
        }
        else if (num == 1)
        {
            // 부정청약 위험도
            filename = $"{currentTime}_부정청약 위험도(%)";
            content = $"{Date} {mapTitle[num].text} 부정청약 위험도(%) 그래프";
        }
        else if (num == 2)
        {
            // 부정청약 적발 수
            filename = $"{currentTime}_부정청약 적발 수";
            content = $"{Date} {mapTitle[num].text} 부정청약 적발수 그래프";
        }
        else if (num == 3)
        {
            // 부정청약 위험도 순위
            filename = $"{currentTime}_부정청약 위험도 순위";
            content = $"{Date} {mapTitle[num].text} 부정청약 위험도 순위 그래프";
        }

        mainManager.canvasInImage.filename = filename;

        mainManager.canvasInImage.GetCalcPosition();

        //그래프 캡처 이력 저장
        mainManager.DownloadLodInsert(MenuName, content);



    }


    /// <summary>
    ///  지도
    /// </summary>
    public void UpdateMapView(GameObject map)
    {
        que.Clear();

        text_dateRange.text = viewStatisticDanji.SelectMenu.dateText.Canlendar.text;


        int count = 0;

        Item_Map tempMap = map.GetComponent<Item_Map>();

        // Debug.Log("listMapTable.Rows count : " + listMapTable.Rows.Count);

        if (listMapTable.Rows.Count > 0)
        {
            foreach (DataRow row in listMapTable.Rows)
            {
                string columnLocal = "";

                int num = int.Parse(row["지역"].ToString()) / 10;

                //키가 있으면 
                if (localItem.ContainsKey(num))
                {
                    columnLocal = localItem[num];
                }
                else
                {
                    columnLocal = serverManager.LocalName(row["지역"].ToString());
                }

                columnLocal = columnLocal.Substring(columnLocal.IndexOf(' ') + 1).Trim();

                string columnAvg = row["everage"].ToString();

                float columnAvgResult = 0;

                if (!string.IsNullOrEmpty(columnAvg))
                {
                    columnAvgResult = Mathf.Floor(float.Parse(columnAvg) * 10f) / 10f;//위험도 평균

                    
                }
                else
                {
                    columnAvgResult = 0;
                }

                que.Enqueue(columnAvgResult);

                for (int i = 0; i < tempMap.map.Length; i++)
                {
                    string test1 = tempMap.map[i].transform.parent.GetChild(0).GetComponent<TextMeshProUGUI>().text.Replace("\n", "");

                    string test2 = test1.Replace("\r", "");

                    tempMap.map[i].transform.parent.GetChild(0).GetComponent<TextMeshProUGUI>().text = test2;


                    if (columnLocal.Equals(tempMap.map[i].transform.parent.GetChild(0).GetComponent<TextMeshProUGUI>().text))
                    {
                        tempMap.map[i].text = (Mathf.Floor(float.Parse(que.Dequeue().ToString()) * 10f) / 10f).ToString("N1");
                    }

                    // 데이터가 0일때는 맵 비활성화
                    if (tempMap.map[i].text == "0")
                    {
                        if (mainManager.thememode == MainManager.Thememode.dark)
                        {
                            tempMap.map_bg[i].color = colorMapDisabled_d;

                            tempMap.mapName[i].color = colorTextDisabled_d;
                            tempMap.map[i].color = colorTextDisabled_d;

                            tempMap.map_bg[i].GetComponent<Button>().interactable = false;

                            tempMap.map_bg[i].GetComponent<MapEvent>().originalColor = colorMapDisabled_d;
                            tempMap.map_bg[i].GetComponent<MapEvent>().originalTextColor = colorTextDisabled_d;

                        }
                        else
                        {
                            tempMap.map_bg[i].color = colorMapDisabled_w;

                            tempMap.mapName[i].color = colorTextDisabled_w;
                            tempMap.map[i].color = colorTextDisabled_w;

                            tempMap.map_bg[i].GetComponent<Button>().interactable = false;

                            tempMap.map_bg[i].GetComponent<MapEvent>().originalColor = colorMapDisabled_w;
                            tempMap.map_bg[i].GetComponent<MapEvent>().originalTextColor = colorTextDisabled_w;
                        }

                    }
                    else
                    {
                        if (mainManager.thememode == MainManager.Thememode.dark)
                        {
                            tempMap.map_bg[i].color = colorMapDefault_d;

                            tempMap.mapName[i].color = colorTextDeFault_d;
                            tempMap.map[i].color = colorTextDeFault_d;

                            tempMap.map_bg[i].GetComponent<Button>().interactable = true;

                            tempMap.map_bg[i].GetComponent<MapEvent>().originalColor = colorMapDefault_d;
                            tempMap.map_bg[i].GetComponent<MapEvent>().originalTextColor = colorTextDeFault_d;

                        }
                        else
                        {
                            tempMap.map_bg[i].color = colorMapDefault_w;

                            tempMap.mapName[i].color = colorTextDeFault_w;
                            tempMap.map[i].color = colorTextDeFault_w;

                            tempMap.map_bg[i].GetComponent<Button>().interactable = true;

                            tempMap.map_bg[i].GetComponent<MapEvent>().originalColor = colorMapDefault_w;
                            tempMap.map_bg[i].GetComponent<MapEvent>().originalTextColor = colorTextDeFault_w;

                        }
                    }
                }
                count++;
            }
        }
        else
        {
            for (int i = 0; i < tempMap.map.Length; i++)
            {

                // 데이터가 0일때는 맵 비활성화
                if (tempMap.map[i].text == "0")
                {
                    if (mainManager.thememode == MainManager.Thememode.dark)
                    {
                        tempMap.map_bg[i].color = colorMapDisabled_d;

                        tempMap.mapName[i].color = colorTextDisabled_d;
                        tempMap.map[i].color = colorTextDisabled_d;

                        tempMap.map_bg[i].GetComponent<Button>().interactable = false;

                        tempMap.map_bg[i].GetComponent<MapEvent>().originalColor = colorMapDisabled_d;
                        tempMap.map_bg[i].GetComponent<MapEvent>().originalTextColor = colorTextDisabled_d;

                    }
                    else
                    {
                        tempMap.map_bg[i].color = colorMapDisabled_w;

                        tempMap.mapName[i].color = colorTextDisabled_w;
                        tempMap.map[i].color = colorTextDisabled_w;

                        tempMap.map_bg[i].GetComponent<Button>().interactable = false;

                        tempMap.map_bg[i].GetComponent<MapEvent>().originalColor = colorMapDisabled_w;
                        tempMap.map_bg[i].GetComponent<MapEvent>().originalTextColor = colorTextDisabled_w;
                    }

                }
                else
                {
                    if (mainManager.thememode == MainManager.Thememode.dark)
                    {
                        tempMap.map_bg[i].color = colorMapDefault_d;

                        tempMap.mapName[i].color = colorTextDeFault_d;
                        tempMap.map[i].color = colorTextDeFault_d;

                        tempMap.map_bg[i].GetComponent<Button>().interactable = true;

                        tempMap.map_bg[i].GetComponent<MapEvent>().originalColor = colorMapDefault_d;
                        tempMap.map_bg[i].GetComponent<MapEvent>().originalTextColor = colorTextDeFault_d;

                    }
                    else
                    {
                        tempMap.map_bg[i].color = colorMapDefault_w;

                        tempMap.mapName[i].color = colorTextDeFault_w;
                        tempMap.map[i].color = colorTextDeFault_w;

                        tempMap.map_bg[i].GetComponent<Button>().interactable = true;

                        tempMap.map_bg[i].GetComponent<MapEvent>().originalColor = colorMapDefault_w;
                        tempMap.map_bg[i].GetComponent<MapEvent>().originalTextColor = colorTextDeFault_w;

                    }
                }
            }

        }
        
        

    }

    /// <summary>
    /// 위험도 순위 막대그래프
    /// </summary>
    public void UpdateSortView()
    {
        series1 = new Series();
        DFRankChart.chartData.series.Clear();
        DFRankChart.chartData.categories.Clear();


        series1.name = "지역별 위험도 평균";

        //높은순 낮은순 정렬 - 위험도 순위 그래프
        switch (listMode)
        {
            case 0: SortTarget = "everage DESC"; break;
            case 1: SortTarget = "everage ASC"; break;
        }

        listSortTable2.DefaultView.Sort = SortTarget;

        listSortTable = null;
        listSortTable = listSortTable2.DefaultView.ToTable();

        float contentHeight = 0;
        int sort = 0;
        foreach (DataRow row in listSortTable.Rows)
        {
            sort++;

            contentHeight += 1;

            string columnLocal = "";

            //string sort = row["NO"].ToString();


            if (localType == "All")
            {
                int num = int.Parse(row["지역"].ToString()) / 10;

                //키가 있으면 
                if (localItem.ContainsKey(num))
                {
                    columnLocal = localItem[num];
                }
                else
                {
                    columnLocal = serverManager.LocalName(row["지역"].ToString());
                }
            }
            else if (localType == "Local")
            {
                columnLocal = row["단지"].ToString();
            }

            string columnAvg = row["everage"].ToString();

            float columnAvgResult = 0;

            if (!string.IsNullOrEmpty(columnAvg))
            {
                columnAvgResult = Mathf.Floor(float.Parse(columnAvg) * 10f) / 10f;//위험도 평균

                series1.data.Add(new Data(columnAvgResult, true));
            }
            else
            {
                // 데이터가 없는 경우 0으로 할당
                columnAvgResult = 0;

                series1.data.Add(new Data(columnAvgResult, true));
            }


            int bytecount = System.Text.Encoding.Default.GetByteCount(columnLocal);

            //이름이 10글자 이상이면 줄바꿈
            if (columnLocal.Length > 9)
            {
                string firstName = columnLocal.Substring(0, 9);
                string secendName = columnLocal.Substring(9);

                if (secendName.Length > 10)
                {
                    secendName = $"{secendName.Substring(0, 9)}...";
                }


                bytecount = System.Text.Encoding.Default.GetByteCount(firstName);

                if (bytecount < 30)
                {
                    for (int startLength = bytecount; startLength < 30; startLength++)
                    {
                        firstName += " ";
                    }
                }

                columnLocal = $"{firstName}\n{secendName}";
            }
            else
            {
                if (bytecount < 30)
                {
                    for (int startLength = bytecount; startLength < 30; startLength++)
                    {
                        columnLocal += " ";
                    }
                }
            }

            DFRankChart.chartData.categories.Add(sort + " " + columnLocal);

        }

        DFRankChart.transform.parent.GetComponent<RectTransform>().sizeDelta = new Vector2(DFRankChart.transform.parent.GetComponent<RectTransform>().sizeDelta.x, contentHeight * 48);

        DFRankChart.chartData.series.Add(series1);
        DFRankChart.UpdateChart();
    }

    /// <summary>
    /// 위험도 퍼센트 막대 그래프
    /// </summary>
    public void UpdatePercentView()
    {
        series2 = new Series();
        DFPercentChart.chartData.series.Clear();

        foreach (DataRow row in listPercentTable.Rows)
        {                
            string columnAvg = row["전체"].ToString();


            if (!string.IsNullOrEmpty(columnAvg))
            {
                float columnAvgResult = Mathf.Floor(float.Parse(columnAvg) * 10f) / 10f;//위험도 평균

                series2.data.Add(new Data(columnAvgResult, true));
            }
            else
            {
                // 데이터가 없는 경우 0으로 할당

                float columnAvgResult = 0;

                series2.data.Add(new Data(columnAvgResult, true));
            }
        }

        DFPercentChart.chartData.series.Add(series2);
        DFPercentChart.UpdateChart();

    }

    /// <summary>
    /// 적발수 그래프
    /// </summary>
    /// <param name="type"></param>
    public void UpdateDFCountView()
    {
        series3 = new Series();
        DFCountChart.chartData.series.Clear();

        series3.name = "부정 청약 적발수";

        int CheckAll = 0;
        int CheckCamouflage = 0;
        int CheckBankbook = 0;
        int CheckOther = 0;


        foreach (DataRow row in listDFCountTable.Rows)
        {
            string columnAll = row["전체"].ToString();
            string columnCamouflage = row["위장전입"].ToString();
            string columnBankbook = row["통장매매"].ToString();
            string columnOther = row["기타"].ToString();

            CheckAll += int.Parse(columnAll);
            CheckCamouflage += int.Parse(columnCamouflage);
            CheckBankbook += int.Parse(columnBankbook);
            CheckOther += int.Parse(columnOther);
        }

        if (listDFCountTable.Rows.Count > 0)
        {

            series3.data.Add(new Data(CheckAll, true));
            series3.data.Add(new Data(CheckCamouflage, true));
            series3.data.Add(new Data(CheckBankbook, true));
            series3.data.Add(new Data(CheckOther, true));
        }
        DFCountChart.chartData.series.Add(series3);
        DFCountChart.UpdateChart();
    }

    /// <summary>
    /// 최신순 토글 선택
    /// </summary>
    /// <param name="change"></param>
    public void RankDropdownValueChanged(TMP_Dropdown change)
    {
        Debug.Log("sort : " + change.value);

        listMode = change.value;


        if (listSortTable != null)
        {
            UpdateSortView();
        }
    }

    public bool[] CheckCityToggle(bool[] cityMenu, List<CityToggle> cityToggles)
    {

        for (int i = 0; i < cityToggles.Count; i++)
        {
            cityMenu[i] = false;

            if (cityToggles[i].toggle.isOn)
            {
                cityMenu[i] = true;
            }
        }

        return cityMenu;
    }

    public bool[] CheckCityToggle(int index, bool[] cityMenu)
    {
        if(index != 0)
        {
            for (int i = 0; i < cityMenu.Length; i++)
            {
                cityMenu[i] = false;

                if (i == index)
                {
                    cityMenu[i] = true;
                }
            }            
        }

        return cityMenu;
    }

    /// <summary>
    /// 그래프 별 제목 설정
    /// </summary>
    /// <param name="cityMenu"></param>
    public void CityTitleSetting(bool[] cityMenu)
    {
        bool cityMenuCheck = true;
        
        for (int i = 1; i < cityMenu.Length; i++)
        {
            if(!cityMenu[i])
            {
                cityMenuCheck = false;
            }
        }

        for (int i = 0; i < mapTitle.Length; i++)
        {
            //체크 박스 비활성화 상태가 있을때
            if (!cityMenuCheck)
            {
                int count = 0;
                string title = "";
                for (int j = 1; j < CityMenu.Length; j++)
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

                mapTitle[i].text = title;
            }
            else //전체 체크박스 선택 되었을때
            {
                mapTitle[i].text = mapName[0];//전국 또는 지역
            }
        }

    }
}
