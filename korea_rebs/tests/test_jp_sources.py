"""일본 不動産情報ライブラリ 파싱. 응답은 공개된 필드 이름으로 만든 합성 예시다."""

import unittest
from urllib.parse import parse_qs, urlparse

from krjp import config as configmod, jp_reinfolib, webapi
from tests.helpers import TempDb

XIT001 = {
    'status': 'OK',
    'data': [
        {'PriceCategory': '不動産取引価格情報', 'Type': '中古マンション等', 'MunicipalityCode': '13104',
         'Prefecture': '東京都', 'Municipality': '新宿区', 'DistrictName': '西新宿', 'TradePrice': '72000000',
         'Area': '60', 'FloorPlan': '２ＬＤＫ', 'BuildingYear': '2010年', 'Period': '2025年第1四半期'},
        {'PriceCategory': '成約価格情報', 'Type': '中古マンション等', 'MunicipalityCode': '13104',
         'DistrictName': '西新宿', 'TradePrice': '70000000', 'Area': '60', 'BuildingYear': '昭和60年',
         'Period': '2025年第1四半期'},
        {'PriceCategory': '不動産取引価格情報', 'Type': '宅地(土地と建物)', 'MunicipalityCode': '13120',
         'DistrictName': '豊玉北', 'TradePrice': '80000000', 'Area': '100', 'TotalFloorArea': '120',
         'BuildingYear': '戦前', 'Period': '2025年第1四半期'},
        {'PriceCategory': '不動産取引価格情報', 'Type': '宅地(土地)', 'MunicipalityCode': '13120',
         'DistrictName': '豊玉北', 'TradePrice': '150000000', 'Area': '2,000㎡以上', 'Period': '2025年第1四半期'},
        {'PriceCategory': '不動産取引価格情報', 'Type': '農地', 'MunicipalityCode': '13120', 'TradePrice': ''},
    ],
}


class ReinfolibTest(unittest.TestCase):
    def test_parse(self):
        rows = jp_reinfolib.parse_trades(XIT001, 2025, 1, '13')
        self.assertEqual(len(rows), 4)                                  # 가격 없는 행은 뺀다
        condo, contract, house, land = rows
        self.assertEqual((condo['property_type'], condo['price_kind'], condo['unit_price']), ('apartment', 'transaction', 1200000.0))
        self.assertEqual((condo['floor_plan'], condo['build_year'], condo['currency']), ('2LDK', 2010, 'JPY'))
        self.assertEqual((contract['price_kind'], contract['build_year']), ('contract', 1985))
        self.assertEqual((house['property_type'], house['area_m2'], house['build_year']), ('house', 120.0, None))
        self.assertEqual((land['property_type'], land['area_m2']), ('land', 2000.0))
        self.assertEqual(len(set(r['trade_key'] for r in rows)), 4)

    def test_status_error_and_types(self):
        with self.assertRaises(webapi.ApiError):
            jp_reinfolib.parse_trades({'status': 'ERROR', 'data': []}, 2025, 1, '13')
        self.assertEqual(jp_reinfolib.property_type('Pre-owned Condominiums, etc.'), 'apartment')
        self.assertEqual(jp_reinfolib.property_type('Residential Land(Land Only)'), 'land')
        self.assertEqual(jp_reinfolib.build_year('令和元年'), 2019)

    def test_cities(self):
        self.assertEqual(jp_reinfolib.parse_cities({'status': 'OK', 'data': [{'id': '1101', 'name': '札幌市中央区'}]}),
                         [('01101', '札幌市中央区')])

    def test_fetch_uses_header_and_skips_done(self):
        calls = []

        def fake(url, headers=None, secrets=()):
            calls.append((parse_qs(urlparse(url).query), headers))
            q = parse_qs(urlparse(url).query)
            if q['quarter'] == ['4']:
                raise webapi.ApiError('HTTP 404 Not Found')
            return XIT001

        cfg = configmod.Config({}, {'KOREAREB_REINFOLIB_KEY': 'jp-key'})
        temp = TempDb()
        try:
            conn = temp.connect()
            total = jp_reinfolib.fetch(conn, cfg, [2025], [1, 4], ['13'], get_json=fake, sleep=lambda s: None)
            self.assertEqual(total, 4)
            self.assertEqual(calls[0][1], {'Ocp-Apim-Subscription-Key': 'jp-key'})
            self.assertEqual(calls[0][0]['area'], ['13'])
            self.assertNotIn('jp-key', str(calls[0][0]))                 # 키는 URL 에 넣지 않는다
            jp_reinfolib.fetch(conn, cfg, [2025], [1], ['13'], get_json=fake, sleep=lambda s: None)
            self.assertEqual(len(calls), 2)                                # 받은 분기는 다시 안 부른다
            conn.close()
        finally:
            temp.cleanup()

    def test_fetch_regions(self):
        def fake(url, headers=None, secrets=()):
            q = parse_qs(urlparse(url).query)
            name = '新宿区' if q['language'] == ['ja'] else 'Shinjuku Ward'
            return {'status': 'OK', 'data': [{'id': '13104', 'name': name}]}

        cfg = configmod.Config({}, {'KOREAREB_REINFOLIB_KEY': 'k'})
        temp = TempDb()
        try:
            conn = temp.connect()
            jp_reinfolib.fetch_regions(conn, cfg, ['13'], get_json=fake, sleep=lambda s: None)
            row = conn.execute("SELECT * FROM tb_region WHERE country = 'JP' AND region_code = '13104'").fetchone()
            self.assertEqual((row['name_local'], row['name_en'], row['name_ko'], row['parent_code']),
                             ('新宿区', 'Shinjuku Ward', '신주쿠구', '13'))
            self.assertEqual(conn.execute("SELECT COUNT(*) FROM tb_region WHERE country = 'JP' AND level = 1").fetchone()[0], 47)
            conn.close()
        finally:
            temp.cleanup()


if __name__ == '__main__':
    unittest.main()
