import sqlite3
import pandas as pd
import time

start = time.time()  # 시작 시간 저장

# SQLite 데이터베이스 연결
# conn = sqlite3.connect('C:/Users/user/Desktop/build/부정청약관리프로그램_Data/StreamingAssets/koreareb.db')
conn = sqlite3.connect('C:/Project-Company/koreareb/Assets/StreamingAssets/koreareb.db')

cursor = conn.cursor()

columns = ["data_no", "apt_number", "apt_type", "apt_name", "apt_dong", "apt_hosu", "apt_sup_type", "apt_sp_sup", "apt_sup_pos",
    "apt_size", "apt_gen_sup", "apt_sp_sup1", "apt_sup_amount",
    "user_birth", "user_name", "user_age", "user_relationship", "user_admin_change", "user_change_date",
    "user_spouse", "user_sp_note", "user_admin_address", "user_addr_match", "user_addr_dup",
    "user_sp_simul", "user_sub_num", "user_housemember", "user_hp_housemember", "user_phone_dup",
    "user_ip_dub_win", "user_ip_dub_app", "user_phone", "user_ip_1", "user_ip_2", "user_ip_3",
    "user_ip_4", "ad_ip_red_2", "ad_ip_red_3", "ad_ip_red_4", "receipt_date", "receipt_time", "receipt_medium",
    "app_residence_class", "win_residence_class", "residenct_area", "app_address",
    "app_detail_address", "addpoint_app_status", "addpoint_win_status", "addpoint_sum", "dependent_number",
    "save_sub_period", "homeless_period", "home_owner", "long_soldier", "i_nh_nohome_period",
    "j_save_type", "sub_open_date", "sub_pay_number", "sub_pay_amount", "sub_expiry_period", "multichild_number",
    "multichild_homeless_period", "multichild_residenct_period", "multichild_subsave_period",
    "multichild_totalscore", "multichild_minor_number", "new_public_act", "new_revision_date", "new_income_sep",
    "new_app_rank", "new_underaged_number", "new_fetus_number",
    "new_income_ratio", "new_income_category", "ad_new_income_sep", "new_avgincome_allo", "new_minor_childscore",
    "new_minor_childnumber", "new_residence_allo", "new_residence_period",
    "new_marriageperiod_allo", "new_marriage_period", "new_singleparentchildage_allo",
    "new_singleparentchildage", "new_constructionsaveperiod_allo", "new_construction_saveperiod",
    "new_totalscore",
    "user_recom_type", "sub_rewinlimit_status", "sub_marrysingle_status", "householder_status",
    "last5year_win_status", "homeless_number", "income_base", "incometax_income_base",
    "gen_compose", "homeless_householder", "base_assets", "elderlyparent_status", "minorchild_threemore",
    "marriage_sevenyear", "ad_addressmatch_status", "inspection_status",
    "unauth_sub_decision", "unauth_sub_type", "unauth_regidence_maintain", "unauth_address_maintain",
    "unauth_disguise_transfer", "unauth_forgery_qualifi", "unauth_savecerti_sale",
    "unauth_disguise_divorce", "unauth_other", "rec_anno_date", "win_anno_date", "limittransfer_date",
    "first_requirement", "total_suphouse_number", "saleprice_cap_status", "overcrow_area_status",
    "first_internet_date",
    "second_internet_date", "ad_change_date", "ad_total_supply", "ad_namebirth_dup", "ad_namebirthphone_dup",
    "ad_administrative_change", "ad_reception_time", "ad_appwinresimatch_status", "ad_dependent_number",
    "ad_save_period",
    "ad_homeless_period", "ad_subpay_number", "ad_subpay_amount", "ad_sub_period", "ad_app_type",
    "ad_multichild_infantnumber", "ad_multichildhomeless_period", "ad_multichild_residenceperiod",
    "ad_multichild_residencesavesubperiod", "ad_multichild_totalscore",
    "ad_multichildminor_number", "ad_newminor_number", "ad_newfetus_number", "ad_minor_number",
    "ad_total_points", "ad_changetime2",
    "unauth_rf_prob", "unauth_rf_per50", "unauth_rf_per25", "unauth_rf_per10", "unauth_xgb_prob",
    "unauth_xgb_per50", "unauth_xgb_per25", "unauth_xgb_per10",
    "fake_rf_prob", "fake_rf_per50", "fake_rf_per25", "fake_rf_per10", "fake_xgb_prob", "fake_xgb_per50",
    "fake_xgb_per25", "fake_xgb_per10",
    "sale_rf_prob", "sale_rf_per50", "sale_rf_per25", "sale_rf_per10", "sale_xgb_prob", "sale_xgb_per50",
    "sale_xgb_per25", "sale_xgb_per10"]

setvalues = ["data_no", "주택관리번호", "민영국민", "주택명", "동", "호수", "공급유형", "특별공급종류", "공급위치_시군구코드", "크기", "일반공급", "특별공급", "공급금액", "생년", "성명",
"연령","세대주관계", "행정변경", "변경일자", "배우자", "특이사항", "행정주소", "주소일치여부_부동산원", "주소중복횟수", "특일동시여부", "2년청약건수", "세대원수", "분리세대원수", "폰중복횟수_부동산원",
"IP중복당첨횟수_부동산원", "IP중복신청횟수_부동산원", "폰번호", "IP_1", "IP_2", "IP_3", "IP_4", "ad_IP중복_2자리", "ad_IP중복_3자리", "ad_IP중복_4자리", "접수일자", "접수시간", "접수매체", "신청거주구분",
"당첨거주구분", "거주지역", "신청주소", "상세신청주소", "가점신청여부", "가점당첨여부", "가점합계", "부양가족수", "저축가입기간", "무주택기간", "주택소유구분", "장기복무군인", "I_국민주택무주택기간",
"J_저축종류","청약통장개설일자",  "청약납부회차", "청약납부금액", "청약경과기간", "다자녀_영유아자녀수", "다자녀_무주택기간", "다자녀_해당지역거주기간", "다자녀_입주자저축가입기간", "다자녀_총점", "다자녀_미성년자녀수",
"신혼_공특법", "신혼_개정시행일자", "신혼미공특_소득구분",  "신혼미공특_신청순위", "신혼미공특_미성년자녀수", "신혼미공특_태아수", "신혼미공특_소득비율", "신혼미공특_소득구분명", "ad_신혼미공특소득구분", "신혼공특_월평균소득배점",
"신혼공특_미성년자녀수배점", "신혼공특_미성년자녀수", "신혼공특_해당지역거주기간배점", "신혼공특_해당지역거주기간",  "신혼공특_혼인기간배점","신혼공특_혼인기간",  "신혼공특_한부모가족자녀나이배점",
"신혼공특_한부모가족자녀나이", "신혼공특_입주자저축가입기간배점", "신혼공특_입주자저축가입기간", "신혼공특_총점","기관추천종류", "청약_재당첨제한여부", "청약_혼인및한부모가족", "세대주여부",
"과거5년이내당첨", "무주택세대구성원", "소득기준", "소득(소득세 5개년도 납부)기준", "세대구성", "무주택세대주", "자산기준", "노부모부양", "3명이상미성년자녀", "혼인기간7년이내", "ad_주소일치여부","검사여부",
"부정청약판정", "부정청약유형", "부정_거주지유지", "부정_주소지유지", "부정_위장전입", "부정_자격위조", "부정_입주자저축증서매매", "부정_위장이혼", "부정_기타", "모집공고일", "당첨자발표일", "해당전입제한일",
"1순위요건", "총공급가구수", "분양가상한제여부", "과밀성장권역여부", "1순위인터넷접수일", "2순위인터넷접수일시", "ad_변경일자", "ad_총공급", "ad_성명생년중복", "ad_성명생년전화중복", "ad_행정변경시점",
"ad_접수시간", "ad_신청당첨거주일치여부", "ad_부양가족수", "ad_저축가입기간", "ad_무주택기간", "ad_청약납부회차", "ad_청약납부금액", "ad_청약경과기간", "ad_신청유형", "ad_다자녀영유아자녀수",
"ad_다자녀무주택기간", "ad_다자녀해당지역거주기간", "ad_다자녀입주자저축가입기간", "ad_다자녀총점", "ad_다자녀미성년자녀수", "ad_신혼미공특미성년자녀수", "ad_신혼미공특태아수", "ad_미성년자녀수",
"ad_총점", "ad_변경시점2", "부정청약_rf_prob", "부정청약_rf_0.5",  "부정청약_rf_0.25", "부정청약_rf_0.1", "부정청약_xgb_prob", "부정청약_xgb_0.5", "부정청약_xgb_0.25", "부정청약_xgb_0.1","위장전입_rf_prob",
"위장전입_rf_0.5", "위장전입_rf_0.25", "위장전입_rf_0.1", "위장전입_xgb_prob", "위장전입_xgb_0.5", "위장전입_xgb_0.25", "위장전입_xgb_0.1", "매매_rf_prob", "매매_rf_0.5", "매매_rf_0.25","매매_rf_0.1",
"매매_xgb_prob", "매매_xgb_0.5", "매매_xgb_0.25", "매매_xgb_0.1"]

# 업데이트할 테이블 이름
table_name = 'tb_riskdata'

DF=pd.read_csv(r'SendResult.csv', encoding='CP949', sep=",", low_memory=False)
DF = DF.iloc[:, :-1]


df_tupleList = list(tuple(DF.itertuples(index=False,name=None)))
# columns = DF.columns.tolist()
cursor.execute(f"UPDATE SQLITE_SEQUENCE SET seq = 0 WHERE name = 'tb_checkresult';")

bind_names = ",".join(":" + str(i + 1) for i in range(len(columns)))
set_columns = ", ".join([f"{column}=excluded.{column}" for column in columns])

# insert_query = f"insert into {table_name} ({', '.join(columns)}) values ({bind_names}) on conflict(data_no) do update set {set_columns}"
insert_query = f"INSERT OR REPLACE INTO {table_name} ({', '.join(columns)}) VALUES ({bind_names}) RETURNING data_idx;"

print ("insert_query" + insert_query)

sqe = cursor.executemany(insert_query, df_tupleList)

conn.commit()

conn.close()


print ("insert_query" + insert_query)

print ("END")

print("time :", time.time() - start)  # 현재시각 - 시작시간 = 실행 시간

