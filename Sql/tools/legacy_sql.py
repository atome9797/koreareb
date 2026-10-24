# 리팩터링 이전 DatabaseAccess.cs 의 SQL 문자열 조립 코드를 그대로 옮긴 것.
# verify_queries.py 가 새 쿼리와 결과를 비교하고 속도를 재는 데만 쓴다. (앱에서는 쓰지 않음)

CITY_ID = [11, 26, 27, 28, 29, 30, 31, 36, 41, 42, 43, 44, 45, 46, 47, 48, 50]

LOCAL_CASE = (
    " CASE "
    " WHEN apt_sup_pos/ 1000 = 11 THEN 11"
    " WHEN apt_sup_pos/ 1000 = 26 THEN 26"
    " WHEN apt_sup_pos/ 1000 = 27 THEN 27"
    " WHEN apt_sup_pos/ 1000 = 28 THEN 28"
    " WHEN apt_sup_pos/ 1000 = 29 THEN 29"
    " WHEN apt_sup_pos/ 1000 = 30 THEN 30"
    " WHEN apt_sup_pos/ 1000 = 31 THEN 31"
    " WHEN apt_sup_pos/ 1000 = 36 THEN 36"
    " WHEN apt_sup_pos/ 1000 = 41 THEN 41"
    " WHEN apt_sup_pos/ 1000 = 42 THEN 42"
    " WHEN apt_sup_pos/ 1000 = 43 THEN 43"
    " WHEN apt_sup_pos/ 1000 = 44 THEN 44"
    " WHEN apt_sup_pos/ 1000 = 45 THEN 45"
    " WHEN apt_sup_pos/ 1000 = 46 THEN 46"
    " WHEN apt_sup_pos/ 1000 = 47 THEN 47"
    " WHEN apt_sup_pos/ 1000 = 48 THEN 48"
    " WHEN apt_sup_pos/ 1000 = 50 THEN 50"
    " ELSE 'Other' "
    "END AS LocalCode"
)


def city_query(city_toggle):
    q = " ("
    count = 0
    for i in range(1, len(city_toggle)):
        if city_toggle[i]:
            if count > 0:
                q += " or"
            q += " LocalCode = " + str(CITY_ID[i - 1])
            count += 1
    q += " )"
    return q


def bust_type_query(bust, rfxg):
    q = " "
    if bust[0]:
        if rfxg[0]: q += " AVG(inner_data.unauth_everage) * 100 as '위험도',"
        elif rfxg[1]: q += " AVG(inner_data.unauth_rf_prob) * 100 as '위험도',"
        elif rfxg[2]: q += " AVG(inner_data.unauth_xgb_prob) * 100 as '위험도',"
    elif bust[1]:
        if rfxg[0]: q += " AVG(inner_data.fake_everage) * 100 as '위험도',"
        elif rfxg[1]: q += " AVG(inner_data.fake_rf_prob) * 100 as '위험도',"
        elif rfxg[2]: q += " AVG(inner_data.fake_xgb_prob) * 100 as '위험도',"
    elif bust[2]:
        if rfxg[0]: q += " AVG(inner_data.sale_everage) * 100 as '위험도',"
        elif rfxg[1]: q += " AVG(inner_data.sale_rf_prob) * 100 as '위험도',"
        elif rfxg[2]: q += " AVG(inner_data.sale_xgb_prob) * 100 as '위험도',"
    return q


def bust_type_query2(bust, rfxg):
    q = " "
    if bust[0]:
        if rfxg[0]: q += " inner_data.unauth_everage"
        elif rfxg[1]: q += " inner_data.unauth_rf_prob"
        elif rfxg[2]: q += " inner_data.unauth_xgb_prob"
    elif bust[1]:
        if rfxg[0]: q += " inner_data.fake_everage"
        elif rfxg[1]: q += " inner_data.fake_rf_prob"
        elif rfxg[2]: q += " inner_data.fake_xgb_prob"
    elif bust[2]:
        if rfxg[0]: q += " inner_data.sale_everage"
        elif rfxg[1]: q += " inner_data.sale_rf_prob"
        elif rfxg[2]: q += " inner_data.sale_xgb_prob"
    return q


def risk_std_query(bust, rfxg):
    q = " "
    if bust[0]:
        if rfxg[0]: q += " (unauth_rf_prob + unauth_xgb_prob) / 2 "
        elif rfxg[1]: q += " unauth_rf_prob "
        elif rfxg[2]: q += " unauth_xgb_prob "
    elif bust[1]:
        if rfxg[0]: q += " (fake_rf_prob + fake_xgb_prob) / 2 "
        elif rfxg[1]: q += " fake_rf_prob "
        elif rfxg[2]: q += " fake_xgb_prob "
    elif bust[2]:
        if rfxg[0]: q += " (sale_rf_prob + sale_xgb_prob) / 2 "
        elif rfxg[1]: q += " sale_rf_prob "
        elif rfxg[2]: q += " sale_xgb_prob "
    return q


def percent_query(percent_type, typ):
    q = " "
    percent = ["1", "0.5", "0.25", "0.1"]
    for i in range(len(percent_type)):
        if percent_type[i]:
            if typ == "Top":
                q += f" inner_data.total_suphouse_number * {percent[i]} as '세대수', "
            elif typ == "Bottom":
                q += f" inner_data.rankTest <= inner_data.total_suphouse_number * {percent[i]}"
            elif typ == "Bottom2":
                q += f"  inner_data.rankTest <= inner_data.total_rows * {percent[i]}"
            elif typ == "Bottom3":
                q += f" inner_data.rankTest <= inner_data.countTest * {percent[i]}"
    return q


def search_market(stype, sstr):
    q = " "
    if stype[0] and sstr:
        q += f" AND (apt_name LIKE '%{sstr}%' or apt_number LIKE '%{sstr}%' or location LIKE '%{sstr}%')"
    elif stype[1]:
        q += f" AND apt_name LIKE '%{sstr}%'"
    elif stype[2]:
        q += f" AND apt_number LIKE '%{sstr}%'"
    elif stype[3]:
        q += f" AND location LIKE '%{sstr}%'"
    return q


def statistics_by_region(from_date, to_date, city):
    q = (" SELECT "
         " inner_no as 'NO', "
         " inner_LocalCode as '지역', "
         " inner_apt_name as '단지수', "
         " inner_total_suphouse_number as '세대수', "
         " inner_inspection_member_check as '점검단지', "
         " inner_inspection_member_check_count as '점검세대', "
         " case when inner_unauth_everage is null then 0.0 else inner_unauth_everage * 100 end as '평균', "
         " inner_danger_all as '전체', "
         " inner_danger_camo as '위장전입', "
         " inner_danger_sale as '통장매매', "
         " inner_danger_other as '기타' "
         " FROM ( "
         "SELECT "
         " ROW_NUMBER() OVER("
         " ORDER BY inner_data.LocalCode asc"
         " ) as inner_no ,"
         " inner_data.LocalCode as inner_LocalCode ,"
         " COUNT(inner_data.apt_name) as inner_apt_name ,"
         " SUM(inner_data.total_suphouse_number) as inner_total_suphouse_number , "
         " SUM(CASE WHEN inner_data.inspection_member_check > 0 THEN 1 ELSE 0 END) as inner_inspection_member_check,"
         " SUM(inner_data.inspection_member_check) as inner_inspection_member_check_count , "
         " AVG(CASE WHEN inner_data.inspection_member_check > 0 THEN inner_data.unauth_everage END) AS inner_unauth_everage , "
         " SUM(inner_data.danger_all) as inner_danger_all , "
         " SUM(inner_data.danger_camo) as inner_danger_camo , "
         " SUM(inner_data.danger_sale) as inner_danger_sale , "
         " SUM(inner_data.danger_other) as inner_danger_other "
         " FROM ("
         " SELECT "
         " *,"
         " SUM(CASE WHEN inspection_status_result = 'Y' THEN 1 ELSE 0 END) AS inspection_member_check,"
         " AVG(CASE WHEN inspection_status_result = 'Y' THEN (unauth_rf_prob + unauth_xgb_prob) / 2 END) AS unauth_everage, "
         " SUM(CASE WHEN inspection_status_result = 'Y' and unauth_sub_decision = 1 THEN unauth_disguise_transfer ELSE 0 END) + SUM(CASE WHEN inspection_status_result = 'Y' and unauth_sub_decision = 1 THEN unauth_savecerti_sale ELSE 0 END) + SUM(CASE WHEN inspection_status_result = 'Y' and unauth_sub_decision = 1 THEN unauth_other ELSE 0 END) as danger_all, "
         " SUM(CASE WHEN inspection_status_result = 'Y' and unauth_sub_decision = 1 THEN unauth_disguise_transfer ELSE 0 END) as danger_camo, "
         " SUM(CASE WHEN inspection_status_result = 'Y' and unauth_sub_decision = 1 THEN unauth_savecerti_sale ELSE 0 END) as danger_sale, "
         " SUM(CASE WHEN inspection_status_result = 'Y' and unauth_sub_decision = 1 THEN unauth_other ELSE 0 END) as danger_other, "
         + LOCAL_CASE +
         " FROM tb_riskdata"
         f" WHERE rec_anno_date >= {from_date} AND rec_anno_date <= {to_date}"
         " GROUP BY apt_name, LocalCode"
         " )AS inner_data"
         " WHERE ")
    q += city_query(city)
    q += (" GROUP BY inner_data.LocalCode"
          " order by inner_data.LocalCode ASC)")
    return q


def statistics_by_danji(from_date, to_date, city, bust, rfxg, pct):
    q = ("SELECT "
         " ROW_NUMBER() OVER (ORDER BY  inner_data.rec_anno_date DESC , inner_data.LocalCode asc ) AS 'NO',"
         " inner_data.apt_name as '단지명',"
         " inner_data.location as '지역',")
    q += bust_type_query(bust, rfxg)
    q += (" SUM(CASE WHEN inner_data.unauth_sub_decision = 1 THEN inner_data.unauth_disguise_transfer ELSE 0 END) + SUM(CASE WHEN inner_data.unauth_sub_decision = 1 THEN inner_data.unauth_savecerti_sale ELSE 0 END) + SUM(CASE WHEN inner_data.unauth_sub_decision = 1 THEN inner_data.unauth_other ELSE 0 END) as '전체',"
          " SUM(CASE WHEN inner_data.unauth_sub_decision = 1 THEN inner_data.unauth_disguise_transfer ELSE 0 END) as '위장전입',"
          " SUM(CASE WHEN inner_data.unauth_sub_decision = 1 THEN inner_data.unauth_savecerti_sale ELSE 0 END) as '통장매매',"
          " SUM( CASE WHEN inner_data.unauth_sub_decision = 1 THEN inner_data.unauth_other ELSE 0 END) as '기타',"
          " inner_data.rec_anno_date as '모집공고일' "
          " FROM ("
          " SELECT "
          " *, "
          " COUNT(*) OVER (PARTITION  BY apt_name) AS countTest, "
          f" ROW_NUMBER() OVER (PARTITION  BY apt_name ORDER BY apt_name ASC, {risk_std_query(bust, rfxg)} DESC) AS rankTest, "
          " (unauth_rf_prob +unauth_xgb_prob) / 2 AS unauth_everage,"
          " (fake_rf_prob +fake_xgb_prob) / 2 AS fake_everage,"
          " (sale_rf_prob +sale_xgb_prob) / 2 AS sale_everage,"
          " bjdong.bjdong_name AS location,"
          + LOCAL_CASE +
          " FROM tb_riskdata risk"
          " JOIN tb_bjdong bjdong"
          " ON risk.apt_sup_pos = bjdong.bjdong_code"
          f" WHERE rec_anno_date >= {from_date} AND rec_anno_date <= {to_date} AND inspection_status_result = 'Y'"
          " )AS inner_data"
          " WHERE ")
    q += city_query(city)
    q += " AND "
    q += percent_query(pct, "Bottom3")
    q += " GROUP BY inner_data.apt_name"
    q += " ORDER BY  inner_data.rec_anno_date DESC , inner_data.LocalCode asc ;"
    return q


def market_by_danji(from_date, to_date, stype, sstr, city, bust, rfxg, pct):
    q = ("SELECT "
         " ROW_NUMBER() OVER (ORDER BY  inner_data.rec_anno_date DESC , inner_data.LocalCode asc ) AS 'NO',"
         " CASE WHEN SUM(inner_data.inspection_status_count1) > 0 THEN 'N'  WHEN SUM(inner_data.inspection_status_count2) == 0 THEN '해당없음' ELSE 'Y'  END as '점검여부', "
         " inner_data.apt_number as '주택관리번호',"
         " inner_data.apt_type as '민영국민',"
         " inner_data.apt_name as '단지명',"
         " inner_data.location as '지역',"
         " inner_data.total_suphouse_number as '공급세대수', ")
    q += bust_type_query(bust, rfxg)
    q += percent_query(pct, "Top")
    q += (" inner_data.rec_anno_date as '모집공고일',"
          " inner_data.win_anno_date as '당첨자발표일',"
          " inner_data.limittransfer_date as '전입제한일',"
          " CASE WHEN inner_data.saleprice_cap_status = 0 THEN 'N' ELSE 'Y'  END as '분양가상한제'"
          " "
          " FROM ("
          " SELECT "
          " *, "
          f" ROW_NUMBER() OVER (PARTITION  BY apt_name ORDER BY apt_name ASC, {risk_std_query(bust, rfxg)} DESC) AS rankTest, "
          " CASE WHEN inspection_status_result = 'N' THEN 1 ELSE 0 END as inspection_status_count1, "
          " CASE WHEN inspection_status_result = 'Z' THEN 0 ELSE 1 END as inspection_status_count2, "
          " (unauth_rf_prob + unauth_xgb_prob) / 2 AS unauth_everage,"
          " (fake_rf_prob + fake_xgb_prob) / 2 AS fake_everage,"
          " (sale_rf_prob + sale_xgb_prob) / 2 AS sale_everage,"
          " bjdong.bjdong_name AS location,"
          + LOCAL_CASE +
          " FROM tb_riskdata risk"
          " JOIN tb_bjdong bjdong"
          " ON risk.apt_sup_pos = bjdong.bjdong_code"
          f" WHERE rec_anno_date >= {from_date} AND rec_anno_date <= {to_date}")
    q += " AND "
    q += city_query(city)
    q += search_market(stype, sstr)
    q += (f" ORDER BY apt_name ASC, {risk_std_query(bust, rfxg)} DESC"
          " )AS inner_data"
          " WHERE ")
    q += percent_query(pct, "Bottom")
    q += " GROUP BY inner_data.apt_name"
    q += " ORDER BY  inner_data.rec_anno_date DESC , inner_data.LocalCode asc ;"
    return q


def market_execute(from_date, to_date, stype, sstr, city, bust, rfxg, pct):
    q = ("SELECT "
         f" ROW_NUMBER() OVER (ORDER BY  ({bust_type_query2(bust, rfxg)})  DESC) AS 'NO',"
         " inner_data.*,"
         " CASE WHEN inner_data.inspection_status_result = 'Z' THEN 'Z' ELSE inner_data.inspection_status_result END as '점검여부',"
         " CASE WHEN inner_data.unauth_sub_decision = 0 THEN '통과' ELSE unauth_sub_type  END as '부정청약결과', ")
    q += f" {bust_type_query2(bust, rfxg)} as '위험도',"
    q += (" inner_data.apt_number as '주택관리번호',"
          " inner_data.apt_type as '민영국민',"
          " inner_data.apt_name as '단지명',"
          " inner_data.apt_dong as '동',"
          " inner_data.apt_hosu as '호수',"
          " inner_data.user_name as '성명',"
          " inner_data.user_birth as '생년월일',"
          " inner_data.user_age as '연령',"
          " inner_data.apt_sup_type as '공급유형',"
          " inner_data.apt_sp_sup as '세부유형타입',"
          " inner_data.user_relationship as '세대주관계',"
          " inner_data.user_change_date as '변경일자',"
          " inner_data.user_spouse as '배우자',"
          " inner_data.user_housemember as '세대원수',"
          " inner_data.user_hp_housemember as '분리세대',"
          " inner_data.user_phone_dup as '폰중복',"
          " inner_data.user_ip_dub_win as 'ip중복당첨',"
          " inner_data.user_ip_dub_app as 'ip중복신청',"
          " inner_data.app_residence_class as '신청거주구분',"
          " inner_data.win_residence_class as '당첨거주구분',"
          " CASE WHEN inner_data.ad_addressmatch_status = 0 THEN 'N' ELSE 'Y' END as '주소일치여부',"
          " inner_data.user_admin_address as '등본주소',"
          " inner_data.app_address as '입력주소',"
          " inner_data.user_recom_type as '기관추천',"
          " strftime('%Y.%m.%d %H:%M:%S', datetime(inner_data.change_date, '+9 hours')) as '업로드일시'"
          " FROM ("
          " SELECT "
          " *, "
          " COUNT(*) OVER () AS total_rows, "
          f" ROW_NUMBER() OVER (ORDER BY {risk_std_query(bust, rfxg)} DESC) AS rankTest, "
          " (unauth_rf_prob + unauth_xgb_prob) / 2 AS unauth_everage,"
          " (fake_rf_prob + fake_xgb_prob) / 2 AS fake_everage,"
          " (sale_rf_prob + sale_xgb_prob) / 2 AS sale_everage,"
          " bjdong.bjdong_name AS location,"
          + LOCAL_CASE +
          " FROM tb_riskdata risk"
          " JOIN tb_bjdong bjdong"
          " ON risk.apt_sup_pos = bjdong.bjdong_code"
          f" WHERE rec_anno_date >= {from_date} AND rec_anno_date <= {to_date}")
    q += " AND " + city_query(city)
    q += search_market(stype, sstr)
    q += (f" ORDER BY {risk_std_query(bust, rfxg)} DESC"
          " )AS inner_data"
          " WHERE ")
    q += percent_query(pct, "Bottom2")
    q += f" ORDER BY  {bust_type_query2(bust, rfxg)} DESC;"
    return q


def map_by_danger(city, sort_type, from_date, to_date, typ):
    q = ("SELECT "
         " ROW_NUMBER() OVER("
         " ORDER BY (AVG(inner_data.unauth_everage) * 100) desc"
         " ) as 'NO',"
         " inner_data.LocalCode as '지역',"
         " AVG(inner_data.unauth_everage) * 100 AS everage,"
         " SUM(inner_data.unauth_sub_decision) AS '적발수',"
         " SUM(CASE WHEN inner_data.unauth_sub_decision = 1 THEN inner_data.unauth_disguise_transfer ELSE 0 END) + SUM(CASE WHEN inner_data.unauth_sub_decision = 1 THEN inner_data.unauth_savecerti_sale ELSE 0 END) + SUM(CASE WHEN inner_data.unauth_sub_decision = 1 THEN inner_data.unauth_other ELSE 0 END) as '전체',"
         " SUM(CASE WHEN inner_data.unauth_sub_decision = 1 THEN inner_data.unauth_disguise_transfer  ELSE 0 END) as '위장전입', "
         " SUM(CASE WHEN inner_data.unauth_sub_decision = 1 THEN inner_data.unauth_savecerti_sale  ELSE 0 END) as '통장매매', "
         " SUM(CASE WHEN inner_data.unauth_sub_decision = 1 THEN inner_data.unauth_other ELSE 0 END) as '기타' "
         " FROM ("
         " SELECT "
         " *, "
         " (unauth_rf_prob + unauth_xgb_prob) / 2 AS unauth_everage,"
         + LOCAL_CASE +
         " FROM tb_riskdata"
         " WHERE ")
    if sort_type == "StatisticMapSort":
        q += f" rec_anno_date >= {from_date} AND rec_anno_date <= {to_date} AND inspection_status_result = 'Y'"
    elif sort_type == "MainMapSort":
        q += f" change_date >= '{from_date}' AND change_date  <= '{to_date}'"
    q += (" )AS inner_data"
          " WHERE ")
    q += city_query(city)
    if typ == "map":
        q += " GROUP BY inner_data.LocalCode order by inner_data.LocalCode asc;"
    elif typ == "score":
        q += " GROUP BY inner_data.LocalCode order by everage desc;"
    return q


def map_by_danger_percent(city, sort_type, from_date, to_date, percent):
    q = ("SELECT "
         " AVG(unauth_everage) * 100 AS '전체'"
         " FROM ("
         "   SELECT *,"
         "   \t(unauth_rf_prob + unauth_xgb_prob) / 2 AS unauth_everage,"
         + LOCAL_CASE +
         "   FROM tb_riskdata")
    if sort_type == "StatisticMapSort":
        q += f" WHERE rec_anno_date >= {from_date} AND rec_anno_date <= {to_date} AND inspection_status_result = 'Y' "
    elif sort_type == "MainMapSort":
        q += f" WHERE change_date >= '{from_date}' AND change_date  <= '{to_date}'"
    q += " AND "
    q += city_query(city)
    q += f" ORDER BY unauth_everage DESC   limit {percent});"
    return q


def map_by_danger_local(city, sort_type, from_date, to_date, typ):
    q = ("SELECT "
         " ROW_NUMBER() OVER("
         " ORDER BY (AVG(inner_data.unauth_everage) * 100) desc"
         " ) as 'NO',"
         " inner_data.apt_name as '단지',"
         "     inner_data.apt_sup_pos as '지역',"
         " SUM(CASE WHEN inner_data.unauth_sub_decision = 1 THEN inner_data.unauth_disguise_transfer ELSE 0 END) + SUM(CASE WHEN inner_data.unauth_sub_decision = 1 THEN inner_data.unauth_savecerti_sale ELSE 0 END) + SUM(CASE WHEN inner_data.unauth_sub_decision = 1 THEN inner_data.unauth_other ELSE 0 END) as '전체',"
         " SUM(CASE WHEN inner_data.unauth_sub_decision = 1 THEN inner_data.unauth_disguise_transfer  ELSE 0 END) as '위장전입', "
         " SUM(CASE WHEN inner_data.unauth_sub_decision = 1 THEN inner_data.unauth_savecerti_sale  ELSE 0 END) as '통장매매', "
         " SUM(CASE WHEN inner_data.unauth_sub_decision = 1 THEN inner_data.unauth_other ELSE 0 END) as '기타', "
         " \tAVG(inner_data.unauth_everage) * 100 AS everage"
         " FROM ("
         " SELECT "
         "    *,"
         "    (unauth_rf_prob + unauth_xgb_prob) / 2 AS unauth_everage,"
         + LOCAL_CASE +
         " FROM tb_riskdata risk"
         " JOIN tb_bjdong bjdong"
         " ON risk.apt_sup_pos = bjdong.bjdong_code")
    if sort_type == "StatisticMapSort":
        q += f" WHERE risk.rec_anno_date >= {from_date} AND risk.rec_anno_date <= {to_date} AND risk.inspection_status_result = 'Y' "
    elif sort_type == "MainMapSort":
        q += f" WHERE risk.change_date >= '{from_date}' AND risk.change_date  <=  '{to_date}'"
    q += (" )AS inner_data"
          " WHERE ")
    q += city_query(city)
    if typ == "map":
        q += " GROUP BY inner_data.apt_sup_pos / 10 order by inner_data.apt_sup_pos / 10 asc;"
    elif typ == "score":
        q += " GROUP BY inner_data.apt_name order by everage desc limit 10;"
    return q


def percent_person(sort_type, start_date, end_date, city):
    q = ("SELECT"
         " count(*) as total, "
         " count(*) * 0.05 as high,"
         " count(*) * 0.1 as middle,"
         " count(*) * 0.2 as low"
         " FROM ( "
         " SELECT "
         " *, "
         " (unauth_rf_prob +unauth_xgb_prob) / 2 AS unauth_everage,"
         " (fake_rf_prob +fake_xgb_prob) / 2 AS fake_everage,"
         " (sale_rf_prob +sale_xgb_prob) / 2 AS sale_everage,"
         + LOCAL_CASE +
         " FROM tb_riskdata"
         " ) inner_data")
    if sort_type == "StatisticMapSort":
        q += f" WHERE rec_anno_date >= {start_date} AND rec_anno_date <= {end_date}"
        q += " and inspection_status_result = 'Y' "
    elif sort_type == "MainMapSort":
        q += f" WHERE change_date >= '{start_date}' AND change_date  <= '{end_date}'"
    q += f" and {city_query(city)};"
    return q


def last_create_date():
    return " select datetime(max(change_date) , '-1 year') as '시작시간',  max(change_date) as '끝시간' from tb_riskdata;"
