"""한·일 부동산 공공데이터 수집, 지역별 집계, 대시보드 내보내기.

예)
  python market_data.py regions --db "C:/…/koreareb.db"                 # 한국 시도·시군구 + 일본 都道府県
  python market_data.py regions --jp --db …                            # + 일본 市区町村 (API 키 필요)
  python market_data.py kr-trade --from 202501 --to 202512 --prefs 11,41 --db …
  python market_data.py kr-competition --from 2025-01-01 --to 2025-12-31 --db …
  python market_data.py jp-trade --years 2024-2025 --prefs 13,14,27 --db …
  python market_data.py import-competition --scheme jp_public_housing 練馬区_抽選倍率.csv --db …
  python market_data.py analyze --db …
  python market_data.py export --out dashboard/data/market-data.js --db …
  python market_data.py status --db …

--db, --config 는 하위 명령 앞뒤 어디에 써도 된다 (앱은 앞에 쓴다).
API 키는 환경 변수 KOREAREB_DATA_GO_KR_KEY, KOREAREB_REINFOLIB_KEY 로 넘긴다 (명령줄에 넣지 않는다).
진행 상황은 'PROGRESS …' 줄로, 오류는 표준 오류의 'ERROR …' 줄로 나온다.
"""

import argparse
import datetime
import json
import os
import sys

from krjp import analysis, cli, config as configmod, csv_import, db as dbmod, jp_reinfolib, kr_applyhome, kr_molit, regions, util
from krjp.webapi import ApiError

HERE = os.path.dirname(os.path.abspath(__file__))


def _range(text, low, high):
    """'2023-2025' / '1,3' / 'all' → 정수 목록."""
    text = (text or 'all').strip().lower()
    if text == 'all':
        return list(range(low, high + 1))
    result = []
    for part in text.split(','):
        if '-' in part:
            a, b = part.split('-', 1)
            result.extend(range(int(a), int(b) + 1))
        elif part.strip():
            result.append(int(part))
    return result


def _kr_codes(conn, args, cfg):
    rows = regions.kr_regions_from_bjdong(conn, cfg.extra_regions_csv)
    if args.regions:
        return [regions.kr_current_code(c.strip()) for c in args.regions.split(',') if c.strip()]
    codes = regions.kr_district_codes(rows)
    if args.prefs:
        prefs = set(regions.kr_current_code(p.strip()) for p in args.prefs.split(','))
        codes = [c for c in codes if c[:2] in prefs]
    return codes


def cmd_regions(conn, args, cfg):
    rows = regions.kr_regions_from_bjdong(conn, cfg.extra_regions_csv) + regions.jp_prefecture_rows()
    dbmod.upsert_rows(conn, 'tb_region', ['country', 'region_code'], rows)
    util.info('지역 %d곳 (한국 시도·시군구, 일본 都道府県)' % len(rows))
    if args.jp:
        prefs = [p.zfill(2) for p in args.jp_prefs.split(',')] if args.jp_prefs else None
        count = jp_reinfolib.fetch_regions(conn, cfg, prefs)
        util.info('일본 지역 %d곳' % count)


def cmd_kr_trade(conn, args, cfg):
    codes = _kr_codes(conn, args, cfg)
    if not codes:
        raise SystemExit('받을 시군구가 없습니다. 먼저 regions 를 실행하거나 --regions 로 지정하세요.')
    total = kr_molit.fetch(conn, cfg, codes, args.start, args.end, refresh=args.refresh, today=datetime.date.today())
    util.info('한국 실거래 %d건 저장' % total)


def cmd_kr_competition(conn, args, cfg):
    region_rows = regions.kr_regions_from_bjdong(conn, cfg.extra_regions_csv)
    total = kr_applyhome.fetch(conn, cfg, args.start, args.end, regions.KrRegionMatcher(region_rows))
    util.info('한국 청약 경쟁률 %d건(주택형) 저장' % total)


def cmd_jp_trade(conn, args, cfg):
    prefs = ['%02d' % p for p in _range(args.prefs, 1, 47)]
    total = jp_reinfolib.fetch(conn, cfg, _range(args.years, 2005, datetime.date.today().year),
                               _range(args.quarters, 1, 4), prefs, refresh=args.refresh)
    util.info('일본 거래가격 %d건 저장' % total)


def cmd_import_competition(conn, args, cfg):
    count, skipped = csv_import.import_competition(conn, args.csv, args.scheme)
    util.info('경쟁률 %d건 가져옴, %d건 건너뜀 (필수 값 없음)' % (count, skipped))


def cmd_analyze(conn, args, cfg):
    count = analysis.build_stats(conn)
    util.info('지역별 집계 %d행' % count)


def cmd_export(conn, args, cfg):
    out = args.out or os.path.join(HERE, 'dashboard', 'data', 'market-data.js')
    os.makedirs(os.path.dirname(os.path.abspath(out)), exist_ok=True)
    count = analysis.export(conn, out, fx_krw_per_jpy=args.fx)
    util.info('대시보드 데이터 %d행 → %s' % (count, out))


def cmd_status(conn, args, cfg):
    status = {
        'regions': dbmod.scalar(conn, 'SELECT COUNT(*) FROM tb_region'),
        'trades': [dict(r) for r in conn.execute(
            'SELECT country, property_type, COUNT(*) AS rows, MIN(deal_year) AS from_year, MAX(deal_year) AS to_year '
            'FROM tb_market_trade GROUP BY country, property_type')],
        'competition': [dict(r) for r in conn.execute(
            'SELECT country, scheme, COUNT(*) AS rows, MIN(announce_date) AS from_date, MAX(announce_date) AS to_date '
            'FROM tb_market_competition GROUP BY country, scheme')],
        'stats': dbmod.scalar(conn, 'SELECT COUNT(*) FROM tb_market_region_stat'),
        'jp_applicants': [dict(r) for r in conn.execute(
            'SELECT scheme, COUNT(*) AS rows FROM tb_jp_applicant GROUP BY scheme')],
    }
    sys.stdout.write(json.dumps(status, ensure_ascii=False) + '\n')


def build_parser():
    parser = argparse.ArgumentParser(description='한·일 부동산 공공데이터 수집·집계')
    parser.add_argument('--db', help='앱 DB 경로 (기본: 환경 변수 KOREAREB_DB)')
    parser.add_argument('--config', help='설정 JSON (선택)')
    sub = parser.add_subparsers(dest='command')
    common = [cli.common_options()]     # --db, --config 는 하위 명령 뒤에 써도 된다

    p = sub.add_parser('regions', parents=common, help='지역 마스터 만들기')
    p.add_argument('--jp', action='store_true', help='일본 市区町村도 받기 (API 키 필요)')
    p.add_argument('--jp-prefs', help='일본 都道府県 코드 (예: 13,14). 비우면 47곳 전부')
    p.set_defaults(func=cmd_regions)

    p = sub.add_parser('kr-trade', parents=common, help='한국 아파트 매매 실거래가')
    p.add_argument('--from', dest='start', required=True, help='계약년월 YYYYMM')
    p.add_argument('--to', dest='end', required=True, help='계약년월 YYYYMM')
    p.add_argument('--regions', help='시군구 5자리 코드 (쉼표)')
    p.add_argument('--prefs', help='시도 2자리 코드 (쉼표)')
    p.add_argument('--refresh', action='store_true', help='이미 받은 달도 다시 받기')
    p.set_defaults(func=cmd_kr_trade)

    p = sub.add_parser('kr-competition', parents=common, help='한국 청약홈 APT 경쟁률')
    p.add_argument('--from', dest='start', required=True, help='모집공고일 YYYY-MM-DD')
    p.add_argument('--to', dest='end', required=True, help='모집공고일 YYYY-MM-DD')
    p.set_defaults(func=cmd_kr_competition)

    p = sub.add_parser('jp-trade', parents=common, help='일본 不動産情報ライブラリ 거래가격')
    p.add_argument('--years', required=True, help='예: 2024 / 2023-2025')
    p.add_argument('--quarters', default='all', help='예: 1-4 / 1,2')
    p.add_argument('--prefs', default='13', help='都道府県 코드 (예: 13,14,27 / all)')
    p.add_argument('--refresh', action='store_true')
    p.set_defaults(func=cmd_jp_trade)

    p = sub.add_parser('import-competition', parents=common, help='일본 공영주택·신축 맨션 경쟁률 CSV')
    p.add_argument('--scheme', required=True, choices=csv_import.SCHEMES)
    p.add_argument('csv')
    p.set_defaults(func=cmd_import_competition)

    p = sub.add_parser('analyze', parents=common, help='지역별 집계 다시 만들기')
    p.set_defaults(func=cmd_analyze)

    p = sub.add_parser('export', parents=common, help='대시보드 데이터 내보내기')
    p.add_argument('--out', help='JS 파일 경로 (기본: dashboard/data/market-data.js)')
    p.add_argument('--fx', type=float, help='엔화 환산용 원/엔 환율 (예: 9.3). 없으면 각 나라 통화로만 보여 준다')
    p.set_defaults(func=cmd_export)

    p = sub.add_parser('status', parents=common, help='저장된 데이터 요약 (JSON)')
    p.set_defaults(func=cmd_status)
    return parser


def main(argv=None):
    for stream in (sys.stdout, sys.stderr):
        try:
            stream.reconfigure(encoding='utf-8')
        except (AttributeError, ValueError):
            pass

    parser = build_parser()
    args = parser.parse_args(argv)
    if not getattr(args, 'func', None):
        parser.print_help()
        return 2

    try:
        cfg = configmod.load(args.config)
        conn = dbmod.connect(args.db)
        try:
            args.func(conn, args, cfg)
        finally:
            conn.close()
    except ApiError as e:
        sys.stderr.write('ERROR %s\n' % e)
        return 3
    except csv_import.ImportError_ as e:
        sys.stderr.write('ERROR %s\n' % e)
        return 4
    except (FileNotFoundError, SystemExit) as e:
        message = e.code if isinstance(e, SystemExit) else e
        if isinstance(message, int):
            return message
        sys.stderr.write('ERROR %s\n' % message)
        return 2
    return 0


if __name__ == '__main__':
    sys.exit(main())
