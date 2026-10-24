"""한국 실거래가·청약홈 파싱. 응답은 공개 명세의 필드 이름으로 만든 합성 예시다."""

import datetime
import unittest
from urllib.parse import parse_qs, urlparse

from krjp import config as configmod, kr_applyhome, kr_molit, regions, webapi
from tests.helpers import TempDb

TRADE_XML = """<?xml version="1.0" encoding="UTF-8"?>
<response><header><resultCode>000</resultCode><resultMsg>OK</resultMsg></header>
<body><items>
<item><aptNm>예시아파트</aptNm><aptSeq>11680-1</aptSeq><buildYear>2015</buildYear><dealAmount>120,000</dealAmount>
<dealYear>2025</dealYear><dealMonth>3</dealMonth><dealDay>12</dealDay><excluUseAr>84.5</excluUseAr><floor>10</floor>
<jibun>1</jibun><sggCd>11680</sggCd><umdNm>개포동</umdNm><cdealType></cdealType></item>
<item><aptNm>예시아파트</aptNm><aptSeq>11680-1</aptSeq><buildYear>2015</buildYear><dealAmount>120,000</dealAmount>
<dealYear>2025</dealYear><dealMonth>3</dealMonth><dealDay>12</dealDay><excluUseAr>84.5</excluUseAr><floor>10</floor>
<jibun>1</jibun><sggCd>11680</sggCd><umdNm>개포동</umdNm><cdealType></cdealType></item>
<item><aptNm>취소된아파트</aptNm><buildYear>2001</buildYear><dealAmount>50,000</dealAmount>
<dealYear>2025</dealYear><dealMonth>3</dealMonth><dealDay>2</dealDay><excluUseAr>59.9</excluUseAr><floor>3</floor>
<jibun>2</jibun><sggCd>11680</sggCd><umdNm>대치동</umdNm><cdealType>O</cdealType></item>
</items><numOfRows>1000</numOfRows><pageNo>1</pageNo><totalCount>3</totalCount></body></response>""".encode("utf-8")

AUTH_ERROR_XML = b"""<OpenAPI_ServiceResponse><cmmMsgHeader><errMsg>SERVICE ERROR</errMsg>
<returnAuthMsg>SERVICE_KEY_IS_NOT_REGISTERED_ERROR</returnAuthMsg><returnReasonCode>30</returnReasonCode>
</cmmMsgHeader></OpenAPI_ServiceResponse>"""


def page_xml(items, total):
    body = ''.join('<item><aptNm>A%d</aptNm><dealAmount>10,000</dealAmount><dealYear>2025</dealYear>'
                   '<dealMonth>1</dealMonth><dealDay>%d</dealDay><excluUseAr>50</excluUseAr><sggCd>42110</sggCd>'
                   '</item>' % (i, i % 28 + 1) for i in items)
    return ('<response><header><resultCode>000</resultCode></header><body><items>%s</items>'
            '<totalCount>%d</totalCount></body></response>' % (body, total)).encode('utf-8')


class MolitTest(unittest.TestCase):
    def test_parse_and_convert(self):
        items, total = kr_molit.parse_response(TRADE_XML)
        self.assertEqual(total, 3)
        rows = kr_molit.to_trade_rows(items, '11680', '202503')
        self.assertEqual(len(rows), 3)
        # 내용이 같은 두 거래도 따로 남는다
        self.assertEqual(len(set(r['trade_key'] for r in rows)), 3)
        first = rows[0]
        self.assertEqual(first['price'], 1200000000.0)               # 만원 → 원
        self.assertAlmostEqual(first['unit_price'], 1200000000.0 / 84.5)
        self.assertEqual((first['deal_quarter'], first['region_code'], first['pref_code']), (1, '11680', '11'))
        self.assertEqual(rows[2]['is_cancelled'], 1)

    def test_keys_are_stable(self):
        items, _ = kr_molit.parse_response(TRADE_XML)
        a = [r['trade_key'] for r in kr_molit.to_trade_rows(items, '11680', '202503')]
        b = [r['trade_key'] for r in kr_molit.to_trade_rows(items, '11680', '202503')]
        self.assertEqual(a, b)

    def test_errors(self):
        with self.assertRaises(webapi.ApiError) as ctx:
            kr_molit.parse_response(AUTH_ERROR_XML)
        self.assertIn('SERVICE_KEY_IS_NOT_REGISTERED_ERROR', str(ctx.exception))
        nodata = b'<response><header><resultCode>03</resultCode><resultMsg>NODATA_ERROR</resultMsg></header></response>'
        self.assertEqual(kr_molit.parse_response(nodata), ([], 0))

    def test_paging_remap_and_fetch_log(self):
        calls = []

        def fake_get(url, secrets=()):
            query = parse_qs(urlparse(url).query)
            calls.append(query)
            page = int(query['pageNo'][0])
            return page_xml(range((page - 1) * 1000, min(page * 1000, 1500)), 1500)

        cfg = configmod.Config({}, {'KOREAREB_DATA_GO_KR_KEY': 'abc%2Bdef'})
        temp = TempDb()
        try:
            conn = temp.connect()
            total = kr_molit.fetch(conn, cfg, ['51110'], '202501', '202501', get=fake_get, sleep=lambda s: None,
                                   today=datetime.date(2026, 10, 1))
            self.assertEqual(total, 1500)
            self.assertEqual(len(calls), 2)
            self.assertEqual(calls[0]['serviceKey'], ['abc+def'])          # 인코딩 키를 풀어서 한 번만 인코딩
            # 옛 강원 코드 42110 으로 와도 51110 으로 저장
            self.assertEqual(conn.execute("SELECT COUNT(*) FROM tb_market_trade WHERE region_code = '51110'").fetchone()[0], 1500)
            # 이미 받은 달은 다시 부르지 않는다
            kr_molit.fetch(conn, cfg, ['51110'], '202501', '202501', get=fake_get, sleep=lambda s: None,
                           today=datetime.date(2026, 10, 1))
            self.assertEqual(len(calls), 2)
            # 최근 2개월은 다시 받는다
            kr_molit.fetch(conn, cfg, ['51110'], '202509', '202509', get=fake_get, sleep=lambda s: None,
                           today=datetime.date(2026, 10, 1))
            kr_molit.fetch(conn, cfg, ['51110'], '202609', '202609', get=fake_get, sleep=lambda s: None,
                           today=datetime.date(2026, 10, 1))
            kr_molit.fetch(conn, cfg, ['51110'], '202609', '202609', get=fake_get, sleep=lambda s: None,
                           today=datetime.date(2026, 10, 1))
            self.assertEqual(len(calls), 8)
            conn.close()
        finally:
            temp.cleanup()

    def test_missing_key(self):
        cfg = configmod.Config({}, {})
        with self.assertRaises(SystemExit):
            kr_molit.fetch_month(cfg, '11680', '202501', get=lambda *a, **k: TRADE_XML)


PROJECTS = {
    '2025000001': {'HOUSE_MANAGE_NO': '2025000001', 'PBLANC_NO': '2025000001', 'HOUSE_NM': '예시 단지',
                   'HSSPLY_ADRES': '서울특별시 강남구 개포동 1', 'SUBSCRPT_AREA_CODE_NM': '서울',
                   'RCRIT_PBLANC_DE': '2025-05-02'},
    '2025000002': {'HOUSE_MANAGE_NO': '2025000002', 'PBLANC_NO': '2025000002', 'HOUSE_NM': '세종 단지',
                   'HSSPLY_ADRES': '세종특별자치시 반곡동 1', 'SUBSCRPT_AREA_CODE_NM': '세종',
                   'RCRIT_PBLANC_DE': '2025-06-10'},
}


def comp_row(manage_no, model, house_ty, supply, rank, reside, req, low=None):
    return {'HOUSE_MANAGE_NO': manage_no, 'PBLANC_NO': manage_no, 'MODEL_NO': model, 'HOUSE_TY': house_ty,
            'SUPLY_HSHLDCO': supply, 'SUBSCRPT_RANK_CODE': rank, 'RESIDE_SECD': reside, 'REQ_CNT': req,
            'CMPET_RATE': '(△3)' if req < supply else '%.2f' % (req / float(supply)), 'LWET_SCORE': low}


class ApplyhomeTest(unittest.TestCase):
    def setUp(self):
        self.temp = TempDb()
        conn = self.temp.connect()
        self.matcher = regions.KrRegionMatcher(regions.kr_regions_from_bjdong(conn))
        conn.close()

    def tearDown(self):
        self.temp.cleanup()

    def test_rows(self):
        rows = kr_applyhome.to_competition_rows(PROJECTS, [
            comp_row('2025000001', '01', '084.9800A', 100, 1, '01', 1500, '65'),
            comp_row('2025000001', '01', '084.9800A', 100, 1, '03', 500),
            comp_row('2025000001', '01', '084.9800A', 100, 2, '01', 40),
            comp_row('2025000002', '01', '059.9700A', 50, 1, '01', 20),
            comp_row('9999999999', '01', '059', 50, 1, '01', 999),      # 기간 밖 단지는 버린다
        ], self.matcher)
        self.assertEqual(len(rows), 2)
        gangnam = [r for r in rows if r['project_id'] == '2025000001'][0]
        self.assertEqual((gangnam['supply_units'], gangnam['applications']), (100, 2000))   # 1순위만
        self.assertEqual(gangnam['competition_rate'], 20.0)
        self.assertEqual((gangnam['pref_code'], gangnam['region_code']), ('11', '11680'))
        self.assertEqual(gangnam['min_score'], 65.0)
        sejong = [r for r in rows if r['project_id'] == '2025000002'][0]
        self.assertEqual((sejong['region_code'], sejong['competition_rate']), ('36110', 0.4))

    def _config(self):
        return configmod.Config({}, {'KOREAREB_DATA_GO_KR_KEY': 'key'})

    def test_competition_with_filter(self):
        data = [comp_row('2025000001', '01', 'A', 10, 1, '01', 5), comp_row('2025000002', '01', 'B', 10, 1, '01', 5)]

        def fake(url, secrets=()):
            q = parse_qs(urlparse(url).query)
            wanted = q.get('cond[HOUSE_MANAGE_NO::EQ]', [None])[0]
            rows = [r for r in data if wanted is None or r['HOUSE_MANAGE_NO'] == wanted]
            return {'page': 1, 'perPage': 500, 'matchCount': len(rows), 'totalCount': len(data), 'data': rows}

        rows = kr_applyhome.fetch_competition(self._config(), ['2025000001', '2025000002'], get_json=fake,
                                              sleep=lambda s: None)
        self.assertEqual(sorted(r['HOUSE_MANAGE_NO'] for r in rows), ['2025000001', '2025000002'])

    def test_competition_when_filter_ignored(self):
        data = [comp_row('2025000001', '01', 'A', 10, 1, '01', 5), comp_row('X', '01', 'B', 10, 1, '01', 5),
                comp_row('2025000002', '01', 'B', 10, 1, '01', 5)]
        calls = []

        def fake(url, secrets=()):
            calls.append(url)
            return {'page': 1, 'perPage': 500, 'matchCount': len(data), 'totalCount': len(data), 'data': data}

        rows = kr_applyhome.fetch_competition(self._config(), ['2025000001', '2025000002'], get_json=fake,
                                              sleep=lambda s: None)
        self.assertEqual(sorted(r['HOUSE_MANAGE_NO'] for r in rows), ['2025000001', '2025000002'])
        self.assertEqual(len(calls), 1)

    def test_error_payload(self):
        with self.assertRaises(webapi.ApiError):
            list(kr_applyhome._pages(self._config(), 'https://x', [], lambda url, secrets=(): {'code': -401, 'msg': '인증키'},
                                     lambda s: None, 'test'))

    def test_projects_outside_range_dropped(self):
        def fake(url, secrets=()):
            return {'page': 1, 'perPage': 500, 'matchCount': 2, 'data': list(PROJECTS.values())}

        projects = kr_applyhome.fetch_projects(self._config(), '2025-06-01', '2025-06-30', get_json=fake,
                                               sleep=lambda s: None)
        self.assertEqual(list(projects), ['2025000002'])


class RegionTest(unittest.TestCase):
    def test_kr_regions_and_matcher(self):
        temp = TempDb()
        try:
            conn = temp.connect()
            rows = regions.kr_regions_from_bjdong(conn)
            by_code = {r['region_code']: r for r in rows}
            self.assertEqual(by_code['51']['name_ko'], '강원특별자치도')
            self.assertEqual(by_code['51110']['name_ko'], '강원특별자치도 춘천시')
            self.assertEqual(by_code['27720']['name_ko'], '대구광역시 군위군')
            self.assertNotIn('42110', by_code)
            matcher = regions.KrRegionMatcher(rows)
            self.assertEqual(matcher.match('강원도 춘천시 퇴계동 1'), ('51', '51110'))
            self.assertEqual(matcher.match('경기도 수원시 장안구 정자동'), ('41', '41111'))
            self.assertEqual(matcher.match('서울 어딘가'), ('11', None))
            conn.close()
        finally:
            temp.cleanup()

    def test_jp_pref_code(self):
        self.assertEqual(regions.jp_pref_code('東京都'), '13')
        self.assertEqual(regions.jp_pref_code('도쿄도'), '13')
        self.assertEqual(regions.jp_pref_code('Tokyo'), '13')
        self.assertEqual(regions.jp_pref_code('1'), '01')
        self.assertEqual(regions.jp_pref_code('13101'), '13')
        self.assertEqual(regions.jp_pref_code('大阪'), '27')


class WebApiTest(unittest.TestCase):
    def test_redaction_and_key_normalize(self):
        self.assertEqual(webapi.normalize_service_key(' a%2Bb%3D%3D '), 'a+b==')
        text = webapi._redact('https://x?serviceKey=a%2Bb%3D%3D&p=1 key a+b==', ['a+b=='])
        self.assertNotIn('a%2Bb', text)
        self.assertNotIn('a+b==', text)

    def test_get_decompresses_and_redacts(self):
        import gzip
        import io
        import urllib.error
        import urllib.request

        class FakeResponse(io.BytesIO):
            headers = {'Content-Encoding': 'gzip'}

            def __enter__(self):
                return self

            def __exit__(self, *a):
                return False

        original = urllib.request.urlopen
        try:
            urllib.request.urlopen = lambda req, timeout=60: FakeResponse(gzip.compress(b'{"status":"OK"}'))
            self.assertEqual(webapi.get_json('https://example.invalid'), {'status': 'OK'})

            def deny(req, timeout=60):
                raise urllib.error.HTTPError(req.full_url, 401, 'Unauthorized', {}, io.BytesIO(b'bad key SECRET'))
            urllib.request.urlopen = deny
            with self.assertRaises(webapi.ApiError) as ctx:
                webapi.get('https://example.invalid', headers={'Ocp-Apim-Subscription-Key': 'SECRET'},
                           sleep=lambda s: None)
            self.assertNotIn('SECRET', str(ctx.exception))
            self.assertIn('인증', str(ctx.exception))
        finally:
            urllib.request.urlopen = original


if __name__ == '__main__':
    unittest.main()
