"""CSV 가져오기: 일본 공영주택 응모 결과, 신축 맨션 추첨 결과, 일본 신청자.

API 가 없는 자료라 사람이 공개 자료(예: 구청의 抽せん倍率 페이지)나 사업자 자료를 CSV 로 옮겨 넣는다.
머리글은 일본어·한국어·영어 중 무엇이든 된다 (괄호 속 단위는 무시). 인코딩은 UTF-8, Shift_JIS(cp932), EUC-KR 을 차례로 시도한다.
양식: korea_rebs/templates/*.csv
"""

import csv
import io
import re

from . import db as dbmod
from . import regions
from . import util

SCHEME_PUBLIC = 'jp_public_housing'
SCHEME_CONDO = 'jp_new_condo'
SCHEMES = (SCHEME_PUBLIC, SCHEME_CONDO)

COMPETITION_FIELDS = {
    'round': ['募集回', '募集月', '販売期', '販売時期', '회차', '모집월', '분양회차', 'round'],
    'operator': ['事業主体', '사업주체', 'operator'],
    'pref': ['都道府県コード', '都道府県', '도도부현', 'pref', 'prefecture'],
    'city': ['市区町村コード', '시구정촌코드', 'city', 'municipalitycode'],
    'name': ['住宅名', '物件名', '주택명', '물건명', '단지명', 'name', 'projectname'],
    'category': ['区分', '申込区分', '入居人数', '구분', 'category'],
    'application_no': ['申込番号', '신청번호', 'applicationno'],
    'supply': ['募集戸数', '販売戸数', '모집호수', '공급호수', '분양호수', 'supplyunits', 'supply'],
    'applications': ['応募数', '応募者数', '登録数', '申込数', '登録件数', '응모수', '신청수', 'applications'],
    'rate': ['倍率', '平均倍率', '경쟁률', 'rate'],
    'avg_price': ['平均価格', '평균가격', 'avgprice'],
}

APPLICANT_FIELDS = {
    'round': ['募集回', '販売期', '회차', 'round'],
    'project': ['住宅名', '物件名', '주택명', '물건명', 'projectname'],
    'category': ['区分', '申込区分', '구분', 'category'],
    'unit_no': ['住戸番号', '호실', 'unitno'],
    'application_no': ['申込番号', '登録番号', '신청번호', 'applicationno'],
    'name': ['氏名', '申込者氏名', '이름', '성명', 'name'],
    'birth': ['生年月日', '생년월일', 'birthdate'],
    'phone': ['電話番号', '連絡先', '전화번호', 'phone'],
    'email': ['メール', 'メールアドレス', '이메일', 'email'],
    'address': ['住所', '現住所', '주소', 'address'],
    'pref': ['都道府県コード', '都道府県', 'pref'],
    'city': ['市区町村コード', 'city'],
    'moved_in': ['都内転入日', '住民票転入日', '転入日', '전입일', 'movedindate'],
    'household_size': ['世帯人数', '세대원수', '세대인원', 'householdsize'],
    'household_changed': ['世帯変更日', '세대변경일', 'householdchangeddate'],
    'income': ['世帯所得', '世帯合計所得', '所得', '세대소득', 'householdincome'],
    'disabled': ['障害者等世帯', '特別区分', '特別世帯', '장애인등세대', '특별구분', 'disabledhousehold', 'specialcategory'],
    'current_housing': ['現住居', '現在の住居', '현재주거', 'currenthousing'],
    'corporate': ['法人名', '법인명', 'corporatename'],
    'purpose': ['購入目的', '구입목적', 'purchasepurpose'],
    'will_reside': ['本人居住', '居住予定', '본인거주', 'willreside'],
    'contract_name': ['契約名義', '계약명의', 'contractname'],
    'resale': ['引渡前売出', '引渡し前売出し', '인도전매물', 'resalelisted'],
    'applied': ['申込日', '登録日', '신청일', 'applieddate'],
}

REQUIRED_COMPETITION = ('round', 'name', 'supply', 'applications')
REQUIRED_APPLICANT = ('round', 'application_no')


class ImportError_(Exception):
    pass


def _normalize_header(text):
    text = util.clean(text)
    text = re.sub(r'\(.*?\)', '', text)
    return text.replace(' ', '').replace('_', '').lower()


def read_csv(path):
    with open(path, 'rb') as f:
        raw = f.read()
    for encoding in ('utf-8-sig', 'cp932', 'euc_kr'):
        try:
            text = raw.decode(encoding)
            break
        except UnicodeDecodeError:
            continue
    else:
        raise ImportError_('CSV 인코딩을 알 수 없습니다 (UTF-8, Shift_JIS, EUC-KR 이 아님)')
    reader = csv.reader(io.StringIO(text))
    rows = [row for row in reader if any(cell.strip() for cell in row)]
    if not rows:
        raise ImportError_('CSV 가 비어 있습니다')
    return rows[0], rows[1:]


def map_header(header, fields):
    """머리글 → {표준이름: 열 번호}. 원래 머리글도 돌려준다 (단위 판단용)."""
    lookup = {}
    for key, aliases in fields.items():
        for alias in aliases:
            lookup[_normalize_header(alias)] = key
    mapping = {}
    for index, name in enumerate(header):
        key = lookup.get(_normalize_header(name))
        if key and key not in mapping:
            mapping[key] = index
    return mapping


def _cell(row, mapping, key):
    index = mapping.get(key)
    if index is None or index >= len(row):
        return ''
    return util.clean(row[index])


def _location(row, mapping, jp_matcher=None):
    city = _cell(row, mapping, 'city')
    pref_raw = _cell(row, mapping, 'pref')
    if city and city.isdigit():
        code = city.zfill(5)[:5]
        return code[:2], code
    pref = regions.jp_pref_code(pref_raw) if pref_raw else None
    address = _cell(row, mapping, 'address')
    if jp_matcher and address:
        found_pref, found_region = jp_matcher.match(address)
        return pref or found_pref, found_region
    return pref, None


class JpRegionMatcher(object):
    """'東京都新宿区西新宿…' → ('13', '13104'). tb_region 의 일본 市区町村 이름으로 찾는다."""

    def __init__(self, conn):
        self.pref_names = sorted(((ja, code) for code, _, ja, _ in regions.JP_PREFECTURES), key=lambda x: -len(x[0]))
        self.by_pref = {}
        try:
            for row in conn.execute("SELECT region_code, name_local FROM tb_region WHERE country = 'JP' AND level = 2"):
                if row['name_local']:
                    self.by_pref.setdefault(row['region_code'][:2], []).append((row['name_local'], row['region_code']))
        except Exception:
            pass
        for items in self.by_pref.values():
            items.sort(key=lambda x: -len(x[0]))

    def match(self, address):
        text = util.clean(address).replace(' ', '')
        pref = None
        for name, code in self.pref_names:
            if text.startswith(name):
                pref, text = code, text[len(name):]
                break
        candidates = self.by_pref.get(pref, []) if pref else [c for items in self.by_pref.values() for c in items]
        for name, code in candidates:
            if text.startswith(name):
                return code[:2], code
        return pref, None


def import_competition(conn, path, scheme):
    if scheme not in SCHEMES:
        raise ImportError_('scheme 은 %s 중 하나' % ', '.join(SCHEMES))
    header, data = read_csv(path)
    mapping = map_header(header, COMPETITION_FIELDS)
    missing = [k for k in REQUIRED_COMPETITION if k not in mapping]
    if missing:
        raise ImportError_('필수 열이 없습니다: %s (양식: templates/%s_competition.csv)' % (', '.join(missing), scheme))

    price_in_man = 'avg_price' in mapping and '万' in header[mapping['avg_price']]
    rows, skipped = [], 0
    for row in data:
        round_month = util.parse_month(_cell(row, mapping, 'round'))
        supply = util.to_int(_cell(row, mapping, 'supply'))
        applications = util.to_int(_cell(row, mapping, 'applications'))
        name = _cell(row, mapping, 'name')
        if not round_month or not name or supply is None or applications is None:
            skipped += 1
            continue
        pref, region = _location(row, mapping)
        rate = util.to_float(_cell(row, mapping, 'rate'))
        if supply > 0:
            rate = applications / float(supply)
        price = util.to_float(_cell(row, mapping, 'avg_price'))
        if price is not None and price_in_man:
            price *= 10000.0
        category = _cell(row, mapping, 'category') or None
        operator = _cell(row, mapping, 'operator') or None
        key_no = _cell(row, mapping, 'application_no')
        rows.append({
            'comp_key': util.make_key('csv', scheme, round_month, operator, name, category, key_no, region or pref),
            'country': regions.JP,
            'scheme': scheme,
            'source': 'csv',
            'project_id': key_no or None,
            'project_name': name,
            'unit_type': category,
            'pref_code': pref,
            'region_code': region,
            'announce_date': round_month + '-01',
            'supply_units': supply,
            'applications': applications,
            'competition_rate': rate,
            'min_score': None, 'avg_score': None, 'max_score': None,
            'avg_price': price,
            'currency': 'JPY' if price is not None else None,
        })
    dbmod.upsert_rows(conn, 'tb_market_competition', ['comp_key'], rows)
    return len(rows), skipped


def import_applicants(conn, path, scheme):
    if scheme not in SCHEMES:
        raise ImportError_('scheme 은 %s 중 하나' % ', '.join(SCHEMES))
    header, data = read_csv(path)
    mapping = map_header(header, APPLICANT_FIELDS)
    missing = [k for k in REQUIRED_APPLICANT if k not in mapping]
    if missing:
        raise ImportError_('필수 열이 없습니다: %s (양식: templates/%s_applicants.csv)' % (', '.join(missing), scheme))

    matcher = JpRegionMatcher(conn)
    rows, skipped = [], 0
    for row in data:
        round_id = util.parse_month(_cell(row, mapping, 'round')) or _cell(row, mapping, 'round')
        application_no = _cell(row, mapping, 'application_no')
        if not round_id or not application_no:
            skipped += 1
            continue
        pref, region = _location(row, mapping, matcher)
        rows.append({
            'applicant_key': '%s|%s|%s' % (scheme, round_id, application_no),
            'scheme': scheme,
            'round_id': round_id,
            'project_name': _cell(row, mapping, 'project') or None,
            'unit_category': _cell(row, mapping, 'category') or None,
            'unit_no': _cell(row, mapping, 'unit_no') or None,
            'application_no': application_no,
            'applicant_name': _cell(row, mapping, 'name') or None,
            'birth_date': util.parse_date(_cell(row, mapping, 'birth')),
            'phone': _cell(row, mapping, 'phone') or None,
            'email': _cell(row, mapping, 'email') or None,
            'address': _cell(row, mapping, 'address') or None,
            'pref_code': pref,
            'region_code': region,
            'moved_in_date': util.parse_date(_cell(row, mapping, 'moved_in')),
            'household_size': util.to_int(_cell(row, mapping, 'household_size')),
            'household_changed_date': util.parse_date(_cell(row, mapping, 'household_changed')),
            'household_income': util.to_float(_cell(row, mapping, 'income')),
            'disabled_household': util.to_flag(_cell(row, mapping, 'disabled')),
            'current_housing': _cell(row, mapping, 'current_housing') or None,
            'corporate_name': _cell(row, mapping, 'corporate') or None,
            'purchase_purpose': _cell(row, mapping, 'purpose') or None,
            'will_reside': util.to_flag(_cell(row, mapping, 'will_reside')),
            'contract_name': _cell(row, mapping, 'contract_name') or None,
            'resale_listed': util.to_flag(_cell(row, mapping, 'resale')),
            'applied_date': util.parse_date(_cell(row, mapping, 'applied')),
        })
    # 다시 가져와도 앱에서 입력한 점검 결과는 그대로 둔다
    keep = ('inspection_status_result', 'unauth_decision', 'unauth_type', 'inspected_at')
    dbmod.upsert_rows(conn, 'tb_jp_applicant', ['applicant_key'], rows, keep_columns=keep)
    return len(rows), skipped
