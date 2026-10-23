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
