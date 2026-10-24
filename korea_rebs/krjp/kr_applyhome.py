"""한국: 청약홈 분양정보 + 주택형별 청약 경쟁률 (한국부동산원, 공공데이터포털 odcloud).

분양정보(APT): HOUSE_MANAGE_NO, PBLANC_NO, HOUSE_NM, HSSPLY_ADRES(공급위치), SUBSCRPT_AREA_CODE_NM(공급지역),
              RCRIT_PBLANC_DE(모집공고일), TOT_SUPLY_HSHLDCO(공급규모) …
경쟁률(APT):  HOUSE_MANAGE_NO, PBLANC_NO, MODEL_NO, HOUSE_TY(주택형), SUPLY_HSHLDCO(공급세대수),
              SUBSCRPT_RANK_CODE(순위), RESIDE_SECD/RESIDE_SENM(거주지역), REQ_CNT(접수건수), CMPET_RATE(경쟁률),
              LWET_SCORE/AVRG_SCORE/TOP_SCORE(당첨가점)
경쟁률은 '1순위 접수 건수 ÷ 일반공급 세대수'로 다시 계산한다 (CMPET_RATE 는 미달이면 '(△3)' 같은 글자라서).
"""

import time

from . import db as dbmod
from . import regions
from . import util
from . import webapi

SOURCE = 'applyhome'
SCHEME = 'kr_apt'
PAGE_SIZE = 500


def _check(payload):
    if not isinstance(payload, dict):
        raise webapi.ApiError('청약홈 응답 형식이 다릅니다')
    if 'data' not in payload:
        raise webapi.ApiError('청약홈 API 오류: %s %s' % (payload.get('code', ''), payload.get('msg', payload)))
    return payload


def _pages(config, endpoint, extra_params, get_json, sleep, label):
    key = webapi.normalize_service_key(config.require('data_go_kr_key'))
    page = 1
    while True:
        params = [('serviceKey', key), ('page', page), ('perPage', PAGE_SIZE)] + list(extra_params)
        payload = _check(get_json(webapi.build_url(endpoint, params), secrets=[key]))
        rows = payload.get('data') or []
        for row in rows:
            yield row
        total = payload.get('matchCount') or payload.get('totalCount') or 0
        util.progress('청약홈 %s %d쪽 (%d/%d)' % (label, page, min(page * PAGE_SIZE, total), total))
        if not rows or page * PAGE_SIZE >= total:
            break
        page += 1
        sleep(config.request_interval)


def fetch_projects(config, start_date, end_date, get_json=webapi.get_json, sleep=time.sleep):
    """모집공고일이 start~end 인 APT 분양정보."""
    params = [('cond[RCRIT_PBLANC_DE::GTE]', start_date), ('cond[RCRIT_PBLANC_DE::LTE]', end_date)]
    result = {}
    for row in _pages(config, config.endpoints['applyhome_detail'], params, get_json, sleep, '분양정보'):
        announced = util.parse_date(row.get('RCRIT_PBLANC_DE'))
        # 필터를 무시하는 경우에도 기간 밖은 버린다
        if not announced or announced < start_date or announced > end_date:
            continue
        result[str(row.get('HOUSE_MANAGE_NO'))] = row
    return result


def fetch_competition(config, project_ids, get_json=webapi.get_json, sleep=time.sleep):
    """주택형별 경쟁률. 단지마다 HOUSE_MANAGE_NO 로 걸러 받는다.

    API 가 필터를 무시하고 다른 단지 행을 주면, 전체를 한 번 훑어서 필요한 단지만 남긴다.
    """
    wanted = set(str(p) for p in project_ids)
    endpoint = config.endpoints['applyhome_competition']
    rows = []
    for index, project in enumerate(sorted(wanted), 1):
        page_rows = list(_pages(config, endpoint, [('cond[HOUSE_MANAGE_NO::EQ]', project)], get_json, sleep,
                                '경쟁률 %d/%d' % (index, len(wanted))))
        if any(str(r.get('HOUSE_MANAGE_NO')) != project for r in page_rows):
            # 필터가 무시되면 방금 받은 것이 이미 전체 목록이다
            util.info('경쟁률 API 가 단지 필터를 지원하지 않아 전체 목록에서 고릅니다.')
            return [r for r in page_rows if str(r.get('HOUSE_MANAGE_NO')) in wanted]
        rows.extend(page_rows)
        sleep(config.request_interval)
    return rows


def to_competition_rows(projects, competition_rows, matcher):
    """단지 정보 + 경쟁률 행 → tb_market_competition 행 (주택형마다 1행)."""
    groups = {}
    for row in competition_rows:
        manage_no = str(row.get('HOUSE_MANAGE_NO'))
        key = (manage_no, str(row.get('PBLANC_NO')), str(row.get('MODEL_NO')), util.clean(row.get('HOUSE_TY')))
        groups.setdefault(key, []).append(row)

    result = []
    for (manage_no, pblanc_no, model_no, house_ty), rows in sorted(groups.items()):
        project = projects.get(manage_no)
        if project is None:
            continue
        supply = max((util.to_int(r.get('SUPLY_HSHLDCO')) or 0) for r in rows)
        rank1 = [r for r in rows if util.clean(r.get('SUBSCRPT_RANK_CODE')) in ('1', '01')]
        applications = sum((util.to_int(r.get('REQ_CNT')) or 0) for r in (rank1 or rows))
        local = [r for r in rank1 if util.clean(r.get('RESIDE_SECD')) in ('1', '01')] or rank1 or rows
        scores = local[0]

        pref, region = matcher.match(project.get('HSSPLY_ADRES'))
        if not pref:
            pref = regions.kr_pref_from_short(project.get('SUBSCRPT_AREA_CODE_NM'))

        result.append({
            'comp_key': util.make_key(SOURCE, manage_no, pblanc_no, model_no, house_ty),
            'country': regions.KR,
            'scheme': SCHEME,
            'source': SOURCE,
            'project_id': manage_no,
            'project_name': util.clean(project.get('HOUSE_NM')) or None,
            'unit_type': house_ty or None,
            'pref_code': pref,
            'region_code': region,
            'announce_date': util.parse_date(project.get('RCRIT_PBLANC_DE')),
            'supply_units': supply,
            'applications': applications,
            'competition_rate': (applications / float(supply)) if supply else None,
            'min_score': util.to_float(scores.get('LWET_SCORE')),
            'avg_score': util.to_float(scores.get('AVRG_SCORE')),
            'max_score': util.to_float(scores.get('TOP_SCORE')),
            'avg_price': None,
            'currency': 'KRW',
        })
    return result


def fetch(conn, config, start_date, end_date, matcher, get_json=webapi.get_json, sleep=time.sleep):
    projects = fetch_projects(config, start_date, end_date, get_json=get_json, sleep=sleep)
    util.info('분양 단지 %d곳' % len(projects))
    competition = fetch_competition(config, projects.keys(), get_json=get_json, sleep=sleep) if projects else []
    rows = to_competition_rows(projects, competition, matcher)
    dbmod.upsert_rows(conn, 'tb_market_competition', ['comp_key'], rows)
    dbmod.upsert_rows(conn, 'tb_market_fetch_log', ['source', 'scope', 'period'], [{
        'source': SOURCE, 'scope': 'ALL', 'period': '%s~%s' % (start_date, end_date),
        'row_count': len(rows), 'fetched_at': util.now_text()}])
    return len(rows)
