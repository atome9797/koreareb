"""CSV 가져오기와 일본 위험 점수 규칙. 이름·주소는 모두 지어낸 예시다."""

import os
import unittest

from krjp import csv_import, jp_rules
from tests.helpers import TempDb

import jp_fraud


def write(path, text, encoding='utf-8-sig'):
    with open(path, 'w', encoding=encoding, newline='') as f:
        f.write(text)


PUBLIC_APPLICANTS = '\n'.join([
    '募集回,住宅名,区分,申込番号,氏名,生年月日,電話番号,メール,住所,市区町村コード,都内転入日,世帯人数,世帯変更日,世帯所得,障害者等世帯,現住居,申込日',
    # 1 정상
    '2026-08,예시団地,家族向,A001,見本 一郎,1980-01-01,090-0000-0001,a@example.com,東京都練馬区見本1-1-1,13120,2015-04-01,3,,2000000,N,民間賃貸,2026-08-05',
    # 2 신청 30일 전 전입 + 소득 초과 + 자가
    '2026-08,예시団地,家族向,A002,見本 二郎,1975-02-02,090-0000-0002,,東京都練馬区見本2丁目3番4号,13120,2026-07-06,2,,2500000,N,持家,2026-08-05',
    # 3 단신인데 1년 거주, 세대 2명, 소득이 2인 기준(2,276,000)의 97%
    '2026-08,예시団地,単身者向,A003,見本 三子,1990-03-03,090-0000-0003,,東京都練馬区見本3-3,13120,2025-08-05,2,2026-06-01,2200000,N,民間賃貸,2026-08-05',
    # 4·5 같은 사람이 두 번 (같은 회차)
    '2026-08,예시団地,家族向,A004,見本 四郎,1970-04-04,090-0000-0004,,東京都練馬区見本4-4,13120,2010-01-01,4,,1000000,N,民間賃貸,2026-08-05',
    '2026-08,별団地,家族向,A005,見本　四郎,1970-04-04,090-0000-0009,,東京都練馬区見本9-9,13120,2010-01-01,4,,1000000,N,民間賃貸,2026-08-05',
    # 6 2번과 같은 주소(표기만 다름), 다른 사람
    '2026-08,예시団地,家族向,A006,見本 六子,1985-06-06,090-0000-0006,,東京都練馬区見本2-3-4,13120,2012-01-01,2,,1500000,N,民間賃貸,2026-08-05',
    # 7 장애인 등 세대: 소득 2,600,000 은 일반 기준(1명 1,896,000)은 넘지만 장애 기준(2,568,000)도 넘는다
    '2026-08,예시団地,家族向,A007,見本 七郎,1960-07-07,,,東京都練馬区見本7,13120,2000-01-01,1,,2600000,Y,民間賃貸,2026-08-05',
    # 회차·번호 없는 행은 건너뛴다
    ',예시団地,家族向,,누구,,,,,,,,,,,,',
])

CONDO_APPLICANTS = '\n'.join([
    '販売期,物件名,住戸番号,申込番号,氏名,生年月日,電話番号,メール,住所,市区町村コード,法人名,購入目的,本人居住,契約名義,引渡前売出,申込日',
    '2026-09,見本タワー,1001,B001,見本 花子,1988-01-01,080-1111-0001,h@example.com,東京都港区見本1,13103,,自己居住,Y,見本 花子,N,2026-09-01',
    # 같은 사람이 두 호실, 투자 목적
    '2026-09,見本タワー,1002,B002,見本 太郎,1980-05-05,080-1111-0002,t@example.com,東京都港区見本2,13103,,投資,N,,N,2026-09-01',
    '2026-09,見本タワー,1003,B003,見本 太郎,1980-05-05,080-1111-0002,t@example.com,東京都港区見本2,13103,,投資,N,,N,2026-09-01',
    # 다른 이름인데 연락처가 B002 와 같다 (명의 대여 의심) + 계약 명의 다름 + 인도 전 매물
    '2026-09,見本タワー,1004,B004,見本 次郎,1999-09-09,080-1111-0002,,東京都港区見本5,13103,,自己居住,Y,見本 太郎,Y,2026-09-01',
    # 법인
    '2026-09,見本タワー,1005,B005,,,,,東京都港区見本6,13103,見本商事株式会社,投資,N,,N,2026-09-01',
])


class CsvImportTest(unittest.TestCase):
    def setUp(self):
        self.temp = TempDb()
        self.conn = self.temp.connect()

    def tearDown(self):
        self.conn.close()
        self.temp.cleanup()

    def test_public_housing_competition_cp932(self):
        path = os.path.join(self.temp.dir, 'nerima.csv')
        write(path, '申込番号,住宅名,入居人数,募集戸数（単位：戸）,応募数（単位：件）,倍率（単位：倍）,募集回,都道府県,市区町村コード\r\n'
                    '1,見本中三丁目,1人,1,186,186.0,令和8年8月,東京都,13120\r\n'
                    '2,見本北,2人,4,30,7.5,2026-08,東京都,\r\n'
                    '3,空欄,,,,,2026-08,,\r\n', encoding='cp932')
        count, skipped = csv_import.import_competition(self.conn, path, 'jp_public_housing')
        self.assertEqual((count, skipped), (2, 1))
        rows = self.conn.execute('SELECT * FROM tb_market_competition ORDER BY supply_units').fetchall()
        self.assertEqual((rows[0]['announce_date'], rows[0]['competition_rate'], rows[0]['region_code']), ('2026-08-01', 186.0, '13120'))
        self.assertEqual((rows[1]['pref_code'], rows[1]['region_code'], rows[1]['competition_rate']), ('13', None, 7.5))
        # 다시 가져와도 중복되지 않는다
        csv_import.import_competition(self.conn, path, 'jp_public_housing')
        self.assertEqual(self.conn.execute('SELECT COUNT(*) FROM tb_market_competition').fetchone()[0], 2)

    def test_condo_competition_price_in_man_yen(self):
        path = os.path.join(self.temp.dir, 'condo.csv')
        write(path, '販売期,物件名,都道府県コード,市区町村コード,販売戸数,登録数,平均価格(万円)\n2026-09,見本タワー,13,13103,50,600,12500\n')
        csv_import.import_competition(self.conn, path, 'jp_new_condo')
        row = self.conn.execute('SELECT * FROM tb_market_competition').fetchone()
        self.assertEqual((row['competition_rate'], row['avg_price'], row['currency']), (12.0, 125000000.0, 'JPY'))

    def test_missing_columns(self):
        path = os.path.join(self.temp.dir, 'bad.csv')
        write(path, '住宅名,倍率\nx,1\n')
        with self.assertRaises(csv_import.ImportError_) as ctx:
            csv_import.import_competition(self.conn, path, 'jp_public_housing')
        self.assertIn('round', str(ctx.exception))

    def test_applicants_reimport_keeps_inspection(self):
        path = os.path.join(self.temp.dir, 'public.csv')
        write(path, PUBLIC_APPLICANTS)
        count, skipped = csv_import.import_applicants(self.conn, path, 'jp_public_housing')
        self.assertEqual((count, skipped), (7, 1))
        row = self.conn.execute("SELECT * FROM tb_jp_applicant WHERE application_no = 'A002'").fetchone()
        self.assertEqual((row['moved_in_date'], row['household_income'], row['region_code'], row['inspection_status_result']),
                         ('2026-07-06', 2500000.0, '13120', 'N'))
        self.conn.execute("UPDATE tb_jp_applicant SET inspection_status_result = 'Y', unauth_decision = 1, "
                          "unauth_type = '거주 요건 위장' WHERE application_no = 'A002'")
        csv_import.import_applicants(self.conn, path, 'jp_public_housing')
        row = self.conn.execute("SELECT * FROM tb_jp_applicant WHERE application_no = 'A002'").fetchone()
        self.assertEqual((row['inspection_status_result'], row['unauth_type']), ('Y', '거주 요건 위장'))

    def test_special_category_header(self):
        # 都営住宅 특별 구분(60세 이상·장애인·고교생 이하 자녀 등)을 '特別区分' 머리글로 줘도 같은 열로 읽는다
        path = os.path.join(self.temp.dir, 'special.csv')
        write(path, PUBLIC_APPLICANTS.replace('障害者等世帯', '特別区分'))
        csv_import.import_applicants(self.conn, path, 'jp_public_housing')
        flags = dict(self.conn.execute('SELECT application_no, disabled_household FROM tb_jp_applicant').fetchall())
        self.assertEqual((flags['A007'], flags['A001']), (1, 0))


class RulesTest(unittest.TestCase):
    def setUp(self):
        self.temp = TempDb()
        self.conn = self.temp.connect()
        self.rules = jp_rules.load_rules(None)

    def tearDown(self):
        self.conn.close()
        self.temp.cleanup()

    def _scores(self, text, scheme):
        path = os.path.join(self.temp.dir, scheme + '.csv')
        write(path, text)
        csv_import.import_applicants(self.conn, path, scheme)
        jp_fraud.score(self.conn, self.rules)
        result = {}
        for row in self.conn.execute('SELECT a.application_no, r.* FROM tb_jp_applicant a JOIN tb_jp_risk r USING (applicant_key)'):
            result[row['application_no']] = (row['risk_score'], row['risk_level'], set((row['reason_codes'] or '').split(',')) - {''})
        return result

    def test_public_housing(self):
        s = self._scores(PUBLIC_APPLICANTS, 'jp_public_housing')
        self.assertEqual(s['A001'], (0, '하', set()))
        self.assertEqual(s['A002'][2], {'PH_RECENT_MOVE', 'PH_INCOME_OVER', 'PH_HOUSING_INELIGIBLE', 'PH_DUP_HOUSEHOLD'})
        self.assertEqual(s['A002'][:2], (100, '상'))                          # 25+45+40+30 → 100 상한
        self.assertEqual(s['A003'][2], {'PH_SINGLE_RESIDENCE', 'PH_HOUSEHOLD_CHANGE', 'PH_HOUSEHOLD_MISMATCH', 'PH_INCOME_EDGE'})
        self.assertIn('PH_DUP_PERSON', s['A004'][2])                         # 전각 공백도 같은 사람
        self.assertIn('PH_DUP_PERSON', s['A005'][2])
        self.assertEqual(s['A006'][2], {'PH_DUP_HOUSEHOLD'})                  # '2丁目3番4号' = '2-3-4'
        self.assertEqual(s['A007'][2], {'PH_INCOME_OVER', 'PH_HOUSEHOLD_MISMATCH'})

    def test_new_condo(self):
        s = self._scores(CONDO_APPLICANTS, 'jp_new_condo')
        self.assertEqual(s['B001'], (0, '하', set()))
        self.assertEqual(s['B002'][2], {'NC_MULTI_UNITS', 'NC_INVESTMENT', 'NC_SHARED_CONTACT'})
        self.assertEqual(s['B004'][2], {'NC_SHARED_CONTACT', 'NC_NAME_MISMATCH', 'NC_RESALE_LISTED'})
        self.assertEqual(s['B004'][1], '상')
        self.assertEqual(s['B005'][2], {'NC_CORPORATE', 'NC_INVESTMENT'})
        self.assertEqual(s['B005'][1], '중')

    def test_income_limit_table(self):
        self.assertEqual(jp_rules.income_limit(self.rules, 1, False), 1896000)
        self.assertEqual(jp_rules.income_limit(self.rules, 8, False), 3796000 + 2 * 380000)
        self.assertEqual(jp_rules.income_limit(self.rules, 2, True), 2948000)

    def test_rules_override_file(self):
        path = os.path.join(self.temp.dir, 'rules.json')
        write(path, '{"levels": {"high": 90}, "public_housing": {"weights": {"PH_RECENT_MOVE": 5}}}')
        rules = jp_rules.load_rules(path)
        self.assertEqual((rules['levels']['high'], rules['levels']['medium']), (90, 30))
        self.assertEqual(rules['public_housing']['weights']['PH_RECENT_MOVE'], 5)
        self.assertEqual(rules['public_housing']['weights']['PH_INCOME_OVER'], 45)

    def test_address_key(self):
        self.assertEqual(jp_rules.address_key('東京都練馬区見本2丁目3番4号'), jp_rules.address_key('東京都 練馬区 見本２－３－４'))


if __name__ == '__main__':
    unittest.main()
