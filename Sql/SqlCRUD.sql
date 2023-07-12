select * from tb_riskdata;
select strftime('%Y-%m-%d %H:%M:%S', 'now', 'localtime');

SELECT strftime('%Y-%m-%d %H:%M:%S', datetime('2023-05-31 07:32:39', '-9 hours'));


--시간 업데이트
--update tb_riskdata set create_date = strftime('%Y-%m-%d %H:%M:%S', 'now', 'localtime'), change_date = strftime('%Y-%m-%d %H:%M:%S', 'now', 'localtime');


--통계 위험도 결과값 테이블 (날짜는 숫자 범위 지정해주면됨), 날짜 같으면 인덱스 순서대로
--1. 지역별로 조건 추가
--2. 날짜 범위 별로 조건 추가
select * from tb_riskdata;

SELECT unauth_rf_per50 as 'asd'  FROM tb_riskdata WHERE (apt_sup_pos/1000) = 11 and inspection_status = 1 AND rec_anno_date >= 20200000 AND rec_anno_date <= 20220000;


SELECT unauth_xgb_prob, unauth_rf_prob, sum(unauth_rf_prob + unauth_xgb_prob) AS unauth_everage  FROM tb_riskdata WHERE (apt_sup_pos/1000) = 11;
SELECT unauth_xgb_prob, unauth_rf_prob, avg((unauth_rf_prob + unauth_xgb_prob) / 2) as '평균'  FROM tb_riskdata WHERE (apt_sup_pos/1000) = 11 AND apt_dong = 117;


select * from tb_riskdata where inspection_status_result = 'N';
/*
SELECT * 
FROM tb_riskdata
WHERE apt_sup_pos IN 
    (SELECT COUNT(*)  as 'Soeul' FROM tb_riskdata WHERE (apt_sup_pos/1000) = 11
     UNION ALL
     SELECT COUNT(*) as 'Busan' FROM tb_riskdata WHERE (apt_sup_pos/1000) = 26)
;
*/
--GROUP BY apt_sup_pos;
/*
SELECT 
  CASE 
    WHEN apt_sup_pos BETWEEN 100000 AND 199999 THEN 'Soeul' 
    WHEN apt_sup_pos BETWEEN 200000 AND 299999 THEN 'Busan' 
    ELSE 'Other Regions' 
  END AS apt_sup_pos_test, 
  COUNT(*) AS column_count 
FROM tb_riskdata; */

SELECT 
   COUNT(*) as '단지수',
   SUM(apt_dong) as '세대수',
  CASE 
    WHEN apt_sup_pos/1000 = 11 THEN '서울특별시' 
    WHEN apt_sup_pos/1000 = 26 THEN '부산광역시'
    WHEN apt_sup_pos/1000 = 27 THEN '대구광역시'
    WHEN apt_sup_pos/1000 = 28 THEN '인천광역시'
  	ELSE 'Other' 
  END AS LocalCode
FROM tb_riskdata
GROUP BY LocalCode;

/*
 * inner_data.LocalCode as '지역',
	COUNT(inner_data.apt_name) as '단지수',
	SUM(inner_data.apt_dong) as '세대수'
 * */

/*
 * 통계 지역별 2023-06-09 김영훈
 * */
SELECT  ROW_NUMBER() OVER( ORDER BY inner_data.LocalCode asc ) as 'NO', 
inner_data.LocalCode as '지역', 
COUNT(inner_data.apt_name) as '단지수', 
SUM(inner_data.apt_count) as '세대수', 
SUM(CASE WHEN inner_data.inspection_member_check > 0 THEN 1 ELSE 0 END) as '점검단지', 
SUM(inner_data.inspection_member_check) as '점검세대', 
AVG(inner_data.unauth_everage) * 100 AS '평균', 
SUM(inner_data.danger_all) as '전체', 
SUM(inner_data.danger_camo) as '위장전입', 
SUM(inner_data.danger_sale) as '통장매매', 
SUM(inner_data.danger_other) as '기타' 
FROM ( 
SELECT  *, 
COUNT(*) AS apt_count, -- 단지별 세대수
SUM(CASE WHEN inspection_status_result = 'Z' OR inspection_status_result = 'Y' THEN 1 ELSE 0 END) AS inspection_member_check, -- 단지점검 세대
AVG((unauth_rf_prob + unauth_xgb_prob) / 2) AS unauth_everage, --단지별 위험도 평균,
SUM(unauth_disguise_transfer) + SUM(unauth_savecerti_sale) + SUM(unauth_other) as danger_all, -- 단지별 전체 
SUM(unauth_disguise_transfer) as danger_camo, -- 단지별 위장전입
SUM(unauth_savecerti_sale) as danger_sale, -- 단지별 통장매매 
SUM(unauth_other) as danger_other, -- 단지별 기타 
CASE  WHEN apt_sup_pos/ 1000 = 11 THEN 11 
WHEN apt_sup_pos/ 1000 = 26 THEN 26 WHEN apt_sup_pos/ 1000 = 27 THEN 27 WHEN apt_sup_pos/ 1000 = 28 THEN 28 
WHEN apt_sup_pos/ 1000 = 29 THEN 29 WHEN apt_sup_pos/ 1000 = 30 THEN 30 WHEN apt_sup_pos/ 1000 = 31 THEN 31 
WHEN apt_sup_pos/ 1000 = 36 THEN 36 WHEN apt_sup_pos/ 1000 = 41 THEN 41 WHEN apt_sup_pos/ 1000 = 42 THEN 42 
WHEN apt_sup_pos/ 1000 = 43 THEN 43 WHEN apt_sup_pos/ 1000 = 44 THEN 44 WHEN apt_sup_pos/ 1000 = 45 THEN 45 
WHEN apt_sup_pos/ 1000 = 46 THEN 46 WHEN apt_sup_pos/ 1000 = 47 THEN 47 WHEN apt_sup_pos/ 1000 = 48 THEN 48 
WHEN apt_sup_pos/ 1000 = 50 THEN 50 ELSE 'Other' 
END AS LocalCode 
FROM tb_riskdata 
WHERE rec_anno_date >= 20170601 AND rec_anno_date <= 20250614
GROUP BY apt_name, LocalCode
)AS inner_data 
WHERE  ( LocalCode = 11 or LocalCode = 26 or LocalCode = 27 or LocalCode = 28 or LocalCode = 29 or LocalCode = 30 
or LocalCode = 31 or LocalCode = 36 or LocalCode = 41 or LocalCode = 42 or LocalCode = 43 or LocalCode = 44 
or LocalCode = 45 or LocalCode = 46 or LocalCode = 47 or LocalCode = 48 or LocalCode = 50 ) 
GROUP BY inner_data.LocalCode  order by inner_data.LocalCode ASC;


SELECT COUNT(*) AS '동일결과수' FROM tb_riskdata WHERE apt_number = 11 AND user_name ='asdasd' AND user_birth = 123456 AND user_phone = 123123;

UPDATE tb_riskdata 
SET unauth_rf_prob = 0.11 , unauth_xgb_prob = 0.001
WHERE apt_number = 11 AND user_name ='asdasd' AND user_birth = 123456 AND user_phone = 123123;


SELECT * FROM tb_bjdong;

/*
 * 통계 단지별
 * 2023-05-16 김영훈
 * */
-- 단지 별
SELECT
	*,
    inner_data.apt_name as '단지명',
 	inner_data.location AS '지역',
    inner_data.unauth_rf_prob as '부정청약rf',
    inner_data.unauth_xgb_prob as '부정청약xg',
    inner_data.unauth_everage as '위험도평균',
    inner_data.unauth_disguise_transfer + inner_data.unauth_savecerti_sale + inner_data.unauth_other as '전체',
	inner_data.unauth_disguise_transfer as '위장전입',
	inner_data.unauth_savecerti_sale as '통장매매',
	inner_data.unauth_other as '기타',
    inner_data.rec_anno_date as '모집공고일'
FROM tb_riskdata 
INNER JOIN (
SELECT 
   *,
 (unauth_rf_prob + unauth_xgb_prob) / 2 AS unauth_everage,
 	bjdong.bjdong_name AS location,
  CASE 
    WHEN apt_sup_pos/1000 = 11 THEN 11 
    WHEN apt_sup_pos/1000 = 26 THEN 26
    WHEN apt_sup_pos/1000 = 27 THEN 27
    WHEN apt_sup_pos/1000 = 28 THEN 28
    WHEN apt_sup_pos/1000 = 29 THEN 29
    WHEN apt_sup_pos/1000 = 30 THEN 30
    WHEN apt_sup_pos/1000 = 31 THEN 31
    WHEN apt_sup_pos/1000 = 36 THEN 36
    WHEN apt_sup_pos/1000 = 41 THEN 41
    WHEN apt_sup_pos/1000 = 42 THEN 42
    WHEN apt_sup_pos/1000 = 43 THEN 43
    WHEN apt_sup_pos/1000 = 44 THEN 44
    WHEN apt_sup_pos/1000 = 45 THEN 45
    WHEN apt_sup_pos/1000 = 46 THEN 46
    WHEN apt_sup_pos/1000 = 47 THEN 47
    WHEN apt_sup_pos/1000 = 48 THEN 48
    WHEN apt_sup_pos/1000 = 50 THEN 50
  	ELSE 'Other' 
  END AS LocalCode
FROM tb_riskdata risk
JOIN tb_bjdong bjdong
ON risk.apt_sup_pos = bjdong.bjdong_code
WHERE rec_anno_date >= 20200000 AND rec_anno_date <= 20240000
)AS inner_data
ON tb_riskdata.data_idx = inner_data.data_idx
WHERE LocalCode = 11 OR LocalCode = 26 OR LocalCode = 27 OR LocalCode = 28
order by inner_data.rec_anno_date asc , inner_data.data_idx asc, inner_data.LocalCode asc;


/*
 * 2023-05-16 김영훈 주택청약 시장관리 단지 선정
 * */
SELECT
	*,
	ROW_NUMBER () OVER ( 
        ORDER BY inner_data.rec_anno_date asc
    ) as '번호',
	inner_data.inspection_status_result as '점검여부',
	inner_data.apt_number as '주택관리번호',
	inner_data.apt_type as '민영국민',
    inner_data.apt_name as '단지명',
    inner_data.location as '지역',
    inner_data.user_housemember as '공급세대수',
    inner_data.total_suphouse_number as '총공급가구수',
--    inner_data.fake_rf_prob as '위장전입rf',
--    inner_data.fake_xgb_prob as '위장전입xg',
--    inner_data.fake_everage as '위장전입평균',
--    inner_data.sale_rf_prob as '매매rf',
--    inner_data.sale_xgb_prob as '매매xg',
--    inner_data.sale_everage as '매매평균',
    inner_data.unauth_rf_prob as '부정청약rf',
    inner_data.unauth_xgb_prob as '부정청약xg',
    inner_data.unauth_everage as '부정청약평균',
    inner_data.rec_anno_date as '모집공고일',
    inner_data.win_anno_date as '당첨자발표일',
    inner_data.limittransfer_date as '전입제한일',
    CASE WHEN inner_data.saleprice_cap_status = 0 THEN 'N' ELSE 'Y'  END as '분양가상한제'
FROM tb_riskdata 
INNER JOIN (
SELECT 
 (unauth_rf_prob + unauth_xgb_prob) / 2 AS unauth_everage,
 (fake_rf_prob + fake_xgb_prob) / 2 AS fake_everage,
 (sale_rf_prob + sale_xgb_prob) / 2 AS sale_everage,
 	bjdong.bjdong_name AS location,
   *,
  CASE 
    WHEN apt_sup_pos/1000 = 11 THEN 11 
    WHEN apt_sup_pos/1000 = 26 THEN 26
    WHEN apt_sup_pos/1000 = 27 THEN 27
    WHEN apt_sup_pos/1000 = 28 THEN 28
    WHEN apt_sup_pos/1000 = 29 THEN 29
    WHEN apt_sup_pos/1000 = 30 THEN 30
    WHEN apt_sup_pos/1000 = 31 THEN 31
    WHEN apt_sup_pos/1000 = 36 THEN 36
    WHEN apt_sup_pos/1000 = 41 THEN 41
    WHEN apt_sup_pos/1000 = 42 THEN 42
    WHEN apt_sup_pos/1000 = 43 THEN 43
    WHEN apt_sup_pos/1000 = 44 THEN 44
    WHEN apt_sup_pos/1000 = 45 THEN 45
    WHEN apt_sup_pos/1000 = 46 THEN 46
    WHEN apt_sup_pos/1000 = 47 THEN 47
    WHEN apt_sup_pos/1000 = 48 THEN 48
    WHEN apt_sup_pos/1000 = 50 THEN 50
  	ELSE 'Other' 
  END AS LocalCode
FROM tb_riskdata risk
JOIN tb_bjdong bjdong
ON risk.apt_sup_pos = bjdong.bjdong_code
WHERE rec_anno_date >= 20220526 AND rec_anno_date <= 20230526
)AS inner_data
ON tb_riskdata.data_idx = inner_data.data_idx
WHERE (LocalCode = 11 OR LocalCode = 26 OR LocalCode = 27 OR LocalCode = 28) 
--AND inner_data.apt_name LIKE '%서울%' -- 단지명
--AND inner_data.apt_number LIKE '%123%' --주택 관리 번호
--AND inner_data.location LIKE '%의정부%' -- 지역 
--AND inner_data.unauth_rf_per50 = 1 AND inner_data.unauth_xgb_per50 = 1 --부정청약 xg rf
--AND inner_data.fake_rf_per50 = 1 AND inner_data.unauth_xgb_per50 = 1-- 위장전입 xg rf 
--AND inner_data.sale_rf_per50 = 1 AND inner_data.sale_xgb_per50 = 1 -- 매매 xg rf
order by inner_data.rec_anno_date asc , inner_data.data_idx asc, inner_data.LocalCode asc;



-- 주택청약 시장관리
/*
 * 2023-05-16 김영훈 주택청약 시장관리 실시 동록
 * */
SELECT
	*,
	CASE WHEN inner_data.inspection_status = 0 THEN '미점검' ELSE '점검완료'  END as '점검여부',
	CASE WHEN inner_data.unauth_sub_decision = 0 THEN '통과' ELSE '적발'  END as '부정청약결과',
    inner_data.unauth_rf_prob as '부정청약rf',
    inner_data.unauth_xgb_prob as '부정청약xg',
    inner_data.unauth_everage as '부정청약평균',
    inner_data.apt_number as '주택관리번호',
	inner_data.apt_type as '민영국민',
    inner_data.apt_name as '단지명',
    inner_data.apt_dong as '동',
    inner_data.apt_hosu as '호수',
    inner_data.user_name as '성명',
    inner_data.user_birth as '생년월일',
    inner_data.user_age as '연령',
    inner_data.apt_sup_type as '공급유형',
    inner_data.apt_sp_sup as '세부유형타입',
    inner_data.user_relationship as '세대주관계',
    inner_data.user_change_date as '변경일자',
    inner_data.user_spouse as '배우자',
    inner_data.user_housemember as '세대원수',
    inner_data.user_hp_housemember as '분리세대',
    inner_data.user_phone_dup as '폰중복',
    inner_data.user_ip_dub_win as 'ip중복당첨',
    inner_data.user_ip_dub_app as 'ip중복신청',
    inner_data.app_residence_class as '신청거주구분',
    inner_data.win_residence_class as '당첨거주구분',
    inner_data.user_addr_match as '주소일치여부',
    inner_data.user_admin_address as '등본주소',
    inner_data.app_address as '입력주소',
    inner_data.user_recom_type as '기관추천',
    inner_data.change_date as '업로드일시'
FROM tb_riskdata 
INNER JOIN (
SELECT 
 (unauth_rf_prob + unauth_xgb_prob) / 2 AS unauth_everage,
 	bjdong.bjdong_name AS location,
   *,
  CASE 
    WHEN apt_sup_pos/1000 = 11 THEN 11 
    WHEN apt_sup_pos/1000 = 26 THEN 26
    WHEN apt_sup_pos/1000 = 27 THEN 27
    WHEN apt_sup_pos/1000 = 28 THEN 28
    WHEN apt_sup_pos/1000 = 29 THEN 29
    WHEN apt_sup_pos/1000 = 30 THEN 30
    WHEN apt_sup_pos/1000 = 31 THEN 31
    WHEN apt_sup_pos/1000 = 36 THEN 36
    WHEN apt_sup_pos/1000 = 41 THEN 41
    WHEN apt_sup_pos/1000 = 42 THEN 42
    WHEN apt_sup_pos/1000 = 43 THEN 43
    WHEN apt_sup_pos/1000 = 44 THEN 44
    WHEN apt_sup_pos/1000 = 45 THEN 45
    WHEN apt_sup_pos/1000 = 46 THEN 46
    WHEN apt_sup_pos/1000 = 47 THEN 47
    WHEN apt_sup_pos/1000 = 48 THEN 48
    WHEN apt_sup_pos/1000 = 50 THEN 50
  	ELSE 'Other' 
  END AS LocalCode
FROM tb_riskdata risk
JOIN tb_bjdong bjdong
ON risk.apt_sup_pos = bjdong.bjdong_code
WHERE rec_anno_date >= 20200000 AND rec_anno_date <= 20240000
)AS inner_data
ON tb_riskdata.data_idx = inner_data.data_idx
WHERE (LocalCode = 11 OR LocalCode = 26 OR LocalCode = 27 OR LocalCode = 28) 
--AND inner_data.apt_name LIKE '%서울%' -- 단지명
--AND inner_data.apt_number LIKE '%123%' --주택 관리 번호
--AND inner_data.location LIKE '%의정부%' -- 지역 
--AND inner_data.unauth_rf_per50 = 1 AND inner_data.unauth_xgb_per50 = 1 --부정청약 xg rf
--AND inner_data.unauth_rf_per25 = 1 AND inner_data.unauth_xgb_per25 = 1 --부정청약 xg rf
--AND inner_data.unauth_rf_per10= 1 AND inner_data.unauth_xgb_per10 = 1 --부정청약 xg rf
order by inner_data.rec_anno_date asc , inner_data.data_idx asc;


--이력관리 - 접속 이력
SELECT *, ROW_NUMBER() OVER (ORDER BY create_date desc) AS 'NO' FROM tb_connect_log 
WHERE create_date >= '2020-05-16 12:00:00' AND create_date <= '2024-05-16 12:00:00'
AND mem_userid LIKE '%asd%' AND mem_name LIKE '%asd%' AND mem_depart LIKE '%ASD%'
ORDER BY create_date desc , con_log_idx asc;

--로그인시 이력 추가
insert into tb_connect_log(mem_userid, mem_name, mem_depart, mem_title, create_date) values('asd', 'asd', 'asd','asd', strftime('%Y-%m-%d %H:%M:%S', 'now', 'localtime'));


--이력관리 - 변경 이력
SELECT * FROM tb_change_log 
WHERE create_date >= '2020-05-16 12:00:00' AND create_date <= '2024-05-16 12:00:00'
AND change_name LIKE '%asd%' AND change_menuname LIKE '%asd%' AND change_contents LIKE '%ASD%'
ORDER BY create_date ASC , change_log_idx ASC;

--이력관리 - 다운로드 이력
SELECT * FROM tb_down_log
WHERE create_date >= '2020-05-16 12:00:00' AND create_date <= '2024-05-16 12:00:00'
AND down_name LIKE '%asd%' AND down_menu LIKE '%asd%' AND down_contents LIKE '%ASD%'
ORDER BY create_date ASC , down_log_idx ASC;


--계정 관리
SELECT * FROM tb_member
WHERE create_date >= '2020-05-16 12:00:00' AND create_date <= '2024-05-16 12:00:00'
AND mem_userid LIKE '%asd%' AND mem_name LIKE '%asd%' AND mem_depart LIKE '%ASD%'
ORDER BY create_date ASC , mem_idx ASC;

--시퀀스 초기화
INSERT INTO tb_member(mem_auth_id, mem_status) VALUES('일반관리자', 1);
UPDATE SQLITE_SEQUENCE SET seq = 0 WHERE name = 'tb_member';
DELETE FROM tb_member WHERE mem_idx = 3;

--계정추가
select * from tb_member;
SELECT MAX(mem_idx) as '마지막값' FROM tb_member;
INSERT INTO tb_member(mem_auth_id, mem_status, mem_userid) VALUES('일반관리자', 1, 'admin01');

--비밀번호 초기화
UPDATE tb_member
SET mem_password = '12'
WHERE mem_userid = 'admin3';

--계정 중지
UPDATE tb_member 
SET mem_status = 2
WHERE mem_userid = 'admin01';


--대시보드 위험도 지도 (최근 1년 평균) + 대시보드 부정청약 위험도 순위
SELECT 
    inner_data.LocalCode as '지역',
	AVG(inner_data.unauth_everage) * 100 AS everage
FROM tb_riskdata
INNER JOIN (
SELECT 
   *,
   (unauth_rf_prob + unauth_xgb_prob) / 2 AS unauth_everage,
  CASE 
    WHEN apt_sup_pos/1000 = 11 THEN 11 
    WHEN apt_sup_pos/1000 = 26 THEN 26
    WHEN apt_sup_pos/1000 = 27 THEN 27
    WHEN apt_sup_pos/1000 = 28 THEN 28
    WHEN apt_sup_pos/1000 = 29 THEN 29
    WHEN apt_sup_pos/1000 = 30 THEN 30
    WHEN apt_sup_pos/1000 = 31 THEN 31
    WHEN apt_sup_pos/1000 = 36 THEN 36
    WHEN apt_sup_pos/1000 = 41 THEN 41
    WHEN apt_sup_pos/1000 = 42 THEN 42
    WHEN apt_sup_pos/1000 = 43 THEN 43
    WHEN apt_sup_pos/1000 = 44 THEN 44
    WHEN apt_sup_pos/1000 = 45 THEN 45
    WHEN apt_sup_pos/1000 = 46 THEN 46
    WHEN apt_sup_pos/1000 = 47 THEN 47
    WHEN apt_sup_pos/1000 = 48 THEN 48
    WHEN apt_sup_pos/1000 = 50 THEN 50
  	ELSE 'Other' 
  END AS LocalCode
FROM tb_riskdata
WHERE change_date >= strftime('%Y%m%d', datetime('now','localtime','-1 year')) AND change_date  <= strftime('%Y%m%d', 'now','localtime')
)AS inner_data
ON tb_riskdata.data_idx = inner_data.data_idx
WHERE LocalCode = 11 OR LocalCode = 26 OR LocalCode = 27 OR LocalCode = 28
GROUP BY inner_data.LocalCode
order by everage desc;

select strftime('%Y%m%d', 'now','localtime');

SELECT AVG(unauth_everage) * 100 AS '전체'
FROM (
  SELECT *,
  	(unauth_rf_prob + unauth_xgb_prob) / 2 AS unauth_everage,
    ROW_NUMBER() OVER (ORDER BY (unauth_rf_prob + unauth_xgb_prob) / 2 ASC) AS rank,
    COUNT(*) OVER () AS total_rows,
      CASE 
    WHEN apt_sup_pos/1000 = 11 THEN 11 
    WHEN apt_sup_pos/1000 = 26 THEN 26
    WHEN apt_sup_pos/1000 = 27 THEN 27
    WHEN apt_sup_pos/1000 = 28 THEN 28
    WHEN apt_sup_pos/1000 = 29 THEN 29
    WHEN apt_sup_pos/1000 = 30 THEN 30
    WHEN apt_sup_pos/1000 = 31 THEN 31
    WHEN apt_sup_pos/1000 = 36 THEN 36
    WHEN apt_sup_pos/1000 = 41 THEN 41
    WHEN apt_sup_pos/1000 = 42 THEN 42
    WHEN apt_sup_pos/1000 = 43 THEN 43
    WHEN apt_sup_pos/1000 = 44 THEN 44
    WHEN apt_sup_pos/1000 = 45 THEN 45
    WHEN apt_sup_pos/1000 = 46 THEN 46
    WHEN apt_sup_pos/1000 = 47 THEN 47
    WHEN apt_sup_pos/1000 = 48 THEN 48
    WHEN apt_sup_pos/1000 = 50 THEN 50
  	ELSE 'Other' 
  END AS LocalCode
  FROM tb_riskdata
  WHERE change_date >= strftime('%Y%m%d', datetime('now','localtime','-1 year')) AND change_date  <= strftime('%Y%m%d', 'now','localtime') 
  --AND LocalCode = 11
) ranked;

-- 대시보드 부정청약 위험도 퍼센트
SELECT AVG(unauth_everage) * 100 AS '전체'
FROM (
  SELECT *,
  	(unauth_rf_prob + unauth_xgb_prob) / 2 AS unauth_everage,
    ROW_NUMBER() OVER (ORDER BY (unauth_rf_prob + unauth_xgb_prob) / 2 ASC) AS rank,
    COUNT(*) OVER () AS total_rows,
      CASE 
    WHEN apt_sup_pos/1000 = 11 THEN 11 
    WHEN apt_sup_pos/1000 = 26 THEN 26
    WHEN apt_sup_pos/1000 = 27 THEN 27
    WHEN apt_sup_pos/1000 = 28 THEN 28
    WHEN apt_sup_pos/1000 = 29 THEN 29
    WHEN apt_sup_pos/1000 = 30 THEN 30
    WHEN apt_sup_pos/1000 = 31 THEN 31
    WHEN apt_sup_pos/1000 = 36 THEN 36
    WHEN apt_sup_pos/1000 = 41 THEN 41
    WHEN apt_sup_pos/1000 = 42 THEN 42
    WHEN apt_sup_pos/1000 = 43 THEN 43
    WHEN apt_sup_pos/1000 = 44 THEN 44
    WHEN apt_sup_pos/1000 = 45 THEN 45
    WHEN apt_sup_pos/1000 = 46 THEN 46
    WHEN apt_sup_pos/1000 = 47 THEN 47
    WHEN apt_sup_pos/1000 = 48 THEN 48
    WHEN apt_sup_pos/1000 = 50 THEN 50
  	ELSE 'Other' 
  END AS LocalCode
  FROM tb_riskdata
  WHERE change_date >= strftime('%Y%m%d', datetime('now','localtime','-1 year')) AND change_date  <= strftime('%Y%m%d', 'now','localtime') 
  --AND LocalCode = 11
) ranked
UNION ALL
SELECT 
	AVG(unauth_everage) * 100 AS '상위'
FROM (
  SELECT *,
  	(unauth_rf_prob + unauth_xgb_prob) / 2 AS unauth_everage,
    ROW_NUMBER() OVER (ORDER BY (unauth_rf_prob + unauth_xgb_prob) / 2 DESC) AS rank,
    COUNT(*) OVER () AS total_rows,
      CASE 
    WHEN apt_sup_pos/1000 = 11 THEN 11 
    WHEN apt_sup_pos/1000 = 26 THEN 26
    WHEN apt_sup_pos/1000 = 27 THEN 27
    WHEN apt_sup_pos/1000 = 28 THEN 28
    WHEN apt_sup_pos/1000 = 29 THEN 29
    WHEN apt_sup_pos/1000 = 30 THEN 30
    WHEN apt_sup_pos/1000 = 31 THEN 31
    WHEN apt_sup_pos/1000 = 36 THEN 36
    WHEN apt_sup_pos/1000 = 41 THEN 41
    WHEN apt_sup_pos/1000 = 42 THEN 42
    WHEN apt_sup_pos/1000 = 43 THEN 43
    WHEN apt_sup_pos/1000 = 44 THEN 44
    WHEN apt_sup_pos/1000 = 45 THEN 45
    WHEN apt_sup_pos/1000 = 46 THEN 46
    WHEN apt_sup_pos/1000 = 47 THEN 47
    WHEN apt_sup_pos/1000 = 48 THEN 48
    WHEN apt_sup_pos/1000 = 50 THEN 50
  	ELSE 'Other' 
  END AS LocalCode
  FROM tb_riskdata
  WHERE change_date >= strftime('%Y%m%d', datetime('now','localtime','-1 year')) AND change_date  <= strftime('%Y%m%d', 'now','localtime')
) ranked
WHERE rank <= (total_rows * 0.05)
UNION ALL
SELECT AVG(unauth_everage) * 100 AS '상위10'
FROM (
  SELECT *,
  	(unauth_rf_prob + unauth_xgb_prob) / 2 AS unauth_everage,
    ROW_NUMBER() OVER (ORDER BY (unauth_rf_prob + unauth_xgb_prob) / 2 DESC) AS rank,
    COUNT(*) OVER () AS total_rows,
      CASE 
    WHEN apt_sup_pos/1000 = 11 THEN 11 
    WHEN apt_sup_pos/1000 = 26 THEN 26
    WHEN apt_sup_pos/1000 = 27 THEN 27
    WHEN apt_sup_pos/1000 = 28 THEN 28
    WHEN apt_sup_pos/1000 = 29 THEN 29
    WHEN apt_sup_pos/1000 = 30 THEN 30
    WHEN apt_sup_pos/1000 = 31 THEN 31
    WHEN apt_sup_pos/1000 = 36 THEN 36
    WHEN apt_sup_pos/1000 = 41 THEN 41
    WHEN apt_sup_pos/1000 = 42 THEN 42
    WHEN apt_sup_pos/1000 = 43 THEN 43
    WHEN apt_sup_pos/1000 = 44 THEN 44
    WHEN apt_sup_pos/1000 = 45 THEN 45
    WHEN apt_sup_pos/1000 = 46 THEN 46
    WHEN apt_sup_pos/1000 = 47 THEN 47
    WHEN apt_sup_pos/1000 = 48 THEN 48
    WHEN apt_sup_pos/1000 = 50 THEN 50
  	ELSE 'Other' 
  END AS LocalCode
  FROM tb_riskdata
  WHERE change_date >= strftime('%Y%m%d', datetime('now','localtime','-1 year')) AND change_date  <= strftime('%Y%m%d', 'now','localtime')
  AND LocalCode = 11
) ranked
WHERE rank <= (total_rows * 0.10)
UNION ALL
SELECT AVG(unauth_everage) * 100 AS '상위20'
FROM (
  SELECT *,
  	(unauth_rf_prob + unauth_xgb_prob) / 2 AS unauth_everage,
    ROW_NUMBER() OVER (ORDER BY (unauth_rf_prob + unauth_xgb_prob) / 2 DESC) AS rank,
    COUNT(*) OVER () AS total_rows,
      CASE 
    WHEN apt_sup_pos/1000 = 11 THEN 11 
    WHEN apt_sup_pos/1000 = 26 THEN 26
    WHEN apt_sup_pos/1000 = 27 THEN 27
    WHEN apt_sup_pos/1000 = 28 THEN 28
    WHEN apt_sup_pos/1000 = 29 THEN 29
    WHEN apt_sup_pos/1000 = 30 THEN 30
    WHEN apt_sup_pos/1000 = 31 THEN 31
    WHEN apt_sup_pos/1000 = 36 THEN 36
    WHEN apt_sup_pos/1000 = 41 THEN 41
    WHEN apt_sup_pos/1000 = 42 THEN 42
    WHEN apt_sup_pos/1000 = 43 THEN 43
    WHEN apt_sup_pos/1000 = 44 THEN 44
    WHEN apt_sup_pos/1000 = 45 THEN 45
    WHEN apt_sup_pos/1000 = 46 THEN 46
    WHEN apt_sup_pos/1000 = 47 THEN 47
    WHEN apt_sup_pos/1000 = 48 THEN 48
    WHEN apt_sup_pos/1000 = 50 THEN 50
  	ELSE 'Other' 
  END AS LocalCode
  FROM tb_riskdata
  WHERE change_date >= strftime('%Y%m%d', datetime('now','localtime','-1 year')) AND change_date  <= strftime('%Y%m%d', 'now','localtime')
  AND LocalCode = 11
) ranked
WHERE rank <= (total_rows * 0.20);


-- 대시보드 위험도 지도 지역구
SELECT 
    inner_data.bjdong_name as '지역',
	AVG(inner_data.unauth_everage) * 100 AS everage
FROM tb_riskdata
INNER JOIN (
SELECT 
   *,
   (unauth_rf_prob + unauth_xgb_prob) / 2 AS unauth_everage,
  CASE 
    WHEN apt_sup_pos/1000 = 11 THEN 11 
    WHEN apt_sup_pos/1000 = 26 THEN 26
    WHEN apt_sup_pos/1000 = 27 THEN 27
    WHEN apt_sup_pos/1000 = 28 THEN 28
    WHEN apt_sup_pos/1000 = 29 THEN 29
    WHEN apt_sup_pos/1000 = 30 THEN 30
    WHEN apt_sup_pos/1000 = 31 THEN 31
    WHEN apt_sup_pos/1000 = 36 THEN 36
    WHEN apt_sup_pos/1000 = 41 THEN 41
    WHEN apt_sup_pos/1000 = 42 THEN 42
    WHEN apt_sup_pos/1000 = 43 THEN 43
    WHEN apt_sup_pos/1000 = 44 THEN 44
    WHEN apt_sup_pos/1000 = 45 THEN 45
    WHEN apt_sup_pos/1000 = 46 THEN 46
    WHEN apt_sup_pos/1000 = 47 THEN 47
    WHEN apt_sup_pos/1000 = 48 THEN 48
    WHEN apt_sup_pos/1000 = 50 THEN 50
  	ELSE 'Other' 
  END AS LocalCode
FROM tb_riskdata risk
JOIN tb_bjdong bjdong
ON risk.apt_sup_pos = bjdong.bjdong_code
WHERE risk.change_date >= strftime('%Y%m%d', datetime('now','localtime','-1 year')) AND risk.change_date  <= strftime('%Y%m%d', 'now','localtime')
--WHERE rec_anno_date >= 20200000 AND rec_anno_date <= 20240000
)AS inner_data
ON tb_riskdata.data_idx = inner_data.data_idx
WHERE LocalCode = 41
GROUP BY inner_data.apt_sup_pos / 10
order by everage desc;


   
-- 그래프- 위험도 지도 전체(모집공고일)
SELECT 
    inner_data.LocalCode as '지역',
    AVG(inner_data.unauth_everage) * 100 AS everage,
	SUM(inner_data.unauth_sub_decision) AS '적발수',
	SUM(inner_data.unauth_disguise_transfer) + SUM(inner_data.unauth_savecerti_sale) + SUM(inner_data.unauth_other) as '전체',
	SUM(inner_data.unauth_disguise_transfer) as '위장전입',
	SUM(inner_data.unauth_savecerti_sale) as '통장매매',
	SUM(inner_data.unauth_other) as '기타'
FROM tb_riskdata
INNER JOIN (
SELECT 
   *,
   (unauth_rf_prob + unauth_xgb_prob) / 2 AS unauth_everage,
  CASE 
    WHEN apt_sup_pos/1000 = 11 THEN 11 
    WHEN apt_sup_pos/1000 = 26 THEN 26
    WHEN apt_sup_pos/1000 = 27 THEN 27
    WHEN apt_sup_pos/1000 = 28 THEN 28
    WHEN apt_sup_pos/1000 = 29 THEN 29
    WHEN apt_sup_pos/1000 = 30 THEN 30
    WHEN apt_sup_pos/1000 = 31 THEN 31
    WHEN apt_sup_pos/1000 = 36 THEN 36
    WHEN apt_sup_pos/1000 = 41 THEN 41
    WHEN apt_sup_pos/1000 = 42 THEN 42
    WHEN apt_sup_pos/1000 = 43 THEN 43
    WHEN apt_sup_pos/1000 = 44 THEN 44
    WHEN apt_sup_pos/1000 = 45 THEN 45
    WHEN apt_sup_pos/1000 = 46 THEN 46
    WHEN apt_sup_pos/1000 = 47 THEN 47
    WHEN apt_sup_pos/1000 = 48 THEN 48
    WHEN apt_sup_pos/1000 = 50 THEN 50
  	ELSE 'Other' 
  END AS LocalCode
FROM tb_riskdata
WHERE rec_anno_date >= 20200000 AND rec_anno_date <= 20240000
order by data_idx
)AS inner_data
ON tb_riskdata.data_idx = inner_data.data_idx
WHERE LocalCode = 11 OR LocalCode = 26 OR LocalCode = 27 OR LocalCode = 28
GROUP BY inner_data.LocalCode
order by everage DESC;




SELECT
	*,
	ROW_NUMBER () OVER ( 
        ORDER BY inner_data.rec_anno_date asc
    ) as '번호',
    CASE WHEN SUM(inner_data.inspection_status_count) > 0 THEN 'N' ELSE 'Y'  END as '점검여부',
	inner_data.apt_number as '주택관리번호',
	inner_data.apt_type as '민영국민',
    inner_data.apt_name as '단지명',
    inner_data.location as '지역',
    inner_data.user_housemember as '공급세대수',
    inner_data.total_suphouse_number as '총공급가구수',
    inner_data.unauth_rf_prob as '부정청약rf',
    inner_data.unauth_xgb_prob as '부정청약xg',
    inner_data.unauth_everage as '부정청약평균',
    inner_data.rec_anno_date as '모집공고일',
    inner_data.win_anno_date as '당첨자발표일',
    inner_data.limittransfer_date as '전입제한일',
    CASE WHEN inner_data.saleprice_cap_status = 0 THEN 'N' ELSE 'Y'  END as '분양가상한제'
FROM tb_riskdata 
INNER JOIN (
SELECT 
	CASE WHEN inspection_status_result = 'N' THEN 1 ELSE 0 END as inspection_status_count,  
 (unauth_rf_prob + unauth_xgb_prob) / 2 AS unauth_everage,
 (fake_rf_prob + fake_xgb_prob) / 2 AS fake_everage,
 (sale_rf_prob + sale_xgb_prob) / 2 AS sale_everage,
 	bjdong.bjdong_name AS location,
   *,
  CASE 
    WHEN apt_sup_pos/1000 = 11 THEN 11 
    WHEN apt_sup_pos/1000 = 26 THEN 26
    WHEN apt_sup_pos/1000 = 27 THEN 27
    WHEN apt_sup_pos/1000 = 28 THEN 28
    WHEN apt_sup_pos/1000 = 29 THEN 29
    WHEN apt_sup_pos/1000 = 30 THEN 30
    WHEN apt_sup_pos/1000 = 31 THEN 31
    WHEN apt_sup_pos/1000 = 36 THEN 36
    WHEN apt_sup_pos/1000 = 41 THEN 41
    WHEN apt_sup_pos/1000 = 42 THEN 42
    WHEN apt_sup_pos/1000 = 43 THEN 43
    WHEN apt_sup_pos/1000 = 44 THEN 44
    WHEN apt_sup_pos/1000 = 45 THEN 45
    WHEN apt_sup_pos/1000 = 46 THEN 46
    WHEN apt_sup_pos/1000 = 47 THEN 47
    WHEN apt_sup_pos/1000 = 48 THEN 48
    WHEN apt_sup_pos/1000 = 50 THEN 50
  	ELSE 'Other' 
  END AS LocalCode
FROM tb_riskdata risk
JOIN tb_bjdong bjdong
ON risk.apt_sup_pos = bjdong.bjdong_code
WHERE rec_anno_date >= 20220526 AND rec_anno_date <= 20230526
)AS inner_data
ON tb_riskdata.data_idx = inner_data.data_idx
WHERE (LocalCode = 11 OR LocalCode = 26 OR LocalCode = 27 OR LocalCode = 28) 
GROUP BY inner_data.apt_name
order by inner_data.rec_anno_date asc , inner_data.data_idx asc, inner_data.LocalCode asc;

select sum(unauth_xgb_prob) / count(*) from tb_riskdata;

SELECT  AVG(unauth_everage) * 100 AS '전체' 
FROM (   
SELECT *,   	
(unauth_rf_prob + unauth_xgb_prob) / 2 AS unauth_everage,     
ROW_NUMBER() OVER (ORDER BY (unauth_rf_prob + unauth_xgb_prob) / 2 DESC) AS rank,     
COUNT(*) OVER () AS total_rows, CASE  
	WHEN apt_sup_pos/ 1000 = 11 THEN 11 
	WHEN apt_sup_pos/ 1000 = 26 THEN 26 
	WHEN apt_sup_pos/ 1000 = 27 THEN 27 
	WHEN apt_sup_pos/ 1000 = 28 THEN 28 
	WHEN apt_sup_pos/ 1000 = 29 THEN 29 
	WHEN apt_sup_pos/ 1000 = 30 THEN 30 
	WHEN apt_sup_pos/ 1000 = 31 THEN 31 
	WHEN apt_sup_pos/ 1000 = 36 THEN 36 
	WHEN apt_sup_pos/ 1000 = 41 THEN 41 
	WHEN apt_sup_pos/ 1000 = 42 THEN 42 
	WHEN apt_sup_pos/ 1000 = 43 THEN 43 
	WHEN apt_sup_pos/ 1000 = 44 THEN 44 
	WHEN apt_sup_pos/ 1000 = 45 THEN 45 
	WHEN apt_sup_pos/ 1000 = 46 THEN 46 
	WHEN apt_sup_pos/ 1000 = 47 THEN 47 
	WHEN apt_sup_pos/ 1000 = 48 THEN 48 
	WHEN apt_sup_pos/ 1000 = 50 THEN 50 
	ELSE 'Other' 
	END AS LocalCode   
	FROM tb_riskdata 
	WHERE change_date >= strftime('%Y%m%d', datetime('now','localtime','-1 year')) AND change_date  <= strftime('%Y%m%d', 'now','localtime') 
	AND  ( LocalCode = 11 or LocalCode = 26 or LocalCode = 27 or LocalCode = 28 or LocalCode = 29 or LocalCode = 30 or LocalCode = 31 or 
	LocalCode = 36 or LocalCode = 41 or LocalCode = 42 or LocalCode = 43 or LocalCode = 44 or LocalCode = 45 or LocalCode = 46 or LocalCode = 47 
	or LocalCode = 48 or LocalCode = 50 ) 
	) ranked 
	UNION ALL SELECT  AVG(unauth_everage) * 100 AS '상위5' 
	FROM (   
	SELECT *,   	(unauth_rf_prob + unauth_xgb_prob) / 2 AS unauth_everage,    
	ROW_NUMBER() OVER (ORDER BY (unauth_rf_prob + unauth_xgb_prob) / 2 DESC) AS rank,     
	COUNT(*) OVER () AS total_rows, 
	CASE  
		WHEN apt_sup_pos/ 1000 = 11 THEN 11
	WHEN apt_sup_pos/ 1000 = 26 THEN 26 
	WHEN apt_sup_pos/ 1000 = 27 THEN 27 
	WHEN apt_sup_pos/ 1000 = 28 THEN 28 
	WHEN apt_sup_pos/ 1000 = 29 THEN 29 
	WHEN apt_sup_pos/ 1000 = 30 THEN 30 
	WHEN apt_sup_pos/ 1000 = 31 THEN 31 
	WHEN apt_sup_pos/ 1000 = 36 THEN 36 
	WHEN apt_sup_pos/ 1000 = 41 THEN 41 
	WHEN apt_sup_pos/ 1000 = 42 THEN 42 
	WHEN apt_sup_pos/ 1000 = 43 THEN 43 
	WHEN apt_sup_pos/ 1000 = 44 THEN 44 
	WHEN apt_sup_pos/ 1000 = 45 THEN 45 
	WHEN apt_sup_pos/ 1000 = 46 THEN 46 
	WHEN apt_sup_pos/ 1000 = 47 THEN 47 
	WHEN apt_sup_pos/ 1000 = 48 THEN 48 
	WHEN apt_sup_pos/ 1000 = 50 THEN 50 
	ELSE 'Other' END AS LocalCode   
	FROM tb_riskdata 
	WHERE change_date >= strftime('%Y%m%d', datetime('now','localtime','-1 year')) 
	AND change_date  <= strftime('%Y%m%d', 'now','localtime') 
	AND  ( LocalCode = 11 or LocalCode = 26 or LocalCode = 27 or LocalCode = 28 or LocalCode = 29 or LocalCode = 30 or 
	LocalCode = 31 or LocalCode = 36 or LocalCode = 41 or LocalCode = 42 or LocalCode = 43 or LocalCode = 44 or 
	LocalCode = 45 or LocalCode = 46 or LocalCode = 47 or LocalCode = 48
	or LocalCode = 50 ) 
	) ranked 
	WHERE rank <= (total_rows * 0.05) UNION ALL SELECT  AVG(unauth_everage) * 100 AS '상위10' 
	FROM (   
	SELECT *,   	(unauth_rf_prob + unauth_xgb_prob) / 2 AS unauth_everage,     
	ROW_NUMBER() OVER (ORDER BY (unauth_rf_prob + unauth_xgb_prob) / 2 DESC) AS rank,     
	COUNT(*) OVER () AS total_rows, 
	CASE  WHEN apt_sup_pos/ 1000 = 11 THEN 11 
	WHEN apt_sup_pos/ 1000 = 26 THEN 26 
	WHEN apt_sup_pos/ 1000 = 27 THEN 27 
	WHEN apt_sup_pos/ 1000 = 28 THEN 28 
	WHEN apt_sup_pos/ 1000 = 29 THEN 29 
	WHEN apt_sup_pos/ 1000 = 30  THEN 30 
	WHEN apt_sup_pos/ 1000 = 31 THEN 31 
	WHEN apt_sup_pos/ 1000 = 36 THEN 36 
	WHEN apt_sup_pos/ 1000 = 41 THEN 41 
	WHEN apt_sup_pos/ 1000 = 42 THEN 42 
	WHEN apt_sup_pos/ 1000 = 43 THEN 43 
	WHEN apt_sup_pos/ 1000 = 44 THEN 44 
	WHEN apt_sup_pos/ 1000 = 45 THEN 45 
	WHEN apt_sup_pos/ 1000 = 46 THEN 46 
	WHEN apt_sup_pos/ 1000 = 47 THEN 47 
	WHEN apt_sup_pos/ 1000 = 48 
	THEN 48 WHEN apt_sup_pos/ 1000 = 50 THEN 50 
	ELSE 'Other' 
	END AS LocalCode   
	FROM tb_riskdata 
	WHERE change_date >= strftime('%Y%m%d', datetime('now','localtime','-1 year')) 
	AND change_date  <= strftime('%Y%m%d', 'now','localtime') AND  ( LocalCode = 11 or LocalCode = 26 or LocalCode = 27 or LocalCode = 28 or LocalCode = 29 or LocalCode = 30 
	or LocalCode = 31 or LocalCode = 36 or LocalCode = 41 or LocalCode = 42 or LocalCode = 43 or LocalCode = 44 or LocalCode = 45 or LocalCode = 46 or LocalCode = 47 or LocalCode = 48 
	or LocalCode = 50 ) ) ranked WHERE rank <= (total_rows * 0.1) UNION ALL SELECT  AVG(unauth_everage) * 100 AS '상위20' 
	FROM (   SELECT *,   	(unauth_rf_prob + unauth_xgb_prob) / 2 AS unauth_everage,     ROW_NUMBER() OVER (ORDER BY (unauth_rf_prob + unauth_xgb_prob) / 2 DESC) AS rank,    
	COUNT(*) OVER () AS total_rows, CASE  WHEN apt_sup_pos/ 1000 = 11 THEN 11 WHEN apt_sup_pos/ 1000 = 26 THEN 26 WHEN apt_sup_pos/ 1000 = 27 THEN 27 WHEN apt_sup_pos/ 1000 = 28 THEN 28
	WHEN apt_sup_pos/ 1000 = 29 THEN 29 WHEN apt_sup_pos/ 1000 = 30 THEN 30 WHEN apt_sup_pos/ 1000 = 31 THEN 31 WHEN apt_sup_pos/ 1000 = 36 THEN 36 WHEN apt_sup_pos/ 1000 = 41 THEN 41
	WHEN apt_sup_pos/ 1000 = 42 THEN 42 WHEN apt_sup_pos/ 1000 = 43 THEN 43 WHEN apt_sup_pos/ 1000 = 44 THEN 44 WHEN apt_sup_pos/ 1000 = 45 THEN 45 WHEN apt_sup_pos/ 1000 = 46 THEN 46
	WHEN apt_sup_pos/ 1000 = 47 THEN 47 WHEN apt_sup_pos/ 1000 = 48 THEN 48 WHEN apt_sup_pos/ 1000 = 50 THEN 50 ELSE 'Other' END AS LocalCode   FROM tb_riskdata
	WHERE change_date >= strftime('%Y%m%d', datetime('now','localtime','-1 year')) AND change_date  <= strftime('%Y%m%d', 'now','localtime')
	AND  ( LocalCode = 11 or LocalCode = 26 or LocalCode = 27 or LocalCode = 28 or LocalCode = 29 or LocalCode = 30 or LocalCode = 31 or LocalCode = 36 or LocalCode = 41 
	or LocalCode = 42 or LocalCode = 43 or LocalCode = 44 or LocalCode = 45 or LocalCode = 46 or LocalCode = 47 or LocalCode = 48 or LocalCode = 50 ) ) ranked 
	WHERE rank <= (total_rows * 0.2);
	

SELECT  AVG(unauth_everage) * 100 AS '상위10' 
	FROM (   
	SELECT *,   	(unauth_rf_prob + unauth_xgb_prob) / 2 AS unauth_everage,     
	ROW_NUMBER() OVER (ORDER BY (unauth_rf_prob + unauth_xgb_prob) / 2 DESC) AS rank,     
	COUNT(*) OVER () AS total_rows, 
	CASE  WHEN apt_sup_pos/ 1000 = 11 THEN 11 
	WHEN apt_sup_pos/ 1000 = 26 THEN 26 
	WHEN apt_sup_pos/ 1000 = 27 THEN 27 
	WHEN apt_sup_pos/ 1000 = 28 THEN 28 
	WHEN apt_sup_pos/ 1000 = 29 THEN 29 
	WHEN apt_sup_pos/ 1000 = 30  THEN 30 
	WHEN apt_sup_pos/ 1000 = 31 THEN 31 
	WHEN apt_sup_pos/ 1000 = 36 THEN 36 
	WHEN apt_sup_pos/ 1000 = 41 THEN 41 
	WHEN apt_sup_pos/ 1000 = 42 THEN 42 
	WHEN apt_sup_pos/ 1000 = 43 THEN 43 
	WHEN apt_sup_pos/ 1000 = 44 THEN 44 
	WHEN apt_sup_pos/ 1000 = 45 THEN 45 
	WHEN apt_sup_pos/ 1000 = 46 THEN 46 
	WHEN apt_sup_pos/ 1000 = 47 THEN 47 
	WHEN apt_sup_pos/ 1000 = 48 
	THEN 48 WHEN apt_sup_pos/ 1000 = 50 THEN 50 
	ELSE 'Other' 
	END AS LocalCode   
	FROM tb_riskdata 
	WHERE change_date >= strftime('%Y%m%d', datetime('now','localtime','-1 year')) 
	AND change_date  <= strftime('%Y%m%d', 'now','localtime') 
	 ORDER BY (unauth_rf_prob + unauth_xgb_prob) / 2 DESC
	) ranked WHERE rank <= (total_rows * 0.05);
  
  SELECT 
 		count(*) as total, -- 전체
  		count (*) * 0.05 as high, -- 상위
  		count (*) * 0.1 as middle, -- 중위
  		count (*) * 0.2 as low -- 하위
  FROM tb_riskdata
  WHERE change_date >= strftime('%Y%m%d', datetime('now','localtime','-1 year')) AND change_date  <= strftime('%Y%m%d', 'now','localtime'); 
  
 
 
 -- 위험도 퍼센트 4번 호출하기
 select 
 	 AVG(unauth_everage) * 100 AS '상위5' 
 FROM (
  SELECT *,
  (unauth_rf_prob + unauth_xgb_prob) / 2 AS unauth_everage,
      CASE 
    WHEN apt_sup_pos/1000 = 11 THEN 11 
    WHEN apt_sup_pos/1000 = 26 THEN 26
    WHEN apt_sup_pos/1000 = 27 THEN 27
    WHEN apt_sup_pos/1000 = 28 THEN 28
    WHEN apt_sup_pos/1000 = 29 THEN 29
    WHEN apt_sup_pos/1000 = 30 THEN 30
    WHEN apt_sup_pos/1000 = 31 THEN 31
    WHEN apt_sup_pos/1000 = 36 THEN 36
    WHEN apt_sup_pos/1000 = 41 THEN 41
    WHEN apt_sup_pos/1000 = 42 THEN 42
    WHEN apt_sup_pos/1000 = 43 THEN 43
    WHEN apt_sup_pos/1000 = 44 THEN 44
    WHEN apt_sup_pos/1000 = 45 THEN 45
    WHEN apt_sup_pos/1000 = 46 THEN 46
    WHEN apt_sup_pos/1000 = 47 THEN 47
    WHEN apt_sup_pos/1000 = 48 THEN 48
    WHEN apt_sup_pos/1000 = 50 THEN 50
  	ELSE 'Other' 
  END AS LocalCode
  FROM tb_riskdata
  WHERE change_date >= '2022-05-31 07:32:50' AND change_date  <= '2023-05-31 07:32:50'
  ORDER BY unauth_everage DESC
  limit 66483
  );
  
 
 
 select * from tb_riskdata;
 -- 마지막 riskdata 테이블 날짜 가져오기
select datetime(max(create_date) , '-1 year') as '시작시간',  max(create_date) as '끝시간' from tb_riskdata;
 select strftime('%Y-%m-%d %H:%M:%S', max(create_date), 'localtime') from tb_riskdata;


SELECT  AVG(unauth_everage) * 100 AS '전체' FROM (   SELECT *,   	(unauth_rf_prob + unauth_xgb_prob) / 2 AS unauth_everage, CASE  WHEN apt_sup_pos/ 1000 = 11 THEN 11 WHEN apt_sup_pos/ 1000 = 26 THEN 26 WHEN apt_sup_pos/ 1000 = 27 THEN 27 WHEN apt_sup_pos/ 1000 = 28 THEN 28 WHEN apt_sup_pos/ 1000 = 29 THEN 29 WHEN apt_sup_pos/ 1000 = 30 THEN 30 WHEN apt_sup_pos/ 1000 = 31 THEN 31 WHEN apt_sup_pos/ 1000 = 36 THEN 36 WHEN apt_sup_pos/ 1000 = 41 THEN 41 WHEN apt_sup_pos/ 1000 = 42 THEN 42 WHEN apt_sup_pos/ 1000 = 43 THEN 43 WHEN apt_sup_pos/ 1000 = 44 THEN 44 WHEN apt_sup_pos/ 1000 = 45 THEN 45 WHEN apt_sup_pos/ 1000 = 46 THEN 46 WHEN apt_sup_pos/ 1000 = 47 THEN 47 WHEN apt_sup_pos/ 1000 = 48 THEN 48 WHEN apt_sup_pos/ 1000 = 50 THEN 50 ELSE 'Other' END AS LocalCode   FROM tb_riskdata WHERE rec_anno_date >= 20220601 AND rec_anno_date <= 20230601 AND  ( LocalCode = 11 or LocalCode = 26 or LocalCode = 27 or LocalCode = 28 or LocalCode = 29 or LocalCode = 30 or LocalCode = 31 or LocalCode = 36 or LocalCode = 41 or LocalCode = 42 or LocalCode = 43 or LocalCode = 44 or LocalCode = 45 or LocalCode = 46 or LocalCode = 47 or LocalCode = 48 or LocalCode = 50 ) ORDER BY unauth_everage DESC   limit 66483);

SELECT  AVG(unauth_everage) * 100 AS '전체' FROM (   
SELECT *,   	(unauth_rf_prob + unauth_xgb_prob) / 2 AS unauth_everage, CASE  WHEN apt_sup_pos/ 1000 = 11 THEN 11 
WHEN apt_sup_pos/ 1000 = 26 THEN 26 WHEN apt_sup_pos/ 1000 = 27 THEN 27 WHEN apt_sup_pos/ 1000 = 28 THEN 28 
WHEN apt_sup_pos/ 1000 = 29 THEN 29 WHEN apt_sup_pos/ 1000 = 30 THEN 30 WHEN apt_sup_pos/ 1000 = 31 THEN 31 
WHEN apt_sup_pos/ 1000 = 36 THEN 36 WHEN apt_sup_pos/ 1000 = 41 THEN 41 WHEN apt_sup_pos/ 1000 = 42 THEN 42 
WHEN apt_sup_pos/ 1000 = 43 THEN 43 WHEN apt_sup_pos/ 1000 = 44 THEN 44 WHEN apt_sup_pos/ 1000 = 45 THEN 45 
WHEN apt_sup_pos/ 1000 = 46 THEN 46 WHEN apt_sup_pos/ 1000 = 47 THEN 47 WHEN apt_sup_pos/ 1000 = 48 THEN 48 
WHEN apt_sup_pos/ 1000 = 50 THEN 50 ELSE 'Other' END AS LocalCode   
FROM tb_riskdata 
WHERE rec_anno_date >= 20220601 AND rec_anno_date <= 20230601 
AND  ( LocalCode = 11 or LocalCode = 26 or LocalCode = 27 or LocalCode = 28 or LocalCode = 29 or LocalCode = 30 or LocalCode = 31 
or LocalCode = 36 or LocalCode = 41 or LocalCode = 42 or LocalCode = 43 or LocalCode = 44 or LocalCode = 45 or LocalCode = 46 
or LocalCode = 47 or LocalCode = 48 or LocalCode = 50 ) ORDER BY unauth_everage DESC   
limit 3324);

SELECT  AVG(unauth_everage) * 100 AS '전체' FROM (   SELECT *,   	(unauth_rf_prob + unauth_xgb_prob) / 2 AS unauth_everage, CASE  WHEN apt_sup_pos/ 1000 = 11 THEN 11 WHEN apt_sup_pos/ 1000 = 26 THEN 26 WHEN apt_sup_pos/ 1000 = 27 THEN 27 WHEN apt_sup_pos/ 1000 = 28 THEN 28 WHEN apt_sup_pos/ 1000 = 29 THEN 29 WHEN apt_sup_pos/ 1000 = 30 THEN 30 WHEN apt_sup_pos/ 1000 = 31 THEN 31 WHEN apt_sup_pos/ 1000 = 36 THEN 36 WHEN apt_sup_pos/ 1000 = 41 THEN 41 WHEN apt_sup_pos/ 1000 = 42 THEN 42 WHEN apt_sup_pos/ 1000 = 43 THEN 43 WHEN apt_sup_pos/ 1000 = 44 THEN 44 WHEN apt_sup_pos/ 1000 = 45 THEN 45 WHEN apt_sup_pos/ 1000 = 46 THEN 46 WHEN apt_sup_pos/ 1000 = 47 THEN 47 WHEN apt_sup_pos/ 1000 = 48 THEN 48 WHEN apt_sup_pos/ 1000 = 50 THEN 50 ELSE 'Other' END AS LocalCode   FROM tb_riskdata WHERE rec_anno_date >= 20220601 AND rec_anno_date <= 20230601 AND  ( LocalCode = 11 or LocalCode = 26 or LocalCode = 27 or LocalCode = 28 or LocalCode = 29 or LocalCode = 30 or LocalCode = 31 or LocalCode = 36 or LocalCode = 41 or LocalCode = 42 or LocalCode = 43 or LocalCode = 44 or LocalCode = 45 or LocalCode = 46 or LocalCode = 47 or LocalCode = 48 or LocalCode = 50 ) ORDER BY unauth_everage DESC   limit 6648);



-- 단지 선정 예시
SELECT  inner_data.*, 
ROW_NUMBER() OVER (ORDER BY (unauth_rf_prob + unauth_xgb_prob) / 2 DESC) AS rank,     
CASE WHEN SUM(inner_data.inspection_status_count) > 0 THEN 'N' ELSE 'Y'  END as '점검여부',  
inner_data.inspection_status_result as '점검여부', inner_data.apt_number as '주택관리번호', 
inner_data.apt_type as '민영국민', inner_data.apt_name as '단지명', inner_data.location as '지역', 
inner_data.user_housemember as '공급세대수',  inner_data.total_suphouse_number as '총공급가구수',  
SUM(inner_data.unauth_everage) as 'avg', inner_data.rec_anno_date as '모집공고일', 
inner_data.win_anno_date as '당첨자발표일', inner_data.limittransfer_date as '전입제한일', 
CASE WHEN inner_data.saleprice_cap_status = 0 THEN 'N' ELSE 'Y'  END as '분양가상한제'  
FROM tb_riskdata  INNER JOIN( SELECT  CASE WHEN inspection_status_result = 'N' THEN 1 ELSE 0 END as inspection_status_count, 
(unauth_rf_prob + unauth_xgb_prob) / 2 AS unauth_everage, (fake_rf_prob + fake_xgb_prob) / 2 AS fake_everage,
(sale_rf_prob + sale_xgb_prob) / 2 AS sale_everage, bjdong.bjdong_name AS location, *, 
CASE  WHEN apt_sup_pos/ 1000 = 11 THEN 11 WHEN apt_sup_pos/ 1000 = 26 THEN 26 WHEN apt_sup_pos/ 1000 = 27 THEN 27 
WHEN apt_sup_pos/ 1000 = 28 THEN 28 WHEN apt_sup_pos/ 1000 = 29 THEN 29 WHEN apt_sup_pos/ 1000 = 30 THEN 30
WHEN apt_sup_pos/ 1000 = 31 THEN 31 WHEN apt_sup_pos/ 1000 = 36 THEN 36 WHEN apt_sup_pos/ 1000 = 41 THEN 41
WHEN apt_sup_pos/ 1000 = 42 THEN 42 WHEN apt_sup_pos/ 1000 = 43 THEN 43 WHEN apt_sup_pos/ 1000 = 44 THEN 44
WHEN apt_sup_pos/ 1000 = 45 THEN 45 WHEN apt_sup_pos/ 1000 = 46 THEN 46 WHEN apt_sup_pos/ 1000 = 47 THEN 47
WHEN apt_sup_pos/ 1000 = 48 THEN 48 WHEN apt_sup_pos/ 1000 = 50 THEN 50 ELSE 'Other' END AS LocalCode 
FROM tb_riskdata risk JOIN tb_bjdong bjdong ON risk.apt_sup_pos = bjdong.bjdong_code 
WHERE rec_anno_date >= 20220602 AND rec_anno_date <= 20230602 )AS inner_data ON tb_riskdata.data_idx = inner_data.data_idx 
WHERE  ( LocalCode = 11 or LocalCode = 26 or LocalCode = 27 or LocalCode = 28 or LocalCode = 29 or LocalCode = 30 or LocalCode = 31 
or LocalCode = 36 or LocalCode = 41 or LocalCode = 42 or LocalCode = 43 or LocalCode = 44 or LocalCode = 45 or LocalCode = 46 or 
LocalCode = 47 or LocalCode = 48 or LocalCode = 50 )   GROUP BY inner_data.apt_name order by inner_data.rec_anno_date asc ,
inner_data.data_idx asc, inner_data.LocalCode asc;



            " inner_data.*," +
            " inner_data.data_idx as 'NO'," +
            " CASE WHEN SUM(inner_data.inspection_status_count) > 0 THEN 'N' ELSE 'Y'  END as '점검여부', " +
            " inner_data.inspection_status_result as '점검여부'," +
            " inner_data.apt_number as '주택관리번호'," +
            " inner_data.apt_type as '민영국민'," +
            " inner_data.apt_name as '단지명'," +
            " inner_data.location as '지역'," +
            " inner_data.user_housemember as '공급세대수', " +
            " inner_data.total_suphouse_number as '총공급가구수', ";
        query += BustTypeQuery(BustType, RfxgType);
        query += " inner_data.rec_anno_date as '모집공고일'," +
            " inner_data.win_anno_date as '당첨자발표일'," +
            " inner_data.limittransfer_date as '전입제한일'," +
            " CASE WHEN inner_data.saleprice_cap_status = 0 THEN 'N' ELSE 'Y'  END as '분양가상한제'" +
            
            select apt_sup_pos, apt_name from tb_riskdata
            GROUP BY apt_name;
            
           
-- 단지 선정 수정
SELECT 
	ROW_NUMBER() OVER (ORDER BY  inner_data.rec_anno_date DESC , inner_data.LocalCode asc) AS 'NO',
	CASE WHEN SUM(inner_data.inspection_status_count) > 0 THEN 'N' ELSE 'Y'  END as '점검여부',
	inner_data.apt_number as '주택관리번호',
	inner_data.apt_type as '민영국민',
	inner_data.apt_name as '단지명',
	inner_data.location as '지역',
	inner_data.total_suphouse_number as '공급세대수',
	AVG(inner_data.unauth_everage) as '위험도',
	inner_data.total_suphouse_number * 0.1 as '세대수',
	inner_data.rec_anno_date as '모집공고일',
	inner_data.win_anno_date as '당첨자발표일',
	inner_data.limittransfer_date as '전입제한일',
	CASE WHEN inner_data.saleprice_cap_status = 0 THEN 'N' ELSE 'Y'  END as '분양가상한제'
FROM (
	SELECT 
		*,
		ROW_NUMBER() OVER (PARTITION  BY apt_name ORDER BY apt_name ASC, (unauth_rf_prob + unauth_xgb_prob) / 2 DESC) AS rankTest,
		(unauth_rf_prob + unauth_xgb_prob) / 2 AS unauth_everage,
		(fake_rf_prob + fake_xgb_prob) / 2 AS fake_everage,
		(sale_rf_prob + sale_xgb_prob) / 2 AS sale_everage,
		bjdong.bjdong_name AS location,
		CASE WHEN inspection_status_result = 'N' THEN 1 ELSE 0 END as inspection_status_count,
		CASE  WHEN apt_sup_pos/ 1000 = 11 THEN 11 WHEN apt_sup_pos/ 1000 = 26 THEN 26 WHEN apt_sup_pos/ 1000 = 27 THEN 27 
		WHEN apt_sup_pos/ 1000 = 28 THEN 28 WHEN apt_sup_pos/ 1000 = 29 THEN 29 WHEN apt_sup_pos/ 1000 = 30 THEN 30
		WHEN apt_sup_pos/ 1000 = 31 THEN 31 WHEN apt_sup_pos/ 1000 = 36 THEN 36 WHEN apt_sup_pos/ 1000 = 41 THEN 41
		WHEN apt_sup_pos/ 1000 = 42 THEN 42 WHEN apt_sup_pos/ 1000 = 43 THEN 43 WHEN apt_sup_pos/ 1000 = 44 THEN 44
		WHEN apt_sup_pos/ 1000 = 45 THEN 45 WHEN apt_sup_pos/ 1000 = 46 THEN 46 WHEN apt_sup_pos/ 1000 = 47 THEN 47
		WHEN apt_sup_pos/ 1000 = 48 THEN 48 WHEN apt_sup_pos/ 1000 = 50 THEN 50 ELSE 'Other' END AS LocalCode 
	FROM tb_riskdata risk
	JOIN tb_bjdong bjdong
	ON risk.apt_sup_pos = bjdong.bjdong_code
	ORDER BY apt_name ASC, unauth_everage DESC) inner_data
WHERE inner_data.rankTest <= inner_data.total_suphouse_number * 0.1 AND LocalCode = 41
GROUP BY inner_data.apt_name
ORDER BY  inner_data.rec_anno_date DESC , inner_data.LocalCode asc;


SELECT  ROW_NUMBER() OVER (ORDER BY  inner_data.rec_anno_date DESC , inner_data.LocalCode asc ) AS 'NO', 
CASE WHEN SUM(inner_data.inspection_status_count) > 0 THEN 'N' ELSE 'Y'  END as '점검여부', 
inner_data.apt_number as '주택관리번호', inner_data.apt_type as '민영국민', 
inner_data.apt_name as '단지명', inner_data.location as '지역', 
inner_data.total_suphouse_number as '공급세대수',   
AVG(inner_data.unauth_everage) * 1000 as '위험도',  
inner_data.total_suphouse_number * 1 as '세대수',  
inner_data.rec_anno_date as '모집공고일', 
inner_data.win_anno_date as '당첨자발표일', 
inner_data.limittransfer_date as '전입제한일', 
CASE WHEN inner_data.saleprice_cap_status = 0 THEN 'N' ELSE 'Y'  END as '분양가상한제'  
FROM tb_riskdata  
INNER JOIN( SELECT  *,  ROW_NUMBER() OVER (PARTITION  BY apt_name ORDER BY apt_name ASC,
(unauth_rf_prob + unauth_xgb_prob) / 2  DESC) AS rankTest,  
CASE WHEN inspection_status_result = 'N' THEN 1 ELSE 0 END as inspection_status_count,  
(unauth_rf_prob + unauth_xgb_prob) / 2 AS unauth_everage, 
(fake_rf_prob + fake_xgb_prob) / 2 AS fake_everage, 
(sale_rf_prob + sale_xgb_prob) / 2 AS sale_everage,
bjdong.bjdong_name AS location,
CASE  WHEN apt_sup_pos/ 1000 = 11 THEN 11 WHEN apt_sup_pos/ 1000 = 26 THEN 26 
WHEN apt_sup_pos/ 1000 = 27 THEN 27 WHEN apt_sup_pos/ 1000 = 28 THEN 28 
WHEN apt_sup_pos/ 1000 = 29 THEN 29 WHEN apt_sup_pos/ 1000 = 30 THEN 30 
WHEN apt_sup_pos/ 1000 = 31 THEN 31 WHEN apt_sup_pos/ 1000 = 36 THEN 36 
WHEN apt_sup_pos/ 1000 = 41 THEN 41 WHEN apt_sup_pos/ 1000 = 42 THEN 42 
WHEN apt_sup_pos/ 1000 = 43 THEN 43 WHEN apt_sup_pos/ 1000 = 44 THEN 44 
WHEN apt_sup_pos/ 1000 = 45 THEN 45 WHEN apt_sup_pos/ 1000 = 46 THEN 46 
WHEN apt_sup_pos/ 1000 = 47 THEN 47 WHEN apt_sup_pos/ 1000 = 48 THEN 48 
WHEN apt_sup_pos/ 1000 = 50 THEN 50 ELSE 'Other' END AS LocalCode 
FROM tb_riskdata risk JOIN tb_bjdong bjdong 
ON risk.apt_sup_pos = bjdong.bjdong_code 
WHERE rec_anno_date >= 20200602 
AND rec_anno_date <= 20230602 
ORDER BY apt_name ASC,   
(unauth_rf_prob + unauth_xgb_prob) / 2  DESC )AS inner_data ON tb_riskdata.data_idx = inner_data.data_idx 
WHERE  ( LocalCode = 11 or LocalCode = 26 or LocalCode = 27 or LocalCode = 28 or LocalCode = 29 
or LocalCode = 30 or LocalCode = 31 or LocalCode = 36 or LocalCode = 41 or LocalCode = 42
or LocalCode = 43 or LocalCode = 44 or LocalCode = 45 or LocalCode = 46 or LocalCode = 47 
or LocalCode = 48 or LocalCode = 50 )   AND inner_data.rankTest <= inner_data.total_suphouse_number * 1 
GROUP BY inner_data.apt_name ORDER BY  inner_data.rec_anno_date DESC , inner_data.LocalCode asc ;

SELECT  ROW_NUMBER() OVER (ORDER BY  inner_data.rec_anno_date DESC , inner_data.LocalCode asc ) AS 'NO', CASE WHEN SUM(inner_data.inspection_status_count1) > 0 THEN 'N'  WHEN SUM(inner_data.inspection_status_count2) == 0 THEN 'Z' ELSE 'Y'  END as '점검여부',  inner_data.apt_number as '주택관리번호', inner_data.apt_type as '민영국민', inner_data.apt_name as '단지명', inner_data.location as '지역', inner_data.total_suphouse_number as '공급세대수',   AVG(inner_data.unauth_everage) * 100 as '위험도',  inner_data.total_suphouse_number * 1 as '세대수',  inner_data.rec_anno_date as '모집공고일', inner_data.win_anno_date as '당첨자발표일', inner_data.limittransfer_date as '전입제한일', CASE WHEN inner_data.saleprice_cap_status = 0 THEN 'N' ELSE 'Y'  END as '분양가상한제'  FROM ( SELECT  *,  ROW_NUMBER() OVER (PARTITION  BY apt_name ORDER BY apt_name ASC,   (unauth_rf_prob + unauth_xgb_prob) / 2  DESC) AS rankTest,  CASE WHEN inspection_status_result = 'N' THEN 1 ELSE 0 END as inspection_status_count1,  CASE WHEN inspection_status_result = 'Z' THEN 0 ELSE 1 END as inspection_status_count2,  (unauth_rf_prob + unauth_xgb_prob) / 2 AS unauth_everage, (fake_rf_prob + fake_xgb_prob) / 2 AS fake_everage, (sale_rf_prob + sale_xgb_prob) / 2 AS sale_everage, bjdong.bjdong_name AS location, CASE  WHEN apt_sup_pos/ 1000 = 11 THEN 11 WHEN apt_sup_pos/ 1000 = 26 THEN 26 WHEN apt_sup_pos/ 1000 = 27 THEN 27 WHEN apt_sup_pos/ 1000 = 28 THEN 28 WHEN apt_sup_pos/ 1000 = 29 THEN 29 WHEN apt_sup_pos/ 1000 = 30 THEN 30 WHEN apt_sup_pos/ 1000 = 31 THEN 31 WHEN apt_sup_pos/ 1000 = 36 THEN 36 WHEN apt_sup_pos/ 1000 = 41 THEN 41 WHEN apt_sup_pos/ 1000 = 42 THEN 42 WHEN apt_sup_pos/ 1000 = 43 THEN 43 WHEN apt_sup_pos/ 1000 = 44 THEN 44 WHEN apt_sup_pos/ 1000 = 45 THEN 45 WHEN apt_sup_pos/ 1000 = 46 THEN 46 WHEN apt_sup_pos/ 1000 = 47 THEN 47 WHEN apt_sup_pos/ 1000 = 48 THEN 48 WHEN apt_sup_pos/ 1000 = 50 THEN 50 ELSE 'Other' END AS LocalCode FROM tb_riskdata risk JOIN tb_bjdong bjdong ON risk.apt_sup_pos = bjdong.bjdong_code WHERE rec_anno_date >= 20220609 AND rec_anno_date <= 20230609 ORDER BY apt_name ASC,   (unauth_rf_prob + unauth_xgb_prob) / 2  DESC )AS inner_data WHERE  ( LocalCode = 11 or LocalCode = 26 or LocalCode = 27 or LocalCode = 28 or LocalCode = 29 or LocalCode = 30 or LocalCode = 31 or LocalCode = 36 or LocalCode = 41 or LocalCode = 42 or LocalCode = 43 or LocalCode = 44 or LocalCode = 45 or LocalCode = 46 or LocalCode = 47 or LocalCode = 48 or LocalCode = 50 )   AND inner_data.rankTest <= inner_data.total_suphouse_number * 1 GROUP BY inner_data.apt_name ORDER BY  inner_data.rec_anno_date DESC , inner_data.LocalCode asc ;

select * from tb_riskdata where inspection_status_result = 'N';
select * from tb_riskdata where apt_number = 2023000006 and apt_dong = 104 and user_name ='김응수'; 
update tb_riskdata set inspection_status_result = 'Z' where apt_number = 2023000006; 
