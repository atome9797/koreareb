using System.Collections.Generic;
using System.Data;

/// <summary>
/// 화면에 쓰는 이름 (나라, 제도, 거래 유형, 기간, 지역). 웹 대시보드(korea_rebs/dashboard/index.html)와 같은 이름을 쓴다.
/// 일본 지역은 한국어 이름 → 일본어 이름(일본어 글꼴이 있을 때) → 영어 이름 순으로 고른다.
/// </summary>
public static class ExtNames
{
    public const string Korea = "KR";
    public const string Japan = "JP";
    public const string National = "00";

    private static readonly Dictionary<string, string> KrShort = new Dictionary<string, string>
    {
        { "서울특별시", "서울" }, { "부산광역시", "부산" }, { "대구광역시", "대구" }, { "인천광역시", "인천" },
        { "광주광역시", "광주" }, { "대전광역시", "대전" }, { "울산광역시", "울산" }, { "세종특별자치시", "세종" },
        { "경기도", "경기" }, { "강원특별자치도", "강원" }, { "강원도", "강원" }, { "충청북도", "충북" }, { "충청남도", "충남" },
        { "전북특별자치도", "전북" }, { "전라북도", "전북" }, { "전라남도", "전남" }, { "경상북도", "경북" },
        { "경상남도", "경남" }, { "제주특별자치도", "제주" },
    };

    public static string Country(string country)
    {
        return country == Japan ? "일본" : "한국";
    }

    /// <summary>청약·추첨 제도 (tb_market_competition.scheme, tb_jp_applicant.scheme)</summary>
    public static string Scheme(string scheme)
    {
        switch (scheme)
        {
            case "kr_apt": return "아파트 청약";
            case "jp_public_housing": return "공영주택 추첨";
            case "jp_new_condo": return "신축 맨션 추첨";
            default: return scheme ?? "";
        }
    }

    /// <summary>거래 유형 (tb_market_trade.property_type)</summary>
    public static string PropertyType(string country, string type)
    {
        switch (type)
        {
            case "apartment": return country == Japan ? "맨션" : "아파트";
            case "house": return "토지·건물";
            case "land": return "토지";
            case "other": return "기타";
            default: return type ?? "";
        }
    }

    /// <summary>집계 기간: LAST4Q, ALL, 'YYYY', 'YYYYQn'</summary>
    public static string Period(string period)
    {
        if (period == "LAST4Q")
        {
            return "최근 4개 분기";
        }
        if (period == "ALL")
        {
            return "받은 전체 기간";
        }
        if (period != null && period.Length == 6 && period[4] == 'Q')
        {
            return period.Substring(0, 4) + "년 " + period.Substring(5) + "분기";
        }
        return period + "년";
    }

    /// <summary>tb_region 의 이름 열로 고른 지역 이름 (긴 이름)</summary>
    public static string Pick(string country, string code, string ko, string local, string en)
    {
        if (code == National)
        {
            return "전국";
        }
        if (!string.IsNullOrEmpty(ko))
        {
            return ko;
        }
        if (country == Japan)
        {
            if (!string.IsNullOrEmpty(local) && ExtFonts.HasJapanese)
            {
                return local;
            }
            if (!string.IsNullOrEmpty(en))
            {
                return en;
            }
        }
        if (!string.IsNullOrEmpty(local))
        {
            return local;
        }
        return code ?? "";
    }

    /// <summary>
    /// 그래프 축처럼 좁은 곳에 쓰는 짧은 이름.
    /// 한국: '서울특별시 강남구' → '서울 강남구'. 일본 市区町村: '네리마구 (도쿄)' 처럼 都道府県를 붙인다.
    /// </summary>
    public static string Short(string country, string code, string longName, int level, string parentName)
    {
        if (code == National)
        {
            return "전국";
        }
        if (country == Korea)
        {
            string[] parts = (longName ?? "").Split(' ');
            string head;
            if (parts.Length > 1 && KrShort.TryGetValue(parts[0], out head))
            {
                return head + " " + string.Join(" ", parts, 1, parts.Length - 1);
            }
            return KrShort.TryGetValue(longName ?? "", out head) ? head : longName;
        }
        if (level < 2 || string.IsNullOrEmpty(parentName))
        {
            return longName;
        }
        return longName + " (" + ShortPref(parentName) + ")";
    }

    /// <summary>도쿄도 → 도쿄, 오사카부 → 오사카, 가나가와현 → 가나가와 (홋카이도는 그대로)</summary>
    public static string ShortPref(string name)
    {
        if (string.IsNullOrEmpty(name) || name == "홋카이도" || name.Length < 3)
        {
            return name ?? "";
        }
        char last = name[name.Length - 1];
        return last == '도' || last == '부' || last == '현' ? name.Substring(0, name.Length - 1) : name;
    }

    /// <summary>지역 코드 → 긴 이름 (GetRegionNamesAsync 결과로 만든다)</summary>
    public static Dictionary<string, string> Book(string country, DataTable table)
    {
        var book = new Dictionary<string, string>();
        if (table == null)
        {
            return book;
        }
        foreach (DataRow row in table.Rows)
        {
            string code = ExtUi.Str(row, "region_code");
            book[code] = Pick(country, code, ExtUi.Str(row, "name_ko"), ExtUi.Str(row, "name_local"), ExtUi.Str(row, "name_en"));
        }
        return book;
    }
}
