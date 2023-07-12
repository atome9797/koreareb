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
using ChartUtil;
using System.Threading;
using PygmyMonkey.ColorPalette;

public class ViewDashBoard : BaseView
{
    public MainManager mainManager;

    public string MenuName = "대시보드";

    public ServerManager serverManager;

    public GameObject[] UI_Maps; //모든 지도 종류

    // 지역 타이틀 
    public TMP_Text[] mapTitle; //그래프 별 제목

    public TMP_Dropdown dropdown_rank; //부정청약 위험도 순위 토글
    public TMP_Dropdown dropdown_rank_w; //부정청약 위험도 순위 토글

    // 차트 정렬 모드
    public int listMode;

    public string[] mapName = { "전국","서울특별시","부산광역시","대구광역시","인천광역시","광주광역시","대전광역시","울산광역시","세종특별자치시",
        "경기도","강원도","충청북도","충청남도","전라북도","전라남도","경상북도","경상남도","제주특별자치도" };

    int CityCode;

    public string selectMapname = "";


    // 차트 옵션 디자인 적용을 위해
    public ChartPresetLoader PerChartLoader_d;
    public ChartPresetLoader PerChartLoader_w;

    public ChartPresetLoader RankChartLoader_d;
    public ChartPresetLoader RankChartLoader_w;

    // 부정청약 위험도
    public Chart DFPercentChart;

    // 부정청약 위험도 순위
    public Chart DFRankChart;

    // 캡쳐 영역

    public RectTransform[] rectCapture;

    // 그래프 이력용 시간 저장
    public string captureTime;

    public string logName = "";

    public DataTable listMapTable;//지도 테이블 
    public DataTable listSortTable;//sort 해서 가져오는 테이블
    public DataTable listSortTable2;
    public DataTable listPercentTable;//퍼센트 막대 그래프 테이블
    public DataTable LastRiskDataTableDate;//riskdata 테이블 마지막 일자 불러오기
    public DataTable PercentPersonRate;//퍼센트 사람 비율

    public DataTable listMapTableBackup;
    public DataTable listSortTable2Backup;
    public DataTable listPercentTableBackup;


    public string SortTarget; // 정렬 기준

    Series series1;
    Series series2;


    Queue<float> que;

    public Button ResetButton;

    Dictionary<int, string> localItem = new Dictionary<int, string>();

    string startDate = "";
    string endDate = "";

    bool typeReset;
    string localType = "All";

    bool[] CityMenu = new bool[18];
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
    public bool ignoreValueChanged;

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
    }

    private void OnEnable()
    {
        Debug.Log("thememode : " + mainManager.thememode);

        if (mainManager.thememode == MainManager.Thememode.dark)
        {
            // Dark 일때 Dark

            PerChartLoader_d.chartOptions = DFPercentChart.chartOptions;
            PerChartLoader_d.LoadPresets();

            DFPercentChart.UpdateChart();

            RankChartLoader_d.chartOptions = DFRankChart.chartOptions;
            RankChartLoader_d.LoadPresets();

            DFRankChart.UpdateChart();

            dropdown_rank_w.gameObject.SetActive(false);

            dropdown_rank.value = listMode;

            dropdown_rank.gameObject.SetActive(true);
        }
        else
        {
            PerChartLoader_w.chartOptions = DFPercentChart.chartOptions;
            PerChartLoader_w.LoadPresets();

            DFPercentChart.UpdateChart();

            RankChartLoader_w.chartOptions = DFRankChart.chartOptions;
            RankChartLoader_w.LoadPresets();

            DFRankChart.UpdateChart();

            dropdown_rank.gameObject.SetActive(false);

            dropdown_rank_w.value = listMode;

            dropdown_rank_w.gameObject.SetActive(true);

        }

    }

    public void Init()
    {
        // 지도 초기화
        Select_Maps(0);
        currentNum = 0;

        dropdown_rank.value = 0;
        dropdown_rank_w.value = 0;
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

            dropdown_rank.gameObject.SetActive(false);

            dropdown_rank_w.value = listMode;

            dropdown_rank_w.gameObject.SetActive(true);

        }

        UpdateMapView(UI_Maps[currentNum]);
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

        UI_Maps[selectNum].SetActive(true);
        UI_Maps[selectNum].GetComponent<Item_Map>().init();

        for (int i = 0; i < mapTitle.Length; i++)
        {
            mapTitle[i].text = mapName[selectNum];
        }

        if(!typeReset)
        {

            listMapTable = null;
            listPercentTable = null;
            listSortTable2 = null;

            CheckCityToggle(selectNum, CityMenu);

            //날짜 저장
            LastRiskDataTableDate = new DataTable();
            LastRiskDataTableDate = serverManager.GetLastCreateDateRiskData();
            foreach (DataRow row in LastRiskDataTableDate.Rows)
            {
                startDate = row["시작시간"].ToString();
                endDate = row["끝시간"].ToString();
            }


            PercentPersonRate = new DataTable();

            PercentPersonRate = serverManager.GetPercentPerson(startDate, endDate, CityMenu, "MainMapSort");

            string PercentAll = "";
            string Percent5 = "";
            string Percent10 = "";
            string Percent20 = "";
            foreach (DataRow row in PercentPersonRate.Rows)
            {
                Debug.Log(row["total"].ToString());
                PercentAll = Mathf.Floor(float.Parse(row["total"].ToString())).ToString();
                Percent5 = Mathf.Floor(float.Parse(row["high"].ToString())).ToString();
                Percent10 = Mathf.Floor(float.Parse(row["middle"].ToString())).ToString();
                Percent20 = Mathf.Floor(float.Parse(row["low"].ToString())).ToString();
            }


            //선택한 맵의 INDEX로 불러오기
            if (selectNum != 0)//지역
            {
                listMapTable = serverManager.GetDashBordMapByDangerLocal(CityMenu, startDate, endDate, "map");
                listSortTable2 = serverManager.GetDashBordMapByDangerLocal(CityMenu, startDate, endDate, "score");
                listPercentTable = serverManager.GetDashBordMapByDangerPercent(CityMenu, startDate, endDate, PercentAll, Percent5, Percent10, Percent20);
                localType = "Local";
            }
            else //전국
            {
                listMapTable = serverManager.GetDashBordMapByDanger(CityMenu, startDate, endDate, "map");
                listSortTable2 = serverManager.GetDashBordMapByDanger(CityMenu, startDate, endDate, "score");
                listPercentTable = serverManager.GetDashBordMapByDangerPercent(CityMenu, startDate, endDate, PercentAll, Percent5, Percent10, Percent20);
                listMapTableBackup = listMapTable;
                listSortTable2Backup = listSortTable2;
                listPercentTableBackup = listPercentTable;
                localType = "All";
            }
        }


        UpdateMapView(UI_Maps[selectNum]);
        UpdateSortView();
        UpdatePercentView();

        typeReset = false;
    }

    public void Click_Home()
    {
        //지도 초기화
        typeReset = true;
        listMapTable = listMapTableBackup;
        listSortTable2 = listSortTable2Backup;
        listPercentTable = listPercentTableBackup;
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

        Click_GraphDownload2(2);

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

        Debug.Log("Chart categories Count : " + DFRankChart.chartData.categories.Count);

        captureItem = Instantiate(item_RankChart[(int)mainManager.thememode].gameObject, captureContent);

        captureChartItem = captureItem.GetComponent<Item_RankChart>();

        captureChart =  captureChartItem.chart;

        captureChart.chartData = DFRankChart.chartData;

        float mapy = 251.7f;
        float boxy = 357.7f;

        if (DFRankChart.chartData.categories.Count > 5)
        {
             mapy = DFRankChart.chartData.categories.Count * 50.34f;
             boxy = mapy + 106;
        }

        captureChartItem.Box_RankMap.sizeDelta = new Vector2(850, boxy);
        captureChartItem.UI_RankMap.sizeDelta = new Vector2(778, mapy);

        captureChart.UpdateChart();

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
            content = $"{mapTitle[num].text} 위험도 지도(모집공고일) 그래프";
        }
        else if (num == 1)
        {
            // 부정청약 위험도
            filename = $"{currentTime}_부정청약 위험도(%)";
            content = $"{mapTitle[num].text} 부정청약 위험도(%) 그래프";
        }
        else if (num == 2)
        {
            // 부정청약 적발 수
            filename = $"{currentTime}_부정청약 적발 수";
            content = $"{mapTitle[num].text} 부정청약 적발수 그래프";
        }
        else if (num == 3)
        {
            // 부정청약 위험도 순위
            filename = $"{currentTime}_부정청약 위험도 순위";
            content = $"{mapTitle[num].text} 부정청약 위험도 순위 그래프";
        }

        canvasInImage.filename = filename;

        canvasInImage.GetCalcPosition();

        //그래프 캡처 이력 저장
        mainManager.DownloadLodInsert(MenuName, content);


    }
    public void Click_GraphDownload(int num)
    {
        string currentTime = DateTime.Now.ToString("yyyyMMdd_HHmmss"); // 현재 시간을 yyyyMMdd_HHmmss 형식으로 가져옴

        string filename = "";
        string content = "";

        mainManager.canvasInImage.Image = rectCapture[num];

        if (num == 0)
        {
            // 지도 그래프 
            filename = $"{currentTime}_위험도 지도(최근1년 평균)";
            content = $"{mapTitle[num].text} 위험도 지도(최근1년 평균) 그래프";
        }
        else if(num == 1)
        {
            // 부정청약 위험도
            filename = $"{currentTime}_부정청약 위험도(%)";
            content = $"{mapTitle[num].text} 부정청약 위험도(%) 그래프";
        }
        else if(num == 2)
        {
            // 부정청약 위험도 순위
            filename = $"{currentTime}_부정청약 위험도 순위";
            content = $"{mapTitle[num].text} 부정청약 위험도 순위 그래프";
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
        Debug.Log("UpdateMapView ");

        que = new Queue<float>();

        int count = 0;

        Item_Map tempMap = map.GetComponent<Item_Map>();

        Debug.Log("listMapTable.Rows count : " + listMapTable.Rows.Count);

        if(listMapTable.Rows.Count > 0)
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

                    // Debug.Log($"<color=yellow>[UpdateMapView] 여기{columnLocal}지역 {count}" + columnAvgResult + "</color>");
                }
                else
                {
                    columnAvgResult = 0;
                }

                que.Enqueue(columnAvgResult);

                for (int i = 0; i < tempMap.map.Length; i++)
                {
                    string test1 = tempMap.mapName[i].text.Replace("\n", "");

                    string test2 = test1.Replace("\r", "");

                    tempMap.mapName[i].text = test2;

                    if (columnLocal.Equals(tempMap.mapName[i].text))
                    {

                        tempMap.map[i].text = (Mathf.Floor(float.Parse(que.Dequeue().ToString()) * 10f) / 10f).ToString("N1");
                    }

                    //Debug.Log("tempMap.map[i].text : " + tempMap.map[i].text);

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

            if(localType == "All")
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
            else if(localType == "Local")
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
            if(columnLocal.Length > 9)
            {
                string firstName = columnLocal.Substring(0, 9);
                string secendName = columnLocal.Substring(9);

                if(secendName.Length > 10)
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


            DFRankChart.chartData.categories.Add(sort +  " " + columnLocal);
        }

        DFRankChart.transform.parent.GetComponent<RectTransform>().sizeDelta = new Vector2(DFRankChart.transform.parent.GetComponent<RectTransform>().sizeDelta.x, contentHeight * 52);

        
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

        //Debug.Log("listPercentTable.Rows Count : " + listPercentTable.Rows.Count);

        foreach (DataRow row in listPercentTable.Rows)
        {
            string columnAvg = row["전체"].ToString();

            if(!string.IsNullOrEmpty(columnAvg))
            {
                float columnAvgResult = Mathf.Floor(float.Parse(columnAvg) * 100f) / 100f;//위험도 평균

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
    /// 최신순 토글 선택
    /// </summary>
    /// <param name="change"></param>
    public void RankDropdownValueChanged(TMP_Dropdown change)
    {
        Debug.Log("sort : " + change.value);

        listMode = change.value;

        if (ignoreValueChanged)
            return;

        if (listSortTable != null)
        {
            UpdateSortView();
        }
    }

    public bool[] CheckCityToggle(int index, bool[] cityMenu)
    {

        for (int i = 0; i < cityMenu.Length; i++)
        {
            cityMenu[i] = false;

            if (i == index || index == 0)
            {
                cityMenu[i] = true;
            }
        }

        return cityMenu;
    }
}
