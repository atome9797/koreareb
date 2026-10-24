"""지역별 집계 (tb_market_region_stat)와 대시보드용 내보내기.

거래(trade):   나라·유형·지역·기간별 ㎡당 가격 중앙값(사분위), 거래가 중앙값, 면적 중앙값, 거래 수,
              가격 지수 = 지역 ㎡당 중앙값 ÷ 같은 나라·유형·기간의 전국 ㎡당 중앙값 × 100 (통화와 무관하게 비교)
경쟁(competition): 나라·제도·지역·기간별 공급 합계, 신청 합계, 경쟁률 = 신청 ÷ 공급
기간: ALL(받은 전체), LAST4Q(가장 최근 4개 분기), 'YYYY', 'YYYYQn'(분기는 전국·시도/都道府県 수준만)
수준: 0 전국('00'), 1 시도·都道府県, 2 시군구·市区町村
"""

import json

import pandas as pd

from . import db as dbmod
from . import util

STAT_COLUMNS = ['country', 'metric', 'category', 'level', 'region_code', 'period', 'n_records',
                'median_unit_price', 'p25_unit_price', 'p75_unit_price', 'median_price', 'median_area_m2',
                'supply_units', 'applications', 'competition_rate', 'price_index', 'currency']

NATIONAL = '00'


def _last_quarters(latest_key, count):
    """latest_key = 연도*10+분기. 최근 count 개 분기 키."""
    year, quarter = divmod(int(latest_key), 10)
    keys = []
    for _ in range(count):
        keys.append(year * 10 + quarter)
        quarter -= 1
        if quarter == 0:
            year, quarter = year - 1, 4
    return keys


def _periods(frame, key_column, with_quarters):
    """(기간 이름, 부분 프레임, 시군구 수준 포함 여부) 목록."""
    if frame.empty:
        return []
    latest = frame[key_column].max()
    result = [('ALL', frame, True), ('LAST4Q', frame[frame[key_column].isin(_last_quarters(latest, 4))], True)]
    for year, part in frame.groupby(frame[key_column] // 10):
        result.append((str(int(year)), part, True))
    if with_quarters:
        for key, part in frame.groupby(key_column):
            result.append(('%dQ%d' % divmod(int(key), 10), part, False))
    return result


def _trade_rows(country, category, currency, period, part, include_regions):
    rows = []
    levels = [(0, None), (1, 'pref_code')] + ([(2, 'region_code')] if include_regions else [])
    for level, column in levels:
        if column is None:
            grouped = part.assign(_all=NATIONAL).groupby('_all')
        else:
            grouped = part.groupby(column, observed=True)     # category 열: 데이터가 있는 지역만
        quant = grouped['unit_price'].quantile([0.25, 0.5, 0.75]).unstack()
        sizes = grouped.size()
        sizes = sizes[sizes > 0]
        prices = grouped['price'].median()
        areas = grouped['area_m2'].median()
        for code in sizes.index:
            rows.append({
                'country': country, 'metric': 'trade', 'category': category, 'level': level,
                'region_code': str(code), 'period': period, 'n_records': int(sizes[code]),
                'median_unit_price': _num(quant.loc[code, 0.5]), 'p25_unit_price': _num(quant.loc[code, 0.25]),
                'p75_unit_price': _num(quant.loc[code, 0.75]), 'median_price': _num(prices[code]),
                'median_area_m2': _num(areas[code]), 'supply_units': None, 'applications': None,
                'competition_rate': None, 'price_index': None, 'currency': currency,
            })
    national = [r for r in rows if r['level'] == 0]
    base = national[0]['median_unit_price'] if national else None
    if base:
        for r in rows:
            if r['median_unit_price'] is not None:
                r['price_index'] = round(r['median_unit_price'] / base * 100.0, 1)
    return rows


def _competition_rows(country, category, period, part, include_regions):
    rows = []
    levels = [(0, None), (1, 'pref_code')] + ([(2, 'region_code')] if include_regions else [])
    for level, column in levels:
        source = part.assign(_all=NATIONAL)
        key = '_all' if column is None else column
        source = source[source[key].notna()]
        if source.empty:
            continue
        grouped = source.groupby(key)
        sums = grouped[['supply_units', 'applications']].sum()
        counts = grouped.size()
        for code in counts.index:
            supply = int(sums.loc[code, 'supply_units'])
            applications = int(sums.loc[code, 'applications'])
            rows.append({
                'country': country, 'metric': 'competition', 'category': category, 'level': level,
                'region_code': str(code), 'period': period, 'n_records': int(counts[code]),
                'median_unit_price': None, 'p25_unit_price': None, 'p75_unit_price': None,
                'median_price': None, 'median_area_m2': None, 'supply_units': supply,
                'applications': applications,
                'competition_rate': round(applications / float(supply), 2) if supply else None,
                'price_index': None, 'currency': None,
            })
    return rows


def _num(value):
    try:
        if value is None or pd.isna(value):
            return None
    except (TypeError, ValueError):
        return None
    return round(float(value), 2)


def _frame(conn, sql, params=(), chunk=200000, category_columns=(), float_columns=()):
    """쿼리 → DataFrame. 큰 표는 나눠 읽고, 글자 열은 category 로 줄인다 (메모리). 숫자는 금액 정밀도 때문에 float64.

    연결의 row_factory(sqlite3.Row) 대신 튜플로 읽는다 (pandas 버전에 따라 Row 를 못 받는다).
    """
    cursor = conn.cursor()
    cursor.row_factory = None
    cursor.execute(sql, params)
    columns = [d[0] for d in cursor.description]
    parts = []
    while True:
        rows = cursor.fetchmany(chunk)
        if not rows:
            break
        part = pd.DataFrame.from_records(rows, columns=columns)
        for name in float_columns:
            part[name] = pd.to_numeric(part[name], errors='coerce').astype('float64')
        parts.append(part)
    if not parts:
        return pd.DataFrame(columns=columns)
    frame = pd.concat(parts, ignore_index=True)
    for name in category_columns:
        frame[name] = frame[name].astype('category')
    return frame


def build_stats(conn):
    """tb_market_region_stat 를 지우고 다시 만든다. 반환: 행 수."""
    rows = []
    # 나라·유형마다 따로 읽어서 한 번에 메모리에 올리는 양을 줄인다
    groups = conn.execute('SELECT DISTINCT country, property_type, currency FROM tb_market_trade '
                          'WHERE is_cancelled = 0 AND unit_price > 0').fetchall()
    for country, category, currency in groups:
        part = _frame(conn,
            'SELECT pref_code, region_code, deal_year * 10 + deal_quarter AS qkey, unit_price, price, area_m2 '
            'FROM tb_market_trade WHERE is_cancelled = 0 AND unit_price > 0 AND country = ? AND property_type = ? '
            'AND currency = ?', (country, category, currency),
            category_columns=('pref_code', 'region_code'), float_columns=('unit_price', 'price', 'area_m2'))
        for period, sub, regions_too in _periods(part, 'qkey', with_quarters=True):
            rows.extend(_trade_rows(country, category, currency, period, sub, regions_too))
        del part

    comps = _frame(conn,
        'SELECT country, scheme AS category, pref_code, region_code, announce_date, supply_units, applications '
        'FROM tb_market_competition WHERE supply_units > 0 AND applications IS NOT NULL '
        'AND announce_date IS NOT NULL')

    if not comps.empty:
        dates = pd.to_datetime(comps['announce_date'], errors='coerce')
        comps = comps[dates.notna()].copy()
        dates = dates[dates.notna()]
        comps['qkey'] = dates.dt.year * 10 + (dates.dt.month - 1) // 3 + 1
        for (country, category), part in comps.groupby(['country', 'category']):
            for period, sub, regions_too in _periods(part, 'qkey', with_quarters=True):
                rows.extend(_competition_rows(country, category, period, sub, regions_too))

    with dbmod.transaction(conn):
        conn.execute('DELETE FROM tb_market_region_stat')
        if rows:
            conn.executemany(
                'INSERT INTO tb_market_region_stat (%s) VALUES (%s)' % (
                    ', '.join(STAT_COLUMNS), ', '.join('?' for _ in STAT_COLUMNS)),
                [tuple(r[c] for c in STAT_COLUMNS) for r in rows])
    return len(rows)


def top_projects(conn, limit=100):
    """경쟁률 높은 단지·주택 (최근 4개 분기, 공급 10세대 이상)."""
    result = []
    for country, scheme in conn.execute('SELECT DISTINCT country, scheme FROM tb_market_competition').fetchall():
        latest = dbmod.scalar(conn, 'SELECT MAX(announce_date) FROM tb_market_competition WHERE country = ? AND scheme = ?',
                              (country, scheme))
        if not latest:
            continue
        year, month = int(latest[:4]), int(latest[5:7])
        start_q = _last_quarters(year * 10 + (month - 1) // 3 + 1, 4)[-1]
        start = '%04d-%02d-01' % (start_q // 10, (start_q % 10 - 1) * 3 + 1)
        cursor = conn.execute(
            'SELECT project_name, pref_code, region_code, MIN(announce_date) AS announce_date, '
            'SUM(supply_units) AS supply, SUM(applications) AS apps FROM tb_market_competition '
            'WHERE country = ? AND scheme = ? AND announce_date >= ? GROUP BY project_id, project_name, pref_code, region_code '
            'HAVING SUM(supply_units) >= 10 ORDER BY CAST(SUM(applications) AS REAL) / SUM(supply_units) DESC LIMIT ?',
            (country, scheme, start, limit))
        for row in cursor:
            result.append({
                'country': country, 'scheme': scheme, 'name': row['project_name'], 'pref': row['pref_code'],
                'region': row['region_code'], 'date': row['announce_date'], 'supply': row['supply'],
                'applications': row['apps'], 'rate': round(row['apps'] / float(row['supply']), 2),
            })
    return result


def export(conn, out_path, fx_krw_per_jpy=None, sample=False):
    """대시보드가 읽는 JS 파일 (window.KOREAREB_MARKET = {...}). file:// 로 열어도 읽히도록 JSON 대신 JS 로 쓴다."""
    regions_map = {}
    for row in conn.execute('SELECT country, region_code, level, parent_code, name_ko, name_local, name_en FROM tb_region'):
        regions_map['%s:%s' % (row['country'], row['region_code'])] = [
            row['level'], row['parent_code'], row['name_ko'], row['name_local'], row['name_en']]

    stats = [list(r) for r in conn.execute('SELECT %s FROM tb_market_region_stat ORDER BY country, metric, category, level, region_code, period'
                                           % ', '.join(STAT_COLUMNS))]
    coverage = {}
    for row in conn.execute('SELECT country, property_type, MIN(deal_year * 10 + deal_quarter), MAX(deal_year * 10 + deal_quarter), COUNT(*) '
                            'FROM tb_market_trade GROUP BY country, property_type'):
        coverage['trade:%s:%s' % (row[0], row[1])] = {'from': row[2], 'to': row[3], 'rows': row[4]}
    for row in conn.execute('SELECT country, scheme, MIN(announce_date), MAX(announce_date), COUNT(*) FROM tb_market_competition GROUP BY country, scheme'):
        coverage['competition:%s:%s' % (row[0], row[1])] = {'from': row[2], 'to': row[3], 'rows': row[4]}

    payload = {
        'meta': {'built_at': util.now_text(), 'fx_krw_per_jpy': fx_krw_per_jpy, 'sample': bool(sample),
                 'coverage': coverage},
        'stat_columns': STAT_COLUMNS,
        'regions': regions_map,
        'stats': stats,
        'top_projects': top_projects(conn),
    }
    text = 'window.KOREAREB_MARKET = ' + json.dumps(payload, ensure_ascii=False, separators=(',', ':')) + ';\n'
    with open(out_path, 'w', encoding='utf-8') as f:
        f.write(text)
    return len(stats)
