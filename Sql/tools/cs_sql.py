"""Assets/Scripts/Data/DatabaseAccess.cs 안의 SQL 템플릿(const string)을 그대로 읽어 쿼리를 만든다.

C# 컴파일 없이 앱이 실제로 실행할 SQL 을 검증하기 위한 것. 템플릿 문자열은 C# 파일에서 그대로 가져오고,
{TOKEN} 을 채우는 작은 함수들(Sido, CityFilter, RiskExpr ...)만 C# 코드를 한 줄씩 옮겼다.
함수 이름/인자는 legacy_sql.py 와 같아서 verify_queries.py 가 둘을 같은 방식으로 실행한다.
"""
import os, re

CS_PATH = os.environ.get("DATABASE_ACCESS_CS", os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "Scripts", "Data", "DatabaseAccess.cs"))
_src = open(CS_PATH, encoding="utf-8").read()


def _const(name):
    m = re.search(r'public const string ' + name + r' = @"((?:[^"]|"")*)";', _src)
    if not m:
        raise KeyError(name)
    return m.group(1).replace('""', '"')


T = {n: _const(n) for n in [
    "SqlStatisticsByRegion", "SqlStatisticsByDanji", "SqlMarketByDanji", "SqlExecuteRankedCte", "SqlExecutePageCte",
    "SqlExecuteListColumns", "SqlExecuteAliasColumns", "SqlExecuteList", "SqlExecuteFull", "SqlExecuteCounts",
    "SqlPercentPerson", "SqlMapByDanger", "SqlMapByDangerLocal", "SqlMapByDangerPercent"]}

CITY_ID = [11, 26, 27, 28, 29, 30, 31, 36, 41, 42, 43, 44, 45, 46, 47, 48, 50]
PERCENT = ["1", "0.5", "0.25", "0.1"]


def _flag(a, i):
    return a is not None and 0 <= i < len(a) and a[i]


def _first(a, fb):
    for i, v in enumerate(a or []):
        if v:
            return i
    return fb


def Sido(p):
    return "(CASE " + p + "apt_sup_pos / 1000 WHEN 51 THEN 42 WHEN 52 THEN 45 ELSE " + p + "apt_sup_pos / 1000 END)"


def Sigungu(p):
    return "(CASE " + p + "apt_sup_pos / 1000 WHEN 51 THEN " + p + "apt_sup_pos - 9000 WHEN 52 THEN " + p + "apt_sup_pos - 7000 ELSE " + p + "apt_sup_pos END)"


def CityFilter(toggle, p, prefix):
    names = []
    if toggle is not None:
        i = 1
        while i < len(toggle) and i <= len(CITY_ID):
            if toggle[i]:
                name = "@c" + str(len(names))
                p[name] = CITY_ID[i - 1]
                names.append(name)
            i += 1
    if not names:
        return "0"
    return Sido(prefix) + " IN (" + ", ".join(names) + ")"


def RiskExpr(bust, rfxg, prefix):
    b = _first(bust, 0); m = _first(rfxg, 0)
    kind = "fake" if b == 1 else "sale" if b == 2 else "unauth"
    if m == 1:
        return prefix + kind + "_rf_prob"
    if m == 2:
        return prefix + kind + "_xgb_prob"
    return "(" + prefix + kind + "_rf_prob + " + prefix + kind + "_xgb_prob) / 2"


def PercentLiteral(pct):
    i = _first(pct, 0)
    return PERCENT[i] if i < len(PERCENT) else PERCENT[0]


def LikePattern(t):
    s = t or ""
    return "%" + s.replace("\\", "\\\\").replace("%", "\\%").replace("_", "\\_") + "%"


def Like(col):
    return col + " LIKE @q ESCAPE '\\'"


def MarketSearchFilter(st, text, p):
    if _flag(st, 0) and text:
        p["@q"] = LikePattern(text)
        return " AND (" + Like("r.apt_name") + " OR " + Like("r.apt_number") + " OR " + Like("b.bjdong_name") + ")"
    if _flag(st, 1):
        p["@q"] = LikePattern(text); return " AND " + Like("r.apt_name")
    if _flag(st, 2):
        p["@q"] = LikePattern(text); return " AND " + Like("r.apt_number")
    if _flag(st, 3):
        p["@q"] = LikePattern(text); return " AND " + Like("b.bjdong_name")
    return ""


def LocationJoin(st, text):
    needed = (_flag(st, 0) and bool(text)) or (not _flag(st, 0) and not _flag(st, 1) and not _flag(st, 2) and _flag(st, 3))
    return "\n    LEFT JOIN tb_bjdong b ON b.bjdong_code = " + Sigungu("r.") if needed else ""


def DateFilter(sort_type, f, t, p, prefix):
    if sort_type == "StatisticMapSort":
        p["@from"] = int(str(f).strip() or 0); p["@to"] = int(str(t).strip() or 0)
        return prefix + "rec_anno_date BETWEEN @from AND @to AND " + prefix + "inspection_status_result = 'Y'"
    p["@from"] = f or ""; p["@to"] = t or ""
    return prefix + "change_date BETWEEN @from AND @to"


def Fill(template, *kv):
    s = template
    for i in range(0, len(kv) - 1, 2):
        s = s.replace(kv[i], kv[i + 1])
    return s


def _py(p):
    """C# keys carry '@'; Python's sqlite3 wants the bare name."""
    return {k[1:] if k.startswith("@") else k: v for k, v in p.items()}


# ---- mirrors of the public C# methods --------------------------------------------------
def statistics_by_region(f, t, city):
    p = {"@from": int(f), "@to": int(t)}
    return Fill(T["SqlStatisticsByRegion"], "{SIDO}", Sido(""), "{CITY}", CityFilter(city, p, "")), _py(p)


def statistics_by_danji(f, t, city, bust, rfxg, pct):
    p = {"@from": int(f), "@to": int(t)}
    sql = Fill(T["SqlStatisticsByDanji"], "{SIDO}", Sido(""), "{RISK}", RiskExpr(bust, rfxg, ""), "{CITY}", CityFilter(city, p, ""),
               "{SIGUNGU_K}", Sigungu("k."), "{PCT}", PercentLiteral(pct))
    return sql, _py(p)


def market_by_danji(f, t, st, text, city, bust, rfxg, pct):
    p = {"@from": int(f), "@to": int(t)}
    sql = Fill(T["SqlMarketByDanji"], "{SIDO_R}", Sido("r."), "{RISK_R}", RiskExpr(bust, rfxg, "r."), "{LOCATION_JOIN}", LocationJoin(st, text),
               "{CITY_R}", CityFilter(city, p, "r."), "{SEARCH}", MarketSearchFilter(st, text, p), "{PCT}", PercentLiteral(pct),
               "{SIGUNGU_R}", Sigungu("r."))
    return sql, _py(p)


def _ranked(f, t, st, text, city, bust, rfxg, pct, p):
    p["@from"] = int(f); p["@to"] = int(t)
    return Fill(T["SqlExecuteRankedCte"], "{RISK_R}", RiskExpr(bust, rfxg, "r."), "{LOCATION_JOIN}", LocationJoin(st, text),
                "{CITY_R}", CityFilter(city, p, "r."), "{SEARCH}", MarketSearchFilter(st, text, p), "{PCT}", PercentLiteral(pct))


def market_execute_list(f, t, st, text, city, bust, rfxg, pct, offset=None, limit=None):
    p = {}
    cte = _ranked(f, t, st, text, city, bust, rfxg, pct, p)
    source = "k"
    if limit is not None and limit > 0:
        cte += T["SqlExecutePageCte"]
        p["@limit"] = int(limit); p["@offset"] = max(0, int(offset or 0))
        source = "pg"
    sql = cte + Fill(T["SqlExecuteList"], "{LIST_COLUMNS}", T["SqlExecuteListColumns"], "{ALIAS_COLUMNS}", T["SqlExecuteAliasColumns"], "{SOURCE}", source)
    return sql, _py(p)


def market_execute_full(f, t, st, text, city, bust, rfxg, pct):
    p = {}
    cte = _ranked(f, t, st, text, city, bust, rfxg, pct, p)
    sql = cte + Fill(T["SqlExecuteFull"], "{ALIAS_COLUMNS}", T["SqlExecuteAliasColumns"], "{SIDO_R}", Sido("r."), "{SIGUNGU_R}", Sigungu("r."))
    return sql, _py(p)


def market_execute_counts(f, t, st, text, city, bust, rfxg, pct):
    p = {}
    cte = _ranked(f, t, st, text, city, bust, rfxg, pct, p)
    return cte + T["SqlExecuteCounts"], _py(p)


def percent_person(sort_type, s, e, city):
    p = {}
    sql = Fill(T["SqlPercentPerson"], "{DATE}", DateFilter(sort_type, s, e, p, ""), "{CITY}", CityFilter(city, p, ""))
    return sql, _py(p)


def map_by_danger(city, sort_type, f, t, typ):
    p = {}
    order = "everage DESC, " + Sido("") if typ == "score" else Sido("")
    sql = Fill(T["SqlMapByDanger"], "{DATE}", DateFilter(sort_type, f, t, p, ""), "{CITY}", CityFilter(city, p, ""), "{ORDER}", order, "{SIDO}", Sido(""))
    return sql, _py(p)


def map_by_danger_local(city, sort_type, f, t, typ):
    p = {}
    score = typ == "score"
    sql = Fill(T["SqlMapByDangerLocal"], "{DATE}", DateFilter(sort_type, f, t, p, "r."), "{CITY_R}", CityFilter(city, p, "r."),
               "{GROUP}", "r.apt_name" if score else Sigungu("r.") + " / 10",
               "{ORDER}", "everage DESC, MIN(r.apt_name)" if score else Sigungu("r.") + " / 10",
               "{SIGUNGU_R}", Sigungu("r."))
    if score:
        sql += " LIMIT 10"
    return sql, _py(p)


def map_by_danger_percent(city, sort_type, f, t, limits):
    p = {}
    for i in range(4):
        text = limits[i] if limits is not None and i < len(limits) else "0"
        try:
            p["@n" + str(i)] = int(float(text) // 1)
        except ValueError:
            p["@n" + str(i)] = 0
    sql = Fill(T["SqlMapByDangerPercent"], "{DATE}", DateFilter(sort_type, f, t, p, ""), "{CITY}", CityFilter(city, p, ""))
    return sql, _py(p)


def last_create_date():
    m = re.search(r'return Query\("(SELECT datetime\(MAX\(change_date\)[^"]*)"\);', _src)
    return m.group(1), {}


def leftover_tokens():
    """Any {TOKEN} left unreplaced would be a bug in the C# Fill() calls."""
    return sorted(set(re.findall(r"\{[A-Z_]+\}", "".join(T.values()))))
