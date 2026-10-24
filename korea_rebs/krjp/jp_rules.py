"""일본 신청자 위험 점수 (규칙 기반).

일본은 판정 결과가 쌓인 학습 데이터가 없어서, 한국처럼 모델로 확률을 내지 않고 제도 요건에 맞춘 규칙으로 점수를 낸다.
점수는 '먼저 볼 신청자'를 고르는 참고값이지 부정 판정이 아니다.

공영주택(都営住宅 기준): 都内 거주(단신은 3년 이상), 동거 친족, 세대 소득 기준, 주택 곤궁(자가·공영주택 거주자 불가)
신축 맨션: 不動産協会 「分譲マンションの投機的短期転売問題にかかる取組みについて」(2025-11-25)
  — 1물건 구입 호수 제한, 신청 명의로 계약·인도·등기, 계약~인도 사이 매도 활동 금지, 구입 목적 확인
기준값과 가중치는 config/jp_rules.json 으로 바꿀 수 있다.
"""

import copy
import json
import os
import re
import unicodedata

from . import util

DEFAULT_RULES = {
    'levels': {'high': 60, 'medium': 30},
    'public_housing': {
        'recent_move_days': 90,
        'single_min_residence_years': 3,
        'household_change_days': 180,
        'income_edge_ratio': 0.95,
        # 都営住宅 所得基準 (円, 세대 인원별 상한). 7명부터는 1명마다 add_per_person 을 더한다.
        # 'disabled' = 특별 구분(60세 이상·장애인·고교 졸업 전 자녀가 있는 세대 등). CSV 의 障害者等世帯 = 1 이면 이 표를 쓴다.
        'income_limits': {
            'general': {'1': 1896000, '2': 2276000, '3': 2656000, '4': 3036000, '5': 3416000, '6': 3796000,
                        'add_per_person': 380000},
            'disabled': {'1': 2568000, '2': 2948000, '3': 3328000, '4': 3708000, '5': 4088000, '6': 4468000,
                         'add_per_person': 380000},
        },
        'single_categories': ['単身者向', '単身', '단신', '1인'],
        'family_categories': ['家族向', '가족', '세대'],
        'ineligible_housing': ['持家', '持ち家', '自己所有', '公営住宅', '都営住宅', '자가', '공영주택'],
        'weights': {
            'PH_RECENT_MOVE': 25, 'PH_SINGLE_RESIDENCE': 35, 'PH_INCOME_OVER': 45, 'PH_INCOME_EDGE': 15,
            'PH_HOUSEHOLD_CHANGE': 20, 'PH_HOUSEHOLD_MISMATCH': 30, 'PH_HOUSING_INELIGIBLE': 40,
            'PH_DUP_PERSON': 40, 'PH_DUP_HOUSEHOLD': 30,
        },
    },
    'new_condo': {
        'max_units_per_person': 1,
        'investment_purposes': ['投資', '투자', 'investment', '賃貸', '임대', '転売', '전매'],
        'weights': {
            'NC_MULTI_UNITS': 40, 'NC_SHARED_CONTACT': 30, 'NC_NAME_MISMATCH': 50, 'NC_RESALE_LISTED': 50,
            'NC_INVESTMENT': 15, 'NC_CORPORATE': 15,
        },
    },
}

REASON_TITLES = {
    'PH_RECENT_MOVE': '신청 직전 전입',
    'PH_SINGLE_RESIDENCE': '단신 거주 기간 부족',
    'PH_INCOME_OVER': '소득 기준 초과',
    'PH_INCOME_EDGE': '소득 기준 바로 아래',
    'PH_HOUSEHOLD_CHANGE': '신청 직전 세대 변경',
    'PH_HOUSEHOLD_MISMATCH': '세대 인원과 신청 구분 불일치',
    'PH_HOUSING_INELIGIBLE': '자가·공영주택 거주',
    'PH_DUP_PERSON': '같은 사람 중복 신청',
    'PH_DUP_HOUSEHOLD': '같은 주소·연락처 다른 세대',
    'NC_MULTI_UNITS': '한 물건 여러 호실 신청',
    'NC_SHARED_CONTACT': '연락처·주소 공유 (명의 대여 의심)',
    'NC_NAME_MISMATCH': '신청·계약 명의 다름',
    'NC_RESALE_LISTED': '인도 전 매물 등록',
    'NC_INVESTMENT': '투자 목적·본인 미거주',
    'NC_CORPORATE': '법인 신청',
}


def load_rules(path=None):
    rules = copy.deepcopy(DEFAULT_RULES)
    if path and os.path.exists(path):
        with open(path, encoding='utf-8-sig') as f:
            _merge(rules, json.load(f))
    return rules


def _merge(base, override):
    for key, value in override.items():
        if isinstance(value, dict) and isinstance(base.get(key), dict):
            _merge(base[key], value)
        else:
            base[key] = value


# ---------------------------------------------------------------- 정규화

def _nfkc(text):
    return unicodedata.normalize('NFKC', text or '')


def person_key(name, birth):
    name = re.sub(r'\s+', '', _nfkc(name)).lower()
    return (name + '|' + (birth or '')) if name else None


def phone_key(phone):
    digits = re.sub(r'\D', '', _nfkc(phone))
    return digits if len(digits) >= 8 else None


def email_key(email):
    text = _nfkc(email).strip().lower()
    return text if '@' in text else None


def address_key(address):
    text = re.sub(r'\s+', '', _nfkc(address))
    if not text:
        return None
    text = re.sub(r'[‐‑‒–—―ー−－-]', '-', text)
    text = re.sub(r'(丁目|番地|番|号)', '-', text)
    text = re.sub(r'-+', '-', text).strip('-')
    return text


def _contains_any(text, words):
    text = _nfkc(text)
    return any(_nfkc(w) in text for w in words)


def income_limit(rules, household_size, disabled):
    table = rules['public_housing']['income_limits']['disabled' if disabled else 'general']
    size = max(1, int(household_size or 1))
    if size <= 6:
        return float(table[str(size)])
    return float(table['6']) + (size - 6) * float(table['add_per_person'])


# ---------------------------------------------------------------- 규칙

def score_public_housing(applicants, rules):
    cfg = rules['public_housing']
    w = cfg['weights']
    results = {a['applicant_key']: [] for a in applicants}

    for a in applicants:
        reasons = results[a['applicant_key']]
        applied = a.get('applied_date')
        moved = a.get('moved_in_date')
        days = util.days_between(moved, applied)
        category = a.get('unit_category') or ''
        is_single = _contains_any(category, cfg['single_categories'])
        is_family = _contains_any(category, cfg['family_categories'])

        if days is not None and 0 <= days <= cfg['recent_move_days']:
            reasons.append(('PH_RECENT_MOVE', w['PH_RECENT_MOVE'], '신청 %d일 전에 전입 (거주 요건 위장 확인)' % days))
        if is_single and days is not None and days < cfg['single_min_residence_years'] * 365:
            reasons.append(('PH_SINGLE_RESIDENCE', w['PH_SINGLE_RESIDENCE'],
                            '단신 신청인데 거주 %.1f년 (기준 %d년)' % (days / 365.0, cfg['single_min_residence_years'])))

        income = a.get('household_income')
        if income is not None:
            limit = income_limit(rules, a.get('household_size'), a.get('disabled_household'))
            if income > limit:
                reasons.append(('PH_INCOME_OVER', w['PH_INCOME_OVER'],
                                '세대 소득 %s엔 > 기준 %s엔' % (format(int(income), ','), format(int(limit), ','))))
            elif income >= limit * cfg['income_edge_ratio']:
                reasons.append(('PH_INCOME_EDGE', w['PH_INCOME_EDGE'],
                                '세대 소득이 기준의 %.0f%% (소득 신고 확인)' % (income * 100.0 / limit)))

        changed_days = util.days_between(a.get('household_changed_date'), applied)
        if changed_days is not None and 0 <= changed_days <= cfg['household_change_days']:
            reasons.append(('PH_HOUSEHOLD_CHANGE', w['PH_HOUSEHOLD_CHANGE'],
                            '신청 %d일 전에 세대 구성 변경 (동거 친족 확인)' % changed_days))

        size = a.get('household_size')
        if size is not None and ((is_single and size > 1) or (is_family and size <= 1)):
            reasons.append(('PH_HOUSEHOLD_MISMATCH', w['PH_HOUSEHOLD_MISMATCH'],
                            '%s 신청인데 세대 인원 %d명' % (category, size)))

        if a.get('current_housing') and _contains_any(a['current_housing'], cfg['ineligible_housing']):
            reasons.append(('PH_HOUSING_INELIGIBLE', w['PH_HOUSING_INELIGIBLE'],
                            '현재 주거가 %s (주택 곤궁 요건 확인)' % a['current_housing']))

    _duplicates(applicants, results, rules, ('round_id',), public=True)
    return results


def score_new_condo(applicants, rules):
    cfg = rules['new_condo']
    w = cfg['weights']
    results = {a['applicant_key']: [] for a in applicants}

    for a in applicants:
        reasons = results[a['applicant_key']]
        contract = a.get('contract_name')
        if contract and person_key(contract, '') != person_key(a.get('applicant_name'), ''):
            reasons.append(('NC_NAME_MISMATCH', w['NC_NAME_MISMATCH'], '신청 명의와 계약 명의가 다름'))
        if a.get('resale_listed') == 1:
            reasons.append(('NC_RESALE_LISTED', w['NC_RESALE_LISTED'], '계약 뒤 인도 전에 매물로 나옴'))
        purpose = a.get('purchase_purpose') or ''
        if (purpose and _contains_any(purpose, cfg['investment_purposes'])) or a.get('will_reside') == 0:
            reasons.append(('NC_INVESTMENT', w['NC_INVESTMENT'], '구입 목적 %s / 본인 거주 %s' % (
                purpose or '-', {1: '예', 0: '아니오'}.get(a.get('will_reside'), '-'))))
        if a.get('corporate_name'):
            reasons.append(('NC_CORPORATE', w['NC_CORPORATE'], '법인 신청 (%s)' % a['corporate_name']))

    # 같은 사람·법인이 한 물건(같은 판매기)에 여러 호실
    groups = {}
    for a in applicants:
        owner = person_key(a.get('applicant_name'), a.get('birth_date')) or (
            'corp|' + _nfkc(a.get('corporate_name')) if a.get('corporate_name') else None)
        if owner:
            groups.setdefault((a.get('project_name'), a.get('round_id'), owner), []).append(a)
    for members in groups.values():
        if len(members) > cfg['max_units_per_person']:
            for a in members:
                results[a['applicant_key']].append(('NC_MULTI_UNITS', w['NC_MULTI_UNITS'],
                                                     '같은 명의로 %d호실 신청 (제한 %d)' % (len(members), cfg['max_units_per_person'])))

    _duplicates(applicants, results, rules, ('project_name', 'round_id'), public=False)
    return results


def _duplicates(applicants, results, rules, scope_fields, public):
    """같은 범위(회차, 물건) 안에서 같은 사람 / 같은 주소·연락처를 쓰는 다른 사람."""
    weights = rules['public_housing' if public else 'new_condo']['weights']
    by_person, by_contact = {}, {}
    for a in applicants:
        scope = tuple(a.get(f) for f in scope_fields)
        person = person_key(a.get('applicant_name'), a.get('birth_date'))
        if person and public:
            by_person.setdefault((scope, person), []).append(a)
        for kind, value in (('주소', address_key(a.get('address'))), ('전화', phone_key(a.get('phone'))),
                            ('메일', email_key(a.get('email')))):
            if value:
                by_contact.setdefault((scope, kind, value), []).append(a)

    if public:
        for members in by_person.values():
            if len(members) > 1:
                for a in members:
                    results[a['applicant_key']].append(('PH_DUP_PERSON', weights['PH_DUP_PERSON'],
                                                        '같은 회차에 %d건 신청' % len(members)))

    code = 'PH_DUP_HOUSEHOLD' if public else 'NC_SHARED_CONTACT'
    flagged = {}
    for (scope, kind, _), members in by_contact.items():
        people = set(person_key(a.get('applicant_name'), a.get('birth_date')) for a in members)
        if len(people) > 1:
            for a in members:
                flagged.setdefault(a['applicant_key'], set()).add((kind, len(people)))
    for key, kinds in flagged.items():
        detail = ', '.join('%s %d명' % (kind, n) for kind, n in sorted(kinds))
        results[key].append((code, weights[code], '다른 신청자와 %s 공유' % detail))


def summarize(reasons, rules):
    """(코드, 가중치, 설명) 목록 → (점수, 등급, 코드 문자열, 사유 문자열). 같은 코드는 한 번만 센다."""
    seen = {}
    for code, weight, text in reasons:
        if code not in seen:
            seen[code] = (weight, text)
    score = min(100, sum(weight for weight, _ in seen.values()))
    levels = rules['levels']
    level = '상' if score >= levels['high'] else '중' if score >= levels['medium'] else '하'
    ordered = sorted(seen.items(), key=lambda item: -item[1][0])
    codes = ','.join(code for code, _ in ordered)
    text = ' / '.join('%s: %s' % (REASON_TITLES.get(code, code), detail) for code, (_, detail) in ordered)
    return score, level, codes, text
