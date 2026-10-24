"""공공 API 호출 (표준 라이브러리만 사용).

- gzip 응답을 풀고, 일시적인 오류(접속 실패, 429, 5xx)는 몇 번 다시 시도한다.
- 오류 메시지와 로그에 API 키가 남지 않도록 지운다.
"""

import gzip
import json
import time
import urllib.error
import urllib.parse
import urllib.request

USER_AGENT = 'koreareb-market/1.0'


class ApiError(Exception):
    pass


def _redact(text, secrets):
    text = str(text)
    for secret in secrets:
        if secret:
            text = text.replace(secret, '***')
            text = text.replace(urllib.parse.quote(secret, safe=''), '***')
            text = text.replace(urllib.parse.quote_plus(secret), '***')
    return text


def build_url(base, params):
    query = urllib.parse.urlencode([(k, v) for k, v in params if v is not None and v != ''])
    return base + ('&' if '?' in base else '?') + query if query else base


def normalize_service_key(key):
    """공공데이터포털 키는 '인코딩 키'(%2B 등)와 '디코딩 키' 두 가지로 발급된다.

    URL 을 만들 때 다시 인코딩하므로 여기서는 디코딩한 값으로 맞춘다 (두 번 인코딩되면 인증 오류가 난다).
    """
    key = (key or '').strip()
    if '%' in key:
        key = urllib.parse.unquote(key)
    return key


def get(url, headers=None, secrets=(), retries=3, timeout=60, sleep=time.sleep):
    """GET → bytes. 실패하면 ApiError (메시지에 키 없음)."""
    request_headers = {'User-Agent': USER_AGENT, 'Accept-Encoding': 'gzip'}
    if headers:
        request_headers.update(headers)
    secrets = tuple(secrets) + tuple(v for v in (headers or {}).values())

    last_error = None
    for attempt in range(retries):
        try:
            request = urllib.request.Request(url, headers=request_headers)
            with urllib.request.urlopen(request, timeout=timeout) as response:
                body = response.read()
                if response.headers.get('Content-Encoding', '').lower() == 'gzip' or body[:2] == b'\x1f\x8b':
                    body = gzip.decompress(body)
                return body
        except urllib.error.HTTPError as e:
            detail = ''
            try:
                detail = e.read()[:300].decode('utf-8', 'replace')
            except Exception:
                pass
            last_error = 'HTTP %s %s' % (e.code, detail)
            if e.code in (401, 403):
                raise ApiError(_redact('인증 실패 (%s). API 키와 이용 신청 상태를 확인하세요.' % last_error, secrets))
            if e.code not in (429, 500, 502, 503, 504):
                raise ApiError(_redact(last_error, secrets))
        except (urllib.error.URLError, OSError) as e:
            last_error = '접속 실패: %s' % getattr(e, 'reason', e)

        if attempt < retries - 1:
            sleep(2 ** attempt)

    raise ApiError(_redact(last_error or '알 수 없는 오류', secrets))


def get_json(url, headers=None, secrets=(), **kwargs):
    body = get(url, headers=headers, secrets=secrets, **kwargs)
    try:
        return json.loads(body.decode('utf-8-sig'))
    except ValueError:
        raise ApiError(_redact('JSON 이 아닌 응답: %s' % body[:200].decode('utf-8', 'replace'), secrets))
