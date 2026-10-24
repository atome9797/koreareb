using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SFB;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 시장분석 화면 (GNB '시장분석').
///  - 청약·추첨 경쟁률: 어느 지역에 신청이 몰리는지 (한국 청약홈, 일본 공영주택·신축 맨션 추첨)
///  - 가격: 어느 지역이 싼지 (㎡당 거래가 중앙값, 가격 지수)
///  - 한·일 비교: 두 나라를 나란히 (가격 지수는 나라 안 상대값이라 통화와 무관)
///  - 데이터 수집: API 키, 수집·집계 실행 (korea_rebs/market_data.py), 저장된 자료 요약
/// 집계표 tb_market_region_stat 를 읽기만 한다. 집계는 파이썬(market_data.py analyze)이 만든다.
/// 정의는 웹 대시보드(korea_rebs/dashboard)와 같다.
/// </summary>
public sealed class MarketAnalysisScreen : ExtScreen
{
    public override string MenuName
    {
        get { return "시장 분석"; }
    }

    private const int MinSupply = 20;     // 경쟁률 순위에 넣는 최소 공급 호수 (웹 대시보드와 같다)
    private const int MinTrades = 10;     // 가격 순위에 넣는 최소 거래 수 (웹 대시보드와 같다)
    private const int ChartRows = 15;
    private const int CompareRows = 8;
    private const int TableRows = 16;

    private const int TabCompetition = 0;
    private const int TabPrice = 1;
    private const int TabCompare = 2;
    private const int TabCollect = 3;

    private static readonly string[] TabNames = { "청약·추첨 경쟁률", "가격", "한·일 비교", "데이터 수집" };
    private static readonly string[] Countries = { ExtNames.Korea, ExtNames.Japan };

    private static readonly Dictionary<string, string[]> Schemes = new Dictionary<string, string[]>
    {
        { ExtNames.Korea, new[] { "kr_apt" } },
        { ExtNames.Japan, new[] { "jp_public_housing", "jp_new_condo" } },
    };

    private static readonly Dictionary<string, string[]> PropertyTypes = new Dictionary<string, string[]>
    {
        { ExtNames.Korea, new[] { "apartment" } },
        { ExtNames.Japan, new[] { "apartment", "house", "land" } },
    };

    // ------------------------------------------------------------------ 상태

    private int tab;
    private int countryIndex;
    private int level = 1;                                  // 1 시도·都道府県, 2 시군구·市区町村
    private readonly List<string> periods = new List<string>();
    private string period = "LAST4Q";
    private int categoryIndex;
    private bool cheapFirst = true;
    private int tablePage;
    private int loadVersion;                                // 늦게 끝난 조회 결과를 버린다
    private readonly List<string[]> tableTexts = new List<string[]>();
    private readonly List<bool> tableRanked = new List<bool>();
    private readonly Dictionary<string, Dictionary<string, string>> prefNames = new Dictionary<string, Dictionary<string, string>>();
    private readonly List<string> log = new List<string>();

    // ------------------------------------------------------------------ 화면

    private ExtUi.Segmented tabs;
    private GameObject filterCard;
    private ExtUi.Segmented countrySeg;
    private ExtUi.Segmented levelSeg;
    private ExtUi.Segmented categorySeg;
    private ExtUi.Segmented orderSeg;
    private ExtUi.Stepper periodStep;
    private TextMeshProUGUI countryLabel;
    private TextMeshProUGUI categoryLabel;
    private TextMeshProUGUI orderLabel;

    private GameObject analysisRoot;
    private TextMeshProUGUI summary;
    private TextMeshProUGUI chartTitle;
    private TextMeshProUGUI chartNote;
    private RectTransform chartArea;
    private TextMeshProUGUI chartEmpty;
    private TextMeshProUGUI tableTitle;
    private TextMeshProUGUI tableNote;
    private GameObject competitionTableRoot;
    private GameObject priceTableRoot;
    private ExtUi.PagedTable competitionTable;
    private ExtUi.PagedTable priceTable;

    private GameObject compareRoot;
    private readonly CompareCard[] compareCards = new CompareCard[2];

    private GameObject collectRoot;
    private TMP_InputField keyDataGoKr;
    private TMP_InputField keyReinfolib;
    private TMP_InputField pythonInput;
    private TMP_InputField krTradeFrom;
    private TMP_InputField krTradeTo;
    private TMP_InputField krCodes;
    private Toggle krRefresh;
    private TMP_InputField krCompFrom;
    private TMP_InputField krCompTo;
    private TMP_InputField jpYears;
    private TMP_InputField jpQuarters;
    private TMP_InputField jpPrefs;
    private Toggle jpRefresh;
    private TMP_InputField fxInput;
    private RectTransform statusArea;
    private TextMeshProUGUI logText;

    private sealed class CompareCard
    {
        public TextMeshProUGUI Title;
        public TextMeshProUGUI[] Lines;
        public TextMeshProUGUI CheapTitle;
        public RectTransform CheapArea;
        public TextMeshProUGUI CompTitle;
        public RectTransform CompArea;
    }

    private string Country
    {
        get { return Countries[countryIndex]; }
    }

    private string Metric
    {
        get { return tab == TabCompetition ? "competition" : "trade"; }
    }

    private string[] Categories
    {
        get { return tab == TabCompetition ? Schemes[Country] : PropertyTypes[Country]; }
    }

    private string Category
    {
        get
        {
            string[] list = Categories;
            return list[Mathf.Clamp(categoryIndex, 0, list.Length - 1)];
        }
    }

    // ================================================================== 만들기

    protected override void Build()
    {
        ExtUi.Title(Root, "한·일 시장 분석");
        Button dashboard = ExtUi.Button(Root, "웹 대시보드 열기", OpenDashboard, true);
        ExtUi.Place(dashboard, 1600f, 18f, 180f, 38f);

        tabs = new ExtUi.Segmented(Root, 40f, 70f, 40f, OnTab);
        tabs.SetLabels(TabNames, 0);

        BuildFilters();
        BuildAnalysis();
        BuildCompare();
        BuildCollect();
        UpdateFilters();
    }

    private void BuildFilters()
    {
        RectTransform card = ExtUi.Card(Root, 40f, 124f, 1740f, 60f);
        filterCard = card.gameObject;
        Transform t = card;

        countryLabel = FilterLabel(t, 24f, "나라");
        countrySeg = new ExtUi.Segmented(t, 68f, 12f, 36f, index =>
        {
            countryIndex = index;
            categoryIndex = 0;
            tablePage = 0;
            UpdateFilters();
            Refresh();
        });
        countrySeg.SetLabels(new[] { "한국", "일본" }, 0);

        FilterLabel(t, 232f, "지역 단위", 72f);
        levelSeg = new ExtUi.Segmented(t, 306f, 12f, 36f, index =>
        {
            level = index + 1;
            tablePage = 0;
            Refresh();
        });

        FilterLabel(t, 510f, "기간");
        periodStep = new ExtUi.Stepper(t, 554f, 12f, 240f, 36f, index =>
        {
            if (index >= 0 && index < periods.Count)
            {
                period = periods[index];
                tablePage = 0;
                Refresh();
            }
        });

        categoryLabel = FilterLabel(t, 826f, "구분");
        categorySeg = new ExtUi.Segmented(t, 870f, 12f, 36f, index =>
        {
            categoryIndex = index;
            tablePage = 0;
            Refresh();
        });

        orderLabel = FilterLabel(t, 1150f, "정렬");
        orderSeg = new ExtUi.Segmented(t, 1194f, 12f, 36f, index =>
        {
            cheapFirst = index == 0;
            tablePage = 0;
            Refresh();
        });
        orderSeg.SetLabels(new[] { "싼 곳부터", "비싼 곳부터" }, 0);
    }

    private static TextMeshProUGUI FilterLabel(Transform parent, float x, string text, float width = 44f)
    {
        TextMeshProUGUI label = ExtUi.Label(parent, text, 15f, ExtUi.TextSub);
        ExtUi.Place(label, x, 0f, width, 60f);
        return label;
    }

    private void BuildAnalysis()
    {
        analysisRoot = ExtUi.Create("Analysis", Root);
        ExtUi.Stretch(analysisRoot, 0f, 0f, 0f, 0f);
        Transform t = analysisRoot.transform;

        summary = ExtUi.Label(t, "", 17f, ExtUi.Text);
        ExtUi.Place(summary, 40f, 196f, 1740f, 32f);

        RectTransform left = ExtUi.Card(t, 40f, 236f, 840f, 764f);
        chartTitle = ExtUi.Heading(left, 24f, 22f, "", 790f);
        chartNote = ExtUi.Label(left, "", 13f, ExtUi.TextMuted);
        ExtUi.Place(chartNote, 24f, 50f, 790f, 22f);
        GameObject area = ExtUi.Create("Chart", left);
        chartArea = ExtUi.Place(area, 16f, 86f, 808f, ChartRows * 40f);
        chartEmpty = ExtUi.Label(left, "", 15f, ExtUi.TextMuted, TextAlignmentOptions.Center);
        ExtUi.Place(chartEmpty, 24f, 300f, 790f, 60f);
        chartEmpty.enableWordWrapping = true;

        RectTransform right = ExtUi.Card(t, 900f, 236f, 880f, 764f);
        tableTitle = ExtUi.Heading(right, 24f, 22f, "", 830f);
        tableNote = ExtUi.Label(right, "", 13f, ExtUi.TextMuted);
        ExtUi.Place(tableNote, 24f, 50f, 830f, 22f);

        competitionTableRoot = ExtUi.Create("CompetitionTable", right);
        ExtUi.Stretch(competitionTableRoot, 0f, 0f, 0f, 0f);
        competitionTable = new ExtUi.PagedTable(competitionTableRoot.transform, 16f, 86f, 848f,
            new[] { "순위", "지역", "경쟁률", "공급", "신청", "자료 수" },
            new[] { 60f, 300f, 130f, 120f, 120f, 100f },
            new[] { true, false, true, true, true, true }, TableRows, 34f, false);
        competitionTable.PageChanged = page => { tablePage = page; FillTable(); };
        competitionTable.RowClicked = slot => competitionTable.Select(slot);

        priceTableRoot = ExtUi.Create("PriceTable", right);
        ExtUi.Stretch(priceTableRoot, 0f, 0f, 0f, 0f);
        priceTable = new ExtUi.PagedTable(priceTableRoot.transform, 16f, 86f, 848f,
            new[] { "순위", "지역", ExtUi.Sqm + "당 중앙값", "가격 지수", "거래가 중앙값", "면적 중앙값", "거래 수" },
            new[] { 56f, 240f, 124f, 96f, 124f, 100f, 90f },
            new[] { true, false, true, true, true, true, true }, TableRows, 34f, false);
        priceTable.PageChanged = page => { tablePage = page; FillTable(); };
        priceTable.RowClicked = slot => priceTable.Select(slot);
    }

    private void BuildCompare()
    {
        compareRoot = ExtUi.Create("Compare", Root);
        ExtUi.Stretch(compareRoot, 0f, 0f, 0f, 0f);
        for (int i = 0; i < 2; i++)
        {
            RectTransform card = ExtUi.Card(compareRoot.transform, 40f + i * 880f, 196f, 860f, 800f);
            var c = new CompareCard();
            c.Title = ExtUi.Heading(card, 24f, 22f, ExtNames.Country(Countries[i]), 800f);
            c.Lines = new TextMeshProUGUI[4];
            for (int j = 0; j < 4; j++)
            {
                c.Lines[j] = ExtUi.Label(card, "", 15f, ExtUi.TextSub);
                ExtUi.Place(c.Lines[j], 24f, 60f + j * 28f, 812f, 26f);
            }
            c.CheapTitle = ExtUi.Label(card, "", 16f, ExtUi.Text, TextAlignmentOptions.MidlineLeft, true);
            ExtUi.Place(c.CheapTitle, 24f, 184f, 812f, 26f);
            c.CheapArea = ExtUi.Place(ExtUi.Create("Cheap", card), 16f, 218f, 828f, CompareRows * 32f);
            c.CompTitle = ExtUi.Label(card, "", 16f, ExtUi.Text, TextAlignmentOptions.MidlineLeft, true);
            ExtUi.Place(c.CompTitle, 24f, 494f, 812f, 26f);
            c.CompArea = ExtUi.Place(ExtUi.Create("Competition", card), 16f, 528f, 828f, CompareRows * 32f);
            compareCards[i] = c;
        }
        TextMeshProUGUI note = ExtUi.Label(compareRoot.transform,
            "가격 지수 = 지역 " + ExtUi.Sqm + "당 거래가 중앙값 / 같은 나라·기간의 전국 중앙값 x 100 (100 = 전국 중앙값). " +
            "원과 엔을 직접 비교하지 않고 나라 안의 상대 위치를 봅니다. 광역 = 시도·도도부현, 기초 = 시군구·시구정촌. '최근 4개 분기'는 나라마다 가장 최근 자료 기준.",
            13f, ExtUi.TextMuted);
        ExtUi.Place(note, 40f, 1002f, 1740f, 24f);
    }

    private void BuildCollect()
    {
        collectRoot = ExtUi.Create("Collect", Root);
        ExtUi.Stretch(collectRoot, 0f, 0f, 0f, 0f);
        Transform t = collectRoot.transform;

        // 키·설정
        RectTransform a = ExtUi.Card(t, 40f, 124f, 520f, 876f);
        ExtUi.Heading(a, 24f, 22f, "API 키 · 실행 설정");
        FieldLabel(a, 24f, 64f, "공공데이터포털 일반 인증키 (실거래가 · 청약홈)", 472f);
        keyDataGoKr = ExtUi.Input(a, "공공데이터포털 마이페이지의 일반 인증키", settings.dataGoKrKey, true);
        ExtUi.Place(keyDataGoKr, 24f, 90f, 472f, 40f);
        FieldLabel(a, 24f, 146f, "일본 부동산정보 라이브러리 API 키", 472f);
        keyReinfolib = ExtUi.Input(a, "일본 국토교통성 reinfolib API 키", settings.reinfolibKey, true);
        ExtUi.Place(keyReinfolib, 24f, 172f, 472f, 40f);
        FieldLabel(a, 24f, 228f, "파이썬 명령", 472f);
        pythonInput = ExtUi.Input(a, "python", settings.pythonCommand);
        ExtUi.Place(pythonInput, 24f, 254f, 472f, 40f);
        ExtUi.ButtonAt(a, 24f, 312f, 40f, "키 · 설정 저장", SaveKeys, true, 140f);
        TextMeshProUGUI notes = ExtUi.Paragraph(a,
            "· 키는 이 PC 의 사용자 폴더(market_settings.json)에만 저장하고, 실행할 때 환경 변수로만 넘깁니다. " +
            "DB · 로그 · 변경 이력에는 남기지 않습니다.\n" +
            "· 공공데이터포털에서 '국토교통부_아파트 매매 실거래가 상세 자료', '한국부동산원_청약홈 분양정보 조회 서비스', " +
            "'한국부동산원_청약홈 청약접수 경쟁률 및 특별공급 신청현황 조회 서비스' 활용 신청이 필요합니다.\n" +
            "· 일본 키는 국토교통성 부동산정보 라이브러리 API 이용 신청으로 받습니다.\n" +
            "· 공영주택 · 신축 맨션 추첨 결과처럼 API 가 없는 일본 자료는 CSV 양식(korea_rebs/templates)으로 가져옵니다.\n" +
            "· 파이썬 스크립트 위치: " + ScriptFolder,
            13f, ExtUi.TextMuted);
        ExtUi.Place(notes, 24f, 372f, 472f, 480f);

        // 수집·집계
        RectTransform b = ExtUi.Card(t, 580f, 124f, 640f, 876f);
        ExtUi.Heading(b, 24f, 22f, "수집 · 집계 실행");
        TextMeshProUGUI runNote = ExtUi.Label(b, "받기가 끝나면 집계를 다시 만듭니다. 이미 받은 기간은 건너뜁니다.", 13f, ExtUi.TextMuted);
        ExtUi.Place(runNote, 24f, 50f, 592f, 22f);

        StepLabel(b, 90f, "1. 지역 목록");
        float x = ExtUi.ButtonAt(b, 24f, 118f, 38f, "지역 만들기 (한국 · 일본 도도부현)", RunRegions);
        ExtUi.ButtonAt(b, x, 118f, 38f, "일본 시구정촌 받기", RunJapanCities);

        StepLabel(b, 176f, "2. 한국 아파트 실거래가 (국토교통부)");
        FieldLabel(b, 24f, 204f, "계약년월", 70f, 38f);
        krTradeFrom = SmallInput(b, 96f, 204f, 110f, "202501", settings.krTradeFrom);
        FieldLabel(b, 212f, 204f, "~", 16f, 38f);
        krTradeTo = SmallInput(b, 230f, 204f, 110f, "202512", settings.krTradeTo);
        FieldLabel(b, 356f, 204f, "지역 코드", 70f, 38f);
        krCodes = SmallInput(b, 430f, 204f, 186f, "11,41 (비우면 전국)", settings.krPrefs);
        krRefresh = CheckWithLabel(b, 24f, 258f, "이미 받은 달도 다시 받기");
        ExtUi.ButtonAt(b, 500f, 250f, 38f, "받기", RunKoreaTrade, true, 116f);

        StepLabel(b, 312f, "3. 한국 청약 경쟁률 (청약홈)");
        FieldLabel(b, 24f, 340f, "모집공고일", 80f, 38f);
        krCompFrom = SmallInput(b, 106f, 340f, 150f, "2025-01-01", settings.krCompFrom);
        FieldLabel(b, 262f, 340f, "~", 16f, 38f);
        krCompTo = SmallInput(b, 280f, 340f, 150f, "2025-12-31", settings.krCompTo);
        ExtUi.ButtonAt(b, 500f, 340f, 38f, "받기", RunKoreaCompetition, true, 116f);

        StepLabel(b, 402f, "4. 일본 거래가격 (부동산정보 라이브러리)");
        FieldLabel(b, 24f, 430f, "연도", 40f, 38f);
        jpYears = SmallInput(b, 66f, 430f, 130f, "2024-2025", settings.jpYears);
        FieldLabel(b, 212f, 430f, "분기", 40f, 38f);
        jpQuarters = SmallInput(b, 254f, 430f, 80f, "1-4", settings.jpQuarters);
        FieldLabel(b, 350f, 430f, "도도부현", 64f, 38f);
        jpPrefs = SmallInput(b, 416f, 430f, 200f, "13,14,27 (비우면 전체)", settings.jpPrefs);
        jpRefresh = CheckWithLabel(b, 24f, 484f, "이미 받은 분기도 다시 받기");
        ExtUi.ButtonAt(b, 500f, 476f, 38f, "받기", RunJapanTrade, true, 116f);

        StepLabel(b, 538f, "5. 일본 추첨 경쟁률 CSV (공영주택 · 신축 맨션)");
        x = ExtUi.ButtonAt(b, 24f, 566f, 38f, "공영주택 CSV 가져오기", () => ImportCompetition("jp_public_housing"));
        x = ExtUi.ButtonAt(b, x, 566f, 38f, "신축 맨션 CSV 가져오기", () => ImportCompetition("jp_new_condo"));
        ExtUi.ButtonAt(b, x, 566f, 38f, "양식 폴더", () => OpenFolder(ScriptPath("templates")));

        StepLabel(b, 628f, "6. 집계 · 웹 대시보드");
        ExtUi.ButtonAt(b, 24f, 656f, 38f, "집계 다시 만들기", RunAnalyze);
        FieldLabel(b, 24f, 712f, "원/엔 환율", 80f, 38f);
        fxInput = SmallInput(b, 106f, 712f, 110f, "예: 9.3", settings.fx);
        x = ExtUi.ButtonAt(b, 232f, 712f, 38f, "내보내고 대시보드 열기", OpenDashboard, true);
        TextMeshProUGUI fxNote = ExtUi.Paragraph(b,
            "환율을 넣으면 웹 대시보드가 엔화 금액을 원으로 환산한 값도 함께 보여 줍니다 (참고용, 고정 환율).", 13f, ExtUi.TextMuted);
        ExtUi.Place(fxNote, 24f, 762f, 592f, 60f);

        // 저장된 자료·실행 기록
        RectTransform c = ExtUi.Card(t, 1240f, 124f, 540f, 876f);
        ExtUi.Heading(c, 24f, 22f, "저장된 자료");
        statusArea = ExtUi.Place(ExtUi.Create("Status", c), 24f, 58f, 492f, 400f);
        ExtUi.Heading(c, 24f, 478f, "실행 기록");
        logText = ExtUi.Paragraph(c, "", 13f, ExtUi.TextSub);
        ExtUi.Place(logText, 24f, 510f, 492f, 344f);
    }

    private static void StepLabel(Transform parent, float y, string text)
    {
        TextMeshProUGUI label = ExtUi.Label(parent, text, 16f, ExtUi.Text, TextAlignmentOptions.MidlineLeft, true);
        ExtUi.Place(label, 24f, y, 592f, 24f);
    }

    private static void FieldLabel(Transform parent, float x, float y, string text, float width, float height = 22f)
    {
        TextMeshProUGUI label = ExtUi.Label(parent, text, 14f, ExtUi.TextSub);
        ExtUi.Place(label, x, y, width, height);
    }

    private static TMP_InputField SmallInput(Transform parent, float x, float y, float width, string placeholder, string value)
    {
        TMP_InputField input = ExtUi.Input(parent, placeholder, value, false, 14f);
        ExtUi.Place(input, x, y, width, 38f);
        return input;
    }

    private static Toggle CheckWithLabel(Transform parent, float x, float y, string text)
    {
        Toggle toggle = ExtUi.Check(parent, false, null);
        ExtUi.Place(toggle, x, y, 20f, 20f);
        TextMeshProUGUI label = ExtUi.Label(parent, text, 14f, ExtUi.TextSub);
        ExtUi.Place(label, x + 30f, y - 2f, 300f, 24f);
        return toggle;
    }

    // ================================================================== 탭·필터

    protected override void OnShow()
    {
        Refresh();
    }

    private void OnTab(int index)
    {
        if (tab == TabCollect && index != TabCollect)
        {
            ReadInputs();
            settings.Save();
        }
        tab = index;
        categoryIndex = 0;
        tablePage = 0;
        UpdateFilters();
        Refresh();
    }

    private void UpdateFilters()
    {
        bool analysis = tab == TabCompetition || tab == TabPrice;
        filterCard.SetActive(tab != TabCollect);
        analysisRoot.SetActive(analysis);
        compareRoot.SetActive(tab == TabCompare);
        collectRoot.SetActive(tab == TabCollect);
        competitionTableRoot.SetActive(tab == TabCompetition);
        priceTableRoot.SetActive(tab == TabPrice);

        countryLabel.gameObject.SetActive(analysis);
        countrySeg.SetActive(analysis);
        categoryLabel.gameObject.SetActive(analysis);
        orderLabel.gameObject.SetActive(tab == TabPrice);
        orderSeg.SetActive(tab == TabPrice);

        if (tab == TabCompare)
        {
            levelSeg.SetLabels(new[] { "광역", "기초" }, level - 1);      // 광역 = 시도·도도부현, 기초 = 시군구·시구정촌
        }
        else
        {
            levelSeg.SetLabels(Country == ExtNames.Korea ? new[] { "시도", "시군구" } : new[] { "도도부현", "시구정촌" }, level - 1);
        }

        if (analysis)
        {
            var labels = new List<string>();
            foreach (string category in Categories)
            {
                labels.Add(tab == TabCompetition ? ExtNames.Scheme(category) : ExtNames.PropertyType(Country, category));
            }
            categorySeg.SetLabels(labels, categoryIndex);
            categorySeg.SetActive(true);
        }
        else
        {
            categorySeg.SetActive(false);
        }
    }

    private void SetPeriods(IEnumerable<string> raw)
    {
        periods.Clear();
        foreach (string p in raw)
        {
            if (!string.IsNullOrEmpty(p) && !periods.Contains(p))
            {
                periods.Add(p);
            }
        }
        periods.Sort((a, b) => PeriodKey(a).CompareTo(PeriodKey(b)));
        int index = periods.IndexOf(period);
        if (index < 0)
        {
            index = 0;
            period = periods.Count > 0 ? periods[0] : "LAST4Q";
        }
        var labels = new List<string>();
        foreach (string p in periods)
        {
            labels.Add(ExtNames.Period(p));
        }
        periodStep.SetItems(labels, index);
    }

    /// <summary>최근 4개 분기, 받은 전체 기간, 그다음 최근 연도부터</summary>
    private static int PeriodKey(string p)
    {
        if (p == "LAST4Q")
        {
            return 0;
        }
        if (p == "ALL")
        {
            return 1;
        }
        int year;
        return int.TryParse(p, NumberStyles.Integer, CultureInfo.InvariantCulture, out year) ? 2 + (10000 - year) : 99999;
    }

    private static List<string> Column(DataTable table, string column)
    {
        var values = new List<string>();
        if (table != null)
        {
            foreach (DataRow row in table.Rows)
            {
                values.Add(ExtUi.Str(row, column));
            }
        }
        return values;
    }

    // ================================================================== 조회

    private async void Refresh()
    {
        int version = ++loadVersion;
        try
        {
            if (tab == TabCollect)
            {
                await RefreshCollect(version);
            }
            else if (tab == TabCompare)
            {
                await RefreshCompare(version);
            }
            else
            {
                await RefreshAnalysis(version);
            }
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    private bool Stale(int version)
    {
        return this == null || version != loadVersion;
    }

    private async Task<Dictionary<string, string>> PrefNames(string country)
    {
        Dictionary<string, string> book;
        if (prefNames.TryGetValue(country, out book))
        {
            return book;
        }
        DataTable table = await Server.GetRegionNamesAsync(country, 1);
        book = ExtNames.Book(country, table);
        prefNames[country] = book;
        return book;
    }

    private string LongName(string country, DataRow row)
    {
        return ExtNames.Pick(country, ExtUi.Str(row, "region_code"), ExtUi.Str(row, "name_ko"),
            ExtUi.Str(row, "name_local"), ExtUi.Str(row, "name_en"));
    }

    private string ShortName(string country, DataRow row, Dictionary<string, string> prefs)
    {
        string parentName;
        prefs.TryGetValue(ExtUi.Str(row, "parent_code"), out parentName);
        int rowLevel = (int)ExtUi.Num(row, "level");
        return ExtNames.Short(country, ExtUi.Str(row, "region_code"), LongName(country, row), rowLevel, parentName);
    }

    private static List<DataRow> Rows(DataTable table)
    {
        var rows = new List<DataRow>();
        if (table != null)
        {
            foreach (DataRow row in table.Rows)
            {
                rows.Add(row);
            }
        }
        return rows;
    }

    // ------------------------------------------------------------------ 경쟁률·가격

    private async Task RefreshAnalysis(int version)
    {
        string country = Country;
        string metric = Metric;
        string category = Category;
        bool competition = tab == TabCompetition;

        DataTable periodTable = await Server.GetStatPeriodsAsync(country, metric, category);
        if (Stale(version))
        {
            return;
        }
        SetPeriods(Column(periodTable, "period"));
        if (periods.Count == 0)
        {
            ShowNoData(competition
                ? ExtNames.Country(country) + " " + ExtNames.Scheme(category) + " 경쟁률 집계가 없습니다."
                : ExtNames.Country(country) + " " + ExtNames.PropertyType(country, category) + " 거래 집계가 없습니다.");
            return;
        }

        // 기다리는 사이 다른 조회가 시작됐거나 화면이 없어졌으면(로그아웃) 더 부르지 않는다
        DataTable regionTable = await Server.GetRegionStatsAsync(country, metric, category, level, period);
        if (Stale(version))
        {
            return;
        }
        DataTable nationalTable = await Server.GetRegionStatsAsync(country, metric, category, 0, period);
        if (Stale(version))
        {
            return;
        }
        Dictionary<string, string> prefs = await PrefNames(country);
        if (Stale(version))
        {
            return;
        }

        List<DataRow> rows = Rows(regionTable);
        DataRow national = nationalTable != null && nationalTable.Rows.Count > 0 ? nationalTable.Rows[0] : null;
        if (competition)
        {
            ShowCompetition(country, category, rows, national, prefs);
        }
        else
        {
            ShowPrice(country, category, rows, national, prefs);
        }
    }

    private void ShowNoData(string what)
    {
        summary.text = what;
        chartTitle.text = "";
        chartNote.text = "";
        ExtUi.Clear(chartArea);
        chartEmpty.text = "데이터 수집 탭에서 자료를 받은 뒤 '집계 다시 만들기'를 실행하세요.";
        tableTitle.text = "";
        tableNote.text = "";
        tableTexts.Clear();
        tableRanked.Clear();
        FillTable();
    }

    private static string AppsLabel(string scheme)
    {
        switch (scheme)
        {
            case "kr_apt": return "1순위 신청";
            case "jp_public_housing": return "응모";
            default: return "등록 신청";
        }
    }

    private void ShowCompetition(string country, string scheme, List<DataRow> rows, DataRow national,
        Dictionary<string, string> prefs)
    {
        var ranked = new List<DataRow>();
        var others = new List<DataRow>();
        foreach (DataRow row in rows)
        {
            double rate = ExtUi.Num(row, "competition_rate");
            if (double.IsNaN(rate))
            {
                continue;
            }
            if (ExtUi.Num(row, "supply_units") >= MinSupply)
            {
                ranked.Add(row);
            }
            else
            {
                others.Add(row);
            }
        }
        Comparison<DataRow> byRate = (a, b) => ExtUi.Num(b, "competition_rate").CompareTo(ExtUi.Num(a, "competition_rate"));
        ranked.Sort(byRate);
        others.Sort(byRate);

        string head = ExtNames.Country(country) + " " + ExtNames.Scheme(scheme) + " · " + ExtNames.Period(period);
        summary.text = national == null
            ? head
            : head + "  |  전국 경쟁률 " + ExtUi.Rate(ExtUi.Num(national, "competition_rate")) +
              " · 공급 " + ExtUi.Int(ExtUi.Num(national, "supply_units")) + "호 · " + AppsLabel(scheme) + " " +
              ExtUi.Int(ExtUi.Num(national, "applications")) + "건 · 자료 " + ExtUi.Int(ExtUi.Num(national, "n_records")) + "건";

        chartTitle.text = "경쟁률 높은 지역 상위 " + Math.Min(ChartRows, ranked.Count);
        chartNote.text = "공급 " + MinSupply + "호 이상 지역 · 경쟁률 = " + AppsLabel(scheme) + " / 공급 호수";
        var items = new List<ExtUi.BarItem>();
        for (int i = 0; i < ranked.Count && i < ChartRows; i++)
        {
            double rate = ExtUi.Num(ranked[i], "competition_rate");
            items.Add(new ExtUi.BarItem { Label = ShortName(country, ranked[i], prefs), Value = rate, Text = ExtUi.Rate(rate) });
        }
        DrawChart(items, "공급 " + MinSupply + "호 이상인 지역이 없습니다.");

        tableTitle.text = "지역별 경쟁률 (" + (ranked.Count + others.Count) + "곳)";
        tableNote.text = others.Count > 0
            ? "공급 " + MinSupply + "호 미만 " + others.Count + "곳은 순위 없이 흐리게 맨 아래에 둡니다."
            : "경쟁률 높은 순";
        tableTexts.Clear();
        tableRanked.Clear();
        int rank = 0;
        foreach (DataRow row in ranked)
        {
            rank++;
            AddCompetitionRow(country, row, prefs, rank.ToString(CultureInfo.InvariantCulture), true);
        }
        foreach (DataRow row in others)
        {
            AddCompetitionRow(country, row, prefs, "-", false);
        }
        FillTable();
    }

    private void AddCompetitionRow(string country, DataRow row, Dictionary<string, string> prefs, string rank, bool isRanked)
    {
        tableTexts.Add(new[]
        {
            rank, ShortName(country, row, prefs), ExtUi.Rate(ExtUi.Num(row, "competition_rate")),
            ExtUi.Int(ExtUi.Num(row, "supply_units")), ExtUi.Int(ExtUi.Num(row, "applications")),
            ExtUi.Int(ExtUi.Num(row, "n_records")),
        });
        tableRanked.Add(isRanked);
    }

    private void ShowPrice(string country, string type, List<DataRow> rows, DataRow national, Dictionary<string, string> prefs)
    {
        var ranked = new List<DataRow>();
        var others = new List<DataRow>();
        foreach (DataRow row in rows)
        {
            if (double.IsNaN(ExtUi.Num(row, "median_unit_price")))
            {
                continue;
            }
            if (ExtUi.Num(row, "n_records") >= MinTrades)
            {
                ranked.Add(row);
            }
            else
            {
                others.Add(row);
            }
        }
        int sign = cheapFirst ? 1 : -1;
        Comparison<DataRow> byPrice = (a, b) =>
            sign * ExtUi.Num(a, "median_unit_price").CompareTo(ExtUi.Num(b, "median_unit_price"));
        ranked.Sort(byPrice);
        others.Sort(byPrice);

        string unit = ExtUi.Sqm + "당";
        string head = ExtNames.Country(country) + " " + ExtNames.PropertyType(country, type) + " · " + ExtNames.Period(period);
        summary.text = national == null
            ? head
            : head + "  |  전국 " + unit + " 중앙값 " + ExtUi.Unit(country, ExtUi.Num(national, "median_unit_price")) +
              " (가운데 절반 " + ExtUi.Unit(country, ExtUi.Num(national, "p25_unit_price")) + " ~ " +
              ExtUi.Unit(country, ExtUi.Num(national, "p75_unit_price")) + ") · 거래가 중앙값 " +
              ExtUi.Price(country, ExtUi.Num(national, "median_price")) + " · 거래 " +
              ExtUi.Int(ExtUi.Num(national, "n_records")) + "건";

        chartTitle.text = unit + " 가격이 " + (cheapFirst ? "싼" : "비싼") + " 지역 상위 " + Math.Min(ChartRows, ranked.Count);
        chartNote.text = "거래 " + MinTrades + "건 이상 지역 · " + unit + " 거래가 중앙값 · 막대 끝: 금액 (가격 지수, 100 = 전국)";
        var items = new List<ExtUi.BarItem>();
        for (int i = 0; i < ranked.Count && i < ChartRows; i++)
        {
            double value = ExtUi.Num(ranked[i], "median_unit_price");
            items.Add(new ExtUi.BarItem
            {
                Label = ShortName(country, ranked[i], prefs),
                Value = value,
                Text = ExtUi.Unit(country, value) + " (" + ExtUi.Index(ExtUi.Num(ranked[i], "price_index")) + ")",
            });
        }
        DrawChart(items, "거래가 " + MinTrades + "건 이상인 지역이 없습니다.");

        tableTitle.text = "지역별 " + unit + " 가격 (" + (ranked.Count + others.Count) + "곳)";
        tableNote.text = others.Count > 0
            ? "거래 " + MinTrades + "건 미만 " + others.Count + "곳은 순위 없이 흐리게 맨 아래에 둡니다."
            : (cheapFirst ? "싼 곳부터" : "비싼 곳부터");
        tableTexts.Clear();
        tableRanked.Clear();
        int rank = 0;
        foreach (DataRow row in ranked)
        {
            rank++;
            AddPriceRow(country, row, prefs, rank.ToString(CultureInfo.InvariantCulture), true);
        }
        foreach (DataRow row in others)
        {
            AddPriceRow(country, row, prefs, "-", false);
        }
        FillTable();
    }

    private void AddPriceRow(string country, DataRow row, Dictionary<string, string> prefs, string rank, bool isRanked)
    {
        tableTexts.Add(new[]
        {
            rank, ShortName(country, row, prefs), ExtUi.Unit(country, ExtUi.Num(row, "median_unit_price")),
            ExtUi.Index(ExtUi.Num(row, "price_index")), ExtUi.Price(country, ExtUi.Num(row, "median_price")),
            ExtUi.Area(ExtUi.Num(row, "median_area_m2")), ExtUi.Int(ExtUi.Num(row, "n_records")),
        });
        tableRanked.Add(isRanked);
    }

    private void DrawChart(List<ExtUi.BarItem> items, string emptyText)
    {
        ExtUi.Clear(chartArea);
        chartEmpty.text = items.Count == 0 ? emptyText : "";
        ExtUi.BarChart(chartArea, 0f, 0f, 808f, items, 40f, 230f);
    }

    private void FillTable()
    {
        ExtUi.PagedTable table = tab == TabCompetition ? competitionTable : priceTable;
        tablePage = table.SetPage(tablePage, tableTexts.Count);
        table.Select(-1);
        table.SetEmptyText("표시할 지역이 없습니다.");
        int start = tablePage * table.PageSize;
        int slot = 0;
        for (; slot < table.PageSize && start + slot < tableTexts.Count; slot++)
        {
            string[] texts = tableTexts[start + slot];
            Color color = tableRanked[start + slot] ? ExtUi.Text : ExtUi.TextMuted;
            var colors = new Color[texts.Length];
            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = color;
            }
            table.SetRow(slot, texts, colors);
        }
        table.HideRowsFrom(slot);
    }

    // ------------------------------------------------------------------ 한·일 비교

    private async Task RefreshCompare(int version)
    {
        DataTable kr = await Server.GetStatPeriodsAsync(ExtNames.Korea);
        if (Stale(version))
        {
            return;
        }
        DataTable jp = await Server.GetStatPeriodsAsync(ExtNames.Japan);
        if (Stale(version))
        {
            return;
        }
        var all = Column(kr, "period");
        all.AddRange(Column(jp, "period"));
        SetPeriods(all);

        for (int i = 0; i < 2; i++)
        {
            string country = Countries[i];
            CompareCard card = compareCards[i];
            DataTable trade0 = await Server.GetRegionStatsAsync(country, "trade", "apartment", 0, period);
            if (Stale(version))
            {
                return;
            }
            DataTable trades = await Server.GetRegionStatsAsync(country, "trade", "apartment", level, period);
            if (Stale(version))
            {
                return;
            }
            var nationalRates = new List<KeyValuePair<string, DataRow>>();
            string chartScheme = null;
            double chartRecords = -1;
            foreach (string scheme in Schemes[country])
            {
                DataTable comp0 = await Server.GetRegionStatsAsync(country, "competition", scheme, 0, period);
                if (Stale(version))
                {
                    return;
                }
                if (comp0 != null && comp0.Rows.Count > 0)
                {
                    nationalRates.Add(new KeyValuePair<string, DataRow>(scheme, comp0.Rows[0]));
                    double records = ExtUi.Num(comp0.Rows[0], "n_records");
                    if (records > chartRecords)
                    {
                        chartRecords = records;
                        chartScheme = scheme;
                    }
                }
            }
            DataTable comps = chartScheme != null
                ? await Server.GetRegionStatsAsync(country, "competition", chartScheme, level, period)
                : null;
            if (Stale(version))
            {
                return;
            }
            Dictionary<string, string> prefs = await PrefNames(country);
            if (Stale(version))
            {
                return;
            }
            FillCompareCard(card, country, trade0, Rows(trades), nationalRates, chartScheme, Rows(comps), prefs);
        }
    }

    private void FillCompareCard(CompareCard card, string country, DataTable trade0, List<DataRow> trades,
        List<KeyValuePair<string, DataRow>> nationalRates, string chartScheme, List<DataRow> comps, Dictionary<string, string> prefs)
    {
        string type = ExtNames.PropertyType(country, "apartment");
        string unit = ExtUi.Sqm + "당";
        card.Title.text = ExtNames.Country(country) + " · " + ExtNames.Period(period);

        DataRow national = trade0 != null && trade0.Rows.Count > 0 ? trade0.Rows[0] : null;
        card.Lines[0].text = national == null
            ? type + " 거래 집계가 이 기간에 없습니다."
            : type + " " + unit + " 중앙값 " + ExtUi.Unit(country, ExtUi.Num(national, "median_unit_price")) +
              " · 거래가 중앙값 " + ExtUi.Price(country, ExtUi.Num(national, "median_price")) +
              " · 거래 " + ExtUi.Int(ExtUi.Num(national, "n_records")) + "건";

        if (nationalRates.Count == 0)
        {
            card.Lines[1].text = "청약 · 추첨 경쟁률 집계가 이 기간에 없습니다.";
        }
        else
        {
            var parts = new List<string>();
            foreach (KeyValuePair<string, DataRow> pair in nationalRates)
            {
                parts.Add(ExtNames.Scheme(pair.Key) + " " + ExtUi.Rate(ExtUi.Num(pair.Value, "competition_rate")) +
                          " (공급 " + ExtUi.Int(ExtUi.Num(pair.Value, "supply_units")) + "호)");
            }
            card.Lines[1].text = "전국 경쟁률: " + string.Join(" · ", parts.ToArray());
        }

        var cheap = new List<DataRow>();
        int low = 0, middle = 0, high = 0;
        foreach (DataRow row in trades)
        {
            double index = ExtUi.Num(row, "price_index");
            if (double.IsNaN(index) || ExtUi.Num(row, "n_records") < MinTrades)
            {
                continue;
            }
            cheap.Add(row);
            if (index < 80)
            {
                low++;
            }
            else if (index < 120)
            {
                middle++;
            }
            else
            {
                high++;
            }
        }
        cheap.Sort((a, b) => ExtUi.Num(a, "price_index").CompareTo(ExtUi.Num(b, "price_index")));
        card.Lines[2].text = cheap.Count == 0
            ? "가격 지수를 낼 지역이 없습니다 (거래 " + MinTrades + "건 이상)."
            : "가격 지수 분포 (" + cheap.Count + "곳): 80 미만 " + low + "곳 · 80~120 " + middle + "곳 · 120 이상 " + high + "곳";
        card.Lines[3].text = cheap.Count == 0
            ? ""
            : "가장 싼 곳 " + ShortName(country, cheap[0], prefs) + " (" + ExtUi.Index(ExtUi.Num(cheap[0], "price_index")) + ")" +
              " · 가장 비싼 곳 " + ShortName(country, cheap[cheap.Count - 1], prefs) + " (" +
              ExtUi.Index(ExtUi.Num(cheap[cheap.Count - 1], "price_index")) + ")";

        card.CheapTitle.text = "전국보다 싼 곳 - 가격 지수 낮은 순 (" + type + ", 상위 " + Math.Min(CompareRows, cheap.Count) + ")";
        var cheapItems = new List<ExtUi.BarItem>();
        for (int i = 0; i < cheap.Count && i < CompareRows; i++)
        {
            double index = ExtUi.Num(cheap[i], "price_index");
            cheapItems.Add(new ExtUi.BarItem
            {
                Label = ShortName(country, cheap[i], prefs),
                Value = index,
                Text = ExtUi.Index(index) + " · " + ExtUi.Unit(country, ExtUi.Num(cheap[i], "median_unit_price")),
            });
        }
        ExtUi.Clear(card.CheapArea);
        ExtUi.BarChart(card.CheapArea, 0f, 0f, 828f, cheapItems, 32f, 230f);

        var ranked = new List<DataRow>();
        foreach (DataRow row in comps)
        {
            if (!double.IsNaN(ExtUi.Num(row, "competition_rate")) && ExtUi.Num(row, "supply_units") >= MinSupply)
            {
                ranked.Add(row);
            }
        }
        ranked.Sort((a, b) => ExtUi.Num(b, "competition_rate").CompareTo(ExtUi.Num(a, "competition_rate")));
        card.CompTitle.text = chartScheme == null
            ? "경쟁이 센 곳 - 경쟁률 집계 없음"
            : "경쟁이 센 곳 - " + ExtNames.Scheme(chartScheme) + " 경쟁률 높은 순 (상위 " + Math.Min(CompareRows, ranked.Count) + ")";
        var compItems = new List<ExtUi.BarItem>();
        for (int i = 0; i < ranked.Count && i < CompareRows; i++)
        {
            double rate = ExtUi.Num(ranked[i], "competition_rate");
            compItems.Add(new ExtUi.BarItem { Label = ShortName(country, ranked[i], prefs), Value = rate, Text = ExtUi.Rate(rate) });
        }
        ExtUi.Clear(card.CompArea);
        ExtUi.BarChart(card.CompArea, 0f, 0f, 828f, compItems, 32f, 230f);
    }

    // ------------------------------------------------------------------ 데이터 수집

    private async Task RefreshCollect(int version)
    {
        RenderLog();
        DataTable status = await Server.GetMarketStatusAsync();
        if (Stale(version))
        {
            return;
        }
        ExtUi.Clear(statusArea);
        float y = 0f;
        int shown = 0;
        foreach (DataRow row in Rows(status))
        {
            string kind = ExtUi.Str(row, "kind");
            double count = ExtUi.Num(row, "row_count");
            if (double.IsNaN(count) || count <= 0)
            {
                continue;       // 집계 행은 자료가 없어도 한 줄이 온다 (COUNT 0)
            }
            shown++;
            string name = StatusName(kind, ExtUi.Str(row, "country"), ExtUi.Str(row, "category"));
            string span = StatusSpan(kind, ExtUi.Str(row, "span_from"), ExtUi.Str(row, "span_to"));
            TextMeshProUGUI a = ExtUi.Label(statusArea, name, 14f, ExtUi.TextSub);
            ExtUi.Place(a, 0f, y, 220f, 26f);
            TextMeshProUGUI b = ExtUi.Label(statusArea, ExtUi.Int(count), 14f, ExtUi.Text, TextAlignmentOptions.MidlineRight);
            ExtUi.Place(b, 220f, y, 90f, 26f);
            TextMeshProUGUI c = ExtUi.Label(statusArea, span, 13f, ExtUi.TextMuted);
            ExtUi.Place(c, 324f, y, 168f, 26f);
            y += 26f;
            if (y > 374f)
            {
                break;
            }
        }
        if (shown == 0)
        {
            TextMeshProUGUI none = ExtUi.Label(statusArea, "아직 받은 자료가 없습니다. 1단계부터 실행하세요.", 14f, ExtUi.TextMuted);
            ExtUi.Place(none, 0f, 0f, 492f, 26f);
        }
    }

    private static string StatusName(string kind, string country, string category)
    {
        switch (kind)
        {
            case "region":
                if (country == ExtNames.Korea)
                {
                    return category == "1" ? "지역 · 한국 시도" : "지역 · 한국 시군구";
                }
                return category == "1" ? "지역 · 일본 도도부현" : "지역 · 일본 시구정촌";
            case "trade":
                return "거래 · " + ExtNames.Country(country) + " " + ExtNames.PropertyType(country, category);
            case "competition":
                return "경쟁률 · " + ExtNames.Scheme(category);
            case "stat":
                return "지역별 집계 (행)";
            case "applicant":
                return "일본 신청자 · " + ExtNames.Scheme(category);
            default:
                return kind;
        }
    }

    private static string StatusSpan(string kind, string first, string last)
    {
        if (kind == "trade")
        {
            return Quarter(first) + " ~ " + Quarter(last);
        }
        if (kind == "stat")
        {
            return string.IsNullOrEmpty(first) ? "" : first + " 기준";
        }
        if (string.IsNullOrEmpty(first) && string.IsNullOrEmpty(last))
        {
            return "";
        }
        return first == last ? first : first + " ~ " + last;
    }

    /// <summary>연도*10+분기 → '2025Q1'</summary>
    private static string Quarter(string key)
    {
        int value;
        if (!int.TryParse(key, NumberStyles.Integer, CultureInfo.InvariantCulture, out value) || value < 10000)
        {
            return key;
        }
        return (value / 10).ToString(CultureInfo.InvariantCulture) + "Q" + (value % 10).ToString(CultureInfo.InvariantCulture);
    }

    private void RenderLog()
    {
        if (logText == null)
        {
            return;
        }
        int start = Math.Max(0, log.Count - 16);
        logText.text = log.Count == 0 ? "아직 실행한 작업이 없습니다." : string.Join("\n", log.GetRange(start, log.Count - start).ToArray());
    }

    private void ReadInputs()
    {
        if (keyDataGoKr == null)
        {
            return;
        }
        settings.dataGoKrKey = keyDataGoKr.text.Trim();
        settings.reinfolibKey = keyReinfolib.text.Trim();
        settings.pythonCommand = string.IsNullOrEmpty(pythonInput.text.Trim()) ? "python" : pythonInput.text.Trim();
        // 숫자 칸은 전각 숫자(입력기)를 반각으로
        settings.krTradeFrom = PythonRunner.Ascii(krTradeFrom.text);
        settings.krTradeTo = PythonRunner.Ascii(krTradeTo.text);
        settings.krPrefs = PythonRunner.Ascii(krCodes.text);
        settings.krCompFrom = PythonRunner.Ascii(krCompFrom.text);
        settings.krCompTo = PythonRunner.Ascii(krCompTo.text);
        settings.jpYears = PythonRunner.Ascii(jpYears.text);
        settings.jpQuarters = PythonRunner.Ascii(jpQuarters.text);
        settings.jpPrefs = PythonRunner.Ascii(jpPrefs.text);
        settings.fx = PythonRunner.Ascii(fxInput.text);
    }

    private void SaveKeys()
    {
        ReadInputs();
        if (settings.Save())
        {
            Toast("저장되었습니다.");
        }
        else
        {
            Alert("설정을 저장하지 못했습니다.");
        }
    }

    // ------------------------------------------------------------------ 실행

    private struct Command
    {
        public string Script;
        public string Arguments;
        public string Title;

        public Command(string script, string arguments, string title)
        {
            Script = script;
            Arguments = arguments;
            Title = title;
        }
    }

    private static Command Market(string arguments, string title)
    {
        return new Command("market_data.py", arguments, title);
    }

    private static Command Analyze()
    {
        return Market("analyze", "집계 다시 만들기");
    }

    /// <summary>명령을 차례로 실행한다. 하나라도 실패하면 멈춘다.</summary>
    private async void Run(string title, List<Command> commands, Action onSuccess = null)
    {
        if (Busy)
        {
            return;
        }
        ReadInputs();
        settings.Save();

        BeginBusy(title + " 준비 중...");
        bool ok = true;
        string error = "";
        try
        {
            foreach (Command command in commands)
            {
                PythonResult result = await RunScript(command.Script, command.Arguments, command.Title, log);
                if (this == null)
                {
                    return;
                }
                RenderLog();
                if (!result.Ok)
                {
                    ok = false;
                    error = command.Title + ": " + Friendly(result);
                    break;
                }
            }
        }
        catch (Exception e)
        {
            ok = false;
            error = e.Message;
            Debug.LogException(e);
        }
        finally
        {
            if (this != null)
            {
                EndBusy();
            }
        }
        if (this == null)
        {
            return;
        }

        prefNames.Clear();
        RenderLog();
        Refresh();
        if (ok)
        {
            try
            {
                main.ChangeLogInsert(MenuName, title);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[확장] 변경 이력을 남기지 못했습니다: " + e.Message);
            }
            if (onSuccess != null)
            {
                onSuccess();
            }
            else
            {
                Toast(title + " 완료");
            }
        }
        else
        {
            Alert(title + " 실패\n" + error);
        }
    }

    private void RunRegions()
    {
        Run("지역 목록 만들기", new List<Command> { Market("regions", "지역 목록 만들기") });
    }

    private void RunJapanCities()
    {
        ReadInputs();
        if (string.IsNullOrEmpty(settings.reinfolibKey))
        {
            Alert("일본 부동산정보 라이브러리 API 키를 먼저 넣고 저장하세요.");
            return;
        }
        string prefs = JapanPrefs(settings.jpPrefs);
        if (prefs == null)
        {
            Alert("도도부현 코드는 13,14,27 처럼 1~47 사이 숫자를 쉼표로 넣으세요 (비우면 47곳 전부).");
            return;
        }
        string arguments = "regions --jp" + (prefs.Length > 0 && prefs != "all" ? " --jp-prefs " + prefs : "");
        Run("일본 시구정촌 받기", new List<Command> { Market(arguments, "일본 시구정촌 받기") });
    }

    private async void RunKoreaTrade()
    {
        if (Busy)
        {
            return;
        }
        ReadInputs();
        if (string.IsNullOrEmpty(settings.dataGoKrKey))
        {
            Alert("공공데이터포털 인증키를 먼저 넣고 저장하세요.");
            return;
        }
        int months = MonthsBetween(settings.krTradeFrom, settings.krTradeTo);
        if (months <= 0)
        {
            Alert("계약년월은 202501 처럼 6자리로, 시작이 끝보다 앞서게 넣으세요 (2006년 이후).");
            return;
        }

        List<string> codes = SplitCodes(settings.krPrefs);
        if (codes == null)
        {
            Alert("지역 코드는 시도 2자리(11,41) 또는 시군구 5자리(11680,41135) 중 한 가지로 쉼표로 넣으세요.");
            return;
        }

        int districts;
        try
        {
            int all = await Server.CountRegionsAsync(ExtNames.Korea, 2, null);
            if (this == null)
            {
                return;
            }
            if (all == 0)
            {
                Alert("지역 목록이 없습니다. 먼저 1단계 '지역 만들기'를 실행하세요.");
                return;
            }
            if (codes.Count > 0 && codes[0].Length == 5)
            {
                districts = codes.Count;
            }
            else
            {
                districts = codes.Count == 0 ? all : await Server.CountRegionsAsync(ExtNames.Korea, 2, codes);
            }
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            Alert("지역 목록을 읽지 못했습니다.\n" + e.Message);
            return;
        }
        if (this == null)
        {
            return;
        }

        string arguments = "kr-trade --from " + settings.krTradeFrom + " --to " + settings.krTradeTo;
        if (codes.Count > 0)
        {
            arguments += (codes[0].Length == 5 ? " --regions " : " --prefs ") + string.Join(",", codes.ToArray());
        }
        if (krRefresh.isOn)
        {
            arguments += " --refresh";
        }
        var commands = new List<Command> { Market(arguments, "한국 실거래가 받기"), Analyze() };

        int calls = districts * months;
        if (calls > 1000)
        {
            Confirm("시군구 " + districts + "곳 x " + months + "개월 = 약 " + calls.ToString("N0", CultureInfo.InvariantCulture) +
                    "번 이상 호출합니다.\n공공데이터포털 개발 계정은 하루 호출 수가 제한됩니다. 계속할까요?\n" +
                    "(이미 받은 달은 건너뜁니다)",
                () => Run("한국 실거래가 받기", commands));
        }
        else
        {
            Run("한국 실거래가 받기", commands);
        }
    }

    private void RunKoreaCompetition()
    {
        ReadInputs();
        if (string.IsNullOrEmpty(settings.dataGoKrKey))
        {
            Alert("공공데이터포털 인증키를 먼저 넣고 저장하세요.");
            return;
        }
        DateTime first, last;
        if (!TryDate(settings.krCompFrom, out first) || !TryDate(settings.krCompTo, out last) || first > last)
        {
            Alert("모집공고일은 2025-01-01 처럼 넣고, 시작이 끝보다 앞서게 넣으세요.");
            return;
        }
        string arguments = "kr-competition --from " + settings.krCompFrom + " --to " + settings.krCompTo;
        Run("한국 청약 경쟁률 받기", new List<Command> { Market(arguments, "한국 청약 경쟁률 받기"), Analyze() });
    }

    private void RunJapanTrade()
    {
        ReadInputs();
        if (string.IsNullOrEmpty(settings.reinfolibKey))
        {
            Alert("일본 부동산정보 라이브러리 API 키를 먼저 넣고 저장하세요.");
            return;
        }
        string years = PythonRunner.Codes(settings.jpYears);
        if (!Regex.IsMatch(years, @"^(19|20)[0-9]{2}(-(19|20)[0-9]{2})?$") && !Regex.IsMatch(years, @"^(19|20)[0-9]{2}(,(19|20)[0-9]{2})*$"))
        {
            Alert("연도는 2024 또는 2023-2025 처럼 넣으세요.");
            return;
        }
        string quarters = PythonRunner.Codes(settings.jpQuarters);
        if (quarters.Length == 0)
        {
            quarters = "1-4";
        }
        if (!Regex.IsMatch(quarters, @"^[1-4](-[1-4])?$") && !Regex.IsMatch(quarters, @"^[1-4](,[1-4])*$"))
        {
            Alert("분기는 1-4 또는 1,2 처럼 넣으세요.");
            return;
        }
        string prefs = JapanPrefs(settings.jpPrefs);
        if (prefs == null)
        {
            Alert("도도부현 코드는 13,14,27 처럼 1~47 사이 숫자를 쉼표로 넣으세요 (비우면 47곳 전부).");
            return;
        }
        string arguments = "jp-trade --years " + years + " --quarters " + quarters + " --prefs " + (prefs.Length == 0 ? "all" : prefs);
        if (jpRefresh.isOn)
        {
            arguments += " --refresh";
        }
        Run("일본 거래가격 받기", new List<Command> { Market(arguments, "일본 거래가격 받기"), Analyze() });
    }

    private void ImportCompetition(string scheme)
    {
        if (Busy)
        {
            return;
        }
        string[] paths = StandaloneFileBrowser.OpenFilePanel(ExtNames.Scheme(scheme) + " 경쟁률 CSV", "", "csv", false);
        if (paths == null || paths.Length == 0 || string.IsNullOrEmpty(paths[0]))
        {
            return;
        }
        string file = paths[0];
        string title = ExtNames.Scheme(scheme) + " 경쟁률 CSV 가져오기 (" + Path.GetFileName(file) + ")";
        Run(title, new List<Command>
        {
            Market("import-competition --scheme " + scheme + " " + PythonRunner.Quote(file), "경쟁률 CSV 가져오기"),
            Analyze(),
        });
    }

    private void RunAnalyze()
    {
        Run("집계 다시 만들기", new List<Command> { Analyze() });
    }

    private void OpenDashboard()
    {
        ReadInputs();
        string fx = PythonRunner.PositiveNumber(settings.fx);
        if (settings.fx.Length > 0 && fx.Length == 0)
        {
            Alert("환율은 9.3 처럼 숫자로 넣으세요 (1엔 = 몇 원).");
            return;
        }
        string arguments = "export" + (fx.Length > 0 ? " --fx " + fx : "");
        string page = ScriptPath("dashboard", "index.html");
        Run("웹 대시보드 데이터 내보내기", new List<Command> { Market(arguments, "웹 대시보드 데이터 내보내기") }, () =>
        {
            try
            {
                Application.OpenURL(new Uri(page).AbsoluteUri);
            }
            catch (Exception e)
            {
                Alert("웹 대시보드를 열지 못했습니다.\n" + page + "\n" + e.Message);
            }
        });
    }

    // ------------------------------------------------------------------ 입력 검사

    /// <summary>YYYYMM 두 개 → 개월 수 (잘못되면 0)</summary>
    private static int MonthsBetween(string first, string last)
    {
        int a, b;
        if (!TryYearMonth(first, out a) || !TryYearMonth(last, out b) || a > b)
        {
            return 0;
        }
        return (b / 100 - a / 100) * 12 + (b % 100 - a % 100) + 1;
    }

    private static bool TryYearMonth(string text, out int value)
    {
        value = 0;
        if (text == null || !Regex.IsMatch(text, @"^[0-9]{6}$"))
        {
            return false;
        }
        value = int.Parse(text, CultureInfo.InvariantCulture);
        int year = value / 100;
        int month = value % 100;
        return year >= 2006 && year <= DateTime.Now.Year && month >= 1 && month <= 12;
    }

    private static bool TryDate(string text, out DateTime value)
    {
        return DateTime.TryParseExact(text ?? "", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out value);
    }

    /// <summary>'11,41' 또는 '11680,41135' → 목록 (비우면 빈 목록, 섞였거나 잘못되면 null)</summary>
    private static List<string> SplitCodes(string text)
    {
        var codes = new List<string>();
        foreach (string part in PythonRunner.Codes(text).Split(','))
        {
            string code = part.Trim();
            if (code.Length == 0)
            {
                continue;
            }
            if (code.Length != 2 && code.Length != 5 || code.Contains("-"))
            {
                return null;
            }
            if (codes.Count > 0 && codes[0].Length != code.Length)
            {
                return null;
            }
            if (!codes.Contains(code))
            {
                codes.Add(code);
            }
        }
        return codes;
    }

    /// <summary>'13,14,27' → '13,14,27', 비우면 '' (전체), 잘못되면 null</summary>
    private static string JapanPrefs(string text)
    {
        if (string.IsNullOrEmpty(text) || text.Trim().ToLowerInvariant() == "all")
        {
            return "";
        }
        var codes = new List<string>();
        foreach (string part in PythonRunner.Codes(text).Split(','))
        {
            int code;
            if (part.Length == 0)
            {
                continue;
            }
            if (!int.TryParse(part, NumberStyles.Integer, CultureInfo.InvariantCulture, out code) || code < 1 || code > 47)
            {
                return null;
            }
            string value = code.ToString("00", CultureInfo.InvariantCulture);
            if (!codes.Contains(value))
            {
                codes.Add(value);
            }
        }
        return string.Join(",", codes.ToArray());
    }
}
