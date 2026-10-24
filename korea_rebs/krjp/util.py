"""숫자·날짜 변환과 진행 표시 등 공통 도우미."""

import datetime
import hashlib
import re
import sys
import unicodedata

_NUMBER = re.compile(r'-?\d+(?:\.\d+)?')

# 和暦 → 서기 (원년)
_ERAS = {'令和': 2018, 'R': 2018, '平成': 1988, 'H': 1988, '昭和': 1925, 'S': 1925}


def make_key(*parts):
    text = '␟'.join('' if p is None else str(p) for p in parts)
    return hashlib.sha1(text.encode('utf-8')).hexdigest()[:24]


def clean(value):
    if value is None:
        return ''
    return unicodedata.normalize('NFKC', str(value)).strip()


def to_float(value):
    """'82,500' → 82500.0, '2,000㎡以上' → 2000.0, '' → None."""
    text = clean(value).replace(',', '')
    if not text or text in ('-', '－'):
        return None
    match = _NUMBER.search(text)
    return float(match.group(0)) if match else None


def to_int(value):
    number = to_float(value)
    return None if number is None else int(round(number))


def to_flag(value):
    """Y/N, 1/0, 예/아니오, はい/いいえ … → 1/0/None."""
    text = clean(value).lower()
    if not text:
        return None
    if text in ('y', 'yes', '1', 'true', 'o', '예', '네', 'はい', '有', 'あり', '○', '〇'):
        return 1
    if text in ('n', 'no', '0', 'false', 'x', '아니오', '아니요', 'いいえ', '無', 'なし', '×'):
        return 0
    return None


def parse_date(value):
    """'2026-08-01', '2026/8/1', '20260801', '令和8年8月1日', 'R8.8.1' → 'YYYY-MM-DD' (못 읽으면 None)."""
    text = clean(value)
    if not text:
        return None

    era = re.match(r'^(令和|平成|昭和|[RHS])\s*(\d+|元)\s*[年.\-/]\s*(\d+)\s*[月.\-/]\s*(\d+)\s*日?$', text)
    if era:
        year = (1 if era.group(2) == '元' else int(era.group(2))) + _ERAS[era.group(1)]
        return _safe_date(year, int(era.group(3)), int(era.group(4)))

    digits = re.match(r'^(\d{4})(\d{2})(\d{2})$', text)
    if digits:
        return _safe_date(int(digits.group(1)), int(digits.group(2)), int(digits.group(3)))

    parts = re.match(r'^(\d{4})\s*[年.\-/]\s*(\d{1,2})\s*[月.\-/]\s*(\d{1,2})\s*日?', text)
    if parts:
        return _safe_date(int(parts.group(1)), int(parts.group(2)), int(parts.group(3)))
    return None


def parse_month(value):
    """'2026-08', '2026/8', '202608', '令和8年8月' → 'YYYY-MM'."""
    text = clean(value)
    if not text:
        return None
    era = re.match(r'^(令和|平成|昭和|[RHS])\s*(\d+|元)\s*[年.\-/]\s*(\d+)\s*月?', text)
    if era:
        year = (1 if era.group(2) == '元' else int(era.group(2))) + _ERAS[era.group(1)]
        month = int(era.group(3))
    else:
        m = re.match(r'^(\d{4})\s*[年.\-/]?\s*(\d{1,2})', text)
        if not m:
            return None
        year, month = int(m.group(1)), int(m.group(2))
    if not 1 <= month <= 12:
        return None
    return '%04d-%02d' % (year, month)


def _safe_date(year, month, day):
    try:
        return datetime.date(year, month, day).isoformat()
    except ValueError:
        return None


def days_between(earlier, later):
    """'YYYY-MM-DD' 두 개 사이의 일수 (later - earlier). 하나라도 없으면 None."""
    if not earlier or not later:
        return None
    a = datetime.date.fromisoformat(earlier)
    b = datetime.date.fromisoformat(later)
    return (b - a).days


def months(start, end):
    """'202401', '202403' → ['202401', '202402', '202403']."""
    y, m = int(start[:4]), int(start[4:6])
    ey, em = int(end[:4]), int(end[4:6])
    result = []
    while (y, m) <= (ey, em):
        result.append('%04d%02d' % (y, m))
        m += 1
        if m == 13:
            y, m = y + 1, 1
    return result


def quarter_of(month):
    return (int(month) - 1) // 3 + 1


def now_text():
    return datetime.datetime.now().strftime('%Y-%m-%d %H:%M:%S')


def shift_month(today, delta):
    """date → delta 개월 뒤의 'YYYYMM'."""
    y, m = today.year, today.month + delta
    while m < 1:
        y, m = y - 1, m + 12
    while m > 12:
        y, m = y + 1, m - 12
    return '%04d%02d' % (y, m)


def progress(message):
    """앱이 표준 출력에서 읽어 진행 상황으로 보여 준다."""
    sys.stdout.write('PROGRESS ' + message + '\n')
    sys.stdout.flush()


def info(message):
    sys.stdout.write(message + '\n')
    sys.stdout.flush()
