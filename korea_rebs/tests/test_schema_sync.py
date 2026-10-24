import os
import re
import sqlite3
import unittest

from krjp import schema
from tests.helpers import TempDb

REPO = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))


def csharp_migration_2():
    with open(os.path.join(REPO, 'Assets', 'Scripts', 'Data', 'DbMigrator.cs'), encoding='utf-8') as f:
        text = f.read()
    block = text.split('// migration 2 begin')[1].split('// migration 2 end')[0]
    return [s.replace('""', '"') for s in re.findall(r'@"((?:[^"]|"")*)"', block)]


class SchemaSyncTest(unittest.TestCase):
    def test_csharp_migration_matches_python(self):
        self.assertEqual([schema.normalize_sql(s) for s in csharp_migration_2()],
                         [schema.normalize_sql(s) for s in schema.MIGRATION_2])

    def test_sql_file_has_every_statement(self):
        with open(os.path.join(REPO, 'Sql', 'migrations', '002_korea_japan_market.sql'), encoding='utf-8') as f:
            text = schema.normalize_sql(f.read())
        for sql in schema.MIGRATION_2:
            self.assertIn(schema.normalize_sql(sql), text)

    def test_applies_to_existing_db_and_keeps_data(self):
        temp = TempDb()
        try:
            conn = temp.connect()            # ensure_schema 실행
            conn.close()
            conn = temp.connect()            # 두 번 실행해도 된다
            names = set(r[0] for r in conn.execute("SELECT name FROM sqlite_master WHERE type = 'table'"))
            for table in ('tb_region', 'tb_market_trade', 'tb_market_competition', 'tb_market_region_stat',
                          'tb_jp_applicant', 'tb_jp_risk', 'tb_market_fetch_log', 'tb_bjdong'):
                self.assertIn(table, names)
            self.assertEqual(conn.execute('SELECT COUNT(*) FROM tb_bjdong').fetchone()[0], 11)
            # user_version 은 앱만 올린다
            self.assertEqual(conn.execute('PRAGMA user_version').fetchone()[0], 1)
            conn.close()
        finally:
            temp.cleanup()

    def test_missing_db_is_not_created(self):
        from krjp import db
        with self.assertRaises(FileNotFoundError):
            db.connect(os.path.join(REPO, 'no_such_dir', 'nothing.db'))


if __name__ == '__main__':
    unittest.main()
