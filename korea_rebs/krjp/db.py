"""SQLite 연결과 공통 쓰기 도우미.

앱(Unity)이 같은 DB 를 열어 두고 있어도 쓸 수 있도록 busy_timeout 을 길게 둔다.
앱은 WAL 모드를 켜므로 앱이 읽는 동안에도 여기서 쓸 수 있다.
"""

import contextlib
import os
import sqlite3

from . import schema

DEFAULT_DB = 'C:/Project-Company/koreareb/Assets/StreamingAssets/koreareb.db'


def resolve_db_path(path=None):
    """인자 > 환경 변수 KOREAREB_DB > 기존 개발 경로."""
    return path or os.environ.get('KOREAREB_DB') or DEFAULT_DB


def connect(path=None, create_schema=True):
    db_path = resolve_db_path(path)
    if not os.path.exists(db_path):
        # sqlite3 는 없는 경로에 빈 DB 를 만든다. 앱 DB 를 잘못 가리킨 채로 진행하지 않는다.
        raise FileNotFoundError('DB 파일이 없습니다: %s' % db_path)

    conn = sqlite3.connect(db_path, isolation_level=None, timeout=30)
    conn.row_factory = sqlite3.Row
    conn.execute('PRAGMA busy_timeout = 30000')
    conn.execute('PRAGMA foreign_keys = ON')

    if create_schema:
        ensure_schema(conn)
    return conn


def ensure_schema(conn):
    with transaction(conn):
        for sql in schema.MIGRATION_2:
            conn.execute(sql)


@contextlib.contextmanager
def transaction(conn):
    """BEGIN IMMEDIATE … COMMIT. 예외가 나면 모두 되돌린다."""
    conn.execute('BEGIN IMMEDIATE')
    try:
        yield conn
    except BaseException:
        conn.execute('ROLLBACK')
        raise
    else:
        conn.execute('COMMIT')


def upsert_rows(conn, table, key_columns, rows, keep_columns=()):
    """rows(dict 목록)를 key_columns 기준으로 넣거나 바꾼다.

    keep_columns 에 있는 컬럼은 이미 있는 행에서 바꾸지 않는다 (예: 일본 신청자의 점검 결과).
    반환: 처리한 행 수.
    """
    rows = list(rows)
    if not rows:
        return 0

    columns = list(rows[0].keys())
    for row in rows:
        if list(row.keys()) != columns:
            raise ValueError('행마다 컬럼이 같아야 합니다')

    update_columns = [c for c in columns if c not in key_columns and c not in keep_columns]
    placeholders = ', '.join('?' for _ in columns)
    sql = 'INSERT INTO %s (%s) VALUES (%s)' % (table, ', '.join(columns), placeholders)
    if update_columns:
        sql += ' ON CONFLICT (%s) DO UPDATE SET %s' % (
            ', '.join(key_columns),
            ', '.join('%s = excluded.%s' % (c, c) for c in update_columns))
    else:
        sql += ' ON CONFLICT (%s) DO NOTHING' % ', '.join(key_columns)

    with transaction(conn):
        conn.executemany(sql, [tuple(row[c] for c in columns) for row in rows])
    return len(rows)


def scalar(conn, sql, params=()):
    row = conn.execute(sql, params).fetchone()
    return None if row is None else row[0]
