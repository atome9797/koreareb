"""지역 마스터 (tb_region): 한국 시도·시군구, 일본 都道府県·市区町村.

한국: 앱 DB 의 tb_bjdong(시도 17 + 시군구 250)에서 만들고, 그 뒤 바뀐 행정코드를 반영한다.
  - 강원도 42 → 강원특별자치도 51 (2023-06-11), 전라북도 45 → 전북특별자치도 52 (2024-01-18)
  - 경상북도 군위군 47720 → 대구광역시 군위군 27720 (2023-07-01)
  그 밖에 새로 생긴 구는 config/kr_extra_regions.csv 로 더한다 (코드,이름).
일본: 都道府県 47개는 아래 표(한국어·일본어·영어), 市区町村은 不動産情報ライブラリ XIT002 로 채운다.
"""

import csv
import os
import unicodedata

KR = 'KR'
JP = 'JP'

# 옛 코드 → 지금 코드 (앞자리 바꾸기)
KR_PREF_REMAP = {'42': '51', '45': '52'}
KR_PREF_RENAME = {'51': '강원특별자치도', '52': '전북특별자치도'}
KR_REGION_REMAP = {'47720': '27720'}

# 청약홈 SUBSCRPT_AREA_CODE_NM(공급지역명) → 시도 코드
KR_SHORT_PREF = {
    '서울': '11', '부산': '26', '대구': '27', '인천': '28', '광주': '29', '대전': '30', '울산': '31',
    '세종': '36', '경기': '41', '강원': '51', '충북': '43', '충남': '44', '전북': '52', '전남': '46',
    '경북': '47', '경남': '48', '제주': '50',
}

# 주소 첫 단어 → 시도 코드 (옛 이름 포함)
KR_PREF_ALIASES = {
    '서울특별시': '11', '서울시': '11', '서울': '11',
    '부산광역시': '26', '부산': '26',
    '대구광역시': '27', '대구': '27',
    '인천광역시': '28', '인천': '28',
    '광주광역시': '29', '광주': '29',
    '대전광역시': '30', '대전': '30',
    '울산광역시': '31', '울산': '31',
    '세종특별자치시': '36', '세종시': '36', '세종': '36',
    '경기도': '41', '경기': '41',
    '강원특별자치도': '51', '강원도': '51', '강원': '51',
    '충청북도': '43', '충북': '43',
    '충청남도': '44', '충남': '44',
    '전북특별자치도': '52', '전라북도': '52', '전북': '52',
    '전라남도': '46', '전남': '46',
    '경상북도': '47', '경북': '47',
    '경상남도': '48', '경남': '48',
    '제주특별자치도': '50', '제주도': '50', '제주': '50',
}

# 일본 都道府県 (JIS X 0401 순서): 코드, 한국어, 일본어, 영어
JP_PREFECTURES = [
    ('01', '홋카이도', '北海道', 'Hokkaido'), ('02', '아오모리현', '青森県', 'Aomori'),
    ('03', '이와테현', '岩手県', 'Iwate'), ('04', '미야기현', '宮城県', 'Miyagi'),
    ('05', '아키타현', '秋田県', 'Akita'), ('06', '야마가타현', '山形県', 'Yamagata'),
    ('07', '후쿠시마현', '福島県', 'Fukushima'), ('08', '이바라키현', '茨城県', 'Ibaraki'),
    ('09', '도치기현', '栃木県', 'Tochigi'), ('10', '군마현', '群馬県', 'Gunma'),
    ('11', '사이타마현', '埼玉県', 'Saitama'), ('12', '지바현', '千葉県', 'Chiba'),
    ('13', '도쿄도', '東京都', 'Tokyo'), ('14', '가나가와현', '神奈川県', 'Kanagawa'),
    ('15', '니가타현', '新潟県', 'Niigata'), ('16', '도야마현', '富山県', 'Toyama'),
    ('17', '이시카와현', '石川県', 'Ishikawa'), ('18', '후쿠이현', '福井県', 'Fukui'),
    ('19', '야마나시현', '山梨県', 'Yamanashi'), ('20', '나가노현', '長野県', 'Nagano'),
    ('21', '기후현', '岐阜県', 'Gifu'), ('22', '시즈오카현', '静岡県', 'Shizuoka'),
    ('23', '아이치현', '愛知県', 'Aichi'), ('24', '미에현', '三重県', 'Mie'),
    ('25', '시가현', '滋賀県', 'Shiga'), ('26', '교토부', '京都府', 'Kyoto'),
    ('27', '오사카부', '大阪府', 'Osaka'), ('28', '효고현', '兵庫県', 'Hyogo'),
    ('29', '나라현', '奈良県', 'Nara'), ('30', '와카야마현', '和歌山県', 'Wakayama'),
    ('31', '돗토리현', '鳥取県', 'Tottori'), ('32', '시마네현', '島根県', 'Shimane'),
    ('33', '오카야마현', '岡山県', 'Okayama'), ('34', '히로시마현', '広島県', 'Hiroshima'),
    ('35', '야마구치현', '山口県', 'Yamaguchi'), ('36', '도쿠시마현', '徳島県', 'Tokushima'),
    ('37', '가가와현', '香川県', 'Kagawa'), ('38', '에히메현', '愛媛県', 'Ehime'),
    ('39', '고치현', '高知県', 'Kochi'), ('40', '후쿠오카현', '福岡県', 'Fukuoka'),
    ('41', '사가현', '佐賀県', 'Saga'), ('42', '나가사키현', '長崎県', 'Nagasaki'),
    ('43', '구마모토현', '熊本県', 'Kumamoto'), ('44', '오이타현', '大分県', 'Oita'),
    ('45', '미야자키현', '宮崎県', 'Miyazaki'), ('46', '가고시마현', '鹿児島県', 'Kagoshima'),
    ('47', '오키나와현', '沖縄県', 'Okinawa'),
]

# 도쿄 23구는 한국어 이름을 둔다 (일본 글꼴이 없는 화면에서도 읽히도록)
JP_TOKYO_WARDS_KO = {
    '13101': '지요다구', '13102': '주오구', '13103': '미나토구', '13104': '신주쿠구', '13105': '분쿄구',
    '13106': '다이토구', '13107': '스미다구', '13108': '고토구', '13109': '시나가와구', '13110': '메구로구',
    '13111': '오타구', '13112': '세타가야구', '13113': '시부야구', '13114': '나카노구', '13115': '스기나미구',
    '13116': '도시마구', '13117': '기타구', '13118': '아라카와구', '13119': '이타바시구', '13120': '네리마구',
    '13121': '아다치구', '13122': '가쓰시카구', '13123': '에도가와구',
}

JP_PREF_BY_NAME = {}
for _code, _ko, _ja, _en in JP_PREFECTURES:
    JP_PREF_BY_NAME[_ja] = _code
    JP_PREF_BY_NAME[_ko] = _code
    JP_PREF_BY_NAME[_en.lower()] = _code
    JP_PREF_BY_NAME[_ja[:-1] if _ja != '北海道' else _ja] = _code   # 東京, 大阪 …


def kr_current_code(code):
    """옛 시도·시군구 코드를 지금 코드로 바꾼다."""
    code = str(code).strip()
    if code in KR_REGION_REMAP:
        return KR_REGION_REMAP[code]
    if code[:2] in KR_PREF_REMAP:
        return KR_PREF_REMAP[code[:2]] + code[2:]
    return code


def kr_current_name(name, current_code):
    """'강원도 춘천시' → '강원특별자치도 춘천시', 군위군은 대구로."""
    name = (name or '').strip()
    pref = current_code[:2]
    if current_code == '27720':
        return '대구광역시 군위군'
    if pref in KR_PREF_RENAME:
        parts = name.split(' ', 1)
        return KR_PREF_RENAME[pref] + ((' ' + parts[1]) if len(parts) > 1 else '')
    return name


def kr_regions_from_bjdong(conn, extra_csv=None):
    """tb_bjdong → tb_region 행 목록."""
    rows = []
    seen = set()
    try:
        source = conn.execute('SELECT bjdong_code, bjdong_name FROM tb_bjdong ORDER BY bjdong_code').fetchall()
    except Exception:
        source = []

    for code, name in source:
        old = str(code).strip()
        if len(old) not in (2, 5):
            continue
        current = kr_current_code(old)
        if current in seen:
            continue
        seen.add(current)
        level = 1 if len(current) == 2 else 2
        rows.append({
            'country': KR,
            'region_code': current,
            'level': level,
            'parent_code': None if level == 1 else current[:2],
            'name_ko': kr_current_name(name, current),
            'name_local': kr_current_name(name, current),
            'name_en': None,
        })

    for code, name in read_extra_regions(extra_csv):
        if code in seen:
            continue
        seen.add(code)
        rows.append({
            'country': KR, 'region_code': code, 'level': 1 if len(code) == 2 else 2,
            'parent_code': None if len(code) == 2 else code[:2],
            'name_ko': name, 'name_local': name, 'name_en': None,
        })
    return rows


def read_extra_regions(path):
    if not path or not os.path.exists(path):
        return []
    result = []
    with open(path, encoding='utf-8-sig', newline='') as f:
        for row in csv.reader(f):
            if len(row) < 2 or not row[0].strip().isdigit():
                continue
            result.append((row[0].strip(), row[1].strip()))
    return result


def kr_district_codes(region_rows):
    """실거래가 API 에 넘길 시군구 5자리 코드 (시 아래에 구가 있으면 구 코드만)."""
    codes = sorted(r['region_code'] for r in region_rows if r['country'] == KR and r['level'] == 2)
    result = []
    for code in codes:
        # 수원시(41110)처럼 구가 있는 시는 tb_bjdong 에 구 코드(41111…)만 있다. 그대로 쓴다.
        result.append(code)
    return result


def jp_prefecture_rows():
    return [{
        'country': JP, 'region_code': code, 'level': 1, 'parent_code': None,
        'name_ko': ko, 'name_local': ja, 'name_en': en,
    } for code, ko, ja, en in JP_PREFECTURES]


def jp_municipality_row(code, name_ja, name_en=None):
    code = str(code).strip().zfill(5)
    return {
        'country': JP, 'region_code': code, 'level': 2, 'parent_code': code[:2],
        'name_ko': JP_TOKYO_WARDS_KO.get(code),
        'name_local': name_ja, 'name_en': name_en,
    }


def normalize_text(value):
    return unicodedata.normalize('NFKC', value or '').strip()


class KrRegionMatcher(object):
    """주소 문자열('서울특별시 강남구 개포동 …') → 시군구 코드."""

    def __init__(self, region_rows):
        self.by_name = {}
        for row in region_rows:
            if row['country'] != KR or row['level'] != 2:
                continue
            key = self._key(row['name_ko'])
            self.by_name[key] = row['region_code']
            # '강원도 …' 같은 옛 이름도 받는다
            for alias, pref in KR_PREF_ALIASES.items():
                if pref == row['region_code'][:2]:
                    parts = row['name_ko'].split(' ', 1)
                    if len(parts) == 2:
                        self.by_name.setdefault(self._key(alias + parts[1]), row['region_code'])

    @staticmethod
    def _key(text):
        return normalize_text(text).replace(' ', '')

    def match(self, address):
        tokens = normalize_text(address).split()
        if not tokens:
            return None, None
        pref = KR_PREF_ALIASES.get(tokens[0])
        # '수원시 장안구'처럼 세 단어까지 먼저, 그다음 두 단어
        for n in (3, 2):
            if len(tokens) >= n:
                code = self.by_name.get(self._key(''.join(tokens[:n])))
                if code:
                    return code[:2], code
        if pref == '36':
            return '36', '36110'
        return pref, None


def kr_pref_from_short(name):
    return KR_SHORT_PREF.get(normalize_text(name))


def jp_pref_code(value):
    """'13', '東京都', '도쿄도', 'Tokyo' → '13'."""
    text = normalize_text(str(value or ''))
    if not text:
        return None
    if text.isdigit():
        return text.zfill(2)[:2] if len(text) <= 2 else text[:2]
    return JP_PREF_BY_NAME.get(text) or JP_PREF_BY_NAME.get(text.lower())
