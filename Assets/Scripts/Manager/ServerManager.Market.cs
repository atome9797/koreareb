using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// 확장 화면(시장 분석, 일본 점검)이 쓰는 DB 조회·저장.
/// 테이블은 마이그레이션 2 (DbMigrator.cs, Sql/migrations/002_korea_japan_market.sql) 가 만든다.
/// 모든 값은 파라미터로 넘기고, 조회는 백그라운드 스레드에서 같은 연결·lock 으로 실행한다 (ServerManager.cs 참고).
/// </summary>
public partial class ServerManager
{
    public static readonly string[] JapanSchemes = { "jp_public_housing", "jp_new_condo" };

    private static Dictionary<string, object> P(params object[] pairs)
    {
        var result = new Dictionary<string, object>();
        for (int i = 0; i + 1 < pairs.Length; i += 2)
        {
            result[(string)pairs[i]] = pairs[i + 1];
        }
        return result;
    }

    // ------------------------------------------------------------------ 시장 분석

    /// <summary>지역별 집계 + 지역 이름. metric: trade / competition, level: 0 전국, 1 시도·都道府県, 2 시군구·市区町村</summary>
    public Task<DataTable> GetRegionStatsAsync(string country, string metric, string category, int level, string period)
    {
        const string sql =
            "SELECT s.region_code, s.level, s.n_records, s.median_unit_price, s.p25_unit_price, s.p75_unit_price, " +
            "s.median_price, s.median_area_m2, s.supply_units, s.applications, s.competition_rate, s.price_index, s.currency, " +
            "r.name_ko, r.name_local, r.name_en, r.parent_code " +
            "FROM tb_market_region_stat s " +
            "LEFT JOIN tb_region r ON r.country = s.country AND r.region_code = s.region_code " +
            "WHERE s.country = @country AND s.metric = @metric AND s.category = @category AND s.level = @level AND s.period = @period;";
        return RunQueryAsync("GetRegionStats", db => db.Query(sql, P(
            "@country", country, "@metric", metric, "@category", category, "@level", level, "@period", period)));
    }

    /// <summary>
    /// 화면에서 고를 수 있는 기간: LAST4Q, ALL, 연도 (분기 'YYYYQn' 은 웹 대시보드의 추이 그래프에서만 쓴다).
    /// metric·category 가 null 이면 그 나라의 모든 집계에서 찾는다.
    /// </summary>
    public Task<DataTable> GetStatPeriodsAsync(string country, string metric = null, string category = null)
    {
        var args = P("@country", country);
        string sql = "SELECT DISTINCT period FROM tb_market_region_stat WHERE country = @country AND level = 0 AND period NOT LIKE '____Q_'";
        if (metric != null)
        {
            sql += " AND metric = @metric";
            args["@metric"] = metric;
        }
        if (category != null)
        {
            sql += " AND category = @category";
            args["@category"] = category;
        }
        return RunQueryAsync("GetStatPeriods", db => db.Query(sql + ";", args));
    }

    /// <summary>tb_region 의 지역 수. prefixes 가 있으면 코드 앞 두 자리(시도·都道府県)로 거른다.</summary>
    public Task<int> CountRegionsAsync(string country, int level, IList<string> prefixes)
    {
        var args = P("@country", country, "@level", level);
        var sql = new StringBuilder("SELECT COUNT(*) FROM tb_region WHERE country = @country AND level = @level");
        if (prefixes != null && prefixes.Count > 0)
        {
            sql.Append(" AND substr(region_code, 1, 2) IN (");
            for (int i = 0; i < prefixes.Count; i++)
            {
                string name = "@p" + i;
                sql.Append(i == 0 ? name : ", " + name);
                args[name] = prefixes[i];
            }
            sql.Append(")");
        }
        string text = sql.Append(";").ToString();
        return RunAsync(db => Convert.ToInt32(db.ExecuteScalar(text, args)));
    }

    /// <summary>지역 이름 (시도·都道府県 이름을 붙일 때)</summary>
    public Task<DataTable> GetRegionNamesAsync(string country, int level)
    {
        const string sql = "SELECT region_code, name_ko, name_local, name_en FROM tb_region WHERE country = @country AND level = @level;";
        return RunQueryAsync("GetRegionNames", db => db.Query(sql, P("@country", country, "@level", level)));
    }

    /// <summary>
    /// 데이터 수집 탭의 요약. kind: region / trade / competition / stat / applicant,
    /// country·category 로 무엇인지, span_from·span_to 로 기간(거래는 연도*10+분기, 경쟁률은 공고일, 신청자는 회차)을 준다.
    /// </summary>
    public Task<DataTable> GetMarketStatusAsync()
    {
        const string sql =
            "SELECT 'region' AS kind, country, CAST(level AS TEXT) AS category, COUNT(*) AS row_count, '' AS span_from, '' AS span_to " +
            "  FROM tb_region GROUP BY country, level " +
            "UNION ALL SELECT 'trade', country, property_type, COUNT(*), CAST(MIN(deal_year * 10 + deal_quarter) AS TEXT), " +
            "  CAST(MAX(deal_year * 10 + deal_quarter) AS TEXT) FROM tb_market_trade GROUP BY country, property_type " +
            "UNION ALL SELECT 'competition', country, scheme, COUNT(*), MIN(announce_date), MAX(announce_date) " +
            "  FROM tb_market_competition GROUP BY country, scheme " +
            "UNION ALL SELECT 'stat', '', '', COUNT(*), MAX(built_at), '' FROM tb_market_region_stat " +
            "UNION ALL SELECT 'applicant', 'JP', scheme, COUNT(*), MIN(round_id), MAX(round_id) FROM tb_jp_applicant GROUP BY scheme;";
        return RunQueryAsync("GetMarketStatus", db => db.Query(sql));
    }

    // ------------------------------------------------------------------ 일본 점검

    private static string JapanWhere(string scheme, string round, string level, string status, Dictionary<string, object> args)
    {
        var where = new StringBuilder(" WHERE a.scheme = @scheme");
        args["@scheme"] = scheme;
        if (!string.IsNullOrEmpty(round))
        {
            where.Append(" AND a.round_id = @round");
            args["@round"] = round;
        }
        if (level == "NONE")
        {
            where.Append(" AND r.risk_level IS NULL");       // 아직 점수를 계산하지 않은 신청자
        }
        else if (!string.IsNullOrEmpty(level))
        {
            where.Append(" AND r.risk_level = @level");
            args["@level"] = level;
        }
        if (!string.IsNullOrEmpty(status))
        {
            where.Append(" AND a.inspection_status_result = @status");
            args["@status"] = status;
        }
        return where.ToString();
    }

    /// <summary>일본 신청자 한 쪽. 위험 점수가 높은 순.</summary>
    public Task<DataTable> GetJapanApplicantsAsync(string scheme, string round, string level, string status, int offset, int limit)
    {
        var args = new Dictionary<string, object>();
        string sql =
            "SELECT a.applicant_key, a.round_id, a.project_name, a.unit_category, a.unit_no, a.application_no, " +
            "a.applicant_name, a.inspection_status_result, a.unauth_decision, a.unauth_type, " +
            "r.risk_score, r.risk_level, r.reason_text " +
            "FROM tb_jp_applicant a LEFT JOIN tb_jp_risk r ON r.applicant_key = a.applicant_key" +
            JapanWhere(scheme, round, level, status, args) +
            " ORDER BY COALESCE(r.risk_score, -1) DESC, a.round_id DESC, a.application_no LIMIT @limit OFFSET @offset;";
        args["@limit"] = limit;
        args["@offset"] = offset;
        return RunQueryAsync("GetJapanApplicants", db => db.Query(sql, args));
    }

    /// <summary>조건에 맞는 전체 수와 등급·점검여부별 수 (한 행)</summary>
    public Task<DataTable> GetJapanCountsAsync(string scheme, string round, string level, string status)
    {
        var args = new Dictionary<string, object>();
        string sql =
            "SELECT COUNT(*) AS total, " +
            "SUM(CASE WHEN r.risk_level = '상' THEN 1 ELSE 0 END) AS high, " +
            "SUM(CASE WHEN r.risk_level = '중' THEN 1 ELSE 0 END) AS medium, " +
            "SUM(CASE WHEN r.risk_level = '하' THEN 1 ELSE 0 END) AS low, " +
            "SUM(CASE WHEN r.risk_level IS NULL THEN 1 ELSE 0 END) AS unscored, " +
            "SUM(CASE WHEN a.inspection_status_result = 'N' THEN 1 ELSE 0 END) AS status_n, " +
            "SUM(CASE WHEN a.inspection_status_result = 'Y' THEN 1 ELSE 0 END) AS status_y, " +
            "SUM(CASE WHEN a.inspection_status_result = 'Z' THEN 1 ELSE 0 END) AS status_z " +
            "FROM tb_jp_applicant a LEFT JOIN tb_jp_risk r ON r.applicant_key = a.applicant_key" +
            JapanWhere(scheme, round, level, status, args) + ";";
        return RunQueryAsync("GetJapanCounts", db => db.Query(sql, args));
    }

    public Task<DataTable> GetJapanRoundsAsync(string scheme)
    {
        const string sql = "SELECT DISTINCT round_id FROM tb_jp_applicant WHERE scheme = @scheme ORDER BY round_id DESC;";
        return RunQueryAsync("GetJapanRounds", db => db.Query(sql, P("@scheme", scheme)));
    }

    /// <summary>상세 패널용 한 명 (개인정보 포함: 화면에만 보이고 로그에 남기지 않는다)</summary>
    public Task<DataTable> GetJapanApplicantDetailAsync(string applicantKey)
    {
        const string sql =
            "SELECT a.*, r.risk_score, r.risk_level, r.reason_codes, r.reason_text, r.scored_at " +
            "FROM tb_jp_applicant a LEFT JOIN tb_jp_risk r ON r.applicant_key = a.applicant_key " +
            "WHERE a.applicant_key = @key;";
        return RunQueryAsync("GetJapanApplicantDetail", db => db.Query(sql, P("@key", applicantKey)));
    }

    /// <summary>점검여부 저장 (N 미점검 / Y 점검완료 / Z 해당없음). 적발 판정이 있는 점검완료 행은 바꾸지 않는다.</summary>
    public Task<int> SaveJapanInspectionAsync(List<string> keys, string status)
    {
        if (status != "N" && status != "Y" && status != "Z")
        {
            throw new ArgumentException("점검여부는 N, Y, Z 중 하나: " + status);
        }

        List<string> copy = new List<string>(keys);
        const string sql =
            "UPDATE tb_jp_applicant SET inspection_status_result = @status, inspected_at = datetime('now', 'localtime') " +
            "WHERE applicant_key = @key AND NOT (inspection_status_result = 'Y' AND unauth_decision = 1);";
        return RunAsync(db =>
        {
            int changed = 0;
            db.RunInTransaction(() =>
            {
                foreach (string key in copy)
                {
                    changed += db.ExecuteNonQuery(sql, P("@status", status, "@key", key));
                }
            });
            return changed;
        });
    }

    /// <summary>점검 결과 저장 (decision 0 통과 / 1 적발 + 유형). 점검완료(Y) 행에만 저장한다.</summary>
    public Task<int> SaveJapanResultAsync(List<string> keys, int decision, string type)
    {
        List<string> copy = new List<string>(keys);
        const string sql =
            "UPDATE tb_jp_applicant SET unauth_decision = @decision, unauth_type = @type, inspected_at = datetime('now', 'localtime') " +
            "WHERE applicant_key = @key AND inspection_status_result = 'Y';";
        return RunAsync(db =>
        {
            int changed = 0;
            db.RunInTransaction(() =>
            {
                foreach (string key in copy)
                {
                    changed += db.ExecuteNonQuery(sql, P("@decision", decision, "@type", type, "@key", key));
                }
            });
            return changed;
        });
    }
}
