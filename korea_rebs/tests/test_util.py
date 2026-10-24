import datetime
import unittest

from krjp import util


class UtilTest(unittest.TestCase):
    def test_parse_date(self):
        self.assertEqual(util.parse_date('2026-08-01'), '2026-08-01')
        self.assertEqual(util.parse_date('2026/8/1'), '2026-08-01')
        self.assertEqual(util.parse_date('20260801'), '2026-08-01')
        self.assertEqual(util.parse_date('2026年8月1日'), '2026-08-01')
        self.assertEqual(util.parse_date('令和8年8月1日'), '2026-08-01')
        self.assertEqual(util.parse_date('R8.8.1'), '2026-08-01')
        self.assertEqual(util.parse_date('平成元年1月8日'), '1989-01-08')
        self.assertEqual(util.parse_date('２０２６－０８－０１'), '2026-08-01')   # 전각
        self.assertIsNone(util.parse_date('2026-02-30'))
        self.assertIsNone(util.parse_date(''))
        self.assertIsNone(util.parse_date('언젠가'))

    def test_parse_month(self):
        self.assertEqual(util.parse_month('2026-08'), '2026-08')
        self.assertEqual(util.parse_month('202608'), '2026-08')
        self.assertEqual(util.parse_month('2026年8月'), '2026-08')
        self.assertEqual(util.parse_month('令和8年8月'), '2026-08')
        self.assertIsNone(util.parse_month('2026-13'))

    def test_numbers_and_flags(self):
        self.assertEqual(util.to_float('82,500'), 82500.0)
        self.assertEqual(util.to_float('2,000㎡以上'), 2000.0)
        self.assertEqual(util.to_float('1,896,000円'), 1896000.0)
        self.assertIsNone(util.to_float('-'))
        self.assertEqual(util.to_int('84.6'), 85)
        self.assertEqual(util.to_flag('Y'), 1)
        self.assertEqual(util.to_flag('いいえ'), 0)
        self.assertIsNone(util.to_flag('모름'))

    def test_months_and_quarters(self):
        self.assertEqual(util.months('202411', '202502'), ['202411', '202412', '202501', '202502'])
        self.assertEqual(util.quarter_of(12), 4)
        self.assertEqual(util.shift_month(datetime.date(2026, 1, 15), -1), '202512')
        self.assertEqual(util.days_between('2026-07-01', '2026-08-01'), 31)
        self.assertIsNone(util.days_between(None, '2026-08-01'))


if __name__ == '__main__':
    unittest.main()
