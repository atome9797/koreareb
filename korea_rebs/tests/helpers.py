"""테스트용 임시 DB (개인정보 없는 합성 데이터만)."""

import os
import shutil
import sqlite3
import tempfile

from krjp import db as dbmod

BJDONG = [
    ('1100000000', '서울특별시', 11), ('1168000000', '서울특별시 강남구', 11680),
    ('1165000000', '서울특별시 서초구', 11650), ('4100000000', '경기도', 41),
    ('4111100000', '경기도 수원시 장안구', 41111), ('4200000000', '강원도', 42),
    ('4211000000', '강원도 춘천시', 42110), ('4700000000', '경상북도', 47),
    ('4772000000', '경상북도 군위군', 47720), ('3600000000', '세종특별자치시', 36),
    ('3611000000', '세종특별자치시', 36110),
]


class TempDb(object):
    def __init__(self):
        self.dir = tempfile.mkdtemp(prefix='krjp_test_')
        self.path = os.path.join(self.dir, 'test.db')
        conn = sqlite3.connect(self.path)
        conn.execute('CREATE TABLE "tb_bjdong" ("bjdong_cd" TEXT, "bjdong_name" TEXT, "bjdong_code" INTEGER)')
        conn.executemany('INSERT INTO tb_bjdong VALUES (?, ?, ?)', BJDONG)
        conn.execute('PRAGMA user_version = 1')
        conn.commit()
        conn.close()

    def connect(self):
        return dbmod.connect(self.path)

    def cleanup(self):
        shutil.rmtree(self.dir, ignore_errors=True)
