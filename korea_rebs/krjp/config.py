"""설정: API 키와 주소.

키는 환경 변수로 받는다 (앱이 실행할 때 넘긴다). 직접 실행할 때는 설정 파일을 써도 된다.
  KOREAREB_DATA_GO_KR_KEY   공공데이터포털 서비스 키 (실거래가, 청약홈)
  KOREAREB_REINFOLIB_KEY    不動産情報ライブラリ API 키
  KOREAREB_MARKET_CONFIG    설정 JSON 경로 (선택). 예: {"endpoints": {"applyhome_competition": "…"}}
키를 저장소나 명령줄 인자에 넣지 말 것 (명령줄은 다른 프로그램이 볼 수 있다).
"""

import json
import os

BASE_DIR = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

DEFAULT_ENDPOINTS = {
    # 국토교통부_아파트 매매 실거래가 상세 자료 (data.go.kr 15126468)
    'molit_apt_trade': 'https://apis.data.go.kr/1613000/RTMSDataSvcAptTradeDev/getRTMSDataSvcAptTradeDev',
    # 한국부동산원_청약홈 분양정보 조회 서비스 (APT 분양정보 상세)
    'applyhome_detail': 'https://api.odcloud.kr/api/ApplyhomeInfoDetailSvc/v1/getAPTLttotPblancDetail',
    # 한국부동산원_청약홈 청약접수 경쟁률 및 특별공급 신청현황 조회 서비스 (APT 분양정보/경쟁률)
    # 공공데이터포털 Swagger 의 오퍼레이션 이름이 다르면 설정 파일 endpoints 로 바꾼다.
    'applyhome_competition': 'https://api.odcloud.kr/api/ApplyhomeInfoCmpetRtSvc/v1/getAPTLttotPblancCmpet',
    # 不動産情報ライブラリ: 不動産価格（取引価格・成約価格）情報 / 都道府県内市区町村一覧
    'reinfolib_trade': 'https://www.reinfolib.mlit.go.jp/ex-api/external/XIT001',
    'reinfolib_cities': 'https://www.reinfolib.mlit.go.jp/ex-api/external/XIT002',
}


class Config(object):
    def __init__(self, data=None, environ=None):
        data = data or {}
        environ = os.environ if environ is None else environ
        self.data_go_kr_key = environ.get('KOREAREB_DATA_GO_KR_KEY') or data.get('data_go_kr_service_key') or ''
        self.reinfolib_key = environ.get('KOREAREB_REINFOLIB_KEY') or data.get('reinfolib_api_key') or ''
        self.endpoints = dict(DEFAULT_ENDPOINTS)
        self.endpoints.update(data.get('endpoints') or {})
        self.extra_regions_csv = data.get('kr_extra_regions_csv') or os.path.join(BASE_DIR, 'config', 'kr_extra_regions.csv')
        self.rules_path = data.get('jp_rules') or os.path.join(BASE_DIR, 'config', 'jp_rules.json')
        self.request_interval = float(data.get('request_interval_seconds', 0.2))

    def require(self, name):
        value = getattr(self, name)
        if not value:
            env = {'data_go_kr_key': 'KOREAREB_DATA_GO_KR_KEY', 'reinfolib_key': 'KOREAREB_REINFOLIB_KEY'}[name]
            raise SystemExit('API 키가 없습니다. 환경 변수 %s 를 설정하거나 앱의 시장분석 > 데이터 수집 탭에서 키를 저장하세요.' % env)
        return value


def load(path=None, environ=None):
    environ = os.environ if environ is None else environ
    path = path or environ.get('KOREAREB_MARKET_CONFIG')
    data = {}
    if path and os.path.exists(path):
        with open(path, encoding='utf-8-sig') as f:
            data = json.load(f)
    return Config(data, environ)
