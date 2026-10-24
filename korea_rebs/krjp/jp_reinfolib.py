"""일본: 国土交通省 不動産情報ライブラリ API.

XIT001 不動産価格（取引価格・成約価格）情報: year, quarter(1~4), area(都道府県 01~47) 또는 city(市区町村 5자리),
       priceClassification(01 取引価格 / 02 成約価格, 비우면 둘 다), language(ja / en)
       헤더 Ocp-Apim-Subscription-Key. 응답 JSON {"status": "OK", "data": [...]} (gzip)
XIT002 都道府県内市区町村一覧: area → [{"id": "13101", "name": "千代田区"}, ...]
"""

import re
import time

from . import db as dbmod
from . import regions
from . import util
from . import webapi

SOURCE = 'reinfolib'

TYPE_MAP = {
    '中古マンション等': 'apartment', 'Pre-owned Condominiums, etc.': 'apartment',
    '宅地(土地と建物)': 'house', 'Residential Land(Land and Building)': 'house',
    '宅地(土地)': 'land', 'Residential Land(Land Only)': 'land',
    '農地': 'other', 'Agricultural Land': 'other',
    '林地': 'other', 'Forest Land': 'other',
}

_ERA_YEAR = re.compile(r'(令和|平成|昭和)(\d+|元)年')
_ERA_BASE = {'令和': 2018, '平成': 1988, '昭和': 1925}


def property_type(raw):
    text = util.clean(raw)
    if text in TYPE_MAP:
        return TYPE_MAP[text]
    if 'マンション' in text or 'Condominium' in text:
        return 'apartment'
    if '土地と建物' in text or 'Land and Building' in text:
        return 'house'
    if '土地' in text or 'Land' in text:
        return 'land'
    return 'other'


def price_kind(category):
    text = util.clean(category)
    if '成約' in text or 'Contract' in text:
        return 'contract'
    return 'transaction'


def build_year(raw):
    """'2014年' → 2014, '昭和60年' → 1985, '戦前' → None."""
    text = util.clean(raw)
    if not text:
        return None
    era = _ERA_YEAR.search(text)
    if era:
        return (1 if era.group(2) == '元' else int(era.group(2))) + _ERA_BASE[era.group(1)]
    match = re.search(r'(\d{4})', text)
    return int(match.group(1)) if match else None


def parse_trades(payload, year, quarter, pref_code):
    """XIT001 응답 → tb_market_trade 행."""
    if not isinstance(payload, dict):
        raise webapi.ApiError('不動産情報ライブラリ 응답 형식이 다릅니다')
    status = util.clean(payload.get('status')).upper()
    if status and status != 'OK':
        raise webapi.ApiError('不動産情報ライブラリ 오류: %s' % payload.get('status'))

    rows = []
    seen = {}
    for item in payload.get('data') or []:
        price = util.to_float(item.get('TradePrice'))
        if price is None:
            continue
        kind = property_type(item.get('Type'))
        area = util.to_float(item.get('Area'))
        floor_area = util.to_float(item.get('TotalFloorArea'))
        unit_area = floor_area if kind == 'house' and floor_area else area
        municipality = util.clean(item.get('MunicipalityCode')).zfill(5) if item.get('MunicipalityCode') else ''
        region_code = municipality or pref_code + '000'

        content = (SOURCE, region_code, item.get('DistrictName'), item.get('Period'), item.get('Type'),
                   item.get('TradePrice'), item.get('Area'), item.get('FloorPlan'), item.get('BuildingYear'),
                   item.get('Structure'), item.get('PriceCategory'), item.get('Remarks'))
        base_key = util.make_key(*content)
        seen[base_key] = seen.get(base_key, 0) + 1

        rows.append({
            'trade_key': base_key + '#%d' % seen[base_key],
            'country': regions.JP,
            'source': SOURCE,
            'property_type': kind,
            'property_type_raw': util.clean(item.get('Type')) or None,
            'pref_code': region_code[:2],
            'region_code': region_code,
            'district_name': util.clean(item.get('DistrictName')) or None,
            'building_name': None,
            'deal_year': int(year),
            'deal_quarter': int(quarter),
            'deal_month': None,
            'price': price,
            'currency': 'JPY',
            'area_m2': unit_area,
            'unit_price': (price / unit_area) if unit_area else util.to_float(item.get('UnitPrice')),
            'build_year': build_year(item.get('BuildingYear')),
            'floor_plan': util.clean(item.get('FloorPlan')) or None,
            'price_kind': price_kind(item.get('PriceCategory')),
            'is_cancelled': 0,
        })
    return rows


def parse_cities(payload):
    if not isinstance(payload, dict) or util.clean(payload.get('status')).upper() not in ('', 'OK'):
        raise webapi.ApiError('市区町村 목록 오류: %s' % (payload.get('status') if isinstance(payload, dict) else payload))
    return [(util.clean(item.get('id')).zfill(5), util.clean(item.get('name'))) for item in payload.get('data') or []]


def _headers(config):
    return {'Ocp-Apim-Subscription-Key': config.require('reinfolib_key')}


def fetch_regions(conn, config, prefs=None, get_json=webapi.get_json, sleep=time.sleep):
    """都道府県 47 + 市区町村 (일본어·영어 이름)."""
    rows = regions.jp_prefecture_rows()
    headers = _headers(config)
    prefs = prefs or [code for code, _, _, _ in regions.JP_PREFECTURES]
    for index, pref in enumerate(prefs, 1):
        names = {}
        for language in ('ja', 'en'):
            url = webapi.build_url(config.endpoints['reinfolib_cities'], [('area', pref), ('language', language)])
            for code, name in parse_cities(get_json(url, headers=headers)):
                names.setdefault(code, {})[language] = name
            sleep(config.request_interval)
        for code, pair in sorted(names.items()):
            rows.append(regions.jp_municipality_row(code, pair.get('ja'), pair.get('en')))
        util.progress('%d/%d 市区町村 목록 %s %d곳' % (index, len(prefs), pref, len(names)))
    dbmod.upsert_rows(conn, 'tb_region', ['country', 'region_code'], rows)
    return len(rows)


def fetch(conn, config, years, quarters, prefs, refresh=False, get_json=webapi.get_json, sleep=time.sleep):
    headers = _headers(config)
    jobs = [(pref, year, q) for pref in prefs for year in years for q in quarters]
    total = 0
    for index, (pref, year, q) in enumerate(jobs, 1):
        period = '%dQ%d' % (year, q)
        if not refresh and dbmod.scalar(
                conn, 'SELECT 1 FROM tb_market_fetch_log WHERE source = ? AND scope = ? AND period = ?',
                (SOURCE, pref, period)):
            continue
        url = webapi.build_url(config.endpoints['reinfolib_trade'], [
            ('year', year), ('quarter', q), ('area', pref), ('language', 'ja')])
        try:
            payload = get_json(url, headers=headers)
        except webapi.ApiError as e:
            # 아직 공개되지 않은 분기는 404/빈 응답이 올 수 있다
            if '404' in str(e):
                util.progress('%d/%d 거래가격 %s %s 없음' % (index, len(jobs), pref, period))
                continue
            raise
        rows = parse_trades(payload, year, q, pref)
        dbmod.upsert_rows(conn, 'tb_market_trade', ['trade_key'], rows)
        dbmod.upsert_rows(conn, 'tb_market_fetch_log', ['source', 'scope', 'period'], [{
            'source': SOURCE, 'scope': pref, 'period': period, 'row_count': len(rows), 'fetched_at': util.now_text()}])
        total += len(rows)
        util.progress('%d/%d 거래가격 %s %s %d건' % (index, len(jobs), pref, period, len(rows)))
        sleep(config.request_interval)
    return total
