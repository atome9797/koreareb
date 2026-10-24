using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Text;
using Mono.Data.Sqlite;

/// <summary>
/// SQLite 접근 계층.
///
/// 이전 버전과 달라진 점
/// - 모든 값은 파라미터로 바인딩한다. (이전: 문자열 결합 → 검색어/아이디에 ' 가 들어가면 오류 또는 SQL 인젝션)
/// - 연결 하나를 열어 두고 재사용한다. 스레드 안전하지 않으므로 ServerManager 가 lock 으로 직렬화한다.
/// - 대량 쓰기는 트랜잭션 하나 + 재사용 커맨드로 처리한다. (이전: 행마다 연결을 열고 자동 커밋)
/// - 조회 쿼리는 필요한 컬럼만 먼저 좁게 걸러낸 뒤(커버링 인덱스) 마지막에만 넓은 행을 읽는다.
///   SQL 은 Sql/tools 의 검증 스크립트가 이 파일에서 그대로 꺼내 이전 쿼리와 결과를 비교한다.
///   그래서 템플릿은 const 문자열 + {토큰} 치환 형태로 둔다. 토큰 이름을 바꾸면 스크립트도 같이 바꿀 것.
/// </summary>
public class DatabaseAccess : IDisposable
{
    private SqliteConnection m_DatabaseConnection;

    /// <summary>SelectMenu.cityToggles[1..17] 과 같은 순서의 시도 코드 (0번 토글은 '전체')</summary>
    private static readonly int[] CityId = { 11, 26, 27, 28, 29, 30, 31, 36, 41, 42, 43, 44, 45, 46, 47, 48, 50 };

    /// <summary>실시·등록 화면에서 '결과 저장' 시 바뀌는 컬럼. ViewExecute.setNames 와 같은 순서.</summary>
    public static readonly string[] ExecuteResultColumns =
    {
        "inspection_status", "unauth_sub_decision", "unauth_sub_type",
        "unauth_disguise_transfer", "unauth_savecerti_sale", "unauth_other", "inspection_status_result"
    };

    private static readonly string[] PercentLiterals = { "1", "0.5", "0.25", "0.1" };

    private static readonly HashSet<string> RiskTables = new HashSet<string> { "tb_riskdata", "tb_checkresult" };
    private static readonly HashSet<string> LogTables = new HashSet<string> { "tb_connect_log", "tb_change_log", "tb_down_log", "tb_member" };
    private static readonly HashSet<string> ReadableTables = new HashSet<string> { "tb_bjdong", "tb_member" };

    public DatabaseAccess(string connectionString)
    {
        OpenDatabase(connectionString);
    }

    public void OpenDatabase(string connectionString)
    {
        m_DatabaseConnection = new SqliteConnection(connectionString);
        m_DatabaseConnection.Open();

        // 다른 프로세스(파이썬 스크립트, DB Browser)가 쓰는 중이면 바로 'database is locked' 로 실패하지 않고 기다린다.
        ExecuteNonQuery("PRAGMA busy_timeout = 5000;");
        // 정렬/윈도우 함수 임시 데이터는 메모리에서 처리, 페이지 캐시 64MB
        ExecuteNonQuery("PRAGMA temp_store = MEMORY;");
        ExecuteNonQuery("PRAGMA cache_size = -65536;");
    }

    public string JournalMode
    {
        get { return Convert.ToString(ExecuteScalar("PRAGMA journal_mode;"), CultureInfo.InvariantCulture); }
    }

    /// <summary>WAL 은 읽기와 쓰기가 서로 막지 않고 커밋이 빠르다. 실패하면(읽기 전용 위치 등) 기존 모드를 유지한다.</summary>
    public bool TryEnableWal()
    {
        try
        {
            string mode = Convert.ToString(ExecuteScalar("PRAGMA journal_mode = WAL;"), CultureInfo.InvariantCulture);

            if (string.Equals(mode, "wal", StringComparison.OrdinalIgnoreCase))
            {
                // WAL 에서는 NORMAL 로도 손상 없이 안전하다(전원 차단 시 마지막 커밋만 사라질 수 있음).
                ExecuteNonQuery("PRAGMA synchronous = NORMAL;");
                return true;
            }
        }
        catch (SqliteException)
        {
        }

        return false;
    }

    public void CloseSqlConnection()
    {
        if (m_DatabaseConnection != null)
        {
            m_DatabaseConnection.Close();
            m_DatabaseConnection.Dispose();
        }

        m_DatabaseConnection = null;
    }

    public void Dispose()
    {
        CloseSqlConnection();
    }

    // =====================================================================================
    // 실행 도우미
    // =====================================================================================

    private static void AddParameters(SqliteCommand cmd, Dictionary<string, object> parameters)
    {
        if (parameters == null)
        {
            return;
        }

        foreach (KeyValuePair<string, object> kv in parameters)
        {
            cmd.Parameters.AddWithValue(kv.Key, kv.Value ?? DBNull.Value);
        }
    }

    public DataTable Query(string sql, Dictionary<string, object> parameters = null)
    {
        using (SqliteDataAdapter adapter = new SqliteDataAdapter(sql, m_DatabaseConnection))
        {
            SqliteCommand cmd = (SqliteCommand)adapter.SelectCommand;
            AddParameters(cmd, parameters);

            DataSet ds = new DataSet();
            adapter.Fill(ds);
            cmd.Dispose();

            if (ds.Tables.Count == 0)
            {
                return new DataTable();
            }

            // 다른 DataSet 에 넣을 수 있도록 떼어서 돌려준다
            DataTable table = ds.Tables[0];
            ds.Tables.Remove(table);
            return table;
        }
    }

    public int ExecuteNonQuery(string sql, Dictionary<string, object> parameters = null)
    {
        using (SqliteCommand cmd = m_DatabaseConnection.CreateCommand())
        {
            cmd.CommandText = sql;
            AddParameters(cmd, parameters);
            return cmd.ExecuteNonQuery();
        }
    }

    public object ExecuteScalar(string sql, Dictionary<string, object> parameters = null)
    {
        using (SqliteCommand cmd = m_DatabaseConnection.CreateCommand())
        {
            cmd.CommandText = sql;
            AddParameters(cmd, parameters);
            return cmd.ExecuteScalar();
        }
    }

    /// <summary>
    /// work 를 트랜잭션 하나로 실행한다. 예외가 나면 전부 되돌린다.
    /// 기본은 BEGIN IMMEDIATE (시작할 때 쓰기 잠금을 잡아 다른 프로세스와 교착하지 않는다).
    /// deferred = true 는 BEGIN (임시 테이블에만 쓰는 경우처럼 DB 쓰기 잠금이 필요 없을 때).
    /// Mono.Data.Sqlite 는 IsolationLevel.ReadCommitted 를 BEGIN, Serializable(기본)을 BEGIN IMMEDIATE 로 바꾼다.
    /// </summary>
    public void RunInTransaction(Action work, bool deferred = false)
    {
        using (SqliteTransaction transaction = deferred
                   ? m_DatabaseConnection.BeginTransaction(IsolationLevel.ReadCommitted)
                   : m_DatabaseConnection.BeginTransaction())
        {
            try
            {
                work();
                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
    }

    private static void Require(HashSet<string> allowed, string name)
    {
        if (name == null || !allowed.Contains(name))
        {
            throw new ArgumentException("허용되지 않은 테이블/컬럼 이름: " + name);
        }
    }

    // =====================================================================================
    // SQL 조각 (전부 화이트리스트 값만 만든다. 사용자 입력은 항상 파라미터로 간다)
    // =====================================================================================

    private static bool Flag(bool[] flags, int index)
    {
        return flags != null && index >= 0 && index < flags.Length && flags[index];
    }

    private static int FirstTrue(bool[] flags, int fallback)
    {
        if (flags != null)
        {
            for (int i = 0; i < flags.Length; i++)
            {
                if (flags[i])
                {
                    return i;
                }
            }
        }

        return fallback;
    }

    // 2023-06 강원(42)→강원특별자치도(51), 2024-01 전북(45)→전북특별자치도(52).
    // 새 코드는 기존 코드로 합쳐서 17개 지역 토글이 그대로 동작하고 행이 조회에서 빠지지 않게 한다.
    private static string Sido(string prefix)
    {
        return "(CASE " + prefix + "apt_sup_pos / 1000 WHEN 51 THEN 42 WHEN 52 THEN 45 ELSE " + prefix + "apt_sup_pos / 1000 END)";
    }

    private static string Sigungu(string prefix)
    {
        return "(CASE " + prefix + "apt_sup_pos / 1000 WHEN 51 THEN " + prefix + "apt_sup_pos - 9000 WHEN 52 THEN " + prefix + "apt_sup_pos - 7000 ELSE " + prefix + "apt_sup_pos END)";
    }

    /// <summary>선택한 시도만 남기는 조건. 아무것도 선택하지 않으면 결과 없음(이전: SQL 문법 오류).</summary>
    private static string CityFilter(bool[] cityToggle, Dictionary<string, object> p, string prefix)
    {
        List<string> names = new List<string>();

        if (cityToggle != null)
        {
            for (int i = 1; i < cityToggle.Length && i <= CityId.Length; i++)
            {
                if (!cityToggle[i])
                {
                    continue;
                }

                string name = "@c" + names.Count;
                p[name] = CityId[i - 1];
                names.Add(name);
            }
        }

        if (names.Count == 0)
        {
            return "0";
        }

        return Sido(prefix) + " IN (" + string.Join(", ", names.ToArray()) + ")";
    }

    /// <summary>위험도 식. BustType: 부정청약/위장전입/통장매매, RfxgType: 평균/RF/XGB</summary>
    private static string RiskExpr(bool[] bustType, bool[] rfxgType, string prefix)
    {
        int bust = FirstTrue(bustType, 0);
        int model = FirstTrue(rfxgType, 0);
        string kind = bust == 1 ? "fake" : bust == 2 ? "sale" : "unauth";

        if (model == 1)
        {
            return prefix + kind + "_rf_prob";
        }

        if (model == 2)
        {
            return prefix + kind + "_xgb_prob";
        }

        return "(" + prefix + kind + "_rf_prob + " + prefix + kind + "_xgb_prob) / 2";
    }

    /// <summary>상위 % 조건. 문자열로 그대로 넣어 '* 1' 의 결과 타입(INTEGER)이 이전 쿼리와 같게 한다.</summary>
    private static string PercentLiteral(bool[] percentType)
    {
        int i = FirstTrue(percentType, 0);
        return i < PercentLiterals.Length ? PercentLiterals[i] : PercentLiterals[0];
    }

    /// <summary>LIKE 검색 패턴. 사용자가 입력한 %, _ 는 글자 그대로 찾도록 이스케이프한다.</summary>
    private static string LikePattern(string text)
    {
        string s = text ?? "";
        return "%" + s.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";
    }

    private static string Like(string column)
    {
        return column + " LIKE @q ESCAPE '\\'";
    }

    /// <summary>시장관리 화면 검색: 0=전체(단지명/주택관리번호/지역), 1=단지명, 2=주택관리번호, 3=지역</summary>
    private static string MarketSearchFilter(bool[] searchType, string searchText, Dictionary<string, object> p)
    {
        if (Flag(searchType, 0) && !string.IsNullOrEmpty(searchText))
        {
            p["@q"] = LikePattern(searchText);
            return " AND (" + Like("r.apt_name") + " OR " + Like("r.apt_number") + " OR " + Like("b.bjdong_name") + ")";
        }

        if (Flag(searchType, 1))
        {
            p["@q"] = LikePattern(searchText);
            return " AND " + Like("r.apt_name");
        }

        if (Flag(searchType, 2))
        {
            p["@q"] = LikePattern(searchText);
            return " AND " + Like("r.apt_number");
        }

        if (Flag(searchType, 3))
        {
            p["@q"] = LikePattern(searchText);
            return " AND " + Like("b.bjdong_name");
        }

        return "";
    }

    /// <summary>지역명 검색이 있을 때만 tb_bjdong 을 조인한다 (행마다 조인하는 비용을 줄임).</summary>
    private static string LocationJoin(bool[] searchType, string searchText)
    {
        bool needed = (Flag(searchType, 0) && !string.IsNullOrEmpty(searchText))
                      || (!Flag(searchType, 0) && !Flag(searchType, 1) && !Flag(searchType, 2) && Flag(searchType, 3));

        return needed ? "\n    LEFT JOIN tb_bjdong b ON b.bjdong_code = " + Sigungu("r.") : "";
    }

    /// <summary>대시보드(MainMapSort)는 업로드일시, 통계(StatisticMapSort)는 모집공고일 + 점검완료 기준.</summary>
    private static string DateFilter(string sortType, string fromDate, string toDate, Dictionary<string, object> p, string prefix)
    {
        if (sortType == "StatisticMapSort")
        {
            p["@from"] = ParseLong(fromDate);
            p["@to"] = ParseLong(toDate);
            return prefix + "rec_anno_date BETWEEN @from AND @to AND " + prefix + "inspection_status_result = 'Y'";
        }

        p["@from"] = fromDate ?? "";
        p["@to"] = toDate ?? "";
        return prefix + "change_date BETWEEN @from AND @to";
    }

    private static long ParseLong(string value)
    {
        long result;
        return long.TryParse((value ?? "").Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out result) ? result : 0L;
    }

    private static string Fill(string template, params string[] tokensAndValues)
    {
        StringBuilder sb = new StringBuilder(template);

        for (int i = 0; i + 1 < tokensAndValues.Length; i += 2)
        {
            sb.Replace(tokensAndValues[i], tokensAndValues[i + 1]);
        }

        return sb.ToString();
    }

    // =====================================================================================
    // 회원 / 로그인
    // =====================================================================================

    public DataTable ReadFullTable(string tableName)
    {
        Require(ReadableTables, tableName);
        return Query("SELECT * FROM " + tableName + ";");
    }

    public DataTable ReadUserTable(string userId)
    {
        return Query("SELECT * FROM tb_member WHERE mem_userid = @id;", new Dictionary<string, object> { { "@id", userId ?? "" } });
    }

    /// <summary>로그인 실패 횟수 +1 (이전: 화면에서 읽은 값 + 1 을 덮어씀 → 동시 로그인 시 누락 가능)</summary>
    public void IncreaseUserFailCount(string userId)
    {
        ExecuteNonQuery("UPDATE tb_member SET mem_failcount = COALESCE(mem_failcount, 0) + 1 WHERE mem_userid = @id;",
            new Dictionary<string, object> { { "@id", userId ?? "" } });
    }

    public void ResetUserFailCount(string userId)
    {
        ExecuteNonQuery("UPDATE tb_member SET mem_failcount = 0 WHERE mem_userid = @id;",
            new Dictionary<string, object> { { "@id", userId ?? "" } });
    }

    /// <summary>비밀번호 변경. passwordHash 는 PasswordHasher.Hash 결과여야 한다. 상태는 정상(1)으로 바뀐다.</summary>
    public void UpdateUserPassword(string userId, string passwordHash)
    {
        ExecuteNonQuery("UPDATE tb_member SET mem_password = @pw, mem_status = 1, change_date = CURRENT_TIMESTAMP WHERE mem_userid = @id;",
            new Dictionary<string, object> { { "@pw", passwordHash ?? "" }, { "@id", userId ?? "" } });
    }

    /// <summary>예전 형식(Base64)으로 저장된 비밀번호를 해시로만 바꾼다. 계정 상태는 건드리지 않는다.</summary>
    public void UpgradePasswordHash(string userId, string passwordHash)
    {
        ExecuteNonQuery("UPDATE tb_member SET mem_password = @pw WHERE mem_userid = @id;",
            new Dictionary<string, object> { { "@pw", passwordHash ?? "" }, { "@id", userId ?? "" } });
    }

    public void UpdateUserStatusLock(string userId)
    {
        ExecuteNonQuery("UPDATE tb_member SET mem_status = 3 WHERE mem_userid = @id;",
            new Dictionary<string, object> { { "@id", userId ?? "" } });
    }

    public void AccountStop(string userId)
    {
        ExecuteNonQuery("UPDATE tb_member SET mem_status = 2 WHERE mem_userid = @id;",
            new Dictionary<string, object> { { "@id", userId ?? "" } });
    }

    public void UpdateMemberInfo(MemberData member)
    {
        ExecuteNonQuery(
            "UPDATE tb_member SET mem_name = @name, mem_depart = @depart, mem_title = @title, mem_work = @work, change_date = CURRENT_TIMESTAMP WHERE mem_userid = @id;",
            new Dictionary<string, object>
            {
                { "@name", member.mem_name ?? "" },
                { "@depart", member.mem_depart ?? "" },
                { "@title", member.mem_title ?? "" },
                { "@work", member.mem_work ?? "" },
                { "@id", member.mem_user_id ?? "" }
            });
    }

    /// <summary>
    /// 계정 추가: admin + (마지막 번호 + 1, 3자리). 이전과 같은 규칙이지만
    /// 회원이 한 명도 없을 때(MAX 가 NULL) 터지던 문제를 고치고, 만든 행을 돌려준다.
    /// </summary>
    public DataTable InsertAccount()
    {
        DataTable inserted = null;

        RunInTransaction(() =>
        {
            object last = ExecuteScalar("SELECT MAX(mem_idx) FROM tb_member;");
            long next = (last == null || last == DBNull.Value ? 0L : Convert.ToInt64(last, CultureInfo.InvariantCulture)) + 1L;
            string userId = "admin" + next.ToString("000", CultureInfo.InvariantCulture);

            ExecuteNonQuery("INSERT INTO tb_member (mem_auth_id, mem_status, mem_userid) VALUES ('일반관리자', 1, @id);",
                new Dictionary<string, object> { { "@id", userId } });

            inserted = Query("SELECT * FROM tb_member WHERE mem_idx = last_insert_rowid();");
        });

        return inserted ?? new DataTable();
    }

    // =====================================================================================
    // 이력 (접속 / 변경 / 다운로드) + 계정 목록
    // =====================================================================================

    public void InsertConnectionLog(string userId, string name, string depart, string title)
    {
        ExecuteNonQuery("INSERT INTO tb_connect_log (mem_userid, mem_name, mem_depart, mem_title) VALUES (@id, @name, @depart, @title);",
            new Dictionary<string, object> { { "@id", userId ?? "" }, { "@name", name ?? "" }, { "@depart", depart ?? "" }, { "@title", title ?? "" } });
    }

    public void InsertChangeLog(string name, string menuName, string content)
    {
        ExecuteNonQuery("INSERT INTO tb_change_log (change_name, change_menuname, change_contents) VALUES (@name, @menu, @content);",
            new Dictionary<string, object> { { "@name", name ?? "" }, { "@menu", menuName ?? "" }, { "@content", content ?? "" } });
    }

    public void InsertDownloadLog(string name, string menuName, string content)
    {
        ExecuteNonQuery("INSERT INTO tb_down_log (down_name, down_menu, down_contents) VALUES (@name, @menu, @content);",
            new Dictionary<string, object> { { "@name", name ?? "" }, { "@menu", menuName ?? "" }, { "@content", content ?? "" } });
    }

    private static string LogSearchFilter(string tableName, bool[] st, string text, Dictionary<string, object> p)
    {
        string[] columns;     // [0] 전체 검색 대상, 이후 드롭다운 순서
        switch (tableName)
        {
            case "tb_connect_log": columns = new[] { "mem_name", "mem_userid", "mem_depart" }; break;
            case "tb_change_log": columns = new[] { "change_name", "change_contents" }; break;
            case "tb_down_log": columns = new[] { "down_name", "down_contents" }; break;
            default: columns = new[] { "mem_userid", "mem_name", "mem_depart" }; break;
        }

        if (Flag(st, 0) && !string.IsNullOrEmpty(text))
        {
            p["@q"] = LikePattern(text);
            List<string> ors = new List<string>();
            foreach (string c in columns)
            {
                ors.Add(Like(c));
            }
            return " AND (" + string.Join(" OR ", ors.ToArray()) + ")";
        }

        for (int i = 1; i <= columns.Length; i++)
        {
            if (Flag(st, i))
            {
                p["@q"] = LikePattern(text);
                return " AND " + Like(columns[i - 1]);
            }
        }

        return "";
    }

    /// <summary>
    /// 이력/계정 목록. create_date 는 UTC(CURRENT_TIMESTAMP)로 저장되고 화면에는 +9시간으로 보여 주므로,
    /// 검색 기간(한국 날짜)도 UTC 로 바꿔서 비교한다. (이전: 00~09시 이력이 전날로 검색되던 문제)
    /// </summary>
    public DataTable GetLog(string tableName, int fromDate, int toDate, bool[] searchType, string searchText)
    {
        Require(LogTables, tableName);

        string idx;
        switch (tableName)
        {
            case "tb_connect_log": idx = "con_log_idx"; break;
            case "tb_change_log": idx = "change_log_idx"; break;
            case "tb_down_log": idx = "down_log_idx"; break;
            default: idx = "mem_idx"; break;
        }

        Dictionary<string, object> p = new Dictionary<string, object>
        {
            { "@from", DayStart(fromDate, 0) },
            { "@to", DayStart(toDate, 1) }
        };

        string sql = "SELECT *, 0 AS 'isSelect', strftime('%Y.%m.%d %H:%M:%S', datetime(create_date, '+9 hours')) AS '날짜', "
                     + "ROW_NUMBER() OVER (ORDER BY create_date ASC, " + idx + " ASC) AS 'NO' "
                     + "FROM " + tableName + " "
                     + "WHERE create_date >= datetime(@from, '-9 hours') AND create_date < datetime(@to, '-9 hours')"
                     + LogSearchFilter(tableName, searchType, searchText, p)
                     + " ORDER BY NO DESC;";

        return Query(sql, p);
    }

    private static string DayStart(int yyyymmdd, int addDays)
    {
        DateTime day = DateTime.ParseExact(yyyymmdd.ToString("00000000", CultureInfo.InvariantCulture), "yyyyMMdd", CultureInfo.InvariantCulture);
        return day.AddDays(addDays).ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
    }

    // =====================================================================================
    // 통계
    // =====================================================================================

    public const string SqlStatisticsByRegion = @"
WITH apt AS (
    SELECT {SIDO} AS sido,
           MAX(total_suphouse_number) AS total_suphouse_number,
           SUM(CASE WHEN inspection_status_result = 'Y' THEN 1 ELSE 0 END) AS checked,
           AVG(CASE WHEN inspection_status_result = 'Y' THEN (unauth_rf_prob + unauth_xgb_prob) / 2 END) AS avg_risk,
           SUM(CASE WHEN inspection_status_result = 'Y' AND unauth_sub_decision = 1 THEN unauth_disguise_transfer ELSE 0 END) AS camo,
           SUM(CASE WHEN inspection_status_result = 'Y' AND unauth_sub_decision = 1 THEN unauth_savecerti_sale ELSE 0 END) AS sale,
           SUM(CASE WHEN inspection_status_result = 'Y' AND unauth_sub_decision = 1 THEN unauth_other ELSE 0 END) AS other,
           apt_name
    FROM tb_riskdata
    WHERE rec_anno_date BETWEEN @from AND @to AND {CITY}
    GROUP BY apt_name, sido
)
SELECT ROW_NUMBER() OVER (ORDER BY sido) AS 'NO',
       sido AS '지역',
       COUNT(apt_name) AS '단지수',
       SUM(total_suphouse_number) AS '세대수',
       SUM(CASE WHEN checked > 0 THEN 1 ELSE 0 END) AS '점검단지',
       SUM(checked) AS '점검세대',
       CASE WHEN AVG(CASE WHEN checked > 0 THEN avg_risk END) IS NULL THEN 0.0 ELSE AVG(CASE WHEN checked > 0 THEN avg_risk END) * 100 END AS '평균',
       SUM(camo + sale + other) AS '전체',
       SUM(camo) AS '위장전입',
       SUM(sale) AS '통장매매',
       SUM(other) AS '기타'
FROM apt
GROUP BY sido
ORDER BY sido";

    /// <summary>통계 - 지역별</summary>
    public DataTable GetStatisticsByRegion(int fromDate, int toDate, bool[] cityToggle)
    {
        Dictionary<string, object> p = new Dictionary<string, object> { { "@from", (long)fromDate }, { "@to", (long)toDate } };
        string sql = Fill(SqlStatisticsByRegion, "{SIDO}", Sido(""), "{CITY}", CityFilter(cityToggle, p, ""));
        return Query(sql, p);
    }

    public const string SqlStatisticsByDanji = @"
WITH ranked AS (
    SELECT apt_name, rec_anno_date, apt_sup_pos,
           {SIDO} AS sido,
           {RISK} AS risk,
           unauth_sub_decision, unauth_disguise_transfer, unauth_savecerti_sale, unauth_other,
           ROW_NUMBER() OVER (PARTITION BY apt_name ORDER BY {RISK} DESC, data_idx) AS rk,
           COUNT(*) OVER (PARTITION BY apt_name) AS cnt
    FROM tb_riskdata
    WHERE rec_anno_date BETWEEN @from AND @to AND inspection_status_result = 'Y' AND {CITY}
)
SELECT ROW_NUMBER() OVER (ORDER BY MAX(k.rec_anno_date) DESC, k.sido, k.apt_name) AS 'NO',
       k.apt_name AS '단지명',
       b.bjdong_name AS '지역',
       AVG(k.risk) * 100 AS '위험도',
       SUM(CASE WHEN k.unauth_sub_decision = 1 THEN k.unauth_disguise_transfer ELSE 0 END) + SUM(CASE WHEN k.unauth_sub_decision = 1 THEN k.unauth_savecerti_sale ELSE 0 END) + SUM(CASE WHEN k.unauth_sub_decision = 1 THEN k.unauth_other ELSE 0 END) AS '전체',
       SUM(CASE WHEN k.unauth_sub_decision = 1 THEN k.unauth_disguise_transfer ELSE 0 END) AS '위장전입',
       SUM(CASE WHEN k.unauth_sub_decision = 1 THEN k.unauth_savecerti_sale ELSE 0 END) AS '통장매매',
       SUM(CASE WHEN k.unauth_sub_decision = 1 THEN k.unauth_other ELSE 0 END) AS '기타',
       MAX(k.rec_anno_date) AS '모집공고일'
FROM ranked k
LEFT JOIN tb_bjdong b ON b.bjdong_code = {SIGUNGU_K}
WHERE k.rk <= k.cnt * {PCT}
GROUP BY k.apt_name
ORDER BY MAX(k.rec_anno_date) DESC, k.sido, k.apt_name";

    /// <summary>통계 - 단지별 (점검완료 행 중 단지별 상위 % 기준)</summary>
    public DataTable GetStatisticsByDanji(int fromDate, int toDate, bool[] cityToggle, bool[] bustType, bool[] rfxgType, bool[] percentType)
    {
        Dictionary<string, object> p = new Dictionary<string, object> { { "@from", (long)fromDate }, { "@to", (long)toDate } };
        string sql = Fill(SqlStatisticsByDanji,
            "{SIDO}", Sido(""),
            "{RISK}", RiskExpr(bustType, rfxgType, ""),
            "{CITY}", CityFilter(cityToggle, p, ""),
            "{SIGUNGU_K}", Sigungu("k."),
            "{PCT}", PercentLiteral(percentType));
        return Query(sql, p);
    }

    // =====================================================================================
    // 주택청약 시장관리 - 단지 선정
    // =====================================================================================

    public const string SqlMarketByDanji = @"
WITH f AS MATERIALIZED (
    SELECT r.data_idx, r.apt_name, r.apt_type, r.total_suphouse_number, r.rec_anno_date, r.inspection_status_result,
           {SIDO_R} AS sido,
           {RISK_R} AS risk
    FROM tb_riskdata r{LOCATION_JOIN}
    WHERE r.rec_anno_date BETWEEN @from AND @to AND {CITY_R}{SEARCH}
), ranked AS (
    SELECT f.*, ROW_NUMBER() OVER (PARTITION BY apt_name ORDER BY risk DESC, data_idx) AS rk
    FROM f
), apt AS (
    SELECT apt_name,
           MIN(data_idx) AS any_idx,
           CASE WHEN MIN(apt_type) = MAX(apt_type) THEN MIN(apt_type) ELSE MIN(apt_type) || '/' || MAX(apt_type) END AS apt_type,
           MAX(sido) AS sido,
           MAX(rec_anno_date) AS rec_anno_date,
           AVG(risk) * 100 AS risk_avg,
           SUM(CASE WHEN inspection_status_result = 'N' THEN 1 ELSE 0 END) AS cnt_n,
           SUM(CASE WHEN inspection_status_result = 'Z' THEN 0 ELSE 1 END) AS cnt_not_z
    FROM ranked
    WHERE rk <= total_suphouse_number * {PCT}
    GROUP BY apt_name
)
SELECT ROW_NUMBER() OVER (ORDER BY a.rec_anno_date DESC, a.sido, a.apt_name) AS 'NO',
       CASE WHEN a.cnt_n > 0 THEN 'N' WHEN a.cnt_not_z = 0 THEN '해당없음' ELSE 'Y' END AS '점검여부',
       r.apt_number AS '주택관리번호',
       a.apt_type AS '민영국민',
       a.apt_name AS '단지명',
       b.bjdong_name AS '지역',
       r.total_suphouse_number AS '공급세대수',
       a.risk_avg AS '위험도',
       r.total_suphouse_number * {PCT} AS '세대수',
       r.rec_anno_date AS '모집공고일',
       r.win_anno_date AS '당첨자발표일',
       r.limittransfer_date AS '전입제한일',
       CASE WHEN r.saleprice_cap_status = 0 THEN 'N' ELSE 'Y' END AS '분양가상한제'
FROM apt a
JOIN tb_riskdata r ON r.data_idx = a.any_idx
LEFT JOIN tb_bjdong b ON b.bjdong_code = {SIGUNGU_R}
ORDER BY a.rec_anno_date DESC, a.sido, a.apt_name";

    public DataTable GetMarketManageByDanji(int fromDate, int toDate, bool[] searchType, string searchText, bool[] cityToggle, bool[] bustType, bool[] rfxgType, bool[] percentType)
    {
        Dictionary<string, object> p = new Dictionary<string, object> { { "@from", (long)fromDate }, { "@to", (long)toDate } };
        string sql = Fill(SqlMarketByDanji,
            "{SIDO_R}", Sido("r."),
            "{RISK_R}", RiskExpr(bustType, rfxgType, "r."),
            "{LOCATION_JOIN}", LocationJoin(searchType, searchText),
            "{CITY_R}", CityFilter(cityToggle, p, "r."),
            "{SEARCH}", MarketSearchFilter(searchType, searchText, p),
            "{PCT}", PercentLiteral(percentType),
            "{SIGUNGU_R}", Sigungu("r."));
        return Query(sql, p);
    }

    // =====================================================================================
    // 주택청약 시장관리 - 실시·등록 (신청자 단위: 데이터가 가장 큰 화면)
    // =====================================================================================

    /// <summary>
    /// 조건에 맞는 행의 (data_idx, 위험도) 만 좁게 뽑아 순위를 매긴다(커버링 인덱스만 읽음).
    /// 넓은 tb_riskdata 행은 마지막에 필요한 행/페이지만 rowid 로 읽는다.
    /// </summary>
    public const string SqlExecuteRankedCte = @"
WITH f AS MATERIALIZED (
    SELECT r.data_idx, {RISK_R} AS risk
    FROM tb_riskdata r{LOCATION_JOIN}
    WHERE r.rec_anno_date BETWEEN @from AND @to AND {CITY_R}{SEARCH}
), k AS (
    SELECT data_idx, risk, rk, total_rows
    FROM (SELECT data_idx, risk,
                 ROW_NUMBER() OVER (ORDER BY risk DESC, data_idx) AS rk,
                 (SELECT COUNT(*) FROM f) AS total_rows
          FROM f)
    WHERE rk <= total_rows * {PCT}
)";

    public const string SqlExecutePageCte = @", pg AS (SELECT data_idx, risk, rk, total_rows FROM k ORDER BY rk LIMIT @limit OFFSET @offset)";

    /// <summary>실시·등록 화면이 실제로 읽는 원본 컬럼</summary>
    public const string SqlExecuteListColumns = @"
       r.data_no, r.apt_number, r.apt_type, r.apt_name, r.apt_dong, r.apt_hosu, r.apt_sup_type, r.apt_sp_sup, r.apt_sup_pos,
       r.user_birth, r.user_name,
       r.inspection_status, r.unauth_sub_decision, r.unauth_sub_type,
       r.unauth_disguise_transfer, r.unauth_savecerti_sale, r.unauth_other, r.inspection_status_result,";

    /// <summary>화면 표시용 한글 별칭 컬럼 (이전 쿼리와 같은 이름/순서)</summary>
    public const string SqlExecuteAliasColumns = @"
       CASE WHEN r.inspection_status_result = 'Z' THEN 'Z' ELSE r.inspection_status_result END AS '점검여부',
       CASE WHEN r.unauth_sub_decision = 0 THEN '통과' ELSE r.unauth_sub_type END AS '부정청약결과',
       k.risk AS '위험도',
       r.apt_number AS '주택관리번호',
       r.apt_type AS '민영국민',
       r.apt_name AS '단지명',
       r.apt_dong AS '동',
       r.apt_hosu AS '호수',
       r.user_name AS '성명',
       r.user_birth AS '생년월일',
       r.user_age AS '연령',
       r.apt_sup_type AS '공급유형',
       r.apt_sp_sup AS '세부유형타입',
       r.user_relationship AS '세대주관계',
       r.user_change_date AS '변경일자',
       r.user_spouse AS '배우자',
       r.user_housemember AS '세대원수',
       r.user_hp_housemember AS '분리세대',
       r.user_phone_dup AS '폰중복',
       r.user_ip_dub_win AS 'ip중복당첨',
       r.user_ip_dub_app AS 'ip중복신청',
       r.app_residence_class AS '신청거주구분',
       r.win_residence_class AS '당첨거주구분',
       CASE WHEN r.ad_addressmatch_status = 0 THEN 'N' ELSE 'Y' END AS '주소일치여부',
       r.user_admin_address AS '등본주소',
       r.app_address AS '입력주소',
       r.user_recom_type AS '기관추천',
       strftime('%Y.%m.%d %H:%M:%S', datetime(r.change_date, '+9 hours')) AS '업로드일시'";

    public const string SqlExecuteList = @"
SELECT k.rk AS 'NO',{LIST_COLUMNS}{ALIAS_COLUMNS}
FROM {SOURCE} k
JOIN tb_riskdata r ON r.data_idx = k.data_idx
ORDER BY k.rk";

    public const string SqlExecuteFull = @"
SELECT k.rk AS 'NO',
       r.*,
       b.*,
       k.total_rows AS total_rows,
       k.rk AS rankTest,
       (r.unauth_rf_prob + r.unauth_xgb_prob) / 2 AS unauth_everage,
       (r.fake_rf_prob + r.fake_xgb_prob) / 2 AS fake_everage,
       (r.sale_rf_prob + r.sale_xgb_prob) / 2 AS sale_everage,
       b.bjdong_name AS location,
       {SIDO_R} AS LocalCode,{ALIAS_COLUMNS}
FROM k
JOIN tb_riskdata r ON r.data_idx = k.data_idx
LEFT JOIN tb_bjdong b ON b.bjdong_code = {SIGUNGU_R}
ORDER BY k.rk";

    public const string SqlExecuteCounts = @"
SELECT COUNT(*) AS total,
       SUM(CASE WHEN r.inspection_status_result = 'N' THEN 1 ELSE 0 END) AS cnt_n,
       SUM(CASE WHEN r.inspection_status_result = 'Z' THEN 1 ELSE 0 END) AS cnt_z,
       SUM(CASE WHEN r.inspection_status_result = 'Y' THEN 1 ELSE 0 END) AS cnt_y
FROM k JOIN tb_riskdata r ON r.data_idx = k.data_idx";

    private static string ExecuteRankedCte(int fromDate, int toDate, bool[] searchType, string searchText, bool[] cityToggle, bool[] bustType, bool[] rfxgType, bool[] percentType, Dictionary<string, object> p)
    {
        p["@from"] = (long)fromDate;
        p["@to"] = (long)toDate;

        return Fill(SqlExecuteRankedCte,
            "{RISK_R}", RiskExpr(bustType, rfxgType, "r."),
            "{LOCATION_JOIN}", LocationJoin(searchType, searchText),
            "{CITY_R}", CityFilter(cityToggle, p, "r."),
            "{SEARCH}", MarketSearchFilter(searchType, searchText, p),
            "{PCT}", PercentLiteral(percentType));
    }

    /// <summary>
    /// 실시·등록 목록 (화면용 좁은 컬럼 약 50개). 이전 쿼리는 214개 컬럼을 전부 가져왔다.
    /// limit 가 0 보다 크면 그 페이지만 넓은 테이블에서 읽는다 (서버 페이징).
    /// </summary>
    public DataTable GetExecuteList(int fromDate, int toDate, bool[] searchType, string searchText, bool[] cityToggle, bool[] bustType, bool[] rfxgType, bool[] percentType, int offset = 0, int limit = 0)
    {
        Dictionary<string, object> p = new Dictionary<string, object>();
        string cte = ExecuteRankedCte(fromDate, toDate, searchType, searchText, cityToggle, bustType, rfxgType, percentType, p);
        string source = "k";

        if (limit > 0)
        {
            cte += SqlExecutePageCte;
            p["@limit"] = (long)limit;
            p["@offset"] = (long)Math.Max(0, offset);
            source = "pg";
        }

        string sql = cte + Fill(SqlExecuteList,
            "{LIST_COLUMNS}", SqlExecuteListColumns,
            "{ALIAS_COLUMNS}", SqlExecuteAliasColumns,
            "{SOURCE}", source);

        return Query(sql, p);
    }

    /// <summary>예전 화면 코드와 같은 214개 컬럼(같은 이름/순서)을 돌려주는 호환용 조회.</summary>
    public DataTable GetMarketManageByImplementationAndRegistration(int fromDate, int toDate, bool[] searchType, string searchText, bool[] cityToggle, bool[] bustType, bool[] rfxgType, bool[] percentType)
    {
        Dictionary<string, object> p = new Dictionary<string, object>();
        string cte = ExecuteRankedCte(fromDate, toDate, searchType, searchText, cityToggle, bustType, rfxgType, percentType, p);
        string sql = cte + Fill(SqlExecuteFull,
            "{ALIAS_COLUMNS}", SqlExecuteAliasColumns,
            "{SIDO_R}", Sido("r."),
            "{SIGUNGU_R}", Sigungu("r."));

        return Query(sql, p);
    }

    /// <summary>전체/미점검/해당없음/점검완료 개수 (서버 페이징 시 사용)</summary>
    public DataTable GetExecuteCounts(int fromDate, int toDate, bool[] searchType, string searchText, bool[] cityToggle, bool[] bustType, bool[] rfxgType, bool[] percentType)
    {
        Dictionary<string, object> p = new Dictionary<string, object>();
        string cte = ExecuteRankedCte(fromDate, toDate, searchType, searchText, cityToggle, bustType, rfxgType, percentType, p);
        return Query(cte + SqlExecuteCounts, p);
    }

    /// <summary>
    /// '결과 저장': 점검여부/부정청약 판정 7개 컬럼만 data_no 기준으로 한 트랜잭션에서 갱신한다.
    /// rows 의 각 배열은 [data_no, ExecuteResultColumns 순서의 값 7개].
    /// </summary>
    public int SaveExecuteResults(IList<object[]> rows)
    {
        if (rows == null || rows.Count == 0)
        {
            return 0;
        }

        int affected = 0;

        RunInTransaction(() =>
        {
            using (SqliteCommand cmd = m_DatabaseConnection.CreateCommand())
            {
                StringBuilder sql = new StringBuilder("UPDATE tb_riskdata SET ");
                var values = new List<SqliteParameter>();

                for (int i = 0; i < ExecuteResultColumns.Length; i++)
                {
                    sql.Append(ExecuteResultColumns[i]).Append(" = @v").Append(i).Append(", ");
                    values.Add(cmd.Parameters.AddWithValue("@v" + i, DBNull.Value));
                }

                // change_date 는 '업로드일시' 이고 대시보드 기간(최근 업로드일 기준 1년)을 정하므로 여기서 바꾸지 않는다.
                sql.Length -= 2;
                sql.Append(" WHERE data_no = @key;");
                var key = cmd.Parameters.AddWithValue("@key", "");
                cmd.CommandText = sql.ToString();

                foreach (object[] row in rows)
                {
                    key.Value = row[0] == null ? (object)DBNull.Value : Convert.ToString(row[0], CultureInfo.InvariantCulture);

                    for (int i = 0; i < values.Count; i++)
                    {
                        object v = i + 1 < row.Length ? row[i + 1] : null;
                        values[i].Value = v ?? DBNull.Value;
                    }

                    affected += cmd.ExecuteNonQuery();
                }
            }
        });

        return affected;
    }

    /// <summary>
    /// 엑셀 다운로드/결과 저장 CSV 에 쓰는 컬럼: tb_riskdata 의 실제 컬럼 순서에서 data_idx, create_date, change_date 를 뺀 것.
    /// 이전 버전이 조회 결과에서 이 세 컬럼을 지우고 앞에서부터 korcolname 개수만큼 쓰던 것과 같다.
    /// (ViewExecute.colname 은 인스펙터에 저장된 값이 씬마다 달라서 쓰지 않는다)
    /// </summary>
    public string[] GetRiskExportColumns()
    {
        DataTable info = Query("PRAGMA table_info(tb_riskdata);");
        List<string> names = new List<string>(info.Rows.Count);

        foreach (DataRow row in info.Rows)
        {
            string name = Convert.ToString(row["name"], CultureInfo.InvariantCulture);

            if (name != "data_idx" && name != "create_date" && name != "change_date")
            {
                names.Add(name);
            }
        }

        return names.ToArray();
    }

    /// <summary>
    /// 신청자 원본 행(172개 컬럼)을 CSV 로 바로 쓴다. DataTable 로 전부 올리지 않고 한 행씩 흘려 보낸다.
    /// overlay 에 data_no 가 있으면 화면에서 바꾼 값(아직 저장 전)을 덮어 쓴다.
    /// </summary>
    public int ExportRiskRowsToCsv(string path, IList<string> dataNos, string[] columns, string[] headers,
        string[] overlayColumns, IDictionary<string, object[]> overlay, Encoding encoding)
    {
        // 헤더와 컬럼 수가 다르면 한 칸씩 밀린 파일이 만들어진다. 조용히 틀린 파일을 만들기보다 실패시킨다.
        if (columns == null || headers == null || columns.Length != headers.Length)
        {
            throw new ArgumentException("내보낼 컬럼 수(" + (columns == null ? 0 : columns.Length) +
                                        ")와 헤더 수(" + (headers == null ? 0 : headers.Length) + ")가 다릅니다.");
        }

        foreach (string c in columns)
        {
            RequireColumnName(c);
        }

        Dictionary<string, int> overlayIndex = new Dictionary<string, int>();
        if (overlayColumns != null)
        {
            for (int i = 0; i < overlayColumns.Length; i++)
            {
                overlayIndex[overlayColumns[i]] = i;
            }
        }

        // 순서를 지키기 위해 data_no 목록을 임시 테이블에 넣고 조인한다 (IN (...) 은 개수 제한이 있음)
        ExecuteNonQuery("CREATE TEMP TABLE IF NOT EXISTS export_ids (seq INTEGER PRIMARY KEY, data_no TEXT NOT NULL);");
        ExecuteNonQuery("DELETE FROM temp.export_ids;");

        // 임시 테이블에만 쓰므로 DB 쓰기 잠금을 잡지 않는다 (파이썬이 쓰는 중이어도 내보내기가 막히지 않게)
        RunInTransaction(() =>
        {
            using (SqliteCommand cmd = m_DatabaseConnection.CreateCommand())
            {
                cmd.CommandText = "INSERT INTO temp.export_ids (seq, data_no) VALUES (@seq, @no);";
                var seq = cmd.Parameters.AddWithValue("@seq", 0L);
                var no = cmd.Parameters.AddWithValue("@no", "");

                for (int i = 0; i < dataNos.Count; i++)
                {
                    seq.Value = (long)i;
                    no.Value = dataNos[i] ?? "";
                    cmd.ExecuteNonQuery();
                }
            }
        }, deferred: true);

        StringBuilder select = new StringBuilder("SELECT ");
        for (int i = 0; i < columns.Length; i++)
        {
            if (i > 0)
            {
                select.Append(", ");
            }
            select.Append("r.").Append(columns[i]);
        }
        select.Append(" FROM temp.export_ids e JOIN tb_riskdata r ON r.data_no = e.data_no ORDER BY e.seq;");

        int written = 0;
        int dataNoColumn = Array.IndexOf(columns, "data_no");

        using (StreamWriter writer = new StreamWriter(path, false, encoding))
        using (SqliteCommand cmd = m_DatabaseConnection.CreateCommand())
        {
            writer.WriteLine(CsvUtil.JoinLine(headers));
            cmd.CommandText = select.ToString();

            using (SqliteDataReader reader = cmd.ExecuteReader())
            {
                string[] cells = new string[columns.Length];

                while (reader.Read())
                {
                    object[] edited = null;
                    if (overlay != null && dataNoColumn >= 0 && !reader.IsDBNull(dataNoColumn))
                    {
                        overlay.TryGetValue(Convert.ToString(reader.GetValue(dataNoColumn), CultureInfo.InvariantCulture), out edited);
                    }

                    for (int i = 0; i < columns.Length; i++)
                    {
                        object value = reader.IsDBNull(i) ? null : reader.GetValue(i);

                        int o;
                        if (edited != null && overlayIndex.TryGetValue(columns[i], out o) && o < edited.Length)
                        {
                            value = edited[o];
                        }

                        cells[i] = value == null || value == DBNull.Value ? "" : value.ToString();
                    }

                    writer.WriteLine(CsvUtil.JoinLine(cells));
                    written++;
                }
            }
        }

        ExecuteNonQuery("DELETE FROM temp.export_ids;");
        return written;
    }

    private static void RequireColumnName(string column)
    {
        if (string.IsNullOrEmpty(column))
        {
            throw new ArgumentException("빈 컬럼 이름");
        }

        foreach (char ch in column)
        {
            if (!(ch == '_' || (ch >= 'a' && ch <= 'z') || (ch >= 'A' && ch <= 'Z') || (ch >= '0' && ch <= '9')))
            {
                throw new ArgumentException("허용되지 않은 컬럼 이름: " + column);
            }
        }
    }

    // =====================================================================================
    // 대시보드 / 통계 지도
    // =====================================================================================

    public DataTable GetLastCreateDateRiskData()
    {
        // change_date 인덱스 덕분에 테이블을 훑지 않는다.
        return Query("SELECT datetime(MAX(change_date), '-1 year') AS '시작시간', MAX(change_date) AS '끝시간' FROM tb_riskdata;");
    }

    public const string SqlPercentPerson = @"
SELECT COUNT(*) AS total, COUNT(*) * 0.05 AS high, COUNT(*) * 0.1 AS middle, COUNT(*) * 0.2 AS low
FROM tb_riskdata
WHERE {DATE} AND {CITY}";

    public DataTable GetPercentPerson(string sortType, string startDate, string endDate, bool[] cityToggle)
    {
        Dictionary<string, object> p = new Dictionary<string, object>();
        string sql = Fill(SqlPercentPerson,
            "{DATE}", DateFilter(sortType, startDate, endDate, p, ""),
            "{CITY}", CityFilter(cityToggle, p, ""));
        return Query(sql, p);
    }

    public const string SqlMapByDanger = @"
SELECT ROW_NUMBER() OVER (ORDER BY AVG((unauth_rf_prob + unauth_xgb_prob) / 2) * 100 DESC, {SIDO}) AS 'NO',
       {SIDO} AS '지역',
       AVG((unauth_rf_prob + unauth_xgb_prob) / 2) * 100 AS everage,
       SUM(unauth_sub_decision) AS '적발수',
       SUM(CASE WHEN unauth_sub_decision = 1 THEN unauth_disguise_transfer ELSE 0 END) + SUM(CASE WHEN unauth_sub_decision = 1 THEN unauth_savecerti_sale ELSE 0 END) + SUM(CASE WHEN unauth_sub_decision = 1 THEN unauth_other ELSE 0 END) AS '전체',
       SUM(CASE WHEN unauth_sub_decision = 1 THEN unauth_disguise_transfer ELSE 0 END) AS '위장전입',
       SUM(CASE WHEN unauth_sub_decision = 1 THEN unauth_savecerti_sale ELSE 0 END) AS '통장매매',
       SUM(CASE WHEN unauth_sub_decision = 1 THEN unauth_other ELSE 0 END) AS '기타'
FROM tb_riskdata
WHERE {DATE} AND {CITY}
GROUP BY {SIDO}
ORDER BY {ORDER}";

    /// <summary>지도(시도별) + 순위. type: "map" = 지역코드순, "score" = 위험도 높은 순</summary>
    public DataTable GetMapByDanger(bool[] cityToggle, string sortType, string fromDate, string toDate, string type)
    {
        Dictionary<string, object> p = new Dictionary<string, object>();
        string order = type == "score" ? "everage DESC, " + Sido("") : Sido("");
        string sql = Fill(SqlMapByDanger,
            "{DATE}", DateFilter(sortType, fromDate, toDate, p, ""),
            "{CITY}", CityFilter(cityToggle, p, ""),
            "{ORDER}", order,
            "{SIDO}", Sido(""));
        return Query(sql, p);
    }

    public const string SqlMapByDangerLocal = @"
SELECT ROW_NUMBER() OVER (ORDER BY AVG((r.unauth_rf_prob + r.unauth_xgb_prob) / 2) * 100 DESC, MIN({SIGUNGU_R})) AS 'NO',
       MIN(r.apt_name) AS '단지',
       MIN({SIGUNGU_R}) AS '지역',
       SUM(CASE WHEN r.unauth_sub_decision = 1 THEN r.unauth_disguise_transfer ELSE 0 END) + SUM(CASE WHEN r.unauth_sub_decision = 1 THEN r.unauth_savecerti_sale ELSE 0 END) + SUM(CASE WHEN r.unauth_sub_decision = 1 THEN r.unauth_other ELSE 0 END) AS '전체',
       SUM(CASE WHEN r.unauth_sub_decision = 1 THEN r.unauth_disguise_transfer ELSE 0 END) AS '위장전입',
       SUM(CASE WHEN r.unauth_sub_decision = 1 THEN r.unauth_savecerti_sale ELSE 0 END) AS '통장매매',
       SUM(CASE WHEN r.unauth_sub_decision = 1 THEN r.unauth_other ELSE 0 END) AS '기타',
       AVG((r.unauth_rf_prob + r.unauth_xgb_prob) / 2) * 100 AS everage
FROM tb_riskdata r
WHERE {DATE} AND {CITY_R}
GROUP BY {GROUP}
ORDER BY {ORDER}";

    /// <summary>지도(시군구별, type="map") 또는 위험도 상위 10개 단지(type="score")</summary>
    public DataTable GetMapByDangerLocal(bool[] cityToggle, string sortType, string fromDate, string toDate, string type)
    {
        Dictionary<string, object> p = new Dictionary<string, object>();
        bool score = type == "score";
        string sql = Fill(SqlMapByDangerLocal,
            "{DATE}", DateFilter(sortType, fromDate, toDate, p, "r."),
            "{CITY_R}", CityFilter(cityToggle, p, "r."),
            "{GROUP}", score ? "r.apt_name" : Sigungu("r.") + " / 10",
            "{ORDER}", score ? "everage DESC, MIN(r.apt_name)" : Sigungu("r.") + " / 10",
            "{SIGUNGU_R}", Sigungu("r."));

        if (score)
        {
            sql += " LIMIT 10";
        }

        return Query(sql, p);
    }

    public const string SqlMapByDangerPercent = @"
WITH ranked AS MATERIALIZED (
    SELECT (unauth_rf_prob + unauth_xgb_prob) / 2 AS ev,
           ROW_NUMBER() OVER (ORDER BY (unauth_rf_prob + unauth_xgb_prob) / 2 DESC) AS rn
    FROM tb_riskdata
    WHERE {DATE} AND {CITY}
), lim(i, n) AS (VALUES (0, @n0), (1, @n1), (2, @n2), (3, @n3))
SELECT (SELECT AVG(ev) FROM ranked WHERE rn <= lim.n) * 100 AS '전체'
FROM lim
ORDER BY lim.i";

    /// <summary>
    /// 위험도 상위 N명 평균 4개(전체/5%/10%/20%)를 한 번에 계산한다.
    /// 이전에는 같은 테이블을 4번 정렬했다. 결과는 이전처럼 '전체' 컬럼 4행.
    /// </summary>
    public DataTable GetMapByDangerPercent(bool[] cityToggle, string sortType, string fromDate, string toDate, string[] limits)
    {
        Dictionary<string, object> p = new Dictionary<string, object>();

        for (int i = 0; i < 4; i++)
        {
            string text = limits != null && i < limits.Length ? limits[i] : "0";
            double n;
            p["@n" + i] = double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out n) ? (long)Math.Floor(n) : 0L;
        }

        string sql = Fill(SqlMapByDangerPercent,
            "{DATE}", DateFilter(sortType, fromDate, toDate, p, ""),
            "{CITY}", CityFilter(cityToggle, p, ""));
        return Query(sql, p);
    }

    // =====================================================================================
    // 위험도 원본 테이블 쓰기 (tb_riskdata / tb_checkresult)
    // =====================================================================================

    public int DeleteAll(string tableName)
    {
        Require(RiskTables, tableName);
        return ExecuteNonQuery("DELETE FROM " + tableName + ";");
    }

    /// <summary>키 목록에 해당하는 행을 한 트랜잭션에서 삭제 (이전: 행마다 연결을 열고 자동 커밋 + Task.Delay)</summary>
    public int DeleteByKeys(string tableName, string keyColumn, IList<string> keys)
    {
        Require(RiskTables, tableName);
        if (keyColumn != "data_no" && keyColumn != "data_idx")
        {
            throw new ArgumentException("허용되지 않은 키 컬럼: " + keyColumn);
        }

        if (keys == null || keys.Count == 0)
        {
            return 0;
        }

        int affected = 0;

        RunInTransaction(() =>
        {
            using (SqliteCommand cmd = m_DatabaseConnection.CreateCommand())
            {
                cmd.CommandText = "DELETE FROM " + tableName + " WHERE " + keyColumn + " = @key;";
                var key = cmd.Parameters.AddWithValue("@key", "");

                foreach (string k in keys)
                {
                    key.Value = k ?? "";
                    affected += cmd.ExecuteNonQuery();
                }
            }
        });

        return affected;
    }

    /// <summary>
    /// 여러 행을 한 트랜잭션 + 재사용 커맨드로 넣는다.
    /// 값은 문자열 그대로 바인딩한다: 컬럼 타입(INTEGER/REAL/TEXT)에 맞게 SQLite 가 변환하고,
    /// TEXT 컬럼의 '001' 같은 값이 1 로 바뀌던 이전 문제도 없어진다.
    /// </summary>
    public int InsertRows(string tableName, string[] columns, IEnumerable<object[]> rows, Action<int> progress = null)
    {
        Require(RiskTables, tableName);
        foreach (string c in columns)
        {
            RequireColumnName(c);
        }

        int inserted = 0;

        RunInTransaction(() =>
        {
            using (SqliteCommand cmd = m_DatabaseConnection.CreateCommand())
            {
                var parameters = new List<SqliteParameter>();
                StringBuilder names = new StringBuilder();

                for (int i = 0; i < columns.Length; i++)
                {
                    if (i > 0)
                    {
                        names.Append(", ");
                    }
                    names.Append("@p").Append(i);
                    parameters.Add(cmd.Parameters.AddWithValue("@p" + i, DBNull.Value));
                }

                cmd.CommandText = "INSERT INTO " + tableName + " (" + string.Join(", ", columns) + ") VALUES (" + names + ");";

                foreach (object[] row in rows)
                {
                    for (int i = 0; i < parameters.Count; i++)
                    {
                        object v = i < row.Length ? row[i] : null;
                        parameters[i].Value = v ?? DBNull.Value;
                    }

                    cmd.ExecuteNonQuery();
                    inserted++;

                    if (progress != null && inserted % 1000 == 0)
                    {
                        progress(inserted);
                    }
                }
            }
        });

        return inserted;
    }

    /// <summary>
    /// 이전 UpdateRiskData 와 같은 규칙(values[0] 을 data_no 로 보고 있으면 UPDATE, 없으면 INSERT)을
    /// 파라미터 바인딩 + 트랜잭션으로 처리한다.
    /// </summary>
    public void UpdateOrInsertRow(string tableName, string[] columns, object[] values)
    {
        Require(RiskTables, tableName);
        foreach (string c in columns)
        {
            RequireColumnName(c);
        }

        RunInTransaction(() =>
        {
            object key = values.Length > 0 && values[0] != null ? values[0].ToString() : "";
            object count = ExecuteScalar("SELECT COUNT(*) FROM " + tableName + " WHERE data_no = @key;",
                new Dictionary<string, object> { { "@key", key } });

            Dictionary<string, object> p = new Dictionary<string, object>();
            List<string> sets = new List<string>();
            List<string> names = new List<string>();

            for (int i = 0; i < columns.Length; i++)
            {
                string name = "@v" + i;
                p[name] = i < values.Length && values[i] != null ? values[i] : DBNull.Value;
                sets.Add(columns[i] + " = " + name);
                names.Add(name);
            }

            if (Convert.ToInt64(count, CultureInfo.InvariantCulture) > 0)
            {
                p["@key"] = key;
                ExecuteNonQuery("UPDATE " + tableName + " SET " + string.Join(", ", sets.ToArray()) + " WHERE data_no = @key;", p);
            }
            else
            {
                ExecuteNonQuery("INSERT INTO " + tableName + " (" + string.Join(", ", columns) + ") VALUES (" + string.Join(", ", names.ToArray()) + ");", p);
            }
        });
    }
}
