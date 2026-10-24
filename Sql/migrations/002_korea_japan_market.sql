-- 002_korea_japan_market.sql
--
-- 앱이 시작할 때 DbMigrator.cs 가 같은 내용을 자동으로 적용한다 (PRAGMA user_version 1 → 2).
-- 파이썬 스크립트(korea_rebs/market_data.py, jp_fraud.py)도 같은 테이블을 IF NOT EXISTS 로 만든다 (krjp/schema.py).
-- 수동 적용 후에는 PRAGMA user_version = 2; 도 실행해야 앱이 다시 적용하지 않는다.
--
-- tb_region              지역 마스터. 한국 시도(2자리)·시군구(5자리), 일본 都道府県(2자리)·市区町村(5자리)
-- tb_market_trade        실거래. 한국 국토교통부 아파트 매매(가격 원), 일본 不動産情報ライブラリ 取引価格·成約価格(가격 엔)
--                        unit_price = ㎡당 가격, is_cancelled = 한국 해제 거래(집계에서 뺀다)
-- tb_market_competition  청약·추첨 경쟁률. scheme: kr_apt(청약홈, 1순위 접수 ÷ 일반공급),
--                        jp_public_housing(공영주택 응모 ÷ 모집 호수), jp_new_condo(신축 맨션 등록 ÷ 판매 호수)
-- tb_market_region_stat  지역별 집계 (market_data.py analyze 가 지우고 다시 만든다)
--                        price_index = 지역 ㎡당 중앙값 ÷ 같은 나라·유형·기간 전국 중앙값 × 100
-- tb_jp_applicant        일본 신청자 (개인정보 포함). 점검 결과 컬럼은 앱이 쓰고, 다시 가져와도 지우지 않는다
-- tb_jp_risk             일본 신청자 규칙 기반 위험 점수 (jp_fraud.py score 가 다시 만든다)
-- tb_market_fetch_log    이미 받은 기간 (같은 기간을 다시 호출하지 않도록)

CREATE TABLE IF NOT EXISTS tb_region (
    country TEXT NOT NULL,
    region_code TEXT NOT NULL,
    level INTEGER NOT NULL,
    parent_code TEXT,
    name_ko TEXT,
    name_local TEXT,
    name_en TEXT,
    PRIMARY KEY (country, region_code)
);

CREATE TABLE IF NOT EXISTS tb_market_trade (
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
);

CREATE INDEX IF NOT EXISTS ix_trade_region ON tb_market_trade (country, property_type, region_code, deal_year, deal_quarter);

CREATE INDEX IF NOT EXISTS ix_trade_pref ON tb_market_trade (country, property_type, pref_code, deal_year, deal_quarter);

CREATE TABLE IF NOT EXISTS tb_market_competition (
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
);

CREATE INDEX IF NOT EXISTS ix_comp_region ON tb_market_competition (country, scheme, region_code, announce_date);

CREATE INDEX IF NOT EXISTS ix_comp_pref ON tb_market_competition (country, scheme, pref_code, announce_date);

CREATE TABLE IF NOT EXISTS tb_market_region_stat (
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
);

CREATE TABLE IF NOT EXISTS tb_jp_applicant (
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
);

CREATE INDEX IF NOT EXISTS ix_jp_applicant_round ON tb_jp_applicant (scheme, round_id, inspection_status_result);

CREATE TABLE IF NOT EXISTS tb_jp_risk (
    applicant_key TEXT PRIMARY KEY,
    risk_score INTEGER NOT NULL,
    risk_level TEXT NOT NULL,
    reason_codes TEXT,
    reason_text TEXT,
    scored_at TEXT DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS ix_jp_risk_score ON tb_jp_risk (risk_score);

CREATE TABLE IF NOT EXISTS tb_market_fetch_log (
    source TEXT NOT NULL,
    scope TEXT NOT NULL,
    period TEXT NOT NULL,
    row_count INTEGER,
    fetched_at TEXT DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (source, scope, period)
);
