"""DB 스키마 (마이그레이션 2: 한·일 시장 데이터와 일본 신청자).

앱(Assets/Scripts/Data/DbMigrator.cs 의 마이그레이션 2)과 같은 DDL 이다.
파이썬 스크립트를 앱보다 먼저 실행해도 같은 테이블이 생기도록 여기서도 만든다.
둘 중 하나를 고치면 다른 쪽도 고칠 것 (tests/test_schema_sync.py 가 같은지 확인한다).

user_version 은 앱의 DbMigrator 만 올린다. 여기서는 IF NOT EXISTS 로 테이블과 인덱스만 만든다.
"""

MIGRATION_2 = [
    """CREATE TABLE IF NOT EXISTS tb_region (
    country TEXT NOT NULL,
    region_code TEXT NOT NULL,
    level INTEGER NOT NULL,
    parent_code TEXT,
    name_ko TEXT,
    name_local TEXT,
    name_en TEXT,
    PRIMARY KEY (country, region_code)
);""",
    """CREATE TABLE IF NOT EXISTS tb_market_trade (
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
);""",
    """CREATE INDEX IF NOT EXISTS ix_trade_region ON tb_market_trade (country, property_type, region_code, deal_year, deal_quarter);""",
    """CREATE INDEX IF NOT EXISTS ix_trade_pref ON tb_market_trade (country, property_type, pref_code, deal_year, deal_quarter);""",
    """CREATE TABLE IF NOT EXISTS tb_market_competition (
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
);""",
    """CREATE INDEX IF NOT EXISTS ix_comp_region ON tb_market_competition (country, scheme, region_code, announce_date);""",
    """CREATE INDEX IF NOT EXISTS ix_comp_pref ON tb_market_competition (country, scheme, pref_code, announce_date);""",
    """CREATE TABLE IF NOT EXISTS tb_market_region_stat (
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
);""",
    """CREATE TABLE IF NOT EXISTS tb_jp_applicant (
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
);""",
    """CREATE INDEX IF NOT EXISTS ix_jp_applicant_round ON tb_jp_applicant (scheme, round_id, inspection_status_result);""",
    """CREATE TABLE IF NOT EXISTS tb_jp_risk (
    applicant_key TEXT PRIMARY KEY,
    risk_score INTEGER NOT NULL,
    risk_level TEXT NOT NULL,
    reason_codes TEXT,
    reason_text TEXT,
    scored_at TEXT DEFAULT CURRENT_TIMESTAMP
);""",
    """CREATE INDEX IF NOT EXISTS ix_jp_risk_score ON tb_jp_risk (risk_score);""",
    """CREATE TABLE IF NOT EXISTS tb_market_fetch_log (
    source TEXT NOT NULL,
    scope TEXT NOT NULL,
    period TEXT NOT NULL,
    row_count INTEGER,
    fetched_at TEXT DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (source, scope, period)
);""",
]


def normalize_sql(sql):
    """비교용: 공백을 하나로 줄인다."""
    return " ".join(sql.split())
