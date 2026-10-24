using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using SFB;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 일본점검 화면 (GNB '일본점검'): 일본 기준 부정청약 관리.
///  - 제도: 공영주택 추첨(都営·区営·市営·公社), 신축 맨션 추첨(분양 등록 추첨)
///  - 신청자 CSV 가져오기 → 규칙 기반 위험 점수(jp_fraud.py, 근거: krjp/jp_rules.py) → 위험 높은 순으로 점검
///  - 점검여부(미점검 N / 점검완료 Y / 해당없음 Z)와 결과(통과 / 적발 + 유형)를 저장. 한국 실시·등록 화면과 같은 규칙:
///    적발로 저장된 신청자는 점검여부를 바꿀 수 없고, 결과는 점검완료 신청자에게만 저장한다.
/// 신청자 개인정보는 화면에만 보이고 로그·변경 이력에는 남기지 않는다 (변경 이력에는 제도·회차·인원만).
/// </summary>
public sealed class JapanInspectionScreen : ExtScreen
{
    public override string MenuName
    {
        get { return "일본 점검"; }
    }

    private const int PageSize = 16;

    private static readonly string[] SchemeCodes = { "jp_public_housing", "jp_new_condo" };
    private static readonly string[] LevelCodes = { null, "상", "중", "하", "NONE" };
    private static readonly string[] LevelLabels = { "전체", "상", "중", "하", "미산정" };
    private static readonly string[] StatusCodes = { null, "N", "Y", "Z" };
    private static readonly string[] StatusLabels = { "전체", "미점검", "점검완료", "해당없음" };

    /// <summary>적발 유형. 공영주택: 거주·소득·세대 요건과 명의, 신축: 명의 대여·다호실·전매 (jp_rules.py 의 규칙과 맞춘다)</summary>
    private static readonly Dictionary<string, string[]> ResultTypes = new Dictionary<string, string[]>
    {
        { "jp_public_housing", new[] { "거주 요건 위장", "소득 허위", "세대 위장", "명의 대여·전대", "중복 신청", "기타" } },
        { "jp_new_condo", new[] { "명의 대여", "다호실·중복 신청", "전매 목적", "허위 신고", "기타" } },
    };

    private struct RowState
    {
        public string Status;
        public int Decision;      // -1 없음, 0 통과, 1 적발
    }

    // ------------------------------------------------------------------ 상태

    private int schemeIndex;
    private readonly List<string> rounds = new List<string>();
    private int roundIndex;                  // 0 = 전체 회차
    private int levelIndex;
    private int statusIndex;
    private int page;
    private int loadVersion;                 // 목록·건수 조회
    private int roundsVersion;               // 회차 목록 조회 (목록 조회와 따로 센다: 필터를 눌러도 회차 목록은 버리지 않는다)
    private int detailVersion;
    private readonly List<string> pageKeys = new List<string>();
    private readonly List<RowState> pageStates = new List<RowState>();
    private readonly Dictionary<string, RowState> checkedRows = new Dictionary<string, RowState>();
    private string detailKey;

    // ------------------------------------------------------------------ 화면

    private ExtUi.Segmented schemeSeg;
    private ExtUi.Stepper roundStep;
    private ExtUi.Segmented levelSeg;
    private ExtUi.Segmented statusSeg;
    private TextMeshProUGUI counts;
    private ExtUi.PagedTable table;
    private TextMeshProUGUI detailText;
    private TextMeshProUGUI riskHead;
    private TextMeshProUGUI riskText;
    private TextMeshProUGUI stateText;
    private RectTransform resultButtons;
    private TextMeshProUGUI selectionText;

    private string Scheme
    {
        get { return SchemeCodes[schemeIndex]; }
    }

    private string Round
    {
        get { return roundIndex > 0 && roundIndex - 1 < rounds.Count ? rounds[roundIndex - 1] : null; }
    }

    // ================================================================== 만들기

    protected override void Build()
    {
        ExtUi.Title(Root, "일본 부정청약 점검");

        // 오른쪽 위 버튼 (오른쪽 끝에서 왼쪽으로)
        string[] labels = { "양식 폴더", "위험 점수 다시 계산", "신청자 CSV 가져오기" };
        UnityEngine.Events.UnityAction[] actions =
        {
            () => OpenFolder(ScriptPath("templates")),
            Rescore,
            ImportApplicants,
        };
        float right = 1780f;
        for (int i = 0; i < labels.Length; i++)
        {
            float w = Mathf.Max(110f, ExtUi.TextWidth(labels[i], 15f) + 36f);
            Button button = ExtUi.Button(Root, labels[i], actions[i], i == labels.Length - 1);
            ExtUi.Place(button, right - w, 18f, w, 38f);
            right -= w + 8f;
        }

        // 필터
        RectTransform filter = ExtUi.Card(Root, 40f, 70f, 1740f, 60f);
        FilterLabel(filter, 24f, "제도", 44f);
        schemeSeg = new ExtUi.Segmented(filter, 68f, 12f, 36f, index =>
        {
            schemeIndex = index;
            checkedRows.Clear();
            roundIndex = 0;
            page = 0;
            BuildResultButtons();
            LoadRounds();
        });
        schemeSeg.SetLabels(new[] { ExtNames.Scheme(SchemeCodes[0]), ExtNames.Scheme(SchemeCodes[1]) }, 0);

        FilterLabel(filter, 349f, "회차", 40f);
        roundStep = new ExtUi.Stepper(filter, 393f, 12f, 260f, 36f, index =>
        {
            roundIndex = index;
            page = 0;
            Reload();
        });

        FilterLabel(filter, 685f, "위험 등급", 72f);
        levelSeg = new ExtUi.Segmented(filter, 759f, 12f, 36f, index =>
        {
            levelIndex = index;
            page = 0;
            Reload();
        });
        levelSeg.SetLabels(LevelLabels, 0);

        FilterLabel(filter, 1133f, "점검여부", 64f);
        statusSeg = new ExtUi.Segmented(filter, 1199f, 12f, 36f, index =>
        {
            statusIndex = index;
            page = 0;
            Reload();
        });
        statusSeg.SetLabels(StatusLabels, 0);

        counts = ExtUi.Label(Root, "", 16f, ExtUi.TextSub);
        counts.richText = true;                 // 숫자만 넣는다 (이름 같은 데이터 글자는 넣지 않는다)
        ExtUi.Place(counts, 40f, 140f, 1740f, 30f);

        // 목록
        RectTransform list = ExtUi.Card(Root, 40f, 178f, 1120f, 742f);
        table = new ExtUi.PagedTable(list, 16f, 16f, 1088f,
            new[] { "위험", "회차", "물건 · 단지", "구분 · 호실", "신청번호", "이름", "점검여부", "결과" },
            new[] { 84f, 90f, 240f, 150f, 120f, 150f, 84f, 116f },
            null, PageSize, 36f, true);
        table.RowClicked = OnRowClicked;
        table.RowChecked = OnRowChecked;
        table.AllChecked = OnAllChecked;
        table.PageChanged = target =>
        {
            page = target;
            Reload();
        };

        // 상세
        RectTransform detail = ExtUi.Card(Root, 1180f, 178f, 600f, 742f);
        ExtUi.Heading(detail, 24f, 22f, "신청자 상세");
        detailText = ExtUi.Paragraph(detail, "", 14f, ExtUi.Text);
        ExtUi.Place(detailText, 24f, 58f, 552f, 372f);
        riskHead = ExtUi.Label(detail, "", 18f, ExtUi.Text, TextAlignmentOptions.MidlineLeft, true);
        ExtUi.Place(riskHead, 24f, 440f, 552f, 28f);
        riskText = ExtUi.Paragraph(detail, "", 14f, ExtUi.TextSub);
        ExtUi.Place(riskText, 24f, 474f, 552f, 150f);
        stateText = ExtUi.Paragraph(detail, "", 14f, ExtUi.TextSub);
        ExtUi.Place(stateText, 24f, 630f, 552f, 46f);
        TextMeshProUGUI note = ExtUi.Paragraph(detail,
            "위험 점수는 먼저 볼 신청자를 고르는 참고값이며 부정청약 판정이 아닙니다. 판정은 서류 · 현장 확인 뒤 담당자가 입력합니다.",
            12f, ExtUi.TextMuted);
        ExtUi.Place(note, 24f, 684f, 552f, 44f);

        // 저장 막대
        RectTransform bar = ExtUi.Card(Root, 40f, 932f, 1740f, 68f);
        TextMeshProUGUI statusLabel = ExtUi.Label(bar, "점검여부", 15f, ExtUi.TextSub);
        ExtUi.Place(statusLabel, 24f, 0f, 70f, 68f);
        float x = 100f;
        x = ExtUi.ButtonAt(bar, x, 15f, 38f, "미점검", () => SetStatus("N"));
        x = ExtUi.ButtonAt(bar, x, 15f, 38f, "점검완료", () => SetStatus("Y"));
        x = ExtUi.ButtonAt(bar, x, 15f, 38f, "해당없음", () => SetStatus("Z"));
        TextMeshProUGUI resultLabel = ExtUi.Label(bar, "결과", 15f, ExtUi.TextSub);
        ExtUi.Place(resultLabel, x + 24f, 0f, 40f, 68f);
        resultButtons = ExtUi.Place(ExtUi.Create("Results", bar), x + 68f, 0f, 1430f - (x + 68f), 68f);
        selectionText = ExtUi.Label(bar, "", 14f, ExtUi.TextMuted, TextAlignmentOptions.MidlineRight);
        ExtUi.Place(selectionText, 1440f, 0f, 160f, 68f);
        ExtUi.ButtonAt(bar, 1610f, 15f, 38f, "선택 해제", ClearSelection, false, 106f);

        BuildResultButtons();
        ShowDetail(null);
    }

    private static void FilterLabel(Transform parent, float x, string text, float width)
    {
        TextMeshProUGUI label = ExtUi.Label(parent, text, 15f, ExtUi.TextSub);
        ExtUi.Place(label, x, 0f, width, 60f);
    }

    private void BuildResultButtons()
    {
        if (resultButtons == null)
        {
            return;
        }
        ExtUi.Clear(resultButtons);
        float x = ExtUi.ButtonAt(resultButtons, 0f, 15f, 38f, "통과", () => SetResult(0, null), false, 80f);   // 통과는 유형 없음
        foreach (string type in ResultTypes[Scheme])
        {
            string chosen = type;
            x = ExtUi.ButtonAt(resultButtons, x, 15f, 38f, type, () => SetResult(1, chosen), false, 80f);
        }
    }

    protected override void OnShow()
    {
        LoadRounds();
    }

    // ================================================================== 조회

    private async void LoadRounds()
    {
        int version = ++roundsVersion;
        string current = Round;
        DataTable table1;
        try
        {
            table1 = await Server.GetJapanRoundsAsync(Scheme);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            return;
        }
        if (this == null || version != roundsVersion)
        {
            return;
        }
        rounds.Clear();
        foreach (DataRow row in table1.Rows)
        {
            string round = ExtUi.Str(row, "round_id");
            if (round.Length > 0)
            {
                rounds.Add(round);
            }
        }
        var labels = new List<string> { "전체 회차" };
        labels.AddRange(rounds);
        int index = current != null ? rounds.IndexOf(current) + 1 : roundIndex;
        roundIndex = Mathf.Clamp(index, 0, rounds.Count);
        roundStep.SetItems(labels, roundIndex);
        Reload();
    }

    private async void Reload()
    {
        int version = ++loadVersion;
        string scheme = Scheme;
        string round = Round;
        string level = LevelCodes[levelIndex];
        string status = StatusCodes[statusIndex];
        try
        {
            DataTable all = await Server.GetJapanCountsAsync(scheme, round, null, null);
            if (this == null || version != loadVersion)
            {
                return;
            }
            DataTable filtered = level == null && status == null ? all : await Server.GetJapanCountsAsync(scheme, round, level, status);
            if (this == null || version != loadVersion)
            {
                return;
            }
            DataRow a = all.Rows.Count > 0 ? all.Rows[0] : null;
            DataRow f = filtered.Rows.Count > 0 ? filtered.Rows[0] : null;
            int total = Count(f, "total");
            ShowCounts(a, total);

            page = table.SetPage(page, total);
            DataTable rows = await Server.GetJapanApplicantsAsync(scheme, round, level, status, page * PageSize, PageSize);
            if (this == null || version != loadVersion)
            {
                return;
            }
            FillTable(rows, total);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            Alert("신청자 목록을 읽지 못했습니다.\n" + e.Message);
        }
    }

    private static int Count(DataRow row, string column)
    {
        double value = ExtUi.Num(row, column);
        return double.IsNaN(value) ? 0 : (int)value;
    }

    private void ShowCounts(DataRow all, int filteredTotal)
    {
        if (all == null || Count(all, "total") == 0)
        {
            counts.text = ExtNames.Scheme(Scheme) + " 신청자가 없습니다. 오른쪽 위 '신청자 CSV 가져오기'로 가져오세요 (양식: korea_rebs/templates).";
            UpdateSelectionText();
            return;
        }
        var sb = new StringBuilder();
        sb.Append(ExtNames.Scheme(Scheme)).Append(" · ").Append((Round ?? "전체 회차").Replace("<", "").Replace(">", "")).Append("  ");
        sb.Append("전체 ").Append(Count(all, "total").ToString("N0", CultureInfo.InvariantCulture)).Append("명");
        sb.Append("   위험 <color=#EF8674>상 ").Append(Count(all, "high").ToString("N0", CultureInfo.InvariantCulture)).Append("</color>");
        sb.Append(" · <color=#FAB219>중 ").Append(Count(all, "medium").ToString("N0", CultureInfo.InvariantCulture)).Append("</color>");
        sb.Append(" · 하 ").Append(Count(all, "low").ToString("N0", CultureInfo.InvariantCulture));
        int unscored = Count(all, "unscored");
        if (unscored > 0)
        {
            sb.Append(" · 미산정 ").Append(unscored.ToString("N0", CultureInfo.InvariantCulture));
        }
        sb.Append("   점검 <color=#2DBBBC>미점검 ").Append(Count(all, "status_n").ToString("N0", CultureInfo.InvariantCulture)).Append("</color>");
        sb.Append(" · 점검완료 ").Append(Count(all, "status_y").ToString("N0", CultureInfo.InvariantCulture));
        sb.Append(" · 해당없음 ").Append(Count(all, "status_z").ToString("N0", CultureInfo.InvariantCulture));
        if (LevelCodes[levelIndex] != null || StatusCodes[statusIndex] != null)
        {
            sb.Append("   조건에 맞는 ").Append(filteredTotal.ToString("N0", CultureInfo.InvariantCulture)).Append("명");
        }
        counts.text = sb.ToString();
        UpdateSelectionText();
    }

    private void FillTable(DataTable rows, int total)
    {
        pageKeys.Clear();
        pageStates.Clear();
        table.Select(-1);
        table.SetEmptyText(total == 0 ? "조건에 맞는 신청자가 없습니다." : "");
        int slot = 0;
        bool allChecked = rows.Rows.Count > 0;
        foreach (DataRow row in rows.Rows)
        {
            if (slot >= PageSize)
            {
                break;
            }
            string key = ExtUi.Str(row, "applicant_key");
            var state = new RowState
            {
                Status = ExtUi.Str(row, "inspection_status_result"),
                Decision = double.IsNaN(ExtUi.Num(row, "unauth_decision")) ? -1 : (int)ExtUi.Num(row, "unauth_decision"),
            };
            pageKeys.Add(key);
            pageStates.Add(state);

            string riskLevel = ExtUi.Str(row, "risk_level");
            double score = ExtUi.Num(row, "risk_score");
            string risk = riskLevel.Length == 0 ? "미산정" : riskLevel + " " + ExtUi.Int(score);
            string unit = ExtUi.Str(row, "unit_category");
            string unitNo = ExtUi.Str(row, "unit_no");
            if (unitNo.Length > 0)
            {
                unit = unit.Length > 0 ? unit + " · " + unitNo : unitNo;
            }

            string[] texts =
            {
                risk, ExtUi.Str(row, "round_id"), ExtUi.Str(row, "project_name"), unit,
                ExtUi.Str(row, "application_no"), ExtUi.Str(row, "applicant_name"), StatusName(state.Status),
                ResultName(state.Decision, ExtUi.Str(row, "unauth_type")),
            };
            Color[] colors =
            {
                LevelColor(riskLevel), ExtUi.TextSub, ExtUi.Text, ExtUi.TextSub, ExtUi.TextSub, ExtUi.Text,
                state.Status == "N" ? ExtUi.AccentText : ExtUi.TextSub,
                state.Decision == 1 ? ExtUi.Danger : ExtUi.TextSub,
            };
            bool isChecked = checkedRows.ContainsKey(key);
            allChecked &= isChecked;
            table.SetRow(slot, texts, colors, isChecked);
            if (key == detailKey)
            {
                table.Select(slot);
            }
            slot++;
        }
        table.HideRowsFrom(slot);
        table.SetAllChecked(allChecked);
        UpdateSelectionText();

        if (detailKey != null && !pageKeys.Contains(detailKey))
        {
            ShowDetail(null);
        }
        else if (detailKey != null)
        {
            LoadDetail(detailKey);       // 저장 뒤 점검 상태를 새로 보여 준다
        }
    }

    private static string StatusName(string status)
    {
        switch (status)
        {
            case "Y": return "점검완료";
            case "Z": return "해당없음";
            default: return "미점검";
        }
    }

    private static string ResultName(int decision, string type)
    {
        if (decision == 0)
        {
            return "통과";
        }
        if (decision == 1)
        {
            return string.IsNullOrEmpty(type) ? "적발" : "적발 · " + type;
        }
        return "-";
    }

    private static Color LevelColor(string level)
    {
        switch (level)
        {
            case "상": return ExtUi.Danger;
            case "중": return ExtUi.Warning;
            case "하": return ExtUi.TextSub;
            default: return ExtUi.TextMuted;
        }
    }

    // ------------------------------------------------------------------ 선택·상세

    private void OnRowClicked(int slot)
    {
        if (slot < 0 || slot >= pageKeys.Count)
        {
            return;
        }
        table.Select(slot);
        detailKey = pageKeys[slot];
        LoadDetail(detailKey);
    }

    private void OnRowChecked(int slot, bool on)
    {
        if (slot < 0 || slot >= pageKeys.Count)
        {
            return;
        }
        if (on)
        {
            checkedRows[pageKeys[slot]] = pageStates[slot];
        }
        else
        {
            checkedRows.Remove(pageKeys[slot]);
        }
        bool allChecked = pageKeys.Count > 0;
        foreach (string key in pageKeys)
        {
            allChecked &= checkedRows.ContainsKey(key);
        }
        table.SetAllChecked(allChecked);
        UpdateSelectionText();
    }

    private void OnAllChecked(bool on)
    {
        for (int i = 0; i < pageKeys.Count; i++)
        {
            if (on)
            {
                checkedRows[pageKeys[i]] = pageStates[i];
            }
            else
            {
                checkedRows.Remove(pageKeys[i]);
            }
        }
        Reload();
    }

    private void ClearSelection()
    {
        checkedRows.Clear();
        Reload();
    }

    private void UpdateSelectionText()
    {
        if (selectionText != null)
        {
            selectionText.text = checkedRows.Count == 0 ? "선택 없음" : "선택 " + checkedRows.Count.ToString("N0", CultureInfo.InvariantCulture) + "명";
        }
    }

    private async void LoadDetail(string key)
    {
        int version = ++detailVersion;
        DataTable detail;
        try
        {
            detail = await Server.GetJapanApplicantDetailAsync(key);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            return;
        }
        if (this == null || version != detailVersion)
        {
            return;
        }
        ShowDetail(detail.Rows.Count > 0 ? detail.Rows[0] : null);
    }

    private void ShowDetail(DataRow row)
    {
        if (row == null)
        {
            detailKey = null;
            detailText.text = "목록에서 신청자를 누르면 여기에 자세한 내용이 나옵니다.";
            riskHead.text = "";
            riskText.text = "";
            stateText.text = "";
            return;
        }

        bool condo = ExtUi.Str(row, "scheme") == "jp_new_condo";
        var lines = new List<string>();
        Line(lines, "회차", ExtUi.Str(row, "round_id"));
        Line(lines, "신청번호", ExtUi.Str(row, "application_no"));
        Line(lines, condo ? "물건" : "주택", ExtUi.Str(row, "project_name"));
        Line(lines, condo ? "호실" : "구분", Join(ExtUi.Str(row, "unit_category"), ExtUi.Str(row, "unit_no")));
        Line(lines, "이름", ExtUi.Str(row, "applicant_name"));
        Line(lines, "생년월일", ExtUi.Str(row, "birth_date"));
        Line(lines, "연락처", Join(ExtUi.Str(row, "phone"), ExtUi.Str(row, "email")));
        Line(lines, "주소", ExtUi.Str(row, "address"));
        Line(lines, "지역 코드", ExtUi.Str(row, "region_code"));
        if (condo)
        {
            Line(lines, "법인명", ExtUi.Str(row, "corporate_name"));
            Line(lines, "구입 목적", ExtUi.Str(row, "purchase_purpose"));
            Line(lines, "본인 거주", YesNo(row, "will_reside"));
            Line(lines, "계약 명의", ExtUi.Str(row, "contract_name"));
            Line(lines, "인도 전 매물", YesNo(row, "resale_listed"));
        }
        else
        {
            Line(lines, "도내 전입일", ExtUi.Str(row, "moved_in_date"));
            string household = ExtUi.Int(ExtUi.Num(row, "household_size"));
            string changed = ExtUi.Str(row, "household_changed_date");
            Line(lines, "세대", household == "-" ? "" : household + "명" + (changed.Length > 0 ? " (변경일 " + changed + ")" : ""));
            double income = ExtUi.Num(row, "household_income");
            Line(lines, "세대 소득", double.IsNaN(income) ? "" : ExtUi.Int(income) + "엔");
            Line(lines, "특별 구분 세대", YesNo(row, "disabled_household"));     // 60세 이상·장애인·고교생 이하 자녀 등 (소득 상한이 높다)
            Line(lines, "현 거주", ExtUi.Str(row, "current_housing"));
        }
        Line(lines, "신청일", ExtUi.Str(row, "applied_date"));
        detailText.text = string.Join("\n", lines.ToArray());

        string level = ExtUi.Str(row, "risk_level");
        if (level.Length == 0)
        {
            riskHead.text = "위험 점수: 미산정";
            riskHead.color = ExtUi.TextMuted;
            riskText.text = "'위험 점수 다시 계산'을 누르면 계산합니다.";
        }
        else
        {
            riskHead.text = "위험 점수 " + ExtUi.Int(ExtUi.Num(row, "risk_score")) + "점 · " + level;
            riskHead.color = LevelColor(level) == ExtUi.TextSub ? ExtUi.Text : LevelColor(level);
            string reasons = ExtUi.Str(row, "reason_text");
            riskText.text = reasons.Length == 0
                ? "해당하는 규칙이 없습니다."
                : "· " + reasons.Replace(" / ", "\n· ");
        }

        double decision = ExtUi.Num(row, "unauth_decision");
        string inspected = ExtUi.Str(row, "inspected_at");
        stateText.text = "점검여부: " + StatusName(ExtUi.Str(row, "inspection_status_result")) +
                         " · 결과: " + ResultName(double.IsNaN(decision) ? -1 : (int)decision, ExtUi.Str(row, "unauth_type")) +
                         (inspected.Length > 0 ? " · 마지막 저장 " + inspected : "");
    }

    private static void Line(List<string> lines, string label, string value)
    {
        lines.Add(label + "  " + (string.IsNullOrEmpty(value) ? "-" : value));
    }

    private static string Join(string a, string b)
    {
        if (a.Length > 0 && b.Length > 0)
        {
            return a + " · " + b;
        }
        return a.Length > 0 ? a : b;
    }

    private static string YesNo(DataRow row, string column)
    {
        double value = ExtUi.Num(row, column);
        if (double.IsNaN(value))
        {
            return "";
        }
        return value != 0 ? "예" : "아니오";
    }

    // ================================================================== 저장

    private void SetStatus(string status)
    {
        if (Busy)
        {
            return;
        }
        if (checkedRows.Count == 0)
        {
            Alert("먼저 목록에서 신청자를 선택하세요.");
            return;
        }
        int locked = 0;
        foreach (RowState state in checkedRows.Values)
        {
            if (state.Status == "Y" && state.Decision == 1)
            {
                locked++;
            }
        }
        if (locked > 0)
        {
            Alert("적발로 저장된 신청자 " + locked + "명이 선택되어 있습니다.\n결과를 '통과'로 바꾼 뒤에 점검여부를 바꿀 수 있습니다.");
            return;
        }
        var keys = new List<string>(checkedRows.Keys);
        string label = StatusName(status);
        Confirm("선택한 " + keys.Count + "명의 점검여부를 '" + label + "'(으)로 저장할까요?", () =>
            Save(() => Server.SaveJapanInspectionAsync(keys, status), keys.Count, "점검여부 " + label));
    }

    private void SetResult(int decision, string type)
    {
        if (Busy)
        {
            return;
        }
        if (checkedRows.Count == 0)
        {
            Alert("먼저 목록에서 신청자를 선택하세요.");
            return;
        }
        int notDone = 0;
        foreach (RowState state in checkedRows.Values)
        {
            if (state.Status != "Y")
            {
                notDone++;
            }
        }
        if (notDone > 0)
        {
            Alert("점검완료가 아닌 신청자 " + notDone + "명이 선택되어 있습니다.\n결과는 점검여부가 '점검완료'인 신청자에게만 저장할 수 있습니다.");
            return;
        }
        var keys = new List<string>(checkedRows.Keys);
        string label = decision == 0 ? "통과" : "적발 (" + type + ")";
        Confirm("선택한 " + keys.Count + "명의 결과를 '" + label + "'(으)로 저장할까요?", () =>
            Save(() => Server.SaveJapanResultAsync(keys, decision, type), keys.Count, "결과 " + label));
    }

    private async void Save(Func<Task<int>> work, int requested, string what)
    {
        if (Busy)
        {
            return;
        }
        BeginBusy("저장 중...");
        int changed = -1;
        string error = null;
        try
        {
            changed = await work();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            error = e.Message;
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
        if (error != null)
        {
            Alert("저장하지 못했습니다.\n" + error);
            return;
        }

        try
        {
            main.ChangeLogInsert(MenuName, ExtNames.Scheme(Scheme) + " " + (Round ?? "전체 회차") + " " + changed + "명 " + what);
        }
        catch (Exception e)
        {
            Debug.LogWarning("[확장] 변경 이력을 남기지 못했습니다: " + e.Message);
        }
        checkedRows.Clear();
        Reload();
        if (changed < requested)
        {
            Alert("선택한 " + requested + "명 중 " + changed + "명을 저장했습니다.\n" +
                  "나머지는 조회 뒤 상태가 바뀌었거나(적발 확정, 점검완료 아님) DB 에 없습니다. 목록을 다시 확인하세요.");
        }
        else
        {
            Toast("저장되었습니다.");
        }
    }

    // ================================================================== 가져오기·점수

    private void ImportApplicants()
    {
        if (Busy)
        {
            return;
        }
        string scheme = Scheme;
        string[] paths = StandaloneFileBrowser.OpenFilePanel(ExtNames.Scheme(scheme) + " 신청자 CSV", "", "csv", false);
        if (paths == null || paths.Length == 0 || string.IsNullOrEmpty(paths[0]))
        {
            return;
        }
        string file = paths[0];
        Confirm(ExtNames.Scheme(scheme) + " 신청자 CSV 를 가져옵니다.\n" + Path.GetFileName(file) + "\n\n" +
                "같은 회차 · 신청번호는 새 내용으로 바뀌고 점검 결과는 그대로 둡니다. 가져온 뒤 위험 점수를 다시 계산합니다.",
            () => RunImport(scheme, file));
    }

    private async void RunImport(string scheme, string file)
    {
        if (Busy)
        {
            return;
        }
        BeginBusy("신청자 가져오기 준비 중...");
        PythonResult imported = null;
        PythonResult scored = null;
        try
        {
            imported = await RunScript("jp_fraud.py", "import --scheme " + scheme + " " + PythonRunner.Quote(file), "신청자 가져오기", null);
            if (this != null && imported.Ok)
            {
                scored = await RunScript("jp_fraud.py", "score --scheme " + scheme, "위험 점수 계산", null);
            }
        }
        catch (Exception e)
        {
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

        if (imported == null || !imported.Ok)
        {
            Alert("신청자를 가져오지 못했습니다.\n" + (imported == null ? "" : Friendly(imported)));
            return;
        }
        try
        {
            main.ChangeLogInsert(MenuName, Path.GetFileName(file) + " " + ExtNames.Scheme(scheme) + " 신청자 가져오기");
        }
        catch (Exception e)
        {
            Debug.LogWarning("[확장] 변경 이력을 남기지 못했습니다: " + e.Message);
        }
        string message = imported.LastOutput;
        message += scored != null && scored.Ok ? "\n" + scored.LastOutput : "\n위험 점수를 계산하지 못했습니다: " + (scored == null ? "" : Friendly(scored));
        Alert(message);
        LoadRounds();
    }

    private async void Rescore()
    {
        if (Busy)
        {
            return;
        }
        string scheme = Scheme;
        BeginBusy("위험 점수 계산 준비 중...");
        PythonResult result = null;
        try
        {
            // 중복 규칙은 같은 회차 전체를 봐야 하므로 제도 전체를 다시 계산한다 (회차 단위로 묶어 계산한다)
            result = await RunScript("jp_fraud.py", "score --scheme " + scheme, "위험 점수 계산", null);
        }
        catch (Exception e)
        {
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
        if (result == null || !result.Ok)
        {
            Alert("위험 점수를 계산하지 못했습니다.\n" + (result == null ? "" : Friendly(result)));
            return;
        }
        Toast(result.LastOutput);
        Reload();
    }
}
