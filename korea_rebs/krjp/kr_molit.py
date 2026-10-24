"""한국: 국토교통부 아파트 매매 실거래가 (공공데이터포털).

요청: LAWD_CD(법정동코드 앞 5자리), DEAL_YMD(계약년월 6자리), pageNo, numOfRows, serviceKey
응답: XML. 거래금액(dealAmount)은 만원 단위 문자열("82,500"), 전용면적(excluUseAr)은 ㎡.
"""

import time
import xml.etree.ElementTree as ET

from . import db as dbmod
from . import regions
from . import util
from . import webapi

SOURCE = 'molit_apt'
PAGE_SIZE = 1000


def parse_response(body):
    """XML → (rows: list[dict], total_count: int). 오류 응답이면 webapi.ApiError."""
    try:
        root = ET.fromstring(body)
    except ET.ParseError:
        raise webapi.ApiError('실거래가 응답을 읽지 못했습니다: %r' % body[:200])

    # 게이트웨이 오류 (키 미등록, 트래픽 초과 등)
    auth = root.findtext('.//returnAuthMsg')
    if auth:
        reason = root.findtext('.//returnReasonCode') or ''
        raise webapi.ApiError('공공데이터포털 오류: %s (%s)' % (auth, reason))

    code = (root.findtext('.//header/resultCode') or root.findtext('.//resultCode') or '').strip()
    message = (root.findtext('.//header/resultMsg') or root.findtext('.//resultMsg') or '').strip()
    if code and code not in ('00', '000'):
        if 'NODATA' in message.upper():
            return [], 0
        raise webapi.ApiError('실거래가 API 오류: %s %s' % (code, message))

    items = []
    for item in root.iter('item'):
        items.append({child.tag: (child.text or '').strip() for child in item})
    total = util.to_int(root.findtext('.//body/totalCount') or root.findtext('.//totalCount')) or len(items)
    return items, total


def to_trade_rows(items, lawd_cd, deal_ymd):
    """API item → tb_market_trade 행. 같은 달 안에서 내용이 같은 거래는 순번으로 구분한다."""
    rows = []
    seen = {}
    for item in items:
        amount = util.to_float(item.get('dealAmount'))
        if amount is None:
            continue
        year = util.to_int(item.get('dealYear')) or int(deal_ymd[:4])
        month = util.to_int(item.get('dealMonth')) or int(deal_ymd[4:6])
        area = util.to_float(item.get('excluUseAr'))
        price = amount * 10000.0
        region_code = regions.kr_current_code(item.get('sggCd') or lawd_cd)

        content = (SOURCE, region_code, item.get('aptSeq') or item.get('aptNm'), item.get('jibun'),
                   year, month, item.get('dealDay'), item.get('dealAmount'), item.get('excluUseAr'),
                   item.get('floor'), item.get('aptDong'))
        base_key = util.make_key(*content)
        seen[base_key] = seen.get(base_key, 0) + 1

        rows.append({
            'trade_key': base_key + '#%d' % seen[base_key],
            'country': regions.KR,
            'source': SOURCE,
            'property_type': 'apartment',
            'property_type_raw': '아파트',
            'pref_code': region_code[:2],
            'region_code': region_code,
            'district_name': item.get('umdNm') or None,
            'building_name': item.get('aptNm') or None,
            'deal_year': year,
            'deal_quarter': util.quarter_of(month),
            'deal_month': month,
            'price': price,
            'currency': 'KRW',
            'area_m2': area,
            'unit_price': (price / area) if area else None,
            'build_year': util.to_int(item.get('buildYear')),
            'floor_plan': None,
            'price_kind': 'transaction',
            'is_cancelled': 1 if util.clean(item.get('cdealType')).upper() == 'O' else 0,
        })
    return rows


def fetch_month(config, lawd_cd, deal_ymd, get=webapi.get, sleep=time.sleep):
    key = webapi.normalize_service_key(config.require('data_go_kr_key'))
    items = []
    page = 1
    while True:
        url = webapi.build_url(config.endpoints['molit_apt_trade'], [
            ('serviceKey', key), ('LAWD_CD', lawd_cd), ('DEAL_YMD', deal_ymd),
            ('pageNo', page), ('numOfRows', PAGE_SIZE)])
        page_items, total = parse_response(get(url, secrets=[key]))
        items.extend(page_items)
        if not page_items or len(items) >= total:
            break
        page += 1
        sleep(config.request_interval)
    return to_trade_rows(items, lawd_cd, deal_ymd)


def fetch(conn, config, lawd_codes, start_ymd, end_ymd, refresh=False, today=None, get=webapi.get, sleep=time.sleep):
    """시군구 × 계약월을 차례로 받아 저장한다. 이미 받은 달은 건너뛴다 (최근 2개월은 신고가 늦게 들어오므로 다시 받는다)."""
    months = util.months(start_ymd, end_ymd)
    recent = set(util.months(util.shift_month(today, -1), util.shift_month(today, 0))) if today else set()
    jobs = [(code, ymd) for code in lawd_codes for ymd in months]
    total_rows = 0
    for index, (code, ymd) in enumerate(jobs, 1):
        if not refresh and ymd not in recent and dbmod.scalar(
                conn, 'SELECT 1 FROM tb_market_fetch_log WHERE source = ? AND scope = ? AND period = ?',
                (SOURCE, code, ymd)):
            continue
        rows = fetch_month(config, code, ymd, get=get, sleep=sleep)
        dbmod.upsert_rows(conn, 'tb_market_trade', ['trade_key'], rows)
        dbmod.upsert_rows(conn, 'tb_market_fetch_log', ['source', 'scope', 'period'], [{
            'source': SOURCE, 'scope': code, 'period': ymd, 'row_count': len(rows), 'fetched_at': util.now_text()}])
        total_rows += len(rows)
        util.progress('%d/%d 실거래 %s %s %d건' % (index, len(jobs), code, ymd, len(rows)))
        sleep(config.request_interval)
    return total_rows
