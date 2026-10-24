"""일본 신청자 가져오기와 규칙 기반 위험 점수.

예)
  python jp_fraud.py import --scheme jp_public_housing 申込者.csv --db "C:/…/koreareb.db"
  python jp_fraud.py import --scheme jp_new_condo 登録者.csv --db …
  python jp_fraud.py score --db …                     # 모든 일본 신청자 점수 다시 계산
  python jp_fraud.py score --scheme jp_new_condo --round 2026-08 --db …

--db 는 하위 명령 앞뒤 어디에 써도 된다 (앱은 앞에 쓴다).
점수는 점검 순서를 정하는 참고값이다 (규칙·가중치: config/jp_rules.json, 설명: krjp/jp_rules.py).
신청자 개인정보는 DB 에만 저장하고 출력하지 않는다.
"""

import argparse
import sys

from krjp import cli, config as configmod, csv_import, db as dbmod, jp_rules, util

APPLICANT_COLUMNS = ['applicant_key', 'scheme', 'round_id', 'project_name', 'unit_category', 'unit_no',
                     'application_no', 'applicant_name', 'birth_date', 'phone', 'email', 'address',
                     'moved_in_date', 'household_size', 'household_changed_date', 'household_income',
                     'disabled_household', 'current_housing', 'corporate_name', 'purchase_purpose',
                     'will_reside', 'contract_name', 'resale_listed', 'applied_date']


def cmd_import(conn, args, cfg):
    count, skipped = csv_import.import_applicants(conn, args.csv, args.scheme)
    util.info('신청자 %d명 가져옴, %d행 건너뜀 (회차·신청번호 없음)' % (count, skipped))


def score(conn, rules, scheme=None, round_id=None):
    """점수를 다시 계산해 tb_jp_risk 에 쓴다. 중복 규칙은 같은 회차(신축은 같은 물건) 전체를 봐야 하므로 회차 단위로 계산한다."""
    where, params = [], []
    if scheme:
        where.append('scheme = ?')
        params.append(scheme)
    if round_id:
        where.append('round_id = ?')
        params.append(round_id)
    sql = 'SELECT %s FROM tb_jp_applicant' % ', '.join(APPLICANT_COLUMNS)
    if where:
        sql += ' WHERE ' + ' AND '.join(where)
    applicants = [dict(r) for r in conn.execute(sql, params)]

    rows = []
    for scheme_name in csv_import.SCHEMES:
        group = [a for a in applicants if a['scheme'] == scheme_name]
        if not group:
            continue
        if scheme_name == csv_import.SCHEME_PUBLIC:
            results = jp_rules.score_public_housing(group, rules)
        else:
            results = jp_rules.score_new_condo(group, rules)
        for key, reasons in results.items():
            total, level, codes, text = jp_rules.summarize(reasons, rules)
            rows.append({'applicant_key': key, 'risk_score': total, 'risk_level': level,
                         'reason_codes': codes or None, 'reason_text': text or None, 'scored_at': util.now_text()})
    dbmod.upsert_rows(conn, 'tb_jp_risk', ['applicant_key'], rows)
    return rows


def cmd_score(conn, args, cfg):
    rules = jp_rules.load_rules(cfg.rules_path)
    rows = score(conn, rules, args.scheme, args.round)
    levels = {'상': 0, '중': 0, '하': 0}
    for row in rows:
        levels[row['risk_level']] += 1
    util.info('위험 점수 %d명 (상 %d, 중 %d, 하 %d)' % (len(rows), levels['상'], levels['중'], levels['하']))


def build_parser():
    parser = argparse.ArgumentParser(description='일본 신청자 점검')
    parser.add_argument('--db', help='앱 DB 경로 (기본: 환경 변수 KOREAREB_DB)')
    parser.add_argument('--config', help='설정 JSON (선택)')
    sub = parser.add_subparsers(dest='command')
    common = [cli.common_options()]     # --db, --config 는 하위 명령 뒤에 써도 된다

    p = sub.add_parser('import', parents=common, help='신청자 CSV 가져오기')
    p.add_argument('--scheme', required=True, choices=csv_import.SCHEMES)
    p.add_argument('csv')
    p.set_defaults(func=cmd_import)

    p = sub.add_parser('score', parents=common, help='위험 점수 계산')
    p.add_argument('--scheme', choices=csv_import.SCHEMES)
    p.add_argument('--round')
    p.set_defaults(func=cmd_score)
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
    except csv_import.ImportError_ as e:
        sys.stderr.write('ERROR %s\n' % e)
        return 4
    except FileNotFoundError as e:
        sys.stderr.write('ERROR %s\n' % e)
        return 2
    return 0


if __name__ == '__main__':
    sys.exit(main())
