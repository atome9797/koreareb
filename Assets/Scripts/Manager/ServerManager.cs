using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mono.Data.Sqlite;
using System.IO;
using System.Data;
using UnityEngine.UI;
using TMPro;
using ChartUtil;
using System.Threading.Tasks;

public class ServerManager : MonoBehaviour
{
    public string m_DatabaseFileName = "koreareb.db";
    public string m_TableName = "TestTable1";
    public DatabaseAccess m_DatabaseAccess;

    string filePath;

    public DataSet ds = new DataSet();
    public DataSet ds_member = new DataSet();
    public DataSet ds_test;
    public DataSet ds_map = new DataSet();
    public DataSet ds_satistic;
    public DataSet ds_log;
    public DataSet ds_last_date;
    public DataSet ds_percent_rate;



    void Start()
    {

        filePath = Path.Combine(Application.streamingAssetsPath, m_DatabaseFileName);
        //filePath = Path.Combine(@"C:\korea_rebs\" + m_DatabaseFileName);

        Debug.Log(filePath);
        m_DatabaseAccess = new DatabaseAccess("data source = " + filePath);

        //m_DatabaseAccess.CreateTable("TestTable1",
        //    new string[] { "name", "age" },
        //    new string[] { "text", "int" });

        //m_DatabaseAccess.InsertInto("TestTable1", new string[] { "'Coderzedro'", "'47'" });
        //m_DatabaseAccess.InsertInto("TestTable1", new string[] { "'JD'", "'17'" });
        //m_DatabaseAccess.InsertInto("TestTable1", new string[] { "'Tiger'", "'47'" });

        /*
        var rd = m_DatabaseAccess.ReadFullTable("tb_bjdong");
        
        while (rd.HasRows)
        {
            DataTable dt = new DataTable();
            int len = rd.FieldCount;
            for (int i = 0; i < len; i++)
            {
                dt.Columns.Add(rd.GetName(i), rd.GetFieldType(i));
            }
            dt.BeginLoadData();

            var values = new object[len];
            while (rd.Read())
            {
                for (int i = 0; i < len; i++)
                {
                    values[i] = rd[i];
                }

                dt.Rows.Add(values);
            }
            dt.EndLoadData();
            ds.Tables.Add(dt);
            rd.NextResult();
        }
                rd.Close();
        */

        m_DatabaseAccess.ReadFullTable("tb_bjdong", ds_map);


        //m_DatabaseAccess.InsertIntoMember("tb_member", new string[] { "'일반관리자'", "'0'", "'admin002'", "'조현재'", "'부정청약관리팀'", "'관리자'", "'주요업무'" });





        /*
        m_DatabaseAccess.ReadFullTable("tb_riskdata", ds);

        if (ds.Tables.Count > 0)
        {
            foreach (DataRow r in ds.Tables[0].Rows)
            {
                //Debug.Log(r["bjdong_code"]);
                //Debug.Log(r["bjdong_name"]);
                Debug.Log(r["apt_name"]);

            }
        }*/
        // GetMemberData();

        /*
        var rd = m_DatabaseAccess.ReadMapTable("서울특별시");

        Debug.Log(rd.FieldCount);


        
        while (rd.HasRows)
        {
            DataTable dt = new DataTable();
            int len = rd.FieldCount;
            for (int i = 0; i < len; i++)
            {
                dt.Columns.Add(rd.GetName(i), rd.GetFieldType(i));
            }
            dt.BeginLoadData();

            var values = new object[len];
            while (rd.Read())
            {
                for (int i = 0; i < len; i++)
                {
                    values[i] = rd[i];
                }

                dt.Rows.Add(values);
            }
            dt.EndLoadData();
            ds.Tables.Add(dt);
            rd.NextResult();
        }
        rd.Close();
        
        
        if (ds.Tables.Count > 0)
        {
            foreach (DataRow r in ds.Tables[0].Rows)
            {
                //Debug.Log(r["bjdong_code"]);
                //Debug.Log(r["bjdong_name"]);
                Debug.Log(r["tb_riskdata.unauth_rf_prob * 1000"]);
                Debug.Log(r["unauth_xgb_prob"]);

            }
        }*/


        m_DatabaseAccess.CloseSqlConnection();
    }


    public void DeleteRiskTable(string tableName)
    {
        m_DatabaseAccess = new DatabaseAccess("data source = " + filePath);
        m_DatabaseAccess.DeleteRiskData(tableName);

        m_DatabaseAccess.CloseSqlConnection();

    }

    public void UpdateRiskData(string tableName, string[] cols, string[] checks, params object[] value)
    {
        m_DatabaseAccess = new DatabaseAccess("data source = " + filePath);
        //m_DatabaseAccess.InsertRiskData(tableName, cols, value);

        m_DatabaseAccess.UpdateRiskData(tableName, cols, checks, value);
        m_DatabaseAccess.CloseSqlConnection();
    }

    public async void UpdateRiskDataAsync(string tableName, DataTable dt, string[] cols, string[] columnames )
    {
        m_DatabaseAccess = new DatabaseAccess("data source = " + filePath);
        //m_DatabaseAccess.InsertRiskData(tableName, cols, value);

        await m_DatabaseAccess.InsertReplaceAsync(tableName, dt, cols, columnames);
        m_DatabaseAccess.CloseSqlConnection();
    }

    /// <summary>
    /// 실시등록 위험도 산출 데이터 저장
    /// </summary>
    public void InsertRiskData(string tableName, string[] cols, params object[] value)
    {
        m_DatabaseAccess = new DatabaseAccess("data source = " + filePath);
        //m_DatabaseAccess.InsertRiskData(tableName, cols, value);

        m_DatabaseAccess.InsertRiskDataAsync(tableName, cols, value);
        m_DatabaseAccess.CloseSqlConnection();
    }

    public void DeleteRiskData(string tableName, string cols, string value)
    {
        m_DatabaseAccess = new DatabaseAccess("data source = " + filePath);
        //m_DatabaseAccess.InsertRiskData(tableName, cols, value);

        m_DatabaseAccess.DeleteRiskData(tableName, cols, value);
        m_DatabaseAccess.CloseSqlConnection();
    }

    /// <summary>
    /// 가입자 정보 불러오기
    /// </summary>
    /// <returns></returns>
    public DataSet GetMemberData()
    {
        m_DatabaseAccess.ReadFullTable("tb_member", ds_member);
        m_DatabaseAccess.CloseSqlConnection();

        return ds_member;
    }

    /// <summary>
    /// 유저의 아이디로 검색
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    public DataTable GetMemberDataLogin(string userId)
    {
        ds_member = new DataSet();
        filePath = Path.Combine(Application.streamingAssetsPath, m_DatabaseFileName);
        m_DatabaseAccess = new DatabaseAccess("data source = " + filePath);
        m_DatabaseAccess.ReadUserTable("tb_member", userId, ds_member);
        m_DatabaseAccess.CloseSqlConnection();
        return ds_member.Tables[0];
    }

    /// <summary>
    /// 유저의 비밀번호 변경
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    public void UpdateUserPassword(string userId, string userPassword)
    {
        ds_member = new DataSet();
        filePath = Path.Combine(Application.streamingAssetsPath, m_DatabaseFileName);
        m_DatabaseAccess = new DatabaseAccess("data source = " + filePath);
        m_DatabaseAccess.UpdateUserPassword("tb_member", userId, userPassword, ds_member);
        m_DatabaseAccess.CloseSqlConnection();
    }


    /// <summary>
    /// 유저의 데이터 업데이트
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="failCount"></param>
    public void GetMemberDataUpdate(string userId, int failCount)
    {
        ds_member = new DataSet();
        filePath = Path.Combine(Application.streamingAssetsPath, m_DatabaseFileName);
        m_DatabaseAccess = new DatabaseAccess("data source = " + filePath);
        m_DatabaseAccess.UpdateUserFailCount("tb_member", userId, failCount, ds_member);
        m_DatabaseAccess.CloseSqlConnection();
    }

    /// <summary>
    /// 유저의 데이터 업데이트
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="failCount"></param>
    public void GetMemberStatusDataUpdate(string userId)
    {
        ds_member = new DataSet();
        filePath = Path.Combine(Application.streamingAssetsPath, m_DatabaseFileName);
        m_DatabaseAccess = new DatabaseAccess("data source = " + filePath);
        m_DatabaseAccess.UpdateUserStatusLock("tb_member", userId, ds_member);
        m_DatabaseAccess.CloseSqlConnection();
    }





    public string LocalName(string mapcode)
    {

        ds_map.Tables[0].DefaultView.RowFilter = "bjdong_code = '" + mapcode + "'";

        string mapname = ds_map.Tables[0].DefaultView[0]["bjdong_name"].ToString();

        return mapname;
    }

    /// <summary>
    /// 통계 지역별 항목 조회
    /// </summary>
    /// <param name="CityMenu">지역 검색</param>
    /// <returns>지역별 항목</returns>
    public DataTable GetStatisticsByRegion(int FromDate, int ToDate, bool[] CityToggle)
    {
        ds_satistic = new DataSet();
        filePath = Path.Combine(Application.streamingAssetsPath, m_DatabaseFileName);
        m_DatabaseAccess = new DatabaseAccess("data source = " + filePath);
        //ds_satistic 에서 콜백으로 결과값을 받음
        m_DatabaseAccess.GetStatisticsByRegionSql(ds_satistic, FromDate, ToDate, CityToggle);
        m_DatabaseAccess.CloseSqlConnection();

        return ds_satistic.Tables[0];
    }

    /// <summary>
    /// 통계 단지별 항목조회
    /// </summary>
    /// <param name="CityMenu">지역검색</param>
    /// <returns>단지별 항목</returns>
    public DataTable GetStatisticsByDanji(int FromDate, int ToDate, bool[] CityToggle, bool[] BustType, bool[] RfxgType, bool[] PrecentType)
    {
        ds_satistic = new DataSet();
        filePath = Path.Combine(Application.streamingAssetsPath, m_DatabaseFileName);
        m_DatabaseAccess = new DatabaseAccess("data source = " + filePath);
        m_DatabaseAccess.GetStatisticsByDanjiSql(ds_satistic, FromDate, ToDate, CityToggle, BustType, RfxgType, PrecentType);
        m_DatabaseAccess.CloseSqlConnection();

        return ds_satistic.Tables[0];
    }

    /// <summary>
    /// 주택청약 시장관리 - 단지선정
    /// </summary>
    /// <param name="FromDate">시작일자</param>
    /// <param name="ToDate">종료일자</param>
    /// <param name="ShearchType">검색구분</param>
    /// <param name="ShearchString">검색어</param>
    /// <param name="CityMenu">도시체크</param>
    /// <param name="BustType">적발내용체크박스</param>
    /// <param name="RfxgType">rfxg 체크박스</param>
    /// <param name="PrecentType">상위,중위,하위</param>
    /// <returns></returns>
    public async Task<DataTable> GetMarketManageByDanji(int FromDate, int ToDate, bool[] ShearchType, string ShearchString, bool[] CityToggle, bool[] BustType, bool[] RfxgType, bool[] PrecentType)
    {
        ds_satistic = new DataSet();
        filePath = Path.Combine(Application.streamingAssetsPath, m_DatabaseFileName);
        m_DatabaseAccess = new DatabaseAccess("data source = " + filePath);
        await m_DatabaseAccess.GetMarketManageByDanjiSql(ds_satistic, FromDate, ToDate, ShearchType, ShearchString, CityToggle, BustType, RfxgType, PrecentType);
        m_DatabaseAccess.CloseSqlConnection();

        return ds_satistic.Tables[0];
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
    public async Task<DataTable> GetMarketManageByImplementationAndRegistration(int FromDate, int ToDate, bool[] ShearchType, string ShearchString, bool[] CityToggle, bool[] BustType , bool[] RfxgType, bool[] PrecentType)
    {
        ds_satistic = new DataSet();
        filePath = Path.Combine(Application.streamingAssetsPath, m_DatabaseFileName);
        m_DatabaseAccess = new DatabaseAccess("data source = " + filePath);
        await m_DatabaseAccess.GetMarketManageByImplementationAndRegistrationSql(ds_satistic, FromDate, ToDate, ShearchType, ShearchString, CityToggle, BustType, RfxgType, PrecentType);
        m_DatabaseAccess.CloseSqlConnection();

        return ds_satistic.Tables[0];
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
    public DataTable GetLog(string tableName, int FromDate, int ToDate, bool[] ShearchType, string ShearchString)
    {
        ds_satistic = new DataSet();
        filePath = Path.Combine(Application.streamingAssetsPath, m_DatabaseFileName);
        m_DatabaseAccess = new DatabaseAccess("data source = " + filePath);
        m_DatabaseAccess.GetLogSql(tableName, ds_satistic, FromDate, ToDate, ShearchType, ShearchString);
        m_DatabaseAccess.CloseSqlConnection();

        return ds_satistic.Tables[0];
    }

    /// <summary>
    /// 로그인시 로그 저장
    /// </summary>
    /// <param name="userId">유저 아이디</param>
    /// <param name="name">이름</param>
    /// <param name="depart">직책</param>
    /// <param name="title">직급</param>
    public void InsertConnectionLog(string userId, string name, string depart, string title)
    {
        ds_log = new DataSet();
        filePath = Path.Combine(Application.streamingAssetsPath, m_DatabaseFileName);
        m_DatabaseAccess = new DatabaseAccess("data source = " + filePath);
        m_DatabaseAccess.InsertConnectionLogSql(ds_log, userId, name, depart, title);
        m_DatabaseAccess.CloseSqlConnection();

    }


    /// <summary>
    /// 변경 로그 추가
    /// </summary>
    /// <param name="name">이름</param>
    /// <param name="title">메뉴명</param>
    /// <param name="content">변경사항</param>
    public void InsertChangeLog(string name, string title, string content)
    {
        ds_log = new DataSet();
        filePath = Path.Combine(Application.streamingAssetsPath, m_DatabaseFileName);
        m_DatabaseAccess = new DatabaseAccess("data source = " + filePath);
        m_DatabaseAccess.InsertChangeLogSql(ds_log,  name, title, content);
        m_DatabaseAccess.CloseSqlConnection();

    }

    /// <summary>
    /// 다운로드 로그 추가
    /// </summary>
    /// <param name="name">이름</param>
    /// <param name="title">메뉴명</param>
    /// <param name="content">다운로드 이력</param>
    public void InsertDownloadLog(string name, string title, string content)
    {
        ds_log = new DataSet();
        filePath = Path.Combine(Application.streamingAssetsPath, m_DatabaseFileName);
        m_DatabaseAccess = new DatabaseAccess("data source = " + filePath);
        m_DatabaseAccess.InsertDownloadLogSql(ds_log, name, title, content);
        m_DatabaseAccess.CloseSqlConnection();

    }




    /// <summary>
    /// 유저 실패 카운트 초기화
    /// </summary>
    /// <param name="userId"></param>
    public void UpdateUserFailCount(string userId)
    {
        ds_log = new DataSet();
        filePath = Path.Combine(Application.streamingAssetsPath, m_DatabaseFileName);
        m_DatabaseAccess = new DatabaseAccess("data source = " + filePath);
        m_DatabaseAccess.InsertConnectionLogSql(ds_log, userId);
        m_DatabaseAccess.CloseSqlConnection();
    }

    /// <summary>
    /// 계정 관리 - 계정 추가
    /// </summary>
    /// <param name="ds">데이터 추가</param>
    /// <returns></returns>
    public DataTable InsertAccount()
    {
        ds_satistic = new DataSet();
        filePath = Path.Combine(Application.streamingAssetsPath, m_DatabaseFileName);
        m_DatabaseAccess = new DatabaseAccess("data source = " + filePath);
        m_DatabaseAccess.InsertAccountSql(ds_satistic);
        m_DatabaseAccess.CloseSqlConnection();

        return ds_satistic.Tables[0];
    }

    /// <summary>
    /// 계정 관리 - 비밀번호 초기화
    /// </summary>
    /// <param name="ds"></param>
    /// <returns></returns>
    public void ResetAccountPassword(string userId)
    {
        ds_satistic = new DataSet();
        filePath = Path.Combine(Application.streamingAssetsPath, m_DatabaseFileName);
        m_DatabaseAccess = new DatabaseAccess("data source = " + filePath);
        m_DatabaseAccess.ResetAccountPasswordSql(ds_satistic, userId);
        m_DatabaseAccess.CloseSqlConnection();
    }

    /// <summary>
    /// 계정관리 - 유저 상태 정지
    /// </summary>
    /// <param name="ds"></param>
    /// <param name="userId"></param>
    /// <returns></returns>
    public void AccountStop(string userId)
    {
        ds_satistic = new DataSet();
        filePath = Path.Combine(Application.streamingAssetsPath, m_DatabaseFileName);
        m_DatabaseAccess = new DatabaseAccess("data source = " + filePath);
        m_DatabaseAccess.AccountStopSql(ds_satistic, userId);
        m_DatabaseAccess.CloseSqlConnection();
    }


    /// <summary>
    /// risk_data 테이블 마지막 create_date 가져오기
    /// </summary>
    /// <returns></returns>
    public DataTable GetLastCreateDateRiskData()
    {
        ds_last_date = new DataSet();
        filePath = Path.Combine(Application.streamingAssetsPath, m_DatabaseFileName);
        m_DatabaseAccess = new DatabaseAccess("data source = " + filePath);
        m_DatabaseAccess.GetLastCreateDateRiskDataSql(ds_last_date);
        m_DatabaseAccess.CloseSqlConnection();

        return ds_last_date.Tables[0];
    }

    /// <summary>
    /// 위험도 퍼센트 조회하기
    /// </summary>
    /// <param name="startDate"></param>
    /// <param name="endDate"></param>
    /// <returns></returns>
    public DataTable GetPercentPerson(string startDate, string endDate, bool [] CityId, string StatisticMapSort)
    {
        ds_percent_rate = new DataSet();
        filePath = Path.Combine(Application.streamingAssetsPath, m_DatabaseFileName);
        m_DatabaseAccess = new DatabaseAccess("data source = " + filePath);
        m_DatabaseAccess.GetPercentPersonSql(ds_percent_rate, StatisticMapSort, startDate, endDate, CityId);
        m_DatabaseAccess.CloseSqlConnection();

        return ds_percent_rate.Tables[0];
    }

    /// <summary>
    /// 대시보드 위험도 지도  (최근 1년 평균) + 대시보드 부정청약 위험도 순위 (전국)
    /// </summary>
    /// <param name="CityMenu"></param>
    /// <returns></returns>
    public DataTable GetDashBordMapByDanger(bool [] CityId, string startDate, string endDate, string type)
    {
       
        ds_satistic = new DataSet();
        filePath = Path.Combine(Application.streamingAssetsPath, m_DatabaseFileName);
        m_DatabaseAccess = new DatabaseAccess("data source = " + filePath);
        m_DatabaseAccess.GetMapByDangerSql(ds_satistic, CityId, "MainMapSort", startDate, endDate, type);
        m_DatabaseAccess.CloseSqlConnection();

        return ds_satistic.Tables[0];
    }

    /// <summary>
    /// 통계 위험도 지도  (모집공고일 기준) + 통계 부정청약 위험도 순위 (전국) + 부정청약 적발수 그래프도 가져옴
    /// </summary>
    /// <param name="CityMenu"></param>
    /// <returns></returns>
    public DataTable GetStatisticMapByDanger(bool [] CityId, string FromDate, string ToDate, string type)
    {
        ds_satistic = new DataSet();
        filePath = Path.Combine(Application.streamingAssetsPath, m_DatabaseFileName);
        m_DatabaseAccess = new DatabaseAccess("data source = " + filePath);
        m_DatabaseAccess.GetMapByDangerSql(ds_satistic, CityId, "StatisticMapSort", FromDate, ToDate, type);
        m_DatabaseAccess.CloseSqlConnection();

        return ds_satistic.Tables[0];
    }


    /// <summary>
    /// 대시보드 위험도 지도  (최근 1년 평균) (지역)
    /// </summary>
    /// <param name="CityMenu"></param>
    /// <returns></returns>
    public DataTable GetDashBordMapByDangerLocal(bool [] CityId, string startDate, string endDate, string type)
    {
        ds_satistic = new DataSet();
        filePath = Path.Combine(Application.streamingAssetsPath, m_DatabaseFileName);
        m_DatabaseAccess = new DatabaseAccess("data source = " + filePath);
        m_DatabaseAccess.GetMapByDangerLocalSql(ds_satistic, CityId, "MainMapSort", startDate, endDate, type);
        m_DatabaseAccess.CloseSqlConnection();

        return ds_satistic.Tables[0];
    }

    /// <summary>
    /// 통계 위험도 지도  (모집공고일 기준) (지역)
    /// </summary>
    /// <param name="CityMenu"></param>
    /// <returns></returns>
    public DataTable GetStatisticMapByDangerLocal(bool [] CityId, string FromDate, string ToDate , string type)
    {
        ds_satistic = new DataSet();
        filePath = Path.Combine(Application.streamingAssetsPath, m_DatabaseFileName);
        m_DatabaseAccess = new DatabaseAccess("data source = " + filePath);
        m_DatabaseAccess.GetMapByDangerLocalSql(ds_satistic, CityId, "StatisticMapSort", FromDate, ToDate, type);
        m_DatabaseAccess.CloseSqlConnection();

        return ds_satistic.Tables[0];
    }



    /// <summary>
    /// 대시보드 부정청약 위험도 퍼센트 전국/지역
    /// </summary>
    /// <param name="CityMenu"></param>
    /// <returns></returns>
    public DataTable GetDashBordMapByDangerPercent(bool [] CityId, string startDate, string endDate, string AllPercent, string Percent5, string Percent10, string Percent20)
    {
        
        ds_satistic = new DataSet();
        filePath = Path.Combine(Application.streamingAssetsPath, m_DatabaseFileName);
        m_DatabaseAccess = new DatabaseAccess("data source = " + filePath);
        m_DatabaseAccess.GetMapByDangerPercentSql(ds_satistic,  CityId, "MainMapSort", startDate, endDate, AllPercent);
        m_DatabaseAccess.GetMapByDangerPercentSql(ds_satistic,  CityId, "MainMapSort", startDate, endDate, Percent5);
        m_DatabaseAccess.GetMapByDangerPercentSql(ds_satistic,  CityId, "MainMapSort", startDate, endDate, Percent10);
        m_DatabaseAccess.GetMapByDangerPercentSql(ds_satistic,  CityId, "MainMapSort", startDate, endDate, Percent20);

        m_DatabaseAccess.CloseSqlConnection();

        return ds_satistic.Tables[0];
    }

    /// <summary>
    /// 통계 부정청약 위험도 퍼센트 전국/지역
    /// </summary>
    /// <param name="CityMenu"></param>
    /// <returns></returns>
    public DataTable GetStatisticMapByDangerPercent(bool [] CityId,  string startDate, string endDate, string AllPercent, string Percent5, string Percent10, string Percent20)
    {
        ds_satistic = new DataSet();
        filePath = Path.Combine(Application.streamingAssetsPath, m_DatabaseFileName);
        m_DatabaseAccess = new DatabaseAccess("data source = " + filePath);
        m_DatabaseAccess.GetMapByDangerPercentSql(ds_satistic,  CityId, "StatisticMapSort", startDate, endDate, AllPercent);
        m_DatabaseAccess.GetMapByDangerPercentSql(ds_satistic,  CityId, "StatisticMapSort", startDate, endDate, Percent5);
        m_DatabaseAccess.GetMapByDangerPercentSql(ds_satistic,  CityId, "StatisticMapSort", startDate, endDate, Percent10);
        m_DatabaseAccess.GetMapByDangerPercentSql(ds_satistic,  CityId, "StatisticMapSort", startDate, endDate, Percent20);
        m_DatabaseAccess.CloseSqlConnection();

        return ds_satistic.Tables[0];
    }

    public void AccountUpdate(MemberData member)
    {
        filePath = Path.Combine(Application.streamingAssetsPath, m_DatabaseFileName);
        m_DatabaseAccess = new DatabaseAccess("data source = " + filePath);
        m_DatabaseAccess.updateMemberInfo(member);
        m_DatabaseAccess.CloseSqlConnection();
    }


}
 