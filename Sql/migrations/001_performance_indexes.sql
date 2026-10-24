-- 001_performance_indexes.sql
--
-- 앱이 시작할 때 DbMigrator.cs 가 같은 내용을 자동으로 적용한다 (PRAGMA user_version 0 → 1).
-- 이 파일은 DB Browser for SQLite 등에서 내용을 확인하거나 수동으로 적용할 때 쓴다.
-- 수동 적용 후에는 PRAGMA user_version = 1; 도 실행해야 앱이 다시 적용하지 않는다.
--
-- 왜 '커버링 인덱스'인가
--   tb_riskdata 는 한 행에 175개 컬럼(약 900바이트)이 있다. 통계/대시보드 쿼리는 그중 15개 정도만 쓰는데,
--   인덱스가 없어서 매번 6만~수십만 행 전체를 읽고, SELECT * 서브쿼리로 넓은 행을 정렬했다.
--   쿼리가 쓰는 컬럼을 전부 담은 인덱스를 만들면 SQLite 가 원본 행을 읽지 않고 인덱스만으로 답한다.
--   (EXPLAIN QUERY PLAN 에 'USING COVERING INDEX' 로 표시됨)
--
-- 66,463행 기준 생성 시간 약 0.5초, 664,630행 기준 약 5초 (최초 1회).

-- 모집공고일(rec_anno_date) 기준 화면: 통계(지역/단지), 시장관리(단지선정/실시등록), 통계 지도
CREATE INDEX IF NOT EXISTS ix_riskdata_anno_cover ON tb_riskdata (
    rec_anno_date, apt_sup_pos, inspection_status_result, apt_name, apt_number, apt_type,
    unauth_rf_prob, unauth_xgb_prob, fake_rf_prob, fake_xgb_prob, sale_rf_prob, sale_xgb_prob,
    unauth_sub_decision, unauth_disguise_transfer, unauth_savecerti_sale, unauth_other,
    total_suphouse_number
);

-- 업로드일시(change_date) 기준 화면: 대시보드 (최근 1년), MAX(change_date)
CREATE INDEX IF NOT EXISTS ix_riskdata_change_cover ON tb_riskdata (
    change_date, apt_sup_pos, apt_name,
    unauth_rf_prob, unauth_xgb_prob,
    unauth_sub_decision, unauth_disguise_transfer, unauth_savecerti_sale, unauth_other
);

-- 지역명 조인
CREATE INDEX IF NOT EXISTS ix_bjdong_code ON tb_bjdong (bjdong_code, bjdong_name);

-- 이력/계정 화면 (기간 검색) + 로그인 조회
CREATE INDEX IF NOT EXISTS ix_connect_log_date ON tb_connect_log (create_date);
CREATE INDEX IF NOT EXISTS ix_change_log_date ON tb_change_log (create_date);
CREATE INDEX IF NOT EXISTS ix_down_log_date ON tb_down_log (create_date);
CREATE INDEX IF NOT EXISTS ix_member_userid ON tb_member (mem_userid);
CREATE INDEX IF NOT EXISTS ix_member_date ON tb_member (create_date);

-- 쿼리 플래너용 통계
ANALYZE;

-- (앱은 마이그레이션 뒤 WAL 모드를 켠다. 수동으로 켜려면 트랜잭션 밖에서)
-- PRAGMA journal_mode = WAL;
