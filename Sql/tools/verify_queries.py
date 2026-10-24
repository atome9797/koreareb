"""이전 쿼리 vs 새 쿼리 결과 비교 + 속도 측정.

    python Sql/tools/verify_queries.py Assets/StreamingAssets/koreareb.db

- 원본 DB 는 건드리지 않는다. 임시 폴더에 사본 두 개를 만들어
  하나는 그대로(이전 쿼리), 하나는 마이그레이션(인덱스) 적용 후(새 쿼리) 비교한다.
- 새 쿼리는 Assets/Scripts/Data/DatabaseAccess.cs 의 SQL 템플릿을 그대로 읽어 만든다(cs_sql.py).
- 개인정보가 들어 있는 DB 에서도 돌릴 수 있도록 행 내용은 출력하지 않는다(건수/시간/케이스 이름만).
- 하나라도 다르면 종료 코드 1.

비교 규칙: 동점(같은 위험도) 처리 순서처럼 이전 쿼리에서 원래 정해져 있지 않던 부분은 비교하지 않는다.
"""
import collections
import math
import os
import re
import shutil
import sqlite3
import statistics
import sys
import tempfile
import time

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import legacy_sql as L  # noqa: E402
import cs_sql as N  # noqa: E402

ROOT = os.path.normpath(os.path.join(HERE, "..", ".."))
MIGRATION_SQL = os.path.join(ROOT, "Sql", "migrations", "001_performance_indexes.sql")
MIGRATOR_CS = os.path.join(ROOT, "Assets", "Scripts", "Data", "DbMigrator.cs")

failures = []
report = []


def check(name, ok, detail=""):
    if not ok:
        failures.append(f"{name}: {detail}")


def timed(con, sql, params=None, reps=1):
    ts = []
    for _ in range(reps):
        t = time.perf_counter()
        cur = con.execute(sql, params or {})
        rows = cur.fetchall()
        ts.append(time.perf_counter() - t)
    return statistics.median(ts), [d[0] for d in cur.description], rows


def stream(con, sql, params, make_consumer, batch=2000):
    """결과를 한 번에 다 가져오지 않고 묶음으로 읽어 consumer 에 넘긴다.
    make_consumer(컬럼 이름 목록) → 행 하나를 받는 함수. 돌려주는 시간에서 consumer 처리 시간은 뺀다."""
    t0 = time.perf_counter()
    spent = 0.0
    cur = con.execute(sql, params or {})
    cols = [d[0] for d in cur.description]
    consume = make_consumer(cols)
    n = 0
    while True:
        rows = cur.fetchmany(batch)
        if not rows:
            break
        t1 = time.perf_counter()
        for r in rows:
            consume(r)
        spent += time.perf_counter() - t1
        n += len(rows)
    return time.perf_counter() - t0 - spent, cols, n


def norm(v):
    # feq 처럼 실수의 마지막 자리 오차는 무시한다 (유효숫자 12자리). 1 == 1.0 은 해시도 같다.
    return float(f"{v:.12g}") if isinstance(v, float) else v


def row_hash(row, idx):
    """idx 위치의 값들로 만든 해시 (idx 에 None 이 있으면 그 자리는 NULL 로 본다)."""
    return hash(tuple(None if i is None else norm(row[i]) for i in idx))


def feq(a, b):
    if isinstance(a, float) or isinstance(b, float):
        if a is None or b is None:
            return a is b
        return math.isclose(a, b, rel_tol=1e-9, abs_tol=1e-12)
    return a == b


def rows_equal(r1, r2, skip=()):
    return len(r1) == len(r2) and all(feq(x, y) for i, (x, y) in enumerate(zip(r1, r2)) if i not in skip)


def keyed(cols, rows, key):
    i = cols.index(key)
    return {r[i]: r for r in rows}


def statements(sql_text):
    sql_text = re.sub(r"--[^\n]*", "", sql_text)
    out = []
    for s in sql_text.split(";"):
        s = re.sub(r"\s+", " ", s).strip()
        s = re.sub(r"\(\s+", "(", s)
        s = re.sub(r"\s+\)", ")", s)
        if s:
            out.append(s)
    return out


def check_migration_sync():
    """Sql/migrations 의 SQL 과 DbMigrator.cs 안의 문장이 같은지 (둘을 같이 고쳐야 한다)."""
    file_stmts = statements(open(MIGRATION_SQL, encoding="utf-8").read())
    cs = open(MIGRATOR_CS, encoding="utf-8").read()
    block = cs[cs.index("new[]"):cs.index("};", cs.index("new[]"))]
    joined = "".join(re.findall(r'"((?:[^"\\]|\\.)*)"', block.replace('" +\n', '"\n')))
    cs_stmts = statements(joined)
    check("migration sync", file_stmts == cs_stmts,
          f"Sql/migrations 와 DbMigrator.cs 의 문장이 다릅니다 ({len(file_stmts)} vs {len(cs_stmts)})")
    return file_stmts


def main():
    if len(sys.argv) < 2:
        print(__doc__)
        return 2

    src_db = sys.argv[1]
    work = tempfile.mkdtemp(prefix="koreareb_verify_")
    legacy_db, new_db = os.path.join(work, "legacy.db"), os.path.join(work, "new.db")

    try:
        shutil.copyfile(src_db, legacy_db)
        shutil.copyfile(src_db, new_db)

        stmts = check_migration_sync()
        con = sqlite3.connect(new_db)
        t = time.perf_counter()
        for s in stmts:
            con.execute(s)
        con.commit()
        nrows = con.execute("SELECT COUNT(*) FROM tb_riskdata").fetchone()[0]
        print(f"rows={nrows}  migration {time.perf_counter() - t:.2f}s")
        con.execute("PRAGMA journal_mode=WAL")
        con.close()

        lcon, ncon = sqlite3.connect(legacy_db), sqlite3.connect(new_db)
        ncon.execute("PRAGMA temp_store=MEMORY")
        ncon.execute("PRAGMA cache_size=-65536")
        run_cases(lcon, ncon)
        lcon.close()
        ncon.close()
    finally:
        shutil.rmtree(work, ignore_errors=True)

    print(f"\n{'case':52s} {'before ms':>10s} {'after ms':>9s} {'rows':>15s}")
    for name, tl, tn, nl, nn in report:
        before = "-" if tl is None else f"{tl * 1000:.1f}"
        print(f"{name[:52]:52s} {before:>10s} {tn * 1000:9.1f} {nl:>7d}/{nn:<7d}")
    print()
    if failures:
        print("FAILED", len(failures))
        for f in failures[:50]:
            print(" -", f)
        return 1
    print("ALL CHECKS PASSED")
    return 0


def run_cases(lcon, ncon):
    lo, hi = lcon.execute("SELECT MIN(rec_anno_date), MAX(rec_anno_date) FROM tb_riskdata").fetchone()
    lo, hi = int(lo or 20000101), int(hi or 20991231)
    mid = (lo // 10000 + hi // 10000) // 2
    ranges = [(lo, hi), (mid * 10000 + 101, mid * 10000 + 1231)]

    ALL = [True] * 18
    SEOUL = [False, True] + [False] * 16
    SOME = [False] + [i % 3 == 0 for i in range(17)]
    B0, B1, B2 = [True, False, False], [False, True, False], [False, False, True]
    P = {1: [True, False, False, False], 0.5: [False, True, False, False], 0.25: [False, False, True, False], 0.1: [False, False, False, True]}
    S_NONE = ([True, False, False, False], "")
    S_TEXT = ([True, False, False, False], "아")
    S_LOC = ([False, False, False, True], "시")

    def cmp_keyed(name, lsql, nsql, key, skip_cols=("NO",), order_col=None, desc=False):
        tl, lc, lr = timed(lcon, lsql)
        tn, nc, nr = timed(ncon, nsql[0], nsql[1], reps=3)
        check(name, lc == nc, "컬럼 이름/순서가 다름")
        lk, nk = keyed(lc, lr, key), keyed(nc, nr, key)
        check(name, set(lk) == set(nk), f"키 집합이 다름 ({len(lk)} vs {len(nk)})")
        skip = {lc.index(c) for c in skip_cols if c in lc}
        bad = [k for k in lk if k in nk and not rows_equal(lk[k], nk[k], skip)]
        check(name, not bad, f"{len(bad)}개 행의 값이 다름")
        if order_col:
            i = nc.index(order_col)
            seq = [r[i] for r in nr]
            check(name, seq == sorted(seq, reverse=desc), "정렬 순서가 다름")
        report.append((name, tl, tn, len(lr), len(nr)))

    for city_name, city in (("전국", ALL), ("일부", SOME)):
        for fr, to in ranges:
            cmp_keyed(f"통계 지역별 [{city_name} {fr}-{to}]", L.statistics_by_region(fr, to, city), N.statistics_by_region(fr, to, city), "지역", order_col="지역")

    for b, k in ((B0, B0), (B1, B2)):
        for pv, pct in P.items():
            name = f"통계 단지별 [b{b.index(True)}k{k.index(True)} {pv}]"
            lsql = L.statistics_by_danji(lo, hi, ALL, b, k, pct)
            nsql = N.statistics_by_danji(lo, hi, ALL, b, k, pct)
            if pv == 1:
                cmp_keyed(name, lsql, nsql, "단지명")
            else:
                tl, lc, lr = timed(lcon, lsql)
                tn, nc, nr = timed(ncon, nsql[0], nsql[1], reps=3)
                lk, nk = keyed(lc, lr, "단지명"), keyed(nc, nr, "단지명")
                ri = lc.index("위험도")
                check(name, lc == nc and set(lk) == set(nk) and all(feq(lk[x][ri], nk[x][ri]) for x in lk), "결과가 다름")
                report.append((name, tl, tn, len(lr), len(nr)))

    for st, ss in (S_NONE, S_TEXT, S_LOC):
        for pv, pct in P.items():
            name = f"단지선정 [검색={ss or '-'} {pv}]"
            tl, lc, lr = timed(lcon, L.market_by_danji(lo, hi, st, ss, ALL, B0, B0, pct))
            sql, prm = N.market_by_danji(lo, hi, st, ss, ALL, B0, B0, pct)
            tn, nc, nr = timed(ncon, sql, prm, reps=3)
            check(name, lc == nc, "컬럼이 다름")
            lk, nk = keyed(lc, lr, "단지명"), keyed(nc, nr, "단지명")
            check(name, set(lk) == set(nk), "단지 집합이 다름")
            ti, si = lc.index("민영국민"), lc.index("점검여부")
            skip = {lc.index("NO")} | ({si} if pv != 1 else set())
            # 한 단지에 국민/민영이 섞여 있으면 이전 쿼리는 임의로 하나를, 새 쿼리는 '국민/민영' 을 보여 준다
            bad = [x for x in lk if x in nk and not rows_equal(lk[x], nk[x], skip | ({ti} if "/" in str(nk[x][ti]) else set()))]
            check(name, not bad, f"{len(bad)}개 단지 값이 다름")
            report.append((name, tl, tn, len(lr), len(nr)))

    # 실시등록은 결과가 '행 수 × 214컬럼' 이라 다 메모리에 올리면 66만 행에서 수십 GB 가 된다.
    # 결과를 묶음으로 읽으면서 위험도 순서, data_no → 행 해시, 점검여부 개수만 남겨 비교한다.
    for st, ss in (S_NONE, S_LOC):
        for pv in (1, 0.5):
            pct = P[pv]
            name = f"실시등록 [검색={ss or '-'} {pv}]"

            # 1) 화면용 목록 (47컬럼): data_no → 행 해시, 페이지 비교용 두 번째 컬럼
            lst, lst_order = {}, []

            def on_list(cols):
                di = cols.index("data_no")
                every = list(range(len(cols)))

                def consume(r):
                    lst[r[di]] = row_hash(r, every)
                    lst_order.append(r[1])
                return consume

            sql, prm = N.market_execute_list(lo, hi, st, ss, ALL, B0, B0, pct)
            tn2, nc2, n2 = stream(ncon, sql, prm, on_list)

            # 2) 이전 쿼리 (214컬럼)
            l_risk, l_sig, l_status = [], {}, collections.Counter()

            def on_legacy(cols):
                ri, di, si = cols.index("위험도"), cols.index("data_no"), cols.index("점검여부")
                skip = {cols.index("NO"), cols.index("rankTest")}
                keep = [i for i in range(len(cols)) if i not in skip]

                def consume(r):
                    l_risk.append(r[ri])
                    l_status[r[si]] += 1
                    if pv == 1:
                        l_sig[r[di]] = row_hash(r, keep)
                return consume

            tl, lc, ln = stream(lcon, L.market_execute(lo, hi, st, ss, ALL, B0, B0, pct), None, on_legacy)

            # 3) 새 쿼리 (214컬럼): 이전 결과와 비교 + 화면용 목록의 같은 이름 컬럼과 비교
            n_risk, n_sig, list_bad = [], {}, [0]

            def on_new(cols):
                ri, di = cols.index("위험도"), cols.index("data_no")
                skip = {cols.index("NO"), cols.index("rankTest")}
                keep = [i for i in range(len(cols)) if i not in skip]
                pos = {c: i for i, c in enumerate(cols)}
                list_idx = [pos.get(c) for c in nc2]

                def consume(r):
                    n_risk.append(r[ri])
                    if pv == 1:
                        n_sig[r[di]] = row_hash(r, keep)
                    if lst.get(r[di]) != row_hash(r, list_idx):
                        list_bad[0] += 1
                return consume

            sql, prm = N.market_execute_full(lo, hi, st, ss, ALL, B0, B0, pct)
            tn, nc, nn = stream(ncon, sql, prm, on_new)

            check(name + " (214컬럼)", lc == nc, "컬럼 이름/순서가 다름")
            check(name + " (214컬럼)", ln == nn, f"행 수 {ln} vs {nn}")
            check(name + " (214컬럼)", l_risk == n_risk, "위험도 순서가 다름")
            if pv == 1:
                check(name + " (214컬럼)", l_sig == n_sig, "값이 다름")
            report.append((name + " 214컬럼", tl, tn, ln, nn))

            check(name + " 화면용 목록", list_bad[0] == 0 and n2 == nn, f"{list_bad[0]}개 행이 다름")
            report.append((name + " 화면용 목록", tl, tn2, ln, n2))

            sql, prm = N.market_execute_list(lo, hi, st, ss, ALL, B0, B0, pct, 20, 10)
            tn3, _, pr = timed(ncon, sql, prm, reps=3)
            check(name + " 페이지", [x[1] for x in pr] == lst_order[20:30], "페이지 내용이 다름")
            report.append((name + " 페이지(10)", None, tn3, min(10, max(0, ln - 20)), len(pr)))

            sql, prm = N.market_execute_counts(lo, hi, st, ss, ALL, B0, B0, pct)
            _, _, crow = timed(ncon, sql, prm)
            exp = (ln, l_status["N"], l_status["Z"], l_status["Y"])
            got = tuple(x or 0 for x in crow[0])
            check(name + " 개수", exp == got if pv == 1 else exp[0] == got[0], f"{exp} vs {got}")

    s, e = lcon.execute(L.last_create_date()).fetchone()
    s2, e2 = ncon.execute(N.last_create_date()[0]).fetchone()
    check("최근 업로드일", (s, e) == (s2, e2), "다름")
    for sort_type, fr, to in (("MainMapSort", s, e), ("StatisticMapSort", str(lo), str(hi))):
        for city_name, city in (("전국", ALL), ("서울", SEOUL), ("일부", SOME)):
            for typ in ("map", "score"):
                cmp_keyed(f"지도 {sort_type} {typ} [{city_name}]", L.map_by_danger(city, sort_type, fr, to, typ),
                          N.map_by_danger(city, sort_type, fr, to, typ), "지역",
                          order_col="지역" if typ == "map" else "everage", desc=typ != "map")
                name = f"지역지도 {sort_type} {typ} [{city_name}]"
                tl, lc, lr = timed(lcon, L.map_by_danger_local(city, sort_type, fr, to, typ))
                sql, prm = N.map_by_danger_local(city, sort_type, fr, to, typ)
                tn, nc, nr = timed(ncon, sql, prm, reps=3)
                ei = lc.index("everage")
                ok = lc == nc and len(lr) == len(nr) and all(feq(a[ei], b[ei]) for a, b in zip(lr, nr))
                if typ == "map":
                    gi = lc.index("지역")
                    ok = ok and [a[gi] // 10 for a in lr] == [b[gi] // 10 for b in nr]
                check(name, ok, "결과가 다름")
                report.append((name, tl, tn, len(lr), len(nr)))

            pl = lcon.execute(L.percent_person(sort_type, fr, to, city)).fetchone()
            sql, prm = N.percent_person(sort_type, fr, to, city)
            pn = ncon.execute(sql, prm).fetchone()
            check(f"인원수 {sort_type} [{city_name}]", all(feq(a, b) for a, b in zip(pl, pn)), "다름")
            limits = [str(int(float(x))) for x in pl]
            t = time.perf_counter()
            l4 = []
            for n in limits:
                l4 += lcon.execute(L.map_by_danger_percent(city, sort_type, fr, to, n)).fetchall()
            tl = time.perf_counter() - t
            sql, prm = N.map_by_danger_percent(city, sort_type, fr, to, limits)
            tn, nc, nr = timed(ncon, sql, prm, reps=3)
            check(f"퍼센트 {sort_type} [{city_name}]", nc == ["전체"] and len(nr) == 4 and all(feq(a[0], b[0]) for a, b in zip(l4, nr)), "다름")
            report.append((f"퍼센트(4개) {sort_type} [{city_name}]", tl, tn, 4, len(nr)))


if __name__ == "__main__":
    sys.exit(main())
