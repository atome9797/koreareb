using System;
using System.Diagnostics;
using System.Globalization;

/// <summary>
/// DB 스키마 변경을 코드로 관리한다.
/// 적용한 마지막 번호를 PRAGMA user_version 에 기록하므로 여러 번 실행해도 한 번만 적용된다.
/// 같은 내용이 Sql/migrations/*.sql 에도 있다 (DB Browser 로 확인/수동 적용용). 둘을 같이 고칠 것.
///
/// .db 파일 자체를 바꿔 커밋하는 대신 이렇게 하는 이유:
/// - 배포된 PC 마다 DB 내용이 달라도(운영 데이터) 같은 변경을 안전하게 적용할 수 있다.
/// - 바이너리 DB 를 다시 커밋하지 않아도 된다(DB 에는 개인정보가 들어 있음).
/// </summary>
public static class DbMigrator
{
    private static readonly string[][] Migrations =
    {
        // 1) 조회 성능용 인덱스 (Sql/migrations/001_performance_indexes.sql)
        new[]
        {
            // 모집공고일 기준 화면: 통계(지역/단지), 시장관리(단지선정/실시등록), 통계 지도.
            // 쿼리가 쓰는 컬럼을 모두 넣은 '커버링 인덱스'라서 175개 컬럼짜리 원본 행을 읽지 않는다.
            "CREATE INDEX IF NOT EXISTS ix_riskdata_anno_cover ON tb_riskdata (" +
            "rec_anno_date, apt_sup_pos, inspection_status_result, apt_name, apt_number, apt_type, " +
            "unauth_rf_prob, unauth_xgb_prob, fake_rf_prob, fake_xgb_prob, sale_rf_prob, sale_xgb_prob, " +
            "unauth_sub_decision, unauth_disguise_transfer, unauth_savecerti_sale, unauth_other, total_suphouse_number);",

            // 업로드일시 기준 화면: 대시보드(최근 1년), MAX(change_date)
            "CREATE INDEX IF NOT EXISTS ix_riskdata_change_cover ON tb_riskdata (" +
            "change_date, apt_sup_pos, apt_name, unauth_rf_prob, unauth_xgb_prob, " +
            "unauth_sub_decision, unauth_disguise_transfer, unauth_savecerti_sale, unauth_other);",

            "CREATE INDEX IF NOT EXISTS ix_bjdong_code ON tb_bjdong (bjdong_code, bjdong_name);",

            "CREATE INDEX IF NOT EXISTS ix_connect_log_date ON tb_connect_log (create_date);",
            "CREATE INDEX IF NOT EXISTS ix_change_log_date ON tb_change_log (create_date);",
            "CREATE INDEX IF NOT EXISTS ix_down_log_date ON tb_down_log (create_date);",
            "CREATE INDEX IF NOT EXISTS ix_member_userid ON tb_member (mem_userid);",
            "CREATE INDEX IF NOT EXISTS ix_member_date ON tb_member (create_date);",

            // 쿼리 플래너가 인덱스를 고를 수 있도록 통계 생성
            "ANALYZE;"
        },

        // 2) 한·일 시장 데이터와 일본 신청자 (Sql/migrations/002_korea_japan_market.sql)
        //    파이썬 korea_rebs/krjp/schema.py 의 MIGRATION_2 와 같아야 한다 (tests/test_schema_sync.py 가 확인).
        // migration 2 begin
        new[]
        {
            @"CREATE TABLE IF NOT EXISTS tb_region (
    country TEXT NOT NULL,
    region_code TEXT NOT NULL,
    level INTEGER NOT NULL,
    parent_code TEXT,
    name_ko TEXT,
    name_local TEXT,
    name_en TEXT,
    PRIMARY KEY (country, region_code)
);",
            @"CREATE TABLE IF NOT EXISTS tb_market_trade (
    trade_key TEXT PRIMARY KEY,
    country TEXT NOT NULL,
    source TEXT NOT NULL,
    property_type TEXT NOT NULL,
    property_type_raw TEXT,
    pref_code TEXT NOT NULL,
    region_code TEXT NOT NULL,
    district_name TEXT,
    building_name TEXT,
    deal_year INTEGER NOT NULL,
    deal_quarter INTEGER NOT NULL,
    deal_month INTEGER,
    price REAL NOT NULL,
    currency TEXT NOT NULL,
    area_m2 REAL,
    unit_price REAL,
    build_year INTEGER,
    floor_plan TEXT,
    price_kind TEXT,
    is_cancelled INTEGER NOT NULL DEFAULT 0,
    fetched_at TEXT DEFAULT CURRENT_TIMESTAMP
);",
            @"CREATE INDEX IF NOT EXISTS ix_trade_region ON tb_market_trade (country, property_type, region_code, deal_year, deal_quarter);",
            @"CREATE INDEX IF NOT EXISTS ix_trade_pref ON tb_market_trade (country, property_type, pref_code, deal_year, deal_quarter);",
            @"CREATE TABLE IF NOT EXISTS tb_market_competition (
    comp_key TEXT PRIMARY KEY,
    country TEXT NOT NULL,
    scheme TEXT NOT NULL,
    source TEXT NOT NULL,
    project_id TEXT,
    project_name TEXT,
    unit_type TEXT,
    pref_code TEXT,
    region_code TEXT,
    announce_date TEXT,
    supply_units INTEGER,
    applications INTEGER,
    competition_rate REAL,
    min_score REAL,
    avg_score REAL,
    max_score REAL,
    avg_price REAL,
    currency TEXT,
    fetched_at TEXT DEFAULT CURRENT_TIMESTAMP
);",
            @"CREATE INDEX IF NOT EXISTS ix_comp_region ON tb_market_competition (country, scheme, region_code, announce_date);",
            @"CREATE INDEX IF NOT EXISTS ix_comp_pref ON tb_market_competition (country, scheme, pref_code, announce_date);",
            @"CREATE TABLE IF NOT EXISTS tb_market_region_stat (
    country TEXT NOT NULL,
    metric TEXT NOT NULL,
    category TEXT NOT NULL,
    level INTEGER NOT NULL,
    region_code TEXT NOT NULL,
    period TEXT NOT NULL,
    n_records INTEGER,
    median_unit_price REAL,
    p25_unit_price REAL,
    p75_unit_price REAL,
    median_price REAL,
    median_area_m2 REAL,
    supply_units INTEGER,
    applications INTEGER,
    competition_rate REAL,
    price_index REAL,
    currency TEXT,
    built_at TEXT DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (country, metric, category, level, region_code, period)
);",
            @"CREATE TABLE IF NOT EXISTS tb_jp_applicant (
    applicant_key TEXT PRIMARY KEY,
    scheme TEXT NOT NULL,
    round_id TEXT NOT NULL,
    project_name TEXT,
    unit_category TEXT,
    unit_no TEXT,
    application_no TEXT NOT NULL,
    applicant_name TEXT,
    birth_date TEXT,
    phone TEXT,
    email TEXT,
    address TEXT,
    pref_code TEXT,
    region_code TEXT,
    moved_in_date TEXT,
    household_size INTEGER,
    household_changed_date TEXT,
    household_income REAL,
    disabled_household INTEGER,
    current_housing TEXT,
    corporate_name TEXT,
    purchase_purpose TEXT,
    will_reside INTEGER,
    contract_name TEXT,
    resale_listed INTEGER,
    applied_date TEXT,
    imported_at TEXT DEFAULT CURRENT_TIMESTAMP,
    inspection_status_result TEXT NOT NULL DEFAULT 'N',
    unauth_decision INTEGER,
    unauth_type TEXT,
    inspected_at TEXT
);",
            @"CREATE INDEX IF NOT EXISTS ix_jp_applicant_round ON tb_jp_applicant (scheme, round_id, inspection_status_result);",
            @"CREATE TABLE IF NOT EXISTS tb_jp_risk (
    applicant_key TEXT PRIMARY KEY,
    risk_score INTEGER NOT NULL,
    risk_level TEXT NOT NULL,
    reason_codes TEXT,
    reason_text TEXT,
    scored_at TEXT DEFAULT CURRENT_TIMESTAMP
);",
            @"CREATE INDEX IF NOT EXISTS ix_jp_risk_score ON tb_jp_risk (risk_score);",
            @"CREATE TABLE IF NOT EXISTS tb_market_fetch_log (
    source TEXT NOT NULL,
    scope TEXT NOT NULL,
    period TEXT NOT NULL,
    row_count INTEGER,
    fetched_at TEXT DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (source, scope, period)
);",
        },
        // migration 2 end
    };

    public static int LatestVersion
    {
        get { return Migrations.Length; }
    }

    public static void Migrate(DatabaseAccess db)
    {
        int current = Convert.ToInt32(db.ExecuteScalar("PRAGMA user_version;"), CultureInfo.InvariantCulture);

        for (int index = current; index < Migrations.Length; index++)
        {
            string[] statements = Migrations[index];
            int target = index + 1;
            Stopwatch watch = Stopwatch.StartNew();

            db.RunInTransaction(() =>
            {
                foreach (string sql in statements)
                {
                    db.ExecuteNonQuery(sql);
                }

                db.ExecuteNonQuery("PRAGMA user_version = " + target.ToString(CultureInfo.InvariantCulture) + ";");
            });

            UnityEngine.Debug.Log("[DB] migration " + target + " applied (" + watch.ElapsedMilliseconds + " ms)");
        }

        if (!db.TryEnableWal())
        {
            UnityEngine.Debug.LogWarning("[DB] WAL 모드를 켜지 못했습니다. 기존 저널 모드로 계속합니다: " + db.JournalMode);
        }
    }
}
