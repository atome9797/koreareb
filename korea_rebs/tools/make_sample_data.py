"""대시보드 미리보기용 예시 데이터 만들기 (합성 데이터, 실제 시세·경쟁률이 아니다).

API 키를 받기 전에 대시보드와 앱 화면을 확인하려고 쓴다. 앱 DB 는 건드리지 않고 임시 DB 에서 만든다.
  python tools/make_sample_data.py                      → dashboard/data/sample-data.js
  python tools/make_sample_data.py --db 예시.db          → 그 DB 에도 남긴다 (앱 화면 확인용, 새 파일만)
대시보드는 meta.sample 이 true 이면 '예시 데이터' 표시를 띄운다.
"""

import argparse
import os
import random
import shutil
import sqlite3
import sys
import tempfile

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.dirname(HERE))

from krjp import analysis, db as dbmod, regions  # noqa: E402

# (코드, 이름, ㎡당 기준가) — 기준가는 지역 사이 차이를 보여 주려고 정한 임의의 값이다
KR = [
    ('11110', '서울특별시 종로구', 1500), ('11140', '서울특별시 중구', 1450), ('11170', '서울특별시 용산구', 2000),
    ('11200', '서울특별시 성동구', 1750), ('11215', '서울특별시 광진구', 1500), ('11230', '서울특별시 동대문구', 1100),
    ('11260', '서울특별시 중랑구', 850), ('11290', '서울특별시 성북구', 1000), ('11305', '서울특별시 강북구', 800),
    ('11320', '서울특별시 도봉구', 750), ('11350', '서울특별시 노원구', 850), ('11380', '서울특별시 은평구', 950),
    ('11410', '서울특별시 서대문구', 1100), ('11440', '서울특별시 마포구', 1550), ('11470', '서울특별시 양천구', 1400),
    ('11500', '서울특별시 강서구', 1050), ('11530', '서울특별시 구로구', 900), ('11545', '서울특별시 금천구', 800),
    ('11560', '서울특별시 영등포구', 1300), ('11590', '서울특별시 동작구', 1400), ('11620', '서울특별시 관악구', 950),
    ('11650', '서울특별시 서초구', 2350), ('11680', '서울특별시 강남구', 2500), ('11710', '서울특별시 송파구', 1900),
    ('11740', '서울특별시 강동구', 1350),
    ('26350', '부산광역시 해운대구', 650), ('27260', '대구광역시 수성구', 600), ('28185', '인천광역시 연수구', 600),
    ('29140', '광주광역시 서구', 420), ('30200', '대전광역시 유성구', 520), ('31140', '울산광역시 남구', 450),
    ('36110', '세종특별자치시', 600), ('41117', '경기도 수원시 영통구', 750), ('41135', '경기도 성남시 분당구', 1350),
    ('41285', '경기도 고양시 일산동구', 600), ('41465', '경기도 용인시 수지구', 800), ('41590', '경기도 화성시', 600),
    ('51110', '강원특별자치도 춘천시', 380), ('43113', '충청북도 청주시 흥덕구', 380), ('44133', '충청남도 천안시 서북구', 400),
    ('52111', '전북특별자치도 전주시 완산구', 340), ('46130', '전라남도 여수시', 300), ('47111', '경상북도 포항시 남구', 300),
    ('48123', '경상남도 창원시 성산구', 450), ('50110', '제주특별자치도 제주시', 520),
]
KR_PREFS = {'11': '서울특별시', '26': '부산광역시', '27': '대구광역시', '28': '인천광역시', '29': '광주광역시',
            '30': '대전광역시', '31': '울산광역시', '36': '세종특별자치시', '41': '경기도', '51': '강원특별자치도',
            '43': '충청북도', '44': '충청남도', '52': '전북특별자치도', '46': '전라남도', '47': '경상북도',
            '48': '경상남도', '50': '제주특별자치도'}

# (코드, 일본어 이름, 영어 이름, ㎡당 기준가 만 엔)
JP = [
    ('13101', '千代田区', 'Chiyoda Ward', 210), ('13102', '中央区', 'Chuo Ward', 180), ('13103', '港区', 'Minato Ward', 230),
    ('13104', '新宿区', 'Shinjuku Ward', 150), ('13105', '文京区', 'Bunkyo Ward', 140), ('13106', '台東区', 'Taito Ward', 115),
    ('13107', '墨田区', 'Sumida Ward', 100), ('13108', '江東区', 'Koto Ward', 115), ('13109', '品川区', 'Shinagawa Ward', 130),
    ('13110', '目黒区', 'Meguro Ward', 145), ('13111', '大田区', 'Ota Ward', 95), ('13112', '世田谷区', 'Setagaya Ward', 115),
    ('13113', '渋谷区', 'Shibuya Ward', 190), ('13114', '中野区', 'Nakano Ward', 100), ('13115', '杉並区', 'Suginami Ward', 100),
    ('13116', '豊島区', 'Toshima Ward', 115), ('13117', '北区', 'Kita Ward', 85), ('13118', '荒川区', 'Arakawa Ward', 85),
    ('13119', '板橋区', 'Itabashi Ward', 75), ('13120', '練馬区', 'Nerima Ward', 75), ('13121', '足立区', 'Adachi Ward', 60),
    ('13122', '葛飾区', 'Katsushika Ward', 60), ('13123', '江戸川区', 'Edogawa Ward', 65),
    ('14100', '横浜市', 'Yokohama City', 65), ('14130', '川崎市', 'Kawasaki City', 70), ('11100', 'さいたま市', 'Saitama City', 55),
    ('12100', '千葉市', 'Chiba City', 40), ('27100', '大阪市', 'Osaka City', 75), ('23100', '名古屋市', 'Nagoya City', 50),
    ('26100', '京都市', 'Kyoto City', 60), ('28100', '神戸市', 'Kobe City', 45), ('40130', '福岡市', 'Fukuoka City', 50),
    ('01100', '札幌市', 'Sapporo City', 35),
]

QUARTERS = [(y, q) for y in (2023, 2024, 2025) for q in (1, 2, 3, 4)]


def build(conn, rng):
    region_rows = [{'country': 'KR', 'region_code': c, 'level': 1, 'parent_code': None, 'name_ko': n, 'name_local': n,
                    'name_en': None} for c, n in KR_PREFS.items()]
    region_rows += [{'country': 'KR', 'region_code': c, 'level': 2, 'parent_code': c[:2], 'name_ko': n, 'name_local': n,
                     'name_en': None} for c, n, _ in KR]
    region_rows += regions.jp_prefecture_rows()
    region_rows += [regions.jp_municipality_row(c, ja, en) for c, ja, en, _ in JP]
    dbmod.upsert_rows(conn, 'tb_region', ['country', 'region_code'], region_rows)

    trades = []
    for code, _, base in KR:
        growth = 0.012 if code.startswith('11') else 0.003
        for index, (year, quarter) in enumerate(QUARTERS):
            for n in range(rng.randint(12, 40)):
                area = rng.choice((59.9, 74.9, 84.9, 84.9, 114.9))
                unit = base * 10000 * (1 + growth) ** index * rng.lognormvariate(0, 0.18)
                trades.append(_trade('kr%s%d%d%d' % (code, year, quarter, n), 'KR', code, year, quarter, unit, area, 'KRW'))
    for code, _, _, base in JP:
        growth = 0.015 if code.startswith('13') else 0.006
        for index, (year, quarter) in enumerate(QUARTERS):
            for n in range(rng.randint(8, 30)):
                area = rng.choice((25.0, 40.0, 55.0, 65.0, 70.0, 80.0))
                unit = base * 10000 * (1 + growth) ** index * rng.lognormvariate(0, 0.22)
                trades.append(_trade('jp%s%d%d%d' % (code, year, quarter, n), 'JP', code, year, quarter, unit, area, 'JPY'))
    dbmod.upsert_rows(conn, 'tb_market_trade', ['trade_key'], trades)

    comps = []
    number = 0
    for code, _, base in KR:
        mean = 25.0 if code.startswith('11') else 6.0 if code[:2] in ('41', '28', '36', '26') else 1.5
        for year in (2023, 2024, 2025):
            for _ in range(rng.randint(1, 4)):
                number += 1
                supply = rng.randint(60, 600)
                apps = int(supply * mean * rng.lognormvariate(0, 0.7))
                comps.append(_comp('kc%d' % number, 'KR', 'kr_apt', code, '%d-%02d-%02d' % (year, rng.randint(1, 12), rng.randint(1, 28)),
                                   supply, apps, '예시 단지 %d' % number))
    for code, _, _, base in JP:
        if code.startswith('13'):
            for year in (2023, 2024, 2025):
                for month in (2, 5, 8, 11):
                    number += 1
                    supply = rng.randint(2, 30)
                    apps = int(supply * (8 + base / 6.0) * rng.lognormvariate(0, 0.5))
                    comps.append(_comp('jh%d' % number, 'JP', 'jp_public_housing', code, '%d-%02d-01' % (year, month),
                                       supply, apps, '예시 공영주택 %d' % number))
        for year, quarter in QUARTERS[::2]:
            number += 1
            supply = rng.randint(20, 200)
            apps = int(supply * (1 + base / 60.0) * rng.lognormvariate(0, 0.5))
            comps.append(_comp('jc%d' % number, 'JP', 'jp_new_condo', code, '%d-%02d-01' % (year, quarter * 3 - 2),
                               supply, apps, '예시 맨션 %d' % number))
    dbmod.upsert_rows(conn, 'tb_market_competition', ['comp_key'], comps)


def _trade(key, country, code, year, quarter, unit, area, currency):
    return {'trade_key': key, 'country': country, 'source': 'sample', 'property_type': 'apartment',
            'property_type_raw': None, 'pref_code': code[:2], 'region_code': code, 'district_name': None,
            'building_name': None, 'deal_year': year, 'deal_quarter': quarter, 'deal_month': None,
            'price': round(unit * area, -4), 'currency': currency, 'area_m2': area, 'unit_price': unit,
            'build_year': None, 'floor_plan': None, 'price_kind': 'transaction', 'is_cancelled': 0}


def _comp(key, country, scheme, code, date, supply, apps, name):
    return {'comp_key': key, 'country': country, 'scheme': scheme, 'source': 'sample', 'project_id': key,
            'project_name': name, 'unit_type': None, 'pref_code': code[:2], 'region_code': code, 'announce_date': date,
            'supply_units': supply, 'applications': apps, 'competition_rate': apps / float(supply), 'min_score': None,
            'avg_score': None, 'max_score': None, 'avg_price': None, 'currency': None}


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument('--out', default=os.path.join(os.path.dirname(HERE), 'dashboard', 'data', 'sample-data.js'))
    parser.add_argument('--db', help='예시 데이터를 남길 새 DB 파일 (이미 있으면 거부)')
    parser.add_argument('--seed', type=int, default=20261024)
    args = parser.parse_args(argv)

    if args.db and os.path.exists(args.db):
        raise SystemExit('이미 있는 파일에는 쓰지 않습니다 (앱 DB 보호): %s' % args.db)

    work = tempfile.mkdtemp(prefix='krjp_sample_')
    try:
        path = os.path.join(work, 'sample.db')
        sqlite3.connect(path).close()
        conn = dbmod.connect(path)
        build(conn, random.Random(args.seed))
        analysis.build_stats(conn)
        os.makedirs(os.path.dirname(os.path.abspath(args.out)), exist_ok=True)
        count = analysis.export(conn, args.out, fx_krw_per_jpy=None, sample=True)
        conn.close()
        if args.db:
            shutil.copyfile(path, args.db)
        print('예시 데이터 %d행 → %s' % (count, args.out))
    finally:
        shutil.rmtree(work, ignore_errors=True)


if __name__ == '__main__':
    main()
