using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 화면 코드가 DB 에 접근하는 창구.
///
/// 이전 버전과 달라진 점
/// - 조회할 때마다 연결을 새로 열고 닫던 것을, 연결 하나를 열어 두고 재사용한다.
///   (매번 스키마 재해석, 페이지 캐시 손실, 그리고 async 조회 도중 다른 호출이 m_DatabaseAccess 를
///    바꿔치기해서 연결이 닫히는 경쟁 상태가 있었다)
/// - SQLite 연결은 스레드 안전하지 않으므로 모든 접근을 lock 하나로 직렬화한다.
///   lock 은 동기 코드 안에서만 잡으므로 메인 스레드와 백그라운드 작업 사이에 교착이 생기지 않는다.
/// - 무거운 조회/대량 쓰기는 ...Async 메서드로 백그라운드 스레드에서 실행해 화면이 멈추지 않게 한다.
/// - 조회 실패 시 로그를 남기고 빈 테이블을 돌려준다(로딩 팝업이 영원히 떠 있지 않도록).
///   쓰기 실패는 예외를 그대로 올려 호출한 화면이 알 수 있게 한다.
/// </summary>
public partial class ServerManager : MonoBehaviour
{
    public string m_DatabaseFileName = "koreareb.db";
    public DatabaseAccess m_DatabaseAccess;

    // ViewPrepare, ViewDangerous, ViewMapSearch 가 직접 읽는다.
    public DataSet ds_map = new DataSet();
    public DataSet ds_member = new DataSet();

    // '비밀번호 초기화' 임시 비밀번호. 값은 이전과 같고 DB 에는 해시로 저장한다.
    // TODO: 초기화마다 임의 비밀번호를 만들어 관리자에게 보여 주는 방식으로 바꾸는 것이 안전하다.
    private const string DefaultResetPassword = "kab123!@#";

    private readonly object m_DbLock = new object();
    private volatile DatabaseAccess m_OpenDb;          // 열린 뒤에는 lock 없이 읽는다 (백그라운드 작업 중에도 메인 스레드가 멈추지 않게)
    private volatile bool m_Closed;                    // 종료·씬 전환으로 닫은 뒤에는 다시 열지 않는다 (늦게 끝난 async 조회가 연결을 새로 열어 남기지 않게)
    private Dictionary<string, string> m_LocalNames;
    private string filePath;
    private string m_DefaultResetHash;                 // 임시 비밀번호 해시 (해시 계산이 느려서 한 번만)

    /// <summary>앱이 쓰는 DB 파일 경로. 파이썬 스크립트에 넘길 때 쓴다. 메인 스레드에서 호출.</summary>
    public string DatabasePath
    {
        get { return Path.Combine(Application.streamingAssetsPath, m_DatabaseFileName); }
    }

    void Start()
    {
        EnsureOpen();
    }

    void OnApplicationQuit()
    {
        CloseDatabase();
    }

    void OnDestroy()
    {
        CloseDatabase();
    }

    private void CloseDatabase()
    {
        m_Closed = true;

        // 종료 시 오래 걸리는 백그라운드 작업(엑셀 저장 등)이 끝나기를 무한정 기다리지 않는다.
        // 잠금을 못 잡으면 닫지 않고 둔다 (프로세스가 끝나면 OS 가 정리하고, 커밋된 데이터는 안전하다).
        if (!System.Threading.Monitor.TryEnter(m_DbLock, 3000))
        {
            Debug.LogWarning("[DB] 진행 중인 작업이 있어 연결을 닫지 않고 종료합니다.");
            return;
        }

        try
        {
            if (m_DatabaseAccess != null)
            {
                m_OpenDb = null;
                m_DatabaseAccess.Dispose();
                m_DatabaseAccess = null;
            }
        }
        finally
        {
            System.Threading.Monitor.Exit(m_DbLock);
        }
    }

    /// <summary>메인 스레드에서 호출한다 (Application.streamingAssetsPath).</summary>
    private DatabaseAccess EnsureOpen()
    {
        // 이미 열려 있으면 lock 을 기다리지 않는다
        DatabaseAccess open = m_OpenDb;
        if (open != null)
        {
            return open;
        }

        lock (m_DbLock)
        {
            if (m_DatabaseAccess != null)
            {
                return m_DatabaseAccess;
            }
            if (m_Closed)
            {
                throw new ObjectDisposedException("ServerManager", "DB 연결을 이미 닫았습니다 (앱 종료 또는 로그아웃).");
            }

            filePath = Path.Combine(Application.streamingAssetsPath, m_DatabaseFileName);
            Debug.Log("[DB] " + filePath);

            DatabaseAccess db = new DatabaseAccess("data source=" + filePath);

            try
            {
                try
                {
                    DbMigrator.Migrate(db);
                }
                catch (Exception e)
                {
                    // 인덱스가 없어도 조회는 된다(느릴 뿐). 앱은 계속 띄운다.
                    Debug.LogError("[DB] 마이그레이션 실패: " + e.Message);
                    Debug.LogException(e);
                }

                DataSet map = new DataSet();
                map.Tables.Add(db.ReadFullTable("tb_bjdong"));
                ds_map = map;
                m_LocalNames = null;
            }
            catch
            {
                // 경로가 틀리면 SQLite 는 빈 파일을 새로 만들고 여기서 'no such table' 이 난다. 연결을 남기지 않는다.
                db.Dispose();
                throw;
            }

            m_DatabaseAccess = db;
            m_OpenDb = db;
            return db;
        }
    }

    /// <summary>lock 안에서 호출. 기다리는 사이 연결이 닫혔으면(앱 종료) 닫힌 연결을 쓰지 않는다.</summary>
    private void ThrowIfClosed(DatabaseAccess db)
    {
        if (!ReferenceEquals(db, m_DatabaseAccess))
        {
            throw new ObjectDisposedException("DatabaseAccess");
        }
    }

    private void Run(Action<DatabaseAccess> work)
    {
        DatabaseAccess db = EnsureOpen();

        lock (m_DbLock)
        {
            ThrowIfClosed(db);
            work(db);
        }
    }

    private T Run<T>(Func<DatabaseAccess, T> work)
    {
        DatabaseAccess db = EnsureOpen();

        lock (m_DbLock)
        {
            ThrowIfClosed(db);
            return work(db);
        }
    }

    private DataTable RunQuery(string name, Func<DatabaseAccess, DataTable> work)
    {
        try
        {
            return Run(work) ?? new DataTable();
        }
        catch (Exception e)
        {
            Debug.LogError("[DB] 조회 실패 (" + name + "): " + e.Message);
            Debug.LogException(e);
            return new DataTable();
        }
    }

    private Task<T> RunAsync<T>(Func<DatabaseAccess, T> work)
    {
        DatabaseAccess db = EnsureOpen();

        return Task.Run(() =>
        {
            lock (m_DbLock)
            {
                ThrowIfClosed(db);
                return work(db);
            }
        });
    }

    private async Task<DataTable> RunQueryAsync(string name, Func<DatabaseAccess, DataTable> work)
    {
        try
        {
            DataTable table = await RunAsync(work);
            return table ?? new DataTable();
        }
        catch (Exception e)
        {
            Debug.LogError("[DB] 조회 실패 (" + name + "): " + e.Message);
            Debug.LogException(e);
            return new DataTable();
        }
    }

    // =====================================================================================
    // 회원 / 로그인
    // =====================================================================================

    /// <summary>가입자 정보 불러오기</summary>
    public DataSet GetMemberData()
    {
        DataSet ds = new DataSet();
        ds.Tables.Add(RunQuery("tb_member", db => db.ReadFullTable("tb_member")));
        ds_member = ds;
        return ds_member;
    }

    /// <summary>유저의 아이디로 검색</summary>
    public DataTable GetMemberDataLogin(string userId)
    {
        DataTable table = RunQuery("login", db => db.ReadUserTable(userId));

        DataSet ds = new DataSet();
        ds.Tables.Add(table);
        ds_member = ds;

        return table;
    }

    /// <summary>비밀번호 변경. passwordHash 는 PasswordHasher.Hash 로 만든 값.</summary>
    public void UpdateUserPassword(string userId, string passwordHash)
    {
        Run(db => db.UpdateUserPassword(userId, passwordHash));
    }

    /// <summary>예전 형식(Base64) 비밀번호를 해시로 교체 (로그인 성공 직후 1회)</summary>
    public void UpgradePasswordHash(string userId, string passwordHash)
    {
        Run(db => db.UpgradePasswordHash(userId, passwordHash));
    }

    /// <summary>로그인 실패 횟수 +1 (이름은 기존 호출부 호환용)</summary>
    public void GetMemberDataUpdate(string userId, int failCount)
    {
        Run(db => db.IncreaseUserFailCount(userId));
    }

    /// <summary>계정 잠금</summary>
    public void GetMemberStatusDataUpdate(string userId)
    {
        Run(db => db.UpdateUserStatusLock(userId));
    }

    /// <summary>로그인 성공 시 실패 횟수 초기화</summary>
    public void UpdateUserFailCount(string userId)
    {
        Run(db => db.ResetUserFailCount(userId));
    }

    public void AccountUpdate(MemberData member)
    {
        Run(db => db.UpdateMemberInfo(member));
    }

    /// <summary>계정 관리 - 계정 추가</summary>
    public DataTable InsertAccount()
    {
        return Run(db => db.InsertAccount());
    }

    /// <summary>계정 관리 - 비밀번호 초기화 (임시 비밀번호 + 정상 상태 + 실패 횟수 0)</summary>
    public void ResetAccountPassword(string userId)
    {
        // 해시 계산(PBKDF2 10만 회)은 느리고, 임시 비밀번호는 모든 계정이 같으므로 한 번만 계산한다.
        // (여러 계정을 한꺼번에 초기화할 때 계정 수 × 해시 시간만큼 화면이 멈추지 않게)
        if (m_DefaultResetHash == null)
        {
            m_DefaultResetHash = PasswordHasher.Hash(DefaultResetPassword);
        }

        string hash = m_DefaultResetHash;

        Run(db =>
        {
            db.UpdateUserPassword(userId, hash);
            db.ResetUserFailCount(userId);
        });
    }

    /// <summary>계정관리 - 유저 상태 정지</summary>
    public void AccountStop(string userId)
    {
        Run(db => db.AccountStop(userId));
    }

    // =====================================================================================
    // 이력
    // =====================================================================================

    public void InsertConnectionLog(string userId, string name, string depart, string title)
    {
        Run(db => db.InsertConnectionLog(userId, name, depart, title));
    }

    public void InsertChangeLog(string name, string title, string content)
    {
        Run(db => db.InsertChangeLog(name, title, content));
    }

    public void InsertDownloadLog(string name, string title, string content)
    {
        Run(db => db.InsertDownloadLog(name, title, content));
    }

    /// <summary>이력 관리 조회 - 접속, 변경이력, 다운로드이력, 계정 관리</summary>
    public DataTable GetLog(string tableName, int FromDate, int ToDate, bool[] ShearchType, string ShearchString)
    {
        return RunQuery(tableName, db => db.GetLog(tableName, FromDate, ToDate, ShearchType, ShearchString));
    }

    // =====================================================================================
    // 지역 이름
    // =====================================================================================

    /// <summary>지역 코드 → 이름. 없는 코드면 빈 문자열 (이전: 예외로 화면이 멈춤)</summary>
    public string LocalName(string mapcode)
    {
        EnsureOpen();

        if (m_LocalNames == null)
        {
            Dictionary<string, string> names = new Dictionary<string, string>();
            DataTable table = ds_map != null && ds_map.Tables.Count > 0 ? ds_map.Tables[0] : null;

            if (table != null)
            {
                foreach (DataRow row in table.Rows)
                {
                    string code = row["bjdong_code"].ToString();

                    if (!names.ContainsKey(code))
                    {
                        names.Add(code, row["bjdong_name"].ToString());
                    }
                }
            }

            m_LocalNames = names;
        }

        string name;
        return mapcode != null && m_LocalNames.TryGetValue(mapcode.Trim(), out name) ? name : "";
    }

    // =====================================================================================
    // 통계 / 대시보드
    // =====================================================================================

    /// <summary>통계 지역별 항목 조회</summary>
    public DataTable GetStatisticsByRegion(int FromDate, int ToDate, bool[] CityToggle)
    {
        return RunQuery("통계-지역별", db => db.GetStatisticsByRegion(FromDate, ToDate, CityToggle));
    }

    /// <summary>통계 단지별 항목조회</summary>
    public DataTable GetStatisticsByDanji(int FromDate, int ToDate, bool[] CityToggle, bool[] BustType, bool[] RfxgType, bool[] PrecentType)
    {
        return RunQuery("통계-단지별", db => db.GetStatisticsByDanji(FromDate, ToDate, CityToggle, BustType, RfxgType, PrecentType));
    }

    /// <summary>risk_data 테이블 마지막 change_date 기준 1년</summary>
    public DataTable GetLastCreateDateRiskData()
    {
        return RunQuery("최근 업로드일", db => db.GetLastCreateDateRiskData());
    }

    /// <summary>위험도 퍼센트별 사람 수</summary>
    public DataTable GetPercentPerson(string startDate, string endDate, bool[] CityId, string StatisticMapSort)
    {
        return RunQuery("퍼센트 인원", db => db.GetPercentPerson(StatisticMapSort, startDate, endDate, CityId));
    }

    /// <summary>대시보드 위험도 지도 (최근 1년 평균) + 부정청약 위험도 순위 (전국)</summary>
    public DataTable GetDashBordMapByDanger(bool[] CityId, string startDate, string endDate, string type)
    {
        return RunQuery("대시보드 지도", db => db.GetMapByDanger(CityId, "MainMapSort", startDate, endDate, type));
    }

    /// <summary>통계 위험도 지도 (모집공고일 기준) + 순위 + 적발수 그래프</summary>
    public DataTable GetStatisticMapByDanger(bool[] CityId, string FromDate, string ToDate, string type)
    {
        return RunQuery("통계 지도", db => db.GetMapByDanger(CityId, "StatisticMapSort", FromDate, ToDate, type));
    }

    /// <summary>대시보드 위험도 지도 (최근 1년 평균) (지역)</summary>
    public DataTable GetDashBordMapByDangerLocal(bool[] CityId, string startDate, string endDate, string type)
    {
        return RunQuery("대시보드 지역 지도", db => db.GetMapByDangerLocal(CityId, "MainMapSort", startDate, endDate, type));
    }

    /// <summary>통계 위험도 지도 (모집공고일 기준) (지역)</summary>
    public DataTable GetStatisticMapByDangerLocal(bool[] CityId, string FromDate, string ToDate, string type)
    {
        return RunQuery("통계 지역 지도", db => db.GetMapByDangerLocal(CityId, "StatisticMapSort", FromDate, ToDate, type));
    }

    /// <summary>대시보드 부정청약 위험도 퍼센트 (전체/5/10/20%) - '전체' 컬럼 4행</summary>
    public DataTable GetDashBordMapByDangerPercent(bool[] CityId, string startDate, string endDate, string AllPercent, string Percent5, string Percent10, string Percent20)
    {
        string[] limits = { AllPercent, Percent5, Percent10, Percent20 };
        return RunQuery("대시보드 퍼센트", db => db.GetMapByDangerPercent(CityId, "MainMapSort", startDate, endDate, limits));
    }

    /// <summary>통계 부정청약 위험도 퍼센트 (전체/5/10/20%)</summary>
    public DataTable GetStatisticMapByDangerPercent(bool[] CityId, string startDate, string endDate, string AllPercent, string Percent5, string Percent10, string Percent20)
    {
        string[] limits = { AllPercent, Percent5, Percent10, Percent20 };
        return RunQuery("통계 퍼센트", db => db.GetMapByDangerPercent(CityId, "StatisticMapSort", startDate, endDate, limits));
    }

    // =====================================================================================
    // 주택청약 시장관리
    // =====================================================================================

    // 백그라운드 조회는 SQL 을 작업 스레드에서 만든다. 화면 코드는 호출 직후(await 전에) 같은 선택 배열을
    // 다시 쓰므로(SelectMenu: 전부 false → 선택한 것만 true) 호출 시점의 값을 복사해서 넘긴다.
    private static bool[] Snapshot(bool[] flags)
    {
        return flags == null ? null : (bool[])flags.Clone();
    }

    /// <summary>단지 선정</summary>
    public Task<DataTable> GetMarketManageByDanji(int FromDate, int ToDate, bool[] ShearchType, string ShearchString, bool[] CityToggle, bool[] BustType, bool[] RfxgType, bool[] PrecentType)
    {
        bool[] st = Snapshot(ShearchType), city = Snapshot(CityToggle), bust = Snapshot(BustType), rfxg = Snapshot(RfxgType), pct = Snapshot(PrecentType);
        return RunQueryAsync("단지선정", db => db.GetMarketManageByDanji(FromDate, ToDate, st, ShearchString, city, bust, rfxg, pct));
    }

    /// <summary>
    /// 실시 등록 - 이전과 같은 214개 컬럼. 호환용으로 남겨 둔다.
    /// 화면에서는 필요한 컬럼만 가져오는 GetExecuteList 를 쓴다.
    /// </summary>
    public Task<DataTable> GetMarketManageByImplementationAndRegistration(int FromDate, int ToDate, bool[] ShearchType, string ShearchString, bool[] CityToggle, bool[] BustType, bool[] RfxgType, bool[] PrecentType)
    {
        bool[] st = Snapshot(ShearchType), city = Snapshot(CityToggle), bust = Snapshot(BustType), rfxg = Snapshot(RfxgType), pct = Snapshot(PrecentType);
        return RunQueryAsync("실시등록(전체 컬럼)", db => db.GetMarketManageByImplementationAndRegistration(FromDate, ToDate, st, ShearchString, city, bust, rfxg, pct));
    }

    /// <summary>실시 등록 - 화면 표시/편집에 필요한 컬럼만 (약 50개)</summary>
    public Task<DataTable> GetExecuteList(int FromDate, int ToDate, bool[] ShearchType, string ShearchString, bool[] CityToggle, bool[] BustType, bool[] RfxgType, bool[] PrecentType)
    {
        bool[] st = Snapshot(ShearchType), city = Snapshot(CityToggle), bust = Snapshot(BustType), rfxg = Snapshot(RfxgType), pct = Snapshot(PrecentType);
        return RunQueryAsync("실시등록 목록", db => db.GetExecuteList(FromDate, ToDate, st, ShearchString, city, bust, rfxg, pct));
    }

    /// <summary>
    /// 실시 등록 - 한 페이지만 (서버 페이징). 위험도 순위는 DB 가 매기고 넓은 행은 그 페이지만 읽는다.
    /// 수십만 건 이상에서는 목록 화면을 이 메서드 + GetExecuteCounts 로 바꾸는 것을 권장 (docs 참고).
    /// </summary>
    public Task<DataTable> GetExecutePage(int FromDate, int ToDate, bool[] ShearchType, string ShearchString, bool[] CityToggle, bool[] BustType, bool[] RfxgType, bool[] PrecentType, int offset, int limit)
    {
        bool[] st = Snapshot(ShearchType), city = Snapshot(CityToggle), bust = Snapshot(BustType), rfxg = Snapshot(RfxgType), pct = Snapshot(PrecentType);
        return RunQueryAsync("실시등록 페이지", db => db.GetExecuteList(FromDate, ToDate, st, ShearchString, city, bust, rfxg, pct, offset, Math.Max(1, limit)));
    }

    /// <summary>실시 등록 - 전체/미점검(N)/해당없음(Z)/점검완료(Y) 개수</summary>
    public Task<DataTable> GetExecuteCounts(int FromDate, int ToDate, bool[] ShearchType, string ShearchString, bool[] CityToggle, bool[] BustType, bool[] RfxgType, bool[] PrecentType)
    {
        bool[] st = Snapshot(ShearchType), city = Snapshot(CityToggle), bust = Snapshot(BustType), rfxg = Snapshot(RfxgType), pct = Snapshot(PrecentType);
        return RunQueryAsync("실시등록 개수", db => db.GetExecuteCounts(FromDate, ToDate, st, ShearchString, city, bust, rfxg, pct));
    }

    /// <summary>
    /// 실시 등록 '결과 저장': [data_no, DatabaseAccess.ExecuteResultColumns 순서의 값 7개] 목록을 한 트랜잭션으로 저장.
    /// 이전에는 CSV 를 쓰고 파이썬(SaveResult.py)을 실행했다.
    /// </summary>
    public Task<int> SaveExecuteResultsAsync(List<object[]> rows)
    {
        return RunAsync(db => db.SaveExecuteResults(rows));
    }

    /// <summary>
    /// 신청자 원본 행을 CSV(EUC-KR)로 내보낸다. 메모리에 전부 올리지 않고 한 행씩 쓴다.
    /// columns 가 null 이면 tb_riskdata 의 실제 컬럼(data_idx, create_date, change_date 제외) 중 앞의 headers 개수만큼 쓴다.
    /// </summary>
    public Task<int> ExportRiskRowsToCsvAsync(string path, List<string> dataNos, string[] columns, string[] headers, string[] overlayColumns, Dictionary<string, object[]> overlay)
    {
        Encoding encoding = Encoding.GetEncoding("EUC-KR");
        return RunAsync(db =>
        {
            string[] cols = columns;

            if (cols == null)
            {
                string[] all = db.GetRiskExportColumns();
                int n = headers == null ? 0 : headers.Length;

                if (all.Length < n)
                {
                    throw new InvalidOperationException("tb_riskdata 컬럼(" + all.Length + "개)이 엑셀 헤더(" + n + "개)보다 적습니다.");
                }

                cols = new string[n];
                Array.Copy(all, cols, n);
            }

            return db.ExportRiskRowsToCsv(path, dataNos, cols, headers, overlayColumns, overlay, encoding);
        });
    }

    // =====================================================================================
    // 위험도 테이블 쓰기
    // =====================================================================================

    public void DeleteRiskTable(string tableName)
    {
        Run(db => db.DeleteAll(tableName));
    }

    /// <summary>여러 행 삭제를 한 트랜잭션으로 (이전: 행마다 연결 열기 + 자동 커밋 + Task.Delay)</summary>
    public Task<int> DeleteRiskDataBatchAsync(string tableName, string keyColumn, List<string> keys)
    {
        return RunAsync(db => db.DeleteByKeys(tableName, keyColumn, keys));
    }

    public void DeleteRiskData(string tableName, string cols, string value)
    {
        Run(db => db.DeleteByKeys(tableName, cols, new List<string> { value }));
    }

    /// <summary>여러 행 삽입을 한 트랜잭션으로. progress 는 메인 스레드에서 만든 Progress&lt;int&gt; 를 넘기면 안전하게 갱신된다.</summary>
    public Task<int> InsertRowsAsync(string tableName, string[] cols, List<object[]> rows, IProgress<int> progress = null)
    {
        return RunAsync(db => db.InsertRows(tableName, cols, rows, progress == null ? (Action<int>)null : n => progress.Report(n)));
    }

    /// <summary>실시등록 위험도 산출 데이터 저장 (1행). 여러 행이면 InsertRowsAsync 를 쓸 것.</summary>
    public void InsertRiskData(string tableName, string[] cols, params object[] value)
    {
        Run(db => db.InsertRows(tableName, cols, new List<object[]> { value }));
    }

    /// <summary>values[0] 을 data_no 로 보고 있으면 UPDATE, 없으면 INSERT (이전 동작 유지)</summary>
    public void UpdateRiskData(string tableName, string[] cols, string[] checks, params object[] value)
    {
        Run(db => db.UpdateOrInsertRow(tableName, cols, value));
    }
}
