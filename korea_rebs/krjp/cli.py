"""명령줄 도구(market_data.py, jp_fraud.py)가 함께 쓰는 옵션."""

import argparse


def common_options():
    """--db, --config 를 하위 명령 앞이나 뒤 어디에 써도 되게 하는 부모 파서 (뒤에 쓴 값이 이긴다).

    하위 명령 쪽 기본값을 SUPPRESS 로 둬야, 앞에 쓴 값을 하위 명령의 기본값 None 으로 덮지 않는다.
    """
    common = argparse.ArgumentParser(add_help=False)
    common.add_argument('--db', default=argparse.SUPPRESS, help='앱 DB 경로 (기본: 환경 변수 KOREAREB_DB)')
    common.add_argument('--config', default=argparse.SUPPRESS, help='설정 JSON (선택)')
    return common
