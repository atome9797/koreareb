"""지역별 집계, 대시보드 내보내기, 명령줄. 거래는 모두 합성 데이터다."""

import io
import json
import os
import unittest
from contextlib import redirect_stderr, redirect_stdout

from krjp import analysis, db as dbmod
from tests.helpers import TempDb

import jp_fraud
import market_data


def trade(key, country, region, year, quarter, unit, area=50.0, cancelled=0, category='apartment'):
    return {
        'trade_key': key, 'country': country, 'source': 'test', 'property_type': category, 'property_type_raw': None,
        'pref_code': region[:2], 'region_code': region, 'district_name': None, 'building_name': None,
        'deal_year': year, 'deal_quarter': quarter, 'deal_month': None, 'price': unit * area,
        'currency': 'KRW' if country == 'KR' else 'JPY', 'area_m2': area, 'unit_price': unit,
        'build_year': None, 'floor_plan': None, 'price_kind': 'transaction', 'is_cancelled': cancelled,
    }


def comp(key, country, scheme, region, date, supply, apps, name='p'):
    return {
        'comp_key': key, 'country': country, 'scheme': scheme, 'source': 'test', 'project_id': key,
        'project_name': name, 'unit_type': None, 'pref_code': region[:2] if region else None,
        'region_code': region if region and len(region) == 5 else None, 'announce_date': date,
        'supply_units': supply, 'applications': apps, 'competition_rate': None, 'min_score': None,
        'avg_score': None, 'max_score': None, 'avg_price': None, 'currency': None,
    }


class AnalysisTest(unittest.TestCase):
    def setUp(self):
        self.temp = TempDb()
        self.conn = self.temp.connect()
        trades = []
        # 강남 2025Q1~Q4 ㎡당 2000만·서초 1000만, 2024Q4 강남 1500만 (LAST4Q 에서 빠져야 함), 해제 거래 1건
        for q in (1, 2, 3, 4):
            trades.append(trade('g%d' % q, 'KR', '11680', 2025, q, 20000000))
            trades.append(trade('s%d' % q, 'KR', '11650', 2025, q, 10000000))
        trades.append(trade('old', 'KR', '11680', 2024, 4, 15000000))
        trades.append(trade('cancel', 'KR', '11680', 2025, 1, 999999999, cancelled=1))
        trades.append(trade('jp1', 'JP', '13104', 2025, 2, 1200000))
        trades.append(trade('jp2', 'JP', '13120', 2025, 2, 600000))
        dbmod.upsert_rows(self.conn, 'tb_market_trade', ['trade_key'], trades)
        dbmod.upsert_rows(self.conn, 'tb_market_competition', ['comp_key'], [
            comp('c1', 'KR', 'kr_apt', '11680', '2025-05-02', 100, 2000, '강남 단지'),
            comp('c2', 'KR', 'kr_apt', '11650', '2025-06-02', 100, 300, '서초 단지'),
            comp('c3', 'KR', 'kr_apt', '36', '2025-06-10', 50, 20, '세종 단지'),       # 시군구 모름 → 시도까지만
            comp('j1', 'JP', 'jp_public_housing', '13120', '2026-08-01', 1, 186),
        ])
        self.conn.execute("INSERT INTO tb_region VALUES ('KR', '11680', 2, '11', '서울특별시 강남구', '서울특별시 강남구', NULL)")

    def tearDown(self):
        self.conn.close()
        self.temp.cleanup()

    def stat(self, **kw):
        sql = 'SELECT * FROM tb_market_region_stat WHERE ' + ' AND '.join('%s = ?' % k for k in kw)
        rows = self.conn.execute(sql, list(kw.values())).fetchall()
        self.assertEqual(len(rows), 1, kw)
        return rows[0]

    def test_trade_stats(self):
        analysis.build_stats(self.conn)
        gangnam = self.stat(country='KR', metric='trade', category='apartment', level=2, region_code='11680', period='LAST4Q')
        self.assertEqual((gangnam['n_records'], gangnam['median_unit_price']), (4, 20000000))
        national = self.stat(country='KR', metric='trade', category='apartment', level=0, region_code='00', period='LAST4Q')
        self.assertEqual(national['median_unit_price'], 15000000)                 # 2000만 4건 + 1000만 4건
        self.assertEqual(gangnam['price_index'], round(20000000 / 15000000.0 * 100, 1))
        all_period = self.stat(country='KR', metric='trade', category='apartment', level=2, region_code='11680', period='ALL')
        self.assertEqual(all_period['n_records'], 5)                              # 해제 거래는 빠진다
        quarter = self.stat(country='KR', metric='trade', category='apartment', level=1, region_code='11', period='2025Q1')
        self.assertEqual(quarter['n_records'], 2)
        # 분기는 시군구 수준을 만들지 않는다
        self.assertEqual(self.conn.execute("SELECT COUNT(*) FROM tb_market_region_stat WHERE level = 2 AND period LIKE '%Q%' "
                                           "AND period != 'LAST4Q'").fetchone()[0], 0)
        jp = self.stat(country='JP', metric='trade', category='apartment', level=2, region_code='13104', period='ALL')
        self.assertEqual((jp['median_unit_price'], jp['currency'], jp['price_index']), (1200000, 'JPY', 133.3))

    def test_competition_stats(self):
        analysis.build_stats(self.conn)
        seoul = self.stat(country='KR', metric='competition', category='kr_apt', level=1, region_code='11', period='ALL')
        self.assertEqual((seoul['supply_units'], seoul['applications'], seoul['competition_rate']), (200, 2300, 11.5))
        nation = self.stat(country='KR', metric='competition', category='kr_apt', level=0, region_code='00', period='2025')
        self.assertEqual(nation['supply_units'], 250)
        self.assertEqual(self.conn.execute("SELECT COUNT(*) FROM tb_market_region_stat WHERE metric = 'competition' "
                                           "AND level = 2 AND period = 'ALL' AND country = 'KR'").fetchone()[0], 2)
        # 다시 만들면 덮어쓴다
        first = self.conn.execute('SELECT COUNT(*) FROM tb_market_region_stat').fetchone()[0]
        analysis.build_stats(self.conn)
        self.assertEqual(self.conn.execute('SELECT COUNT(*) FROM tb_market_region_stat').fetchone()[0], first)

    def test_export(self):
        analysis.build_stats(self.conn)
        out = os.path.join(self.temp.dir, 'market-data.js')
        analysis.export(self.conn, out, fx_krw_per_jpy=9.3)
        with open(out, encoding='utf-8') as f:
            text = f.read()
        self.assertTrue(text.startswith('window.KOREAREB_MARKET = '))
        payload = json.loads(text[len('window.KOREAREB_MARKET = '):].rstrip().rstrip(';'))
        self.assertEqual(payload['meta']['fx_krw_per_jpy'], 9.3)
        self.assertEqual(payload['regions']['KR:11680'][2], '서울특별시 강남구')
        self.assertEqual(len(payload['stats'][0]), len(payload['stat_columns']))
        top = [p for p in payload['top_projects'] if p['country'] == 'KR']
        self.assertEqual(top[0]['name'], '강남 단지')
        self.assertEqual(top[0]['rate'], 20.0)


class CliTest(unittest.TestCase):
    def setUp(self):
        self.temp = TempDb()

    def tearDown(self):
        self.temp.cleanup()

    def run_cli(self, module, args, env=None):
        out, err = io.StringIO(), io.StringIO()
        saved = dict(os.environ)
        try:
            for name in ('KOREAREB_DATA_GO_KR_KEY', 'KOREAREB_REINFOLIB_KEY', 'KOREAREB_MARKET_CONFIG'):
                os.environ.pop(name, None)
            os.environ.update(env or {})
            with redirect_stdout(out), redirect_stderr(err):
                code = module.main(['--db', self.temp.path] + args)
        finally:
            os.environ.clear()
            os.environ.update(saved)
        return code, out.getvalue(), err.getvalue()

    def test_regions_status_analyze_export(self):
        code, out, _ = self.run_cli(market_data, ['regions'])
        self.assertEqual(code, 0)
        self.assertIn('지역', out)
        code, out, _ = self.run_cli(market_data, ['status'])
        status = json.loads(out)
        self.assertEqual(status['regions'], 11 + 47)                 # 한국 시도·시군구 11곳(옛 코드는 새 코드로), 일본 47
        self.assertEqual(self.run_cli(market_data, ['analyze'])[0], 0)
        out_path = os.path.join(self.temp.dir, 'dash', 'data.js')
        self.assertEqual(self.run_cli(market_data, ['export', '--out', out_path])[0], 0)
        self.assertTrue(os.path.exists(out_path))

    def test_missing_key_message(self):
        code, _, err = self.run_cli(market_data, ['kr-trade', '--from', '202501', '--to', '202501', '--regions', '11680'])
        self.assertEqual(code, 2)
        self.assertIn('ERROR', err)
        self.assertIn('KOREAREB_DATA_GO_KR_KEY', err)

    def test_bad_db_path(self):
        out, errs = io.StringIO(), io.StringIO()
        with redirect_stdout(out), redirect_stderr(errs):
            code = market_data.main(['--db', os.path.join(self.temp.dir, 'nope', 'x.db'), 'status'])
        self.assertEqual(code, 2)
        self.assertIn('DB 파일이 없습니다', errs.getvalue())

    def test_jp_fraud_cli(self):
        path = os.path.join(self.temp.dir, 'a.csv')
        with open(path, 'w', encoding='utf-8-sig') as f:
            f.write('募集回,申込番号,氏名,生年月日,区分,都内転入日,申込日\n2026-08,1,見本,1980-01-01,単身者向,2026-08-01,2026-08-05\n')
        code, out, _ = self.run_cli(jp_fraud, ['import', '--scheme', 'jp_public_housing', path])
        self.assertEqual(code, 0)
        code, out, _ = self.run_cli(jp_fraud, ['score'])
        self.assertEqual(code, 0)
        self.assertIn('상 1', out)                                     # 신청 4일 전 전입 + 단신 거주 부족 = 60
        code, _, err = self.run_cli(jp_fraud, ['import', '--scheme', 'jp_public_housing', os.path.join(self.temp.dir, 'none.csv')])
        self.assertNotEqual(code, 0)


if __name__ == '__main__':
    unittest.main()
