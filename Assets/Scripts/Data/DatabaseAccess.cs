using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mono.Data.Sqlite;
using System.Data;
using UnityEngine.UI;
using System;
using System.Text;
using System.Threading.Tasks;

public class DatabaseAccess
{
    private SqliteConnection m_DatabaseConnection;
    private SqliteCommand m_DatabaseCommand;
    private SqliteDataReader m_Reader;
    private SqliteDataAdapter m_Adapter;
    int[] CityId = new int[] { 11, 26, 27, 28, 29, 30, 31, 36, 41, 42, 43, 44, 45, 46, 47, 48, 50 };

    public DatabaseAccess(string connectionString)
    {
        OpenDatabase(connectionString);
    }

    public void OpenDatabase(string connectionString)
    {
        m_DatabaseConnection = new SqliteConnection(connectionString);
        m_DatabaseConnection.Open();
        // Debug.Log("Connected to database");
    }

    public void CloseSqlConnection()
    {
        if (m_DatabaseCommand != null)
        {
            m_DatabaseCommand.Dispose();
        }

        m_DatabaseCommand = null;

        if (m_Reader != null)
        {
            m_Reader.Dispose();
        }

        m_Reader = null;

        if (m_Adapter != null)
        {
            m_Adapter.Dispose();
        }

        m_Adapter = null;

        if (m_DatabaseConnection != null)
        {
            m_DatabaseConnection.Close();
        }

        m_DatabaseConnection = null;
        // Debug.Log("Disconnected from database.");
    }

    public DataSet ReadFullTable(string tableName, DataSet ds)
    {
        string query = "SELECT * FROM " + tableName;

        m_Adapter = new SqliteDataAdapter(query, m_DatabaseConnection);

        m_Adapter.Fill(ds);

        return ds;
    }

    public DataSet ReadUserTable(string tableName, string userId, DataSet ds)
    {
        string query = $"SELECT * FROM {tableName} WHERE mem_userid = '{userId}';";

        m_Adapter = new SqliteDataAdapter(query, m_DatabaseConnection);

        m_Adapter.Fill(ds);

        return ds;
    }

    public DataSet UpdateUserFailCount(string tableName, string userId, int failCount, DataSet ds)
    {
        string query = $"UPDATE {tableName} SET mem_failcount = {failCount + 1} WHERE mem_userid = '{userId}';";
        m_Adapter = new SqliteDataAdapter(query, m_DatabaseConnection);

        m_Adapter.Fill(ds);

        return ds;
    }

    public DataSet UpdateUserPassword(string tableName, string userId, string userPassword, DataSet ds)
    {
        string query = $"UPDATE {tableName} SET mem_password  = '{userPassword}', mem_status = 1 WHERE mem_userid = '{userId}';";
        m_Adapter = new SqliteDataAdapter(query, m_DatabaseConnection);

        m_Adapter.Fill(ds);

        return ds;
    }

    public DataSet UpdateUserStatusLock(string tableName, string userId, DataSet ds)
    {
        string query = $"UPDATE {tableName} SET mem_status = 3 WHERE mem_userid = '{userId}';";
        m_Adapter = new SqliteDataAdapter(query, m_DatabaseConnection);

        m_Adapter.Fill(ds);

        return ds;
    }


    /// <summary>
    /// 통계 지역별 조회
    /// </summary>
    /// <param name="ds">데이터 정보</param>
    /// <param name="FromDate">시작일자</param>
    /// <param name="ToDate">종료일자</param>
    /// <param name="City">도시체크박스</param>
    /// <returns></returns>
    public DataSet GetStatisticsByRegionSql(DataSet ds, int FromDate, int ToDate, bool [] CityToggle)
    {

        string query =

                " SELECT " +
                " inner_no as 'NO', " +
                " inner_LocalCode as '지역', " +
                " inner_apt_name as '단지수', " +
                " inner_total_suphouse_number as '세대수', " +
                " inner_inspection_member_check as '점검단지', " +
                " inner_inspection_member_check_count as '점검세대', " +
                " case when inner_unauth_everage is null then 0.0 else inner_unauth_everage * 100 end as '평균', " +
                " inner_danger_all as '전체', " +
                " inner_danger_camo as '위장전입', " +
                " inner_danger_sale as '통장매매', " +
                " inner_danger_other as '기타' " +
                " FROM ( " +
                "SELECT " +
                    " ROW_NUMBER() OVER(" +
                    " ORDER BY inner_data.LocalCode asc" +
                    " ) as inner_no ," +
                    " inner_data.LocalCode as inner_LocalCode ," +
                    " COUNT(inner_data.apt_name) as inner_apt_name ," +
                    " SUM(inner_data.total_suphouse_number) as inner_total_suphouse_number , " +
                    " SUM(CASE WHEN inner_data.inspection_member_check > 0 THEN 1 ELSE 0 END) as inner_inspection_member_check," +
                    " SUM(inner_data.inspection_member_check) as inner_inspection_member_check_count , " +
                    " AVG(CASE WHEN inner_data.inspection_member_check > 0 THEN inner_data.unauth_everage END) AS inner_unauth_everage , " +
                    " SUM(inner_data.danger_all) as inner_danger_all , " +
                    " SUM(inner_data.danger_camo) as inner_danger_camo , " +
                    " SUM(inner_data.danger_sale) as inner_danger_sale , " +
                    " SUM(inner_data.danger_other) as inner_danger_other " +
                $" FROM (" +
                " SELECT " +
                   " *," +
                    " SUM(CASE WHEN inspection_status_result = 'Y' THEN 1 ELSE 0 END) AS inspection_member_check," +
                    " AVG(CASE WHEN inspection_status_result = 'Y' THEN (unauth_rf_prob + unauth_xgb_prob) / 2 END) AS unauth_everage, " +
                    " SUM(CASE WHEN inspection_status_result = 'Y' and unauth_sub_decision = 1 THEN unauth_disguise_transfer ELSE 0 END) + SUM(CASE WHEN inspection_status_result = 'Y' and unauth_sub_decision = 1 THEN unauth_savecerti_sale ELSE 0 END) + SUM(CASE WHEN inspection_status_result = 'Y' and unauth_sub_decision = 1 THEN unauth_other ELSE 0 END) as danger_all, " +
                    " SUM(CASE WHEN inspection_status_result = 'Y' and unauth_sub_decision = 1 THEN unauth_disguise_transfer ELSE 0 END) as danger_camo, " +
                    " SUM(CASE WHEN inspection_status_result = 'Y' and unauth_sub_decision = 1 THEN unauth_savecerti_sale ELSE 0 END) as danger_sale, " +
                    " SUM(CASE WHEN inspection_status_result = 'Y' and unauth_sub_decision = 1 THEN unauth_other ELSE 0 END) as danger_other, " +
                 " CASE " +
                    " WHEN apt_sup_pos/ 1000 = 11 THEN 11" +
                    " WHEN apt_sup_pos/ 1000 = 26 THEN 26" +
                    " WHEN apt_sup_pos/ 1000 = 27 THEN 27" +
                    " WHEN apt_sup_pos/ 1000 = 28 THEN 28" +
                    " WHEN apt_sup_pos/ 1000 = 29 THEN 29" +
                    " WHEN apt_sup_pos/ 1000 = 30 THEN 30" +
                    " WHEN apt_sup_pos/ 1000 = 31 THEN 31" +
                    " WHEN apt_sup_pos/ 1000 = 36 THEN 36" +
                    " WHEN apt_sup_pos/ 1000 = 41 THEN 41" +
                    " WHEN apt_sup_pos/ 1000 = 42 THEN 42" +
                    " WHEN apt_sup_pos/ 1000 = 43 THEN 43" +
                    " WHEN apt_sup_pos/ 1000 = 44 THEN 44" +
                    " WHEN apt_sup_pos/ 1000 = 45 THEN 45" +
                    " WHEN apt_sup_pos/ 1000 = 46 THEN 46" +
                    " WHEN apt_sup_pos/ 1000 = 47 THEN 47" +
                    " WHEN apt_sup_pos/ 1000 = 48 THEN 48" +
                    " WHEN apt_sup_pos/ 1000 = 50 THEN 50" +
                      " ELSE 'Other' " +
                  "END AS LocalCode" +
                " FROM tb_riskdata" +
                $" WHERE rec_anno_date >= {FromDate} AND rec_anno_date <= {ToDate}" +
                " GROUP BY apt_name, LocalCode" +
                " )AS inner_data" +
                " WHERE ";
        query += CityQuerytoString(CityToggle);
        query += " GROUP BY inner_data.LocalCode" +
        " order by inner_data.LocalCode ASC)";

        //Debug.Log("QUERY : " + query);

        m_Adapter = new SqliteDataAdapter(query, m_DatabaseConnection);

        m_Adapter.Fill(ds);

        return ds;
    }

    /// <summary>
    /// 통계 단지별 조회
    /// </summary>
    /// <param name="ds">데이터 정보</param>
    /// <param name="FromDate">시작일자</param>
    /// <param name="ToDate">종료일자</param>
    /// <param name="City">도시 체크박스</param>
    /// <returns></returns>
    public async Task<DataSet> GetStatisticsByDanjiSql(DataSet ds, int FromDate, int ToDate, bool [] CityToggle, bool[] BustType, bool[] RfxgType, bool[] PrecentType)
    {
        string query = "SELECT " +
           " ROW_NUMBER() OVER (ORDER BY  inner_data.rec_anno_date DESC , inner_data.LocalCode asc ) AS 'NO'," +
            " inner_data.apt_name as '단지명'," +
            " inner_data.location as '지역',";
        query += BustTypeQuery(BustType, RfxgType);//위험도
        query += " SUM(CASE WHEN inner_data.unauth_sub_decision = 1 THEN inner_data.unauth_disguise_transfer ELSE 0 END) + SUM(CASE WHEN inner_data.unauth_sub_decision = 1 THEN inner_data.unauth_savecerti_sale ELSE 0 END) + SUM(CASE WHEN inner_data.unauth_sub_decision = 1 THEN inner_data.unauth_other ELSE 0 END) as '전체'," +
        " SUM(CASE WHEN inner_data.unauth_sub_decision = 1 THEN inner_data.unauth_disguise_transfer ELSE 0 END) as '위장전입'," +
        " SUM(CASE WHEN inner_data.unauth_sub_decision = 1 THEN inner_data.unauth_savecerti_sale ELSE 0 END) as '통장매매'," +
        " SUM( CASE WHEN inner_data.unauth_sub_decision = 1 THEN inner_data.unauth_other ELSE 0 END) as '기타'," +
        " inner_data.rec_anno_date as '모집공고일' " +
    " FROM (" +
    " SELECT " +
    " *, " +
    " COUNT(*) OVER (PARTITION  BY apt_name) AS countTest, " +
    $" ROW_NUMBER() OVER (PARTITION  BY apt_name ORDER BY apt_name ASC, {RiskRateStandardQuery(BustType, RfxgType)} DESC) AS rankTest, " +
    " (unauth_rf_prob +unauth_xgb_prob) / 2 AS unauth_everage," +
    " (fake_rf_prob +fake_xgb_prob) / 2 AS fake_everage," +
    " (sale_rf_prob +sale_xgb_prob) / 2 AS sale_everage," +
    " bjdong.bjdong_name AS location," +
     " CASE " +
        " WHEN apt_sup_pos/ 1000 = 11 THEN 11" +
        " WHEN apt_sup_pos/ 1000 = 26 THEN 26" +
        " WHEN apt_sup_pos/ 1000 = 27 THEN 27" +
        " WHEN apt_sup_pos/ 1000 = 28 THEN 28" +
        " WHEN apt_sup_pos/ 1000 = 29 THEN 29" +
        " WHEN apt_sup_pos/ 1000 = 30 THEN 30" +
        " WHEN apt_sup_pos/ 1000 = 31 THEN 31" +
        " WHEN apt_sup_pos/ 1000 = 36 THEN 36" +
        " WHEN apt_sup_pos/ 1000 = 41 THEN 41" +
        " WHEN apt_sup_pos/ 1000 = 42 THEN 42" +
        " WHEN apt_sup_pos/ 1000 = 43 THEN 43" +
        " WHEN apt_sup_pos/ 1000 = 44 THEN 44" +
        " WHEN apt_sup_pos/ 1000 = 45 THEN 45" +
        " WHEN apt_sup_pos/ 1000 = 46 THEN 46" +
        " WHEN apt_sup_pos/ 1000 = 47 THEN 47" +
        " WHEN apt_sup_pos/ 1000 = 48 THEN 48" +
        " WHEN apt_sup_pos/ 1000 = 50 THEN 50" +
          " ELSE 'Other' " +
      "END AS LocalCode" +
      " FROM tb_riskdata risk" +
      " JOIN tb_bjdong bjdong" +
    " ON risk.apt_sup_pos = bjdong.bjdong_code" +
    $" WHERE rec_anno_date >= {FromDate} AND rec_anno_date <= {ToDate} AND inspection_status_result = 'Y'" +
    " )AS inner_data" +
    " WHERE ";
        query += CityQuerytoString(CityToggle);
        query += " AND ";
        query += BustAndDangerPercentQuery(PrecentType, "Bottom3");
        query += " GROUP BY inner_data.apt_name";
        query += " ORDER BY  inner_data.rec_anno_date DESC , inner_data.LocalCode asc ;";

        m_Adapter = new SqliteDataAdapter(query, m_DatabaseConnection);

        m_Adapter.Fill(ds);

        return ds;
    }


    /// <summary>
    /// 주택청약 시장관리 - 단지 선정
    /// </summary>
    /// <param name="ds">데이터 정보</param>
    /// <param name="FromDate">시작일자</param>
    /// <param name="ToDate">종료일자</param>
    /// <param name="City">도시 체크박스</param>
    /// <returns></returns>
    public async Task<DataSet> GetMarketManageByDanjiSql(DataSet ds, int FromDate, int ToDate, bool[] ShearchType, string ShearchString, bool[] CityToggle, bool[] BustType, bool[] RfxgType, bool[] PrecentType)
    {
        string query = "SELECT " +
            " ROW_NUMBER() OVER (ORDER BY  inner_data.rec_anno_date DESC , inner_data.LocalCode asc ) AS 'NO'," +
            " CASE WHEN SUM(inner_data.inspection_status_count1) > 0 THEN 'N'  WHEN SUM(inner_data.inspection_status_count2) == 0 THEN '해당없음' ELSE 'Y'  END as '점검여부', " +
            " inner_data.apt_number as '주택관리번호'," +
            " inner_data.apt_type as '민영국민'," +
            " inner_data.apt_name as '단지명'," +
            " inner_data.location as '지역'," +
            " inner_data.total_suphouse_number as '공급세대수', ";
        //" inner_data.countTest as '공급세대수', ";
        query += BustTypeQuery(BustType, RfxgType);//위험도
        query += BustAndDangerPercentQuery(PrecentType, "Top");// 세대수 퍼센트
        query += " inner_data.rec_anno_date as '모집공고일'," +
            " inner_data.win_anno_date as '당첨자발표일'," +
            " inner_data.limittransfer_date as '전입제한일'," +
            " CASE WHEN inner_data.saleprice_cap_status = 0 THEN 'N' ELSE 'Y'  END as '분양가상한제'" +
            " " +
        " FROM (" +
        " SELECT " +
        " *, " +
        //" COUNT(*) OVER (PARTITION  BY apt_name) AS countTest, " +
        $" ROW_NUMBER() OVER (PARTITION  BY apt_name ORDER BY apt_name ASC, {RiskRateStandardQuery(BustType, RfxgType)} DESC) AS rankTest, " +
        " CASE WHEN inspection_status_result = 'N' THEN 1 ELSE 0 END as inspection_status_count1, " +
        " CASE WHEN inspection_status_result = 'Z' THEN 0 ELSE 1 END as inspection_status_count2, " +
        " (unauth_rf_prob + unauth_xgb_prob) / 2 AS unauth_everage," +
        " (fake_rf_prob + fake_xgb_prob) / 2 AS fake_everage," +
        " (sale_rf_prob + sale_xgb_prob) / 2 AS sale_everage," +
        " bjdong.bjdong_name AS location," +
         " CASE " +
            " WHEN apt_sup_pos/ 1000 = 11 THEN 11" +
            " WHEN apt_sup_pos/ 1000 = 26 THEN 26" +
            " WHEN apt_sup_pos/ 1000 = 27 THEN 27" +
            " WHEN apt_sup_pos/ 1000 = 28 THEN 28" +
            " WHEN apt_sup_pos/ 1000 = 29 THEN 29" +
            " WHEN apt_sup_pos/ 1000 = 30 THEN 30" +
            " WHEN apt_sup_pos/ 1000 = 31 THEN 31" +
            " WHEN apt_sup_pos/ 1000 = 36 THEN 36" +
            " WHEN apt_sup_pos/ 1000 = 41 THEN 41" +
            " WHEN apt_sup_pos/ 1000 = 42 THEN 42" +
            " WHEN apt_sup_pos/ 1000 = 43 THEN 43" +
            " WHEN apt_sup_pos/ 1000 = 44 THEN 44" +
            " WHEN apt_sup_pos/ 1000 = 45 THEN 45" +
            " WHEN apt_sup_pos/ 1000 = 46 THEN 46" +
            " WHEN apt_sup_pos/ 1000 = 47 THEN 47" +
            " WHEN apt_sup_pos/ 1000 = 48 THEN 48" +
            " WHEN apt_sup_pos/ 1000 = 50 THEN 50" +
              " ELSE 'Other' " +
          "END AS LocalCode" +
        " FROM tb_riskdata risk" +
        " JOIN tb_bjdong bjdong" +
        " ON risk.apt_sup_pos = bjdong.bjdong_code" +
        $" WHERE rec_anno_date >= {FromDate} AND rec_anno_date <= {ToDate}";
        query += " AND ";
        query += CityQuerytoString(CityToggle);
        query += SearchStringMarketManagementDanji2(ShearchType, ShearchString);
        query += $" ORDER BY apt_name ASC, {RiskRateStandardQuery(BustType, RfxgType)} DESC" +
        " )AS inner_data" +
        " WHERE ";
        query += BustAndDangerPercentQuery(PrecentType, "Bottom");
        query += " GROUP BY inner_data.apt_name";
        query += " ORDER BY  inner_data.rec_anno_date DESC , inner_data.LocalCode asc ;";

        m_Adapter = new SqliteDataAdapter(query, m_DatabaseConnection);

        //m_Adapter.Fill(ds);
        await Task.Run(() => m_Adapter.Fill(ds));

        return ds;
    }

    /// <summary>
    /// 주택청약 시장관리 - 실시 등록
    /// </summary>
    /// <param name="ds">데이터 정보</param>
    /// <param name="FromDate">시작일자</param>
    /// <param name="ToDate">종료일자</param>
    /// <param name="ShearchType">검색구분</param>
    /// <param name="ShearchString">검색어</param>
    /// <param name="CityMenu">도시 체크박스</param>
    /// <param name="RfxgType">xgrf체크박스</param>
    /// <param name="PrecentType">상위중위하위</param>
    /// <returns></returns>
    public async Task<DataSet> GetMarketManageByImplementationAndRegistrationSql(DataSet ds, int FromDate, int ToDate, bool[] ShearchType, string ShearchString, bool [] CityToggle, bool[] BustType , bool[] RfxgType, bool[] PrecentType)
    {
        string query = "SELECT " +
            $" ROW_NUMBER() OVER (ORDER BY  ({BustTypeQuery2(BustType, RfxgType)})  DESC) AS 'NO'," +
           " inner_data.*," +
           " CASE WHEN inner_data.inspection_status_result = 'Z' THEN 'Z' ELSE inner_data.inspection_status_result END as '점검여부'," +
           " CASE WHEN inner_data.unauth_sub_decision = 0 THEN '통과' ELSE unauth_sub_type  END as '부정청약결과', ";
           query += $" {BustTypeQuery2(BustType, RfxgType)} as '위험도',";//위험도
        query += " inner_data.apt_number as '주택관리번호'," +
           " inner_data.apt_type as '민영국민'," +
           " inner_data.apt_name as '단지명'," +
           " inner_data.apt_dong as '동'," +
           " inner_data.apt_hosu as '호수'," +
           " inner_data.user_name as '성명'," +
           " inner_data.user_birth as '생년월일'," +
           " inner_data.user_age as '연령'," +
           " inner_data.apt_sup_type as '공급유형'," +
           " inner_data.apt_sp_sup as '세부유형타입'," +
           " inner_data.user_relationship as '세대주관계'," +
           " inner_data.user_change_date as '변경일자'," +
           " inner_data.user_spouse as '배우자'," +
           " inner_data.user_housemember as '세대원수'," +
           " inner_data.user_hp_housemember as '분리세대'," +
           " inner_data.user_phone_dup as '폰중복'," +
           " inner_data.user_ip_dub_win as 'ip중복당첨'," +
           " inner_data.user_ip_dub_app as 'ip중복신청'," +
           " inner_data.app_residence_class as '신청거주구분'," +
           " inner_data.win_residence_class as '당첨거주구분'," +
           " CASE WHEN inner_data.ad_addressmatch_status = 0 THEN 'N' ELSE 'Y' END as '주소일치여부'," +
           " inner_data.user_admin_address as '등본주소'," +
           " inner_data.app_address as '입력주소'," +
           " inner_data.user_recom_type as '기관추천'," +
           " strftime('%Y.%m.%d %H:%M:%S', datetime(inner_data.change_date, '+9 hours')) as '업로드일시'" +
        " FROM (" +
        " SELECT " +
        " *, " +
        " COUNT(*) OVER () AS total_rows, " +
        $" ROW_NUMBER() OVER (ORDER BY {RiskRateStandardQuery(BustType, RfxgType)} DESC) AS rankTest, " +
       " (unauth_rf_prob + unauth_xgb_prob) / 2 AS unauth_everage," +
        " (fake_rf_prob + fake_xgb_prob) / 2 AS fake_everage," +
        " (sale_rf_prob + sale_xgb_prob) / 2 AS sale_everage," +
        " bjdong.bjdong_name AS location," +
         " CASE " +
            " WHEN apt_sup_pos/ 1000 = 11 THEN 11" +
            " WHEN apt_sup_pos/ 1000 = 26 THEN 26" +
            " WHEN apt_sup_pos/ 1000 = 27 THEN 27" +
            " WHEN apt_sup_pos/ 1000 = 28 THEN 28" +
            " WHEN apt_sup_pos/ 1000 = 29 THEN 29" +
            " WHEN apt_sup_pos/ 1000 = 30 THEN 30" +
            " WHEN apt_sup_pos/ 1000 = 31 THEN 31" +
            " WHEN apt_sup_pos/ 1000 = 36 THEN 36" +
            " WHEN apt_sup_pos/ 1000 = 41 THEN 41" +
            " WHEN apt_sup_pos/ 1000 = 42 THEN 42" +
            " WHEN apt_sup_pos/ 1000 = 43 THEN 43" +
            " WHEN apt_sup_pos/ 1000 = 44 THEN 44" +
            " WHEN apt_sup_pos/ 1000 = 45 THEN 45" +
            " WHEN apt_sup_pos/ 1000 = 46 THEN 46" +
            " WHEN apt_sup_pos/ 1000 = 47 THEN 47" +
            " WHEN apt_sup_pos/ 1000 = 48 THEN 48" +
            " WHEN apt_sup_pos/ 1000 = 50 THEN 50" +
              " ELSE 'Other' " +
          "END AS LocalCode" +
        " FROM tb_riskdata risk" +
        " JOIN tb_bjdong bjdong" +
        " ON risk.apt_sup_pos = bjdong.bjdong_code" +
        $" WHERE rec_anno_date >= {FromDate} AND rec_anno_date <= {ToDate}";
        query += " AND " + CityQuerytoString(CityToggle);
        query += SearchStringMarketManagementDanji2(ShearchType, ShearchString);
        query += $" ORDER BY {RiskRateStandardQuery(BustType, RfxgType)} DESC" +
        " )AS inner_data" +
        " WHERE ";
        query += BustAndDangerPercentQuery(PrecentType, "Bottom2");
        query += $" ORDER BY  {BustTypeQuery2(BustType, RfxgType)} DESC;";

        m_Adapter = new SqliteDataAdapter(query, m_DatabaseConnection);

        //Debug.Log("QUERY : " + query);

        //m_Adapter.Fill(ds);
        await Task.Run(() => m_Adapter.Fill(ds));
        return ds;
    }


    /// <summary>
    /// (대시보드 위험도/그래프 위험도) 지도 최근 1년 (평균위험도/모집공고일기준) + 대시보드 부정청약 위험도 순위
    /// </summary>
    /// <param name="CityMenu"></param>
    /// <returns></returns>
    public DataSet GetMapByDangerSql(DataSet ds, bool [] CityId, string sortType, string FromDate, string ToDate, string type)
    {
        string query = "SELECT " +
        " ROW_NUMBER() OVER(" +
        " ORDER BY (AVG(inner_data.unauth_everage) * 100) desc" +
        " ) as 'NO'," +
        " inner_data.LocalCode as '지역'," +
        " AVG(inner_data.unauth_everage) * 100 AS everage," +
        " SUM(inner_data.unauth_sub_decision) AS '적발수'," +
        " SUM(CASE WHEN inner_data.unauth_sub_decision = 1 THEN inner_data.unauth_disguise_transfer ELSE 0 END) + SUM(CASE WHEN inner_data.unauth_sub_decision = 1 THEN inner_data.unauth_savecerti_sale ELSE 0 END) + SUM(CASE WHEN inner_data.unauth_sub_decision = 1 THEN inner_data.unauth_other ELSE 0 END) as '전체'," +
        " SUM(CASE WHEN inner_data.unauth_sub_decision = 1 THEN inner_data.unauth_disguise_transfer  ELSE 0 END) as '위장전입', " +
        " SUM(CASE WHEN inner_data.unauth_sub_decision = 1 THEN inner_data.unauth_savecerti_sale  ELSE 0 END) as '통장매매', " +
        " SUM(CASE WHEN inner_data.unauth_sub_decision = 1 THEN inner_data.unauth_other ELSE 0 END) as '기타' " +
     " FROM (" +
     " SELECT " +
     " *, " +
     " (unauth_rf_prob + unauth_xgb_prob) / 2 AS unauth_everage," +
      " CASE " +
         " WHEN apt_sup_pos/ 1000 = 11 THEN 11" +
         " WHEN apt_sup_pos/ 1000 = 26 THEN 26" +
         " WHEN apt_sup_pos/ 1000 = 27 THEN 27" +
         " WHEN apt_sup_pos/ 1000 = 28 THEN 28" +
         " WHEN apt_sup_pos/ 1000 = 29 THEN 29" +
         " WHEN apt_sup_pos/ 1000 = 30 THEN 30" +
         " WHEN apt_sup_pos/ 1000 = 31 THEN 31" +
         " WHEN apt_sup_pos/ 1000 = 36 THEN 36" +
         " WHEN apt_sup_pos/ 1000 = 41 THEN 41" +
         " WHEN apt_sup_pos/ 1000 = 42 THEN 42" +
         " WHEN apt_sup_pos/ 1000 = 43 THEN 43" +
         " WHEN apt_sup_pos/ 1000 = 44 THEN 44" +
         " WHEN apt_sup_pos/ 1000 = 45 THEN 45" +
         " WHEN apt_sup_pos/ 1000 = 46 THEN 46" +
         " WHEN apt_sup_pos/ 1000 = 47 THEN 47" +
         " WHEN apt_sup_pos/ 1000 = 48 THEN 48" +
         " WHEN apt_sup_pos/ 1000 = 50 THEN 50" +
           " ELSE 'Other' " +
       "END AS LocalCode" +
     " FROM tb_riskdata" +
     " WHERE ";
        if (sortType == "StatisticMapSort")
        {
            query += $" rec_anno_date >= {FromDate} AND rec_anno_date <= {ToDate} AND inspection_status_result = 'Y'";
        }
        else if (sortType == "MainMapSort")
        {
            query += $" change_date >= '{FromDate}' AND change_date  <= '{ToDate}'";
        }

        query += " )AS inner_data" +
            " WHERE ";


        query += CityQuerytoString(CityId);
        
        if (type == "map")
        {
            query += " GROUP BY inner_data.LocalCode order by inner_data.LocalCode asc;";
        }
        else if (type == "score")
        {
            query += " GROUP BY inner_data.LocalCode order by everage desc;";
        }

        m_Adapter = new SqliteDataAdapter(query, m_DatabaseConnection);

        m_Adapter.Fill(ds);

        return ds;
    }


    /// <summary>
    /// 대시보드 부정청약 위험도 퍼센트 전국/지역 ALL
    /// </summary>
    /// <param name="CityMenu"></param>
    /// <returns></returns>
    public DataSet GetMapByDangerPercentSql(DataSet ds,  bool [] CityId, string sortType, string FromDate, string ToDate, string Percent)
    {
        string query = "SELECT " +
            " AVG(unauth_everage) * 100 AS '전체'" +
            " FROM (" +
            "   SELECT *," +
            "   	(unauth_rf_prob + unauth_xgb_prob) / 2 AS unauth_everage," +
            " CASE " +
                 " WHEN apt_sup_pos/ 1000 = 11 THEN 11" +
                 " WHEN apt_sup_pos/ 1000 = 26 THEN 26" +
                 " WHEN apt_sup_pos/ 1000 = 27 THEN 27" +
                 " WHEN apt_sup_pos/ 1000 = 28 THEN 28" +
                 " WHEN apt_sup_pos/ 1000 = 29 THEN 29" +
                 " WHEN apt_sup_pos/ 1000 = 30 THEN 30" +
                 " WHEN apt_sup_pos/ 1000 = 31 THEN 31" +
                 " WHEN apt_sup_pos/ 1000 = 36 THEN 36" +
                 " WHEN apt_sup_pos/ 1000 = 41 THEN 41" +
                 " WHEN apt_sup_pos/ 1000 = 42 THEN 42" +
                 " WHEN apt_sup_pos/ 1000 = 43 THEN 43" +
                 " WHEN apt_sup_pos/ 1000 = 44 THEN 44" +
                 " WHEN apt_sup_pos/ 1000 = 45 THEN 45" +
                 " WHEN apt_sup_pos/ 1000 = 46 THEN 46" +
                 " WHEN apt_sup_pos/ 1000 = 47 THEN 47" +
                 " WHEN apt_sup_pos/ 1000 = 48 THEN 48" +
                 " WHEN apt_sup_pos/ 1000 = 50 THEN 50" +
                   " ELSE 'Other' " +
               "END AS LocalCode" +
            "   FROM tb_riskdata";
        if (sortType == "StatisticMapSort")
        {
            query += $" WHERE rec_anno_date >= {FromDate} AND rec_anno_date <= {ToDate} AND inspection_status_result = 'Y' ";
        }
        else if (sortType == "MainMapSort")
        {
            query += $" WHERE change_date >= '{FromDate}' AND change_date  <= '{ToDate}'";
        }
        query += " AND ";
        query += CityQuerytoString(CityId);
        
        query += $" ORDER BY unauth_everage DESC   limit {Percent});";


        m_Adapter = new SqliteDataAdapter(query, m_DatabaseConnection);

        m_Adapter.Fill(ds);

        return ds;
    }


    /// <summary>
    /// (대시보드 위험도/그래프 위험도) 지도 최근 1년 (평균위험도/모집공고일기준) (지역) 
    /// </summary>
    /// <param name="CityMenu"></param>
    /// <returns></returns>
    public DataSet GetMapByDangerLocalSql(DataSet ds, bool [] cityCode, string sortType, string FromDate, string ToDate, string type)
    {
        string query = "SELECT " +
            " ROW_NUMBER() OVER(" +
            " ORDER BY (AVG(inner_data.unauth_everage) * 100) desc" +
            " ) as 'NO'," +
            " inner_data.apt_name as '단지',"+
            "     inner_data.apt_sup_pos as '지역'," +
                    " SUM(CASE WHEN inner_data.unauth_sub_decision = 1 THEN inner_data.unauth_disguise_transfer ELSE 0 END) + SUM(CASE WHEN inner_data.unauth_sub_decision = 1 THEN inner_data.unauth_savecerti_sale ELSE 0 END) + SUM(CASE WHEN inner_data.unauth_sub_decision = 1 THEN inner_data.unauth_other ELSE 0 END) as '전체'," +
                    " SUM(CASE WHEN inner_data.unauth_sub_decision = 1 THEN inner_data.unauth_disguise_transfer  ELSE 0 END) as '위장전입', " +
                    " SUM(CASE WHEN inner_data.unauth_sub_decision = 1 THEN inner_data.unauth_savecerti_sale  ELSE 0 END) as '통장매매', " +
                    " SUM(CASE WHEN inner_data.unauth_sub_decision = 1 THEN inner_data.unauth_other ELSE 0 END) as '기타', " +
            " 	AVG(inner_data.unauth_everage) * 100 AS everage" +
            " FROM (" +
            " SELECT " +
            "    *," +
            "    (unauth_rf_prob + unauth_xgb_prob) / 2 AS unauth_everage," +
                 " CASE " +
                     " WHEN apt_sup_pos/ 1000 = 11 THEN 11" +
                     " WHEN apt_sup_pos/ 1000 = 26 THEN 26" +
                     " WHEN apt_sup_pos/ 1000 = 27 THEN 27" +
                     " WHEN apt_sup_pos/ 1000 = 28 THEN 28" +
                     " WHEN apt_sup_pos/ 1000 = 29 THEN 29" +
                     " WHEN apt_sup_pos/ 1000 = 30 THEN 30" +
                     " WHEN apt_sup_pos/ 1000 = 31 THEN 31" +
                     " WHEN apt_sup_pos/ 1000 = 36 THEN 36" +
                     " WHEN apt_sup_pos/ 1000 = 41 THEN 41" +
                     " WHEN apt_sup_pos/ 1000 = 42 THEN 42" +
                     " WHEN apt_sup_pos/ 1000 = 43 THEN 43" +
                     " WHEN apt_sup_pos/ 1000 = 44 THEN 44" +
                     " WHEN apt_sup_pos/ 1000 = 45 THEN 45" +
                     " WHEN apt_sup_pos/ 1000 = 46 THEN 46" +
                     " WHEN apt_sup_pos/ 1000 = 47 THEN 47" +
                     " WHEN apt_sup_pos/ 1000 = 48 THEN 48" +
                     " WHEN apt_sup_pos/ 1000 = 50 THEN 50" +
                       " ELSE 'Other' " +
                   "END AS LocalCode" +
                 " FROM tb_riskdata risk" +
                 " JOIN tb_bjdong bjdong" +
                 " ON risk.apt_sup_pos = bjdong.bjdong_code";
        if (sortType == "StatisticMapSort")
        {
            query += $" WHERE risk.rec_anno_date >= {FromDate} AND risk.rec_anno_date <= {ToDate} AND risk.inspection_status_result = 'Y' ";
        }
        else if (sortType == "MainMapSort")
        {
            query += $" WHERE risk.change_date >= '{FromDate}' AND risk.change_date  <=  '{ToDate}'";
        }
        query += " )AS inner_data" +
     " WHERE ";
           query += CityQuerytoString(cityCode);
        if (type == "map") 
        {
            query += " GROUP BY inner_data.apt_sup_pos / 10 order by inner_data.apt_sup_pos / 10 asc;";
        }
        else if(type == "score")
        {
            query += " GROUP BY inner_data.apt_name order by everage desc limit 10;";
        }

        m_Adapter = new SqliteDataAdapter(query, m_DatabaseConnection);

        m_Adapter.Fill(ds);

        return ds;
    }


    /// <summary>
    /// 이력 관리 조회 - 접속, 변경이력, 다운로드이력, 계정 관리
    /// </summary>
    /// <param name="tableName">테이블 이름</param>
    /// <param name="ds">데이터 정보</param>
    /// <param name="FromDate">시작일자</param>
    /// <param name="ToDate">종료일자</param>
    /// <param name="ShearchType">검색구분</param>
    /// <param name="ShearchString">검색어</param>
    /// <returns></returns>
    public DataSet GetLogSql(string tableName, DataSet ds, int FromDate, int ToDate, bool[] ShearchType, string ShearchString)
    {

        string FromDateString = "";
        string ToDateString = "";

        FromDateString = DateTimeFormat(FromDate);
        ToDateString = DateTimeFormat2(ToDate);


        string query = "SELECT " +
            " * , " +
            " 0 as 'isSelect', " +
            " strftime('%Y.%m.%d %H:%M:%S', datetime(create_date, '+9 hours')) as '날짜', " +
            " ROW_NUMBER() OVER(" +
            " ORDER BY create_date ASC" +
            " ) as 'NO'" +
            $" FROM {tableName} " +
            $" WHERE create_date >= '{FromDateString}' AND create_date <= '{ToDateString}'";
        query += " ";

        if (tableName == "tb_connect_log")
        {
            query += SearchStringConnectionLog(ShearchType, ShearchString);
        }
        else if (tableName == "tb_change_log")
        {
            query += SearchStringChangeLog(ShearchType, ShearchString);
        }
        else if (tableName == "tb_down_log")
        {
            query += SearchStringDownloadLog(ShearchType, ShearchString);
        }
        else if (tableName == "tb_member")
        {
            query += SearchStringAccountManagementLog(ShearchType, ShearchString);
        }

        string orderbyType = "";

        switch (tableName)
        {
            case "tb_connect_log": orderbyType = "con_log_idx"; break;
            case "tb_change_log": orderbyType = "change_log_idx"; break;
            case "tb_down_log": orderbyType = "down_log_idx"; break;
            case "tb_member": orderbyType = "mem_idx"; break;
        }

        query += $" ORDER BY NO DESC;";


        m_Adapter = new SqliteDataAdapter(query, m_DatabaseConnection);

        m_Adapter.Fill(ds);

        return ds;
    }

    /// <summary>
    /// 계정 관리 - 계정 추가
    /// </summary>
    /// <param name="ds">데이터 추가</param>
    /// <returns></returns>
    public DataSet InsertAccountSql(DataSet ds)
    {
        string query = "SELECT MAX(mem_idx) as '마지막값' FROM tb_member;";
        m_Adapter = new SqliteDataAdapter(query, m_DatabaseConnection);
        m_Adapter.Fill(ds);

        string lastIndex = ds.Tables[0].Rows[0]["마지막값"].ToString();
        int num = int.Parse(lastIndex);

        string count = (num + 1).ToString();

        for (int i = 1; i < 3; i++)
        {
            if (count.Length <= i)
            {
                count = "0" + count;
            }
        }


        ds = new DataSet();
        string insertQuery = $"INSERT INTO tb_member(mem_auth_id, mem_status, mem_userid) VALUES('일반관리자', 1 ,'admin{count}');";


        m_Adapter = new SqliteDataAdapter(insertQuery, m_DatabaseConnection);
        m_Adapter.Fill(ds);

        return ds;
    }

    /// <summary>
    /// 로그인시 로그 추가
    /// </summary>
    /// <param name="ds"></param>
    /// <param name="userId">유저 아이디</param>
    /// <param name="name">이름</param>
    /// <param name="depart">부서</param>
    /// <param name="title">직책</param>
    /// <returns></returns>
    public DataSet InsertConnectionLogSql(DataSet ds, string userId, string name, string depart, string title)
    {

        string insertQuery = $"INSERT INTO tb_connect_log(mem_userid, mem_name, mem_depart, mem_title) VALUES('{userId}', '{name}' ,'{depart}', '{title}');";


        m_Adapter = new SqliteDataAdapter(insertQuery, m_DatabaseConnection);
        m_Adapter.Fill(ds);

        return ds;
    }

    /// <summary>
    /// 변경사항 이력 추가
    /// </summary>
    /// <param name="ds"></param>
    /// <param name="name"></param>
    /// <param name="title"></param>
    /// <param name="content"></param>
    /// <returns></returns>
    public DataSet InsertChangeLogSql(DataSet ds, string name, string title, string content)
    {

        //string insertQuery = $"INSERT INTO tb_change_log(change_name, change_menuname, change_contents) VALUES('{name}', '{title}' , strftime('%Y.%m.%d', 'now', 'localtime') || '{content}');";
        string insertQuery = $"INSERT INTO tb_change_log(change_name, change_menuname, change_contents) VALUES('{name}', '{title}' , '{content}');";

        m_Adapter = new SqliteDataAdapter(insertQuery, m_DatabaseConnection);
        m_Adapter.Fill(ds);

        return ds;
    }

    /// <summary>
    /// 다운로드 로그 추가
    /// </summary>
    /// <param name="name">이름</param>
    /// <param name="title">메뉴명</param>
    /// <param name="content">다운로드 이력</param>
    public DataSet InsertDownloadLogSql(DataSet ds, string name, string title, string content)
    {
        string insertQuery = $"INSERT INTO tb_down_log(down_name, down_menu, down_contents) VALUES('{name}', '{title}' , '{content}');";


        m_Adapter = new SqliteDataAdapter(insertQuery, m_DatabaseConnection);
        m_Adapter.Fill(ds);

        return ds;
    }
    

    /// <summary>
    /// 로그인 성공시 실패 카운트 초기화
    /// </summary>
    /// <param name="ds"></param>
    /// <param name="userId"></param>
    public void InsertConnectionLogSql(DataSet ds, string userId)
    {
        string insertQuery = $"UPDATE tb_member SET mem_failcount = 0  WHERE mem_userid = '{userId}'";


        m_Adapter = new SqliteDataAdapter(insertQuery, m_DatabaseConnection);
        m_Adapter.Fill(ds);
    }

    /// <summary>
    /// 계정 관리 - 비밀번호 초기화
    /// </summary>
    /// <param name="ds"></param>
    /// <returns></returns>
    public DataSet ResetAccountPasswordSql(DataSet ds, string userId)
    {
        string query = "UPDATE tb_member" +
            " SET mem_password = 'a2FiMTIzIUAj', mem_status = 1 " +
            $" WHERE mem_userid = '{userId}';";

        m_Adapter = new SqliteDataAdapter(query, m_DatabaseConnection);
        m_Adapter.Fill(ds);
        return ds;
    }

    /// <summary>
    /// 계정관리 - 유저 상태 정지
    /// </summary>
    /// <param name="ds"></param>
    /// <param name="userId"></param>
    /// <returns></returns>
    public DataSet AccountStopSql(DataSet ds, string userId)
    {
        string query = "UPDATE tb_member" +
            " SET mem_status = 2" +
            $" WHERE mem_userid = '{userId}';";

        m_Adapter = new SqliteDataAdapter(query, m_DatabaseConnection);
        m_Adapter.Fill(ds);
        return ds;
    }

    /// <summary>
    /// risk_date 테이블 마지막 change_date 날짜로 정보 가져오기
    /// </summary>
    /// <param name="ds"></param>
    /// <returns></returns>
    public DataSet GetLastCreateDateRiskDataSql(DataSet ds)
    {
        string query = " select datetime(max(change_date) , '-1 year') as '시작시간',  max(change_date) as '끝시간' from tb_riskdata;";

        m_Adapter = new SqliteDataAdapter(query, m_DatabaseConnection);
        m_Adapter.Fill(ds);
        return ds;
    }

    /// <summary>
    /// 위험도 퍼센트 별 사람수 조회
    /// </summary>
    /// <param name="ds"></param>
    /// <returns></returns>
    public DataSet GetPercentPersonSql(DataSet ds, string sortType , string startDate, string endDate, bool [] CityId)
    {

        string query = "SELECT" +
                    " count(*) as total, " +
                    " count(*) * 0.05 as high," +
                    " count(*) * 0.1 as middle," +
                    " count(*) * 0.2 as low" +
                    " FROM ( " +
                     " SELECT " +
                        " *, " +
                        " (unauth_rf_prob +unauth_xgb_prob) / 2 AS unauth_everage," +
                        " (fake_rf_prob +fake_xgb_prob) / 2 AS fake_everage," +
                        " (sale_rf_prob +sale_xgb_prob) / 2 AS sale_everage," +
                         " CASE " +
                            " WHEN apt_sup_pos/ 1000 = 11 THEN 11" +
                            " WHEN apt_sup_pos/ 1000 = 26 THEN 26" +
                            " WHEN apt_sup_pos/ 1000 = 27 THEN 27" +
                            " WHEN apt_sup_pos/ 1000 = 28 THEN 28" +
                            " WHEN apt_sup_pos/ 1000 = 29 THEN 29" +
                            " WHEN apt_sup_pos/ 1000 = 30 THEN 30" +
                            " WHEN apt_sup_pos/ 1000 = 31 THEN 31" +
                            " WHEN apt_sup_pos/ 1000 = 36 THEN 36" +
                            " WHEN apt_sup_pos/ 1000 = 41 THEN 41" +
                            " WHEN apt_sup_pos/ 1000 = 42 THEN 42" +
                            " WHEN apt_sup_pos/ 1000 = 43 THEN 43" +
                            " WHEN apt_sup_pos/ 1000 = 44 THEN 44" +
                            " WHEN apt_sup_pos/ 1000 = 45 THEN 45" +
                            " WHEN apt_sup_pos/ 1000 = 46 THEN 46" +
                            " WHEN apt_sup_pos/ 1000 = 47 THEN 47" +
                            " WHEN apt_sup_pos/ 1000 = 48 THEN 48" +
                            " WHEN apt_sup_pos/ 1000 = 50 THEN 50" +
                              " ELSE 'Other' " +
                          "END AS LocalCode" +
                        " FROM tb_riskdata" +
                        " ) inner_data";
                        if (sortType == "StatisticMapSort")
                        {
                            query += $" WHERE rec_anno_date >= {startDate} AND rec_anno_date <= {endDate}";
                            query += " and inspection_status_result = 'Y' ";
                        }
                        else if (sortType == "MainMapSort")
                        {
                            query += $" WHERE change_date >= '{startDate}' AND change_date  <= '{endDate}'";
                        }
                        query += $" and {CityQuerytoString(CityId)};";

        m_Adapter = new SqliteDataAdapter(query, m_DatabaseConnection);
        m_Adapter.Fill(ds);
        //await Task.Run(() => m_Adapter.Fill(ds));

        return ds;
    }

    private string BustTypeQuery2(bool[] BustType, bool[] RfxgType)
    {
        string query = " ";

        if (BustType[0]) //부정청약
        {
            if (RfxgType[0]) //부정청약평균
            {
                query += " inner_data.unauth_everage";
            }
            else if (RfxgType[1]) //부정청약xg
            {
                query += " inner_data.unauth_rf_prob";

            }
            else if (RfxgType[2]) //부정청약xg
            {
                query += " inner_data.unauth_xgb_prob";
            }
        }
        else if (BustType[1]) //위장전입
        {
            if (RfxgType[0]) //부정청약평균
            {
                query += " inner_data.fake_everage";
            }
            else if (RfxgType[1]) //부정청약xg
            {
                query += " inner_data.fake_rf_prob";

            }
            else if (RfxgType[2]) //부정청약xg
            {
                query += " inner_data.fake_xgb_prob";
            }

        }
        else if (BustType[2]) //통장매매
        {
            if (RfxgType[0]) //부정청약평균
            {
                query += " inner_data.sale_everage";
            }
            else if (RfxgType[1]) //부정청약rf
            {
                query += " inner_data.sale_rf_prob";

            }
            else if (RfxgType[2]) //부정청약xg
            {
                query += " inner_data.sale_xgb_prob";
            }
        }

        return query;
    }


    private string BustTypeQuery(bool[] BustType, bool[] RfxgType)
    {
        string query = " ";

        if (BustType[0]) //부정청약
        {
            if (RfxgType[0]) //부정청약평균
            {
                query += " AVG(inner_data.unauth_everage) * 100 as '위험도',";
            }
            else if (RfxgType[1]) //부정청약xg
            {
                query += " AVG(inner_data.unauth_rf_prob) * 100 as '위험도',";

            }
            else if (RfxgType[2]) //부정청약xg
            {
                query += " AVG(inner_data.unauth_xgb_prob) * 100 as '위험도',";
            }
        }
        else if (BustType[1]) //위장전입
        {
            if (RfxgType[0]) //부정청약평균
            {
                query += " AVG(inner_data.fake_everage) * 100 as '위험도',";
            }
            else if (RfxgType[1]) //부정청약xg
            {
                query += " AVG(inner_data.fake_rf_prob) * 100 as '위험도',";

            }
            else if (RfxgType[2]) //부정청약xg
            {
                query += " AVG(inner_data.fake_xgb_prob) * 100 as '위험도',";
            }

        }
        else if (BustType[2]) //통장매매
        {
            if (RfxgType[0]) //부정청약평균
            {
                query += " AVG(inner_data.sale_everage) * 100 as '위험도',";
            }
            else if (RfxgType[1]) //부정청약rf
            {
                query += " AVG(inner_data.sale_rf_prob) * 100 as '위험도',";

            }
            else if (RfxgType[2]) //부정청약xg
            {
                query += " AVG(inner_data.sale_xgb_prob) * 100 as '위험도',";
            }
        }

        return query;
    }


    private string DateTimeFormat(int Date)
    {
        DateTime inputDate = DateTime.ParseExact(Date.ToString(), "yyyyMMdd", null);

        return inputDate.ToString("yyyy-MM-dd HH:mm:ss");
    }

    private string DateTimeFormat2(int Date)
    {
        DateTime inputDate = DateTime.ParseExact(Date.ToString(), "yyyyMMdd", null);
        DateTime inputDate2 = inputDate.AddDays(+1);

        return inputDate2.ToString("yyyy-MM-dd HH:mm:ss");
    }

    private string SearchStringConnectionLog(bool[] ShearchType, string ShearchString)
    {
        string query = " ";

        if (ShearchType[0] && !string.IsNullOrEmpty(ShearchString)) //검색 구분일때
        {
            query += $" AND (mem_name LIKE '%{ShearchString}%' or mem_userid LIKE '%{ShearchString}%' or mem_depart LIKE '%{ShearchString}%')";
        }
        else if (ShearchType[1]) //이름
        {
            query += $" AND mem_name LIKE '%{ShearchString}%'";
        }
        else if (ShearchType[2]) //아이디
        {
            query += $" AND mem_userid LIKE '%{ShearchString}%'";
        }
        else if (ShearchType[3]) //부서
        {
            query += $" AND mem_depart LIKE '%{ShearchString}%'";
        }


        return query;
    }

    private string SearchStringChangeLog(bool[] ShearchType, string ShearchString)
    {
        string query = " ";

        if (ShearchType[0] && !string.IsNullOrEmpty(ShearchString))//검색구분
        {
            query += $" AND (change_name LIKE '%{ShearchString}%' or change_contents LIKE '%{ShearchString}%')";
        }
        else if (ShearchType[1]) //성명
        {
            query += $" AND change_name LIKE '%{ShearchString}%'";
        }
        else if (ShearchType[2]) //변경사항
        {
            query += $" AND change_contents LIKE '%{ShearchString}%'";
        }


        return query;
    }


    private string SearchStringDownloadLog(bool[] ShearchType, string ShearchString)
    {
        string query = " ";

        if (ShearchType[0] && !string.IsNullOrEmpty(ShearchString))//검색구분
        {
            query += $" AND (down_name LIKE '%{ShearchString}%' or down_contents LIKE '%{ShearchString}%')";
        }
        else if (ShearchType[1]) //성명
        {
            query += $" AND down_name LIKE '%{ShearchString}%'";
        }
        else if (ShearchType[2]) //다운로드 이력
        {
            query += $" AND down_contents LIKE '%{ShearchString}%'";
        }



        return query;
    }

    private string SearchStringAccountManagementLog(bool[] ShearchType, string ShearchString)
    {
        string query = " ";

        if (ShearchType[0] && !string.IsNullOrEmpty(ShearchString))
        {
            query += $" AND (mem_userid LIKE '%{ShearchString}%' or mem_name LIKE '%{ShearchString}%' or mem_depart LIKE '%{ShearchString}%')";
        }
        else if (ShearchType[1]) //id
        {
            query += $" AND mem_userid LIKE '%{ShearchString}%'";
        }
        else if (ShearchType[2]) //성명
        {
            query += $" AND mem_name LIKE '%{ShearchString}%'";
        }
        else if (ShearchType[3]) //부서 
        {
            query += $" AND mem_depart LIKE '%{ShearchString}%'";
        }



        return query;
    }



    private string SearchStringMarketManagementDanji2(bool[] ShearchType, string ShearchString)
    {
        string query = " ";

        if (ShearchType[0] && !string.IsNullOrEmpty(ShearchString)) //검색 구분 선택일때
        {
            query += $" AND (apt_name LIKE '%{ShearchString}%' or apt_number LIKE '%{ShearchString}%' or location LIKE '%{ShearchString}%')";
        }
        else if (ShearchType[1]) //단지명
        {
            query += $" AND apt_name LIKE '%{ShearchString}%'";
        }
        else if (ShearchType[2]) //주택 관리 번호
        {
            query += $" AND apt_number LIKE '%{ShearchString}%'";
        }
        else if (ShearchType[3]) //지역 
        {
            query += $" AND location LIKE '%{ShearchString}%'";
        }


        return query;
    }


    /// <summary>
    /// 위험도 선정 기준이 되는 쿼리 조건
    /// </summary>
    /// <param name="BustType"></param>
    /// <param name="RfxgType"></param>
    /// <returns></returns>
    private string RiskRateStandardQuery(bool[] BustType, bool[] RfxgType)
    {
        string query = " ";

        if (BustType[0]) //부정청약
        {
            if (RfxgType[0]) //부정청약평균
            {
                query += " (unauth_rf_prob + unauth_xgb_prob) / 2 ";
            }
            else if (RfxgType[1]) //부정청약rf
            {
                query += " unauth_rf_prob ";

            }
            else if (RfxgType[2]) //부정청약xg
            {
                query += " unauth_xgb_prob ";
            }
        }
        else if (BustType[1]) //위장전입
        {
            if (RfxgType[0]) //위장전입평균
            {
                query += " (fake_rf_prob + fake_xgb_prob) / 2 ";
            }
            else if (RfxgType[1]) //위장전입rf
            {
                query += " fake_rf_prob ";

            }
            else if (RfxgType[2]) //위장전입xg
            {
                query += " fake_xgb_prob ";
            }

        }
        else if (BustType[2]) //통장매매
        {
            if (RfxgType[0]) //통장매매평균
            {
                query += " (sale_rf_prob + sale_xgb_prob) / 2 ";
            }
            else if (RfxgType[1]) //통장매매rf
            {
                query += " sale_rf_prob ";

            }
            else if (RfxgType[2]) //통장매매xg
            {
                query += " sale_xgb_prob ";
            }
        }

        return query;
    }

    private string BustAndDangerPercentQuery(bool[] PrecentType, string type)
    {
        string query = " ";


        string[] percent = { "1", "0.5", "0.25", "0.1" };


        for (int i = 0; i < PrecentType.Length; i++)
        {
            if (PrecentType[i])
            {
                if (type == "Top")
                {
                    //query += $" inner_data.countTest * {percent[i]} as '세대수', ";
                    query += $" inner_data.total_suphouse_number * {percent[i]} as '세대수', ";
                }
                else if (type == "Bottom")
                {
                    query += $" inner_data.rankTest <= inner_data.total_suphouse_number * {percent[i]}";
                }
                else if(type == "Bottom2")
                {
                    query += $"  inner_data.rankTest <= inner_data.total_rows * {percent[i]}";
                }
                else if(type == "Bottom3")
                {
                    query += $" inner_data.rankTest <= inner_data.countTest * {percent[i]}";
                }
            }
        }

        return query;
    }


    private string CityQuerytoString(bool [] CityToggle)
    {

        string query = " (";

        int count = 0;
        for (int i = 1; i < CityToggle.Length; i++)
        {
            if (CityToggle[i])
            {
                if (count > 0)
                {
                    query += " or";
                }
                query += " LocalCode = " + CityId[i - 1];
                count += 1;
            }
        }

        query += " )";

        return query;
    }



    public SqliteDataReader ExecuteQuery(string sqlQuery)
    {
        m_DatabaseCommand = m_DatabaseConnection.CreateCommand();
        m_DatabaseCommand.CommandText = sqlQuery;

        m_Reader = m_DatabaseCommand.ExecuteReader();

        return m_Reader;
    }

    public SqliteDataReader ReadFullTable(string tableName)
    {
        string query = "SELECT * FROM " + tableName;
        return ExecuteQuery(query);
    }

    public SqliteDataReader ReadMapTable(string mapName)
    {
        string query = "SELECT tb_bjdong.bjdong_name, tb_riskdata.unauth_rf_prob * 1000, tb_riskdata.unauth_xgb_prob "
                        + "FROM tb_bjdong " +
                        "JOIN tb_riskdata " +
                        "ON tb_bjdong.bjdong_code = tb_riskdata.apt_sup_pos " +
                        "WHERE tb_bjdong.bjdong_name " +
                        "LIKE '%" + mapName + "%' " +
                        "GROUP BY (tb_riskdata.apt_sup_pos) ";

        Debug.Log(query);

        return ExecuteQuery(query);

    }

    public SqliteDataReader InsertIntoMember(string tableName, string[] values)
    {
        string query = "INSERT INTO " + tableName +
            "(mem_auth_id, mem_status, mem_userid, mem_name, mem_depart, mem_title, mem_work )" + "VALUES(" + values[0];

        for (int i = 1; i < values.Length; ++i)
        {
            query += ", " + values[i];
        }
        query += ");";

        return ExecuteQuery(query);
    }

    public SqliteDataReader InsertInto(string tableName, string[] values)
    {
        string query = "INSERT INTO " + tableName + " VALUES (" + values[0];
        for (int i = 1; i < values.Length; ++i)
        {
            query += ", " + values[i];
        }
        query += ")";
        return ExecuteQuery(query);
    }

    public SqliteDataReader InsertIntoSpecific(string tableName, string[] cols, string[] values)
    {
        if (cols.Length != values.Length)
        {
            throw new SqliteException("columns.Length != values.Length");
        }
        string query = "INSERT INTO " + tableName + "(" + cols[0];
        for (int i = 1; i < cols.Length; ++i)
        {
            query += ", " + cols[i];
        }
        query += ") VALUES (" + values[0];
        for (int i = 1; i < values.Length; ++i)
        {
            query += ", " + values[i];
        }
        query += ")";
        return ExecuteQuery(query);
    }

    public SqliteDataReader UpdateInto(string tableName, string[] cols, string[] colsvalues, string selectkey, string selectvalue)
    {

        string query = "UPDATE " + tableName + " SET " + cols[0] + " = " + colsvalues[0];

        for (int i = 1; i < colsvalues.Length; ++i)
        {

            query += ", " + cols[i] + " =" + colsvalues[i];
        }

        query += " WHERE " + selectkey + " = " + selectvalue + " ";

        return ExecuteQuery(query);
    }

    public SqliteDataReader DeleteContents(string tableName)
    {
        string query = "DELETE FROM " + tableName;
        return ExecuteQuery(query);
    }

    public SqliteDataReader CreateTable(string name, string[] col, string[] colType)
    {
        if (col.Length != colType.Length)
        {
            throw new SqliteException("columns.Length != colType.Length");
        }
        string query = "CREATE TABLE " + name + " (" + col[0] + " " + colType[0];
        for (int i = 1; i < col.Length; ++i)
        {
            query += ", " + col[i] + " " + colType[i];
        }
        query += ")";
        return ExecuteQuery(query);
    }

    public SqliteDataReader SelectWhere(string tableName, string[] items, string[] col, string[] operation, string[] values)
    {
        if (col.Length != operation.Length || operation.Length != values.Length)
        {
            throw new SqliteException("col.Length != operation.Length != values.Length");
        }
        string query = "SELECT " + items[0];
        for (int i = 1; i < items.Length; ++i)
        {
            query += ", " + items[i];
        }
        query += " FROM " + tableName + " WHERE " + col[0] + operation[0] + "'" + values[0] + "' ";
        for (int i = 1; i < col.Length; ++i)
        {
            query += " AND " + col[i] + operation[i] + "'" + values[0] + "' ";
        }

        return ExecuteQuery(query);
    }

    /// <summary>
    /// 내정보 변경
    /// </summary>
    /// <param name="member"></param>
    /// <returns></returns>
    public SqliteDataReader updateMemberInfo(MemberData member)
    {

        string query = "UPDATE tb_member" +
        $" SET mem_name='{member.mem_name}', mem_depart='{member.mem_depart}', mem_title='{member.mem_title}', mem_work='{member.mem_work}' " +
        $" WHERE mem_userid = '{member.mem_user_id}';";


        return ExecuteQuery(query);

    }

    // 디비버 업데이트
    public DataSet InsertRiskData(string tableName, DataTable riskTable, DataSet dataSet)
    {
        //var dataSet = new DataSet();
        //string selectQuery = $"SELECT * FROM {tableName}";
        //using (var m_Adapter = new SqliteDataAdapter(selectQuery, m_DatabaseConnection))
        //{
        //    m_Adapter.Fill(dataSet, tableName);
        //}

        string selectQuery = $"SELECT * FROM {tableName}";

        using (m_Adapter = new SqliteDataAdapter(selectQuery, m_DatabaseConnection))
        {
            var commandBuilder = new SqliteCommandBuilder(m_Adapter);

            // dataSet.Tables[0] = riskTable;
            m_Adapter.InsertCommand = commandBuilder.GetInsertCommand();

            // 데이터테이블의 행을 반복하며 InsertCommand를 실행하여 데이터베이스에 삽입
            foreach (DataRow row in riskTable.Rows)
            {
                m_Adapter.InsertCommand.Parameters.Clear();
                m_Adapter.InsertCommand.Parameters.AddRange(row.ItemArray);

                m_Adapter.InsertCommand.ExecuteNonQuery();
            }
        }

        m_Adapter.Fill(dataSet);


        return dataSet;

    }

    public SqliteDataReader DeleteRiskData(string tableName)
    {
        string query = $"DELETE FROM {tableName};";

        return ExecuteQuery(query);
    }

    public SqliteDataReader ResetRiskData(string tableName)
    {
        string query = $"UPDATE SQLITE_SEQUENCE SET seq = 0 WHERE name = '{tableName}';";


        return ExecuteQuery(query);
    }


    public void UpdateRiskData(string tableName, string[] columnNames, string[] checkNames, params object[] values)
    {
        using (var transaction = m_DatabaseConnection.BeginTransaction())
        {
            try
            {

                string query = $"SELECT COUNT(*) FROM {tableName} WHERE " +
                    $"data_no = '{values[0]}';";
            //$"apt_number = {int.Parse(values[0].ToString())} AND " +
            //$"apt_dong = {values[3].ToString()} AND " +
            //$"apt_hosu = {values[4].ToString()} AND " + 
            //$"apt_sup_type = {int.Parse(values[5].ToString())} AND " +
            //$"apt_sp_sup = {int.Parse(values[6].ToString())} AND " +
            //$"user_birth = {int.Parse(values[12].ToString())} AND " + 
            //$"user_name = '{values[13]}'";

                using (m_DatabaseCommand = m_DatabaseConnection.CreateCommand())
                {

                    m_DatabaseCommand.CommandText = query;
                    Debug.Log("QUERY : " + query);

                    int rowCount = Convert.ToInt32(m_DatabaseCommand.ExecuteScalar());

                    Debug.Log("ROW COUNT : " + rowCount);

                    if (rowCount > 0)
                    {
                        StringBuilder updateQuery = new StringBuilder($"UPDATE {tableName} ");
                        StringBuilder setQuery = new StringBuilder("SET ");
                        StringBuilder whereQuery = new StringBuilder($" WHERE " +
                                                $"data_no = '{values[0]}';");

                        //$"apt_number = {int.Parse(values[0].ToString())} AND " +
                        //$"apt_dong = {values[3].ToString()} AND " +
                        //$"apt_hosu = {values[4]} AND " +
                        //$"apt_sup_type = {int.Parse(values[5].ToString())} AND " +
                        //$"apt_sp_sup = {int.Parse(values[6].ToString())} AND " +
                        //$"user_birth = {int.Parse(values[12].ToString())} AND " +
                        //$"user_name = '{values[13]}';");

                        using (m_DatabaseCommand = m_DatabaseConnection.CreateCommand())
                        {
                            // 값이 있는 경우                    
                            for (int i = 0; i < columnNames.Length; i++)
                            {
                                string columnName = columnNames[i];
                                string valueString = values[i].ToString();

                                setQuery.Append($"{columnName} = @Value{i}");

                                if (i < columnNames.Length - 1)
                                {
                                    //insertQuery.Append(", ");
                                    setQuery.Append(", ");
                                }

                                string parameterName = $"@Value{i}";

                                // 숫자 또는 double로 형변환
                                int intValue;
                                bool isInt = int.TryParse(valueString, out intValue);

                                if (isInt)
                                {
                                    Debug.Log("문자열은 int입니다." + intValue);
                                    m_DatabaseCommand.Parameters.AddWithValue(parameterName, intValue);
                                }
                                else
                                {
                                    double doubleValue;
                                    bool isDouble = double.TryParse(valueString, out doubleValue);

                                    if (isDouble)
                                    {
                                        Debug.Log("문자열은 double입니다." + doubleValue);
                                        m_DatabaseCommand.Parameters.AddWithValue(parameterName, doubleValue);

                                    }
                                    else
                                    {
                                        Debug.Log("문자열입니다." + valueString);
                                        m_DatabaseCommand.Parameters.AddWithValue(parameterName, valueString);
                                    }
                                }
                            }

                            updateQuery.Append(setQuery.ToString());
                            updateQuery.Append(" ");
                            updateQuery.Append(whereQuery.ToString());


                            Debug.Log("QUERY2 : " + updateQuery.ToString());

                            m_DatabaseCommand.CommandText = updateQuery.ToString();



                            m_DatabaseCommand.ExecuteReader();
                        }
                    }
                    else
                    {
                        StringBuilder insertQuery = new StringBuilder($"INSERT INTO {tableName} (");
                        StringBuilder valuesQuery = new StringBuilder("VALUES (");

                        using (m_DatabaseCommand = m_DatabaseConnection.CreateCommand())
                        {
                            for (int i = 0; i < columnNames.Length; i++)
                            {
                                string columnName = columnNames[i];
                                string valueString = values[i].ToString();

                                insertQuery.Append(columnName);
                                valuesQuery.Append($"@Value{i}");

                                if (i < columnNames.Length - 1)
                                {
                                    insertQuery.Append(", ");
                                    valuesQuery.Append(", ");
                                }

                                string parameterName = $"@Value{i}";

                                // 숫자 또는 double로 형변환
                                int intValue;
                                bool isInt = int.TryParse(valueString, out intValue);

                                if (isInt)
                                {
                                    Debug.Log("문자열은 int입니다." + intValue);
                                    m_DatabaseCommand.Parameters.AddWithValue(parameterName, intValue);
                                }
                                else
                                {
                                    double doubleValue;
                                    bool isDouble = double.TryParse(valueString, out doubleValue);

                                    if (isDouble)
                                    {
                                        Debug.Log("문자열은 double입니다."+ doubleValue);
                                        m_DatabaseCommand.Parameters.AddWithValue(parameterName, doubleValue);

                                    }
                                    else
                                    {
                                        Debug.Log("문자열입니다."+ valueString);
                                        m_DatabaseCommand.Parameters.AddWithValue(parameterName, valueString);
                                    }
                                }
                            }

                            insertQuery.Append(") ");
                            valuesQuery.Append(")");

                            string query2 = insertQuery.ToString() + valuesQuery.ToString();
                            Debug.Log("query2 : " + query2);

                            m_DatabaseCommand.CommandText = query2;

                            m_DatabaseCommand.ExecuteReader();
                        }
                    }
                }

            transaction.Commit();
             //Debug.Log("Transaction committed successfully.");
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            Debug.LogError("Transaction failed and rolled back: " + ex.Message);
        }
    }
}

    // 디비버 업데이트

    public SqliteDataReader DeleteRiskData(string tableName, string columnName, string value)
    {
        string query = $"DELETE FROM {tableName} "+
            $" WHERE {columnName} = '{value}';";

        return ExecuteQuery(query);
    }

    public SqliteDataReader InsertRiskData(string tableName, string[] columnNames, params object[] values)
    {
        StringBuilder insertQuery = new StringBuilder($"INSERT INTO {tableName} (");
        StringBuilder valuesQuery = new StringBuilder("VALUES (");

        using (m_DatabaseCommand = m_DatabaseConnection.CreateCommand())
        {
            for (int i = 0; i < columnNames.Length; i++)
            {
                string columnName = columnNames[i];
                string valueString = values[i].ToString();

                insertQuery.Append(columnName);
                valuesQuery.Append($"@Value{i}");

                if (i < columnNames.Length - 1)
                {
                    insertQuery.Append(", ");
                    valuesQuery.Append(", ");
                }

                string parameterName = $"@Value{i}";

                // 숫자 또는 double로 형변환
                double numericValue;

                bool isNumeric = double.TryParse(valueString, out numericValue);
                if (isNumeric)
                    m_DatabaseCommand.Parameters.AddWithValue(parameterName, numericValue);
                else
                    m_DatabaseCommand.Parameters.AddWithValue(parameterName, valueString);
            }


            insertQuery.Append(") ");
            valuesQuery.Append(")");

            string query = insertQuery.ToString() + valuesQuery.ToString();

            m_DatabaseCommand.CommandText = query;

            return m_DatabaseCommand.ExecuteReader();
        }
    }

    public SqliteDataReader InsertRiskDataAsync(string tableName, string[] columnNames, params object[] values)
    {
        StringBuilder insertQuery = new StringBuilder($"INSERT INTO {tableName} (");
        StringBuilder valuesQuery = new StringBuilder("VALUES (");

        using (m_DatabaseCommand = m_DatabaseConnection.CreateCommand())
        {
            for (int i = 0; i < columnNames.Length; i++)
            {
                string columnName = columnNames[i];
                string valueString = values[i].ToString();

                insertQuery.Append(columnName);
                valuesQuery.Append($"@Value{i}");

                if (i < columnNames.Length - 1)
                {
                    insertQuery.Append(", ");
                    valuesQuery.Append(", ");
                }

                string parameterName = $"@Value{i}";

                // 숫자 또는 double로 형변환
                int intValue;
                bool isInt = int.TryParse(valueString, out intValue);

                if (isInt)
                {
                    //Debug.Log("문자열은 int입니다.");
                    m_DatabaseCommand.Parameters.AddWithValue(parameterName, intValue);
                }
                else
                {
                    double doubleValue;
                    bool isDouble = double.TryParse(valueString, out doubleValue);

                    if (isDouble)
                    {
                        //Debug.Log("문자열은 double입니다.");
                        m_DatabaseCommand.Parameters.AddWithValue(parameterName, doubleValue);

                    }
                    else
                    {
                        //Debug.Log("문자열입니다.");
                        m_DatabaseCommand.Parameters.AddWithValue(parameterName, valueString);
                    }
                }


                //double numericValue;

                //bool isNumeric = double.TryParse(valueString, out numericValue);
                //if (isNumeric)
                //    m_DatabaseCommand.Parameters.AddWithValue(parameterName, numericValue);
                //else
                //    m_DatabaseCommand.Parameters.AddWithValue(parameterName, valueString);
            }

            insertQuery.Append(") ");
            valuesQuery.Append(")");

            string query = insertQuery.ToString() + valuesQuery.ToString();

            m_DatabaseCommand.CommandText = query;

            return m_DatabaseCommand.ExecuteReader();
        }
    }

    public SqliteDataReader InsertRiskDataAsync2(string tableName, string[] columnNames,  params object[] values)
    {
        StringBuilder insertQuery = new StringBuilder($"INSERT INTO {tableName} (");
        StringBuilder valuesQuery = new StringBuilder("VALUES (");

        using (m_DatabaseCommand = m_DatabaseConnection.CreateCommand())
        {
            // 트랜잭션 시작
            using (var transaction = m_DatabaseConnection.BeginTransaction())
            {
                try
                {
                    for (int i = 0; i < columnNames.Length; i++)
                    {
                        string columnName = columnNames[i];
                        string valueString = values[i].ToString();

                        insertQuery.Append(columnName);
                        valuesQuery.Append($"@Value{i}");

                        if (i < columnNames.Length - 1)
                        {
                            insertQuery.Append(", ");
                            valuesQuery.Append(", ");
                        }

                        string parameterName = $"@Value{i}";

                        int intValue;
                        bool isInt = int.TryParse(valueString, out intValue);

                        if (isInt)
                        {
                            m_DatabaseCommand.Parameters.AddWithValue(parameterName, intValue);
                        }
                        else
                        {
                            double doubleValue;
                            bool isDouble = double.TryParse(valueString, out doubleValue);

                            if (isDouble)
                            {
                                m_DatabaseCommand.Parameters.AddWithValue(parameterName, doubleValue);
                            }
                            else
                            {
                                m_DatabaseCommand.Parameters.AddWithValue(parameterName, valueString);
                            }
                        }
                    }

                    insertQuery.Append(") ");
                    valuesQuery.Append(")");

                    string query = insertQuery.ToString() + valuesQuery.ToString();

                    m_DatabaseCommand.CommandText = query;

                    // 삽입 쿼리 실행
                    m_DatabaseCommand.ExecuteNonQuery();

                    // 트랜잭션 커밋
                    transaction.Commit();

                    // 결과 반환 (생략)
                }
                catch (System.Exception ex)
                {
                    // 트랜잭션 롤백
                    transaction.Rollback();

                    // 예외 처리 (생략)
                }
            }
        }

        return null; // 원하는 반환 값을 지정해 주세요.
    }

    /// <summary>
    /// 위험도 산출 저장
    /// </summary>
    /// <param name="tableName"></param>
    /// <param name="columnNames"></param>
    /// <param name="values"></param>
    /// <returns></returns>
    public async Task InsertReplaceAsync(string tableName, DataTable dataTable, string[] cols, string[] columnNames)
    {
        using (SqliteTransaction transaction = m_DatabaseConnection.BeginTransaction())
        {
            using (m_DatabaseCommand = m_DatabaseConnection.CreateCommand())
            {
                // 생성된 쿼리 문자열을 저장할 StringBuilder
                System.Text.StringBuilder queryBuilder = new System.Text.StringBuilder();

                // INSERT OR REPLACE INTO 절
                queryBuilder.Append($"INSERT OR REPLACE INTO {tableName} (");

                // 컬럼 이름들
                for (int i = 0; i < cols.Length; i++)
                {
                    queryBuilder.Append(cols[i]);
                    if (i < cols.Length - 1)
                    {
                        queryBuilder.Append(", ");
                    }
                }

                queryBuilder.Append(") VALUES ");

                // VALUES 절에 모든 행 추가
                for (int rowIndex = 0; rowIndex < dataTable.Rows.Count; rowIndex++)
                {
                    DataRow row = dataTable.Rows[rowIndex];

                    queryBuilder.Append("(");

                    // 파라미터들
                    for (int i = 0; i < columnNames.Length; i++)
                    {
                        string parameterName = $"@param{rowIndex}_{i}";
                        queryBuilder.Append(parameterName);
                        m_DatabaseCommand.Parameters.AddWithValue(parameterName, row[columnNames[i]]);

                        if (i < columnNames.Length - 1)
                        {
                            queryBuilder.Append(", ");
                        }
                    }

                    queryBuilder.Append(")");

                    if (rowIndex < dataTable.Rows.Count - 1)
                    {
                        queryBuilder.Append(", ");
                    }
                }

                //Debug.Log("QUERY " + queryBuilder.ToString());

                // 쿼리 실행
                m_DatabaseCommand.CommandText = queryBuilder.ToString();

                await m_DatabaseCommand.ExecuteNonQueryAsync();
            }
            
            await transaction.CommitAsync();

        }
    }


}
