"""
Load Test Script – GameServerApi
Mục đích: Đo latency và throughput các endpoint API để đưa vào báo cáo luận văn.

Kịch bản:
  S1 – Login latency (baseline, serial, max 5 lần do rate-limit 5/60s/IP)
  S2 – Concurrent authenticated GETs (N users đồng thời, không bị rate-limit)
  S3 – Ramp-up (1 → 5 → 10 → 20 → 50 concurrent users)
  S4 – Sustained load (50 users × 5 vòng = 250 requests)

Output:
  Scripts/benchmark_results/load_test_results_<ts>.csv
  Scripts/benchmark_results/load_test_report_<ts>.txt
"""

import asyncio
import aiohttp
import time
import csv
import statistics
import sys
import random
import string
import os
from datetime import datetime
from collections import defaultdict

BASE_URL    = os.getenv("API_URL", "http://localhost:5000")
RESULTS_DIR = os.path.join(os.path.dirname(__file__), "benchmark_results")
os.makedirs(RESULTS_DIR, exist_ok=True)

TIMESTAMP   = datetime.now().strftime("%Y%m%d_%H%M%S")
CSV_FILE    = os.path.join(RESULTS_DIR, f"load_test_results_{TIMESTAMP}.csv")
REPORT_FILE = os.path.join(RESULTS_DIR, f"load_test_report_{TIMESTAMP}.txt")
ELEMENTS    = ["Metal", "Wood", "Water", "Fire", "Earth", "Wind"]

# ─── Helpers ──────────────────────────────────────────────────────────────────

def random_suffix(n=8):
    return "".join(random.choices(string.ascii_lowercase + string.digits, k=n))

def percentile(data, p):
    if not data:
        return 0.0
    s = sorted(data)
    return s[min(int(len(s) * p / 100), len(s) - 1)]

def fmt(ms):
    return f"{ms:.1f} ms"

# ─── CSV recording ────────────────────────────────────────────────────────────

_rows = []

def record(scenario, endpoint, status, latency_ms, error=""):
    _rows.append({"scenario": scenario, "endpoint": endpoint,
                  "status_code": status, "latency_ms": round(latency_ms, 2), "error": error})

def flush_csv():
    fields = ["scenario", "endpoint", "status_code", "latency_ms", "error"]
    with open(CSV_FILE, "w", newline="", encoding="utf-8") as f:
        w = csv.DictWriter(f, fieldnames=fields)
        w.writeheader(); w.writerows(_rows)

# ─── HTTP helpers ─────────────────────────────────────────────────────────────

async def http_post(session, scenario, path, payload=None, token=None):
    headers = {"Content-Type": "application/json"}
    if token:
        headers["Authorization"] = f"Bearer {token}"
    t0 = time.perf_counter()
    try:
        async with session.post(f"{BASE_URL}{path}", json=payload, headers=headers,
                                timeout=aiohttp.ClientTimeout(total=15)) as resp:
            ms = (time.perf_counter() - t0) * 1000
            body = await resp.json(content_type=None)
            record(scenario, path, resp.status, ms)
            return resp.status, ms, body
    except Exception as e:
        ms = (time.perf_counter() - t0) * 1000
        record(scenario, path, 0, ms, str(e))
        return 0, ms, {}

async def http_get(session, scenario, path, token=None):
    headers = {}
    if token:
        headers["Authorization"] = f"Bearer {token}"
    t0 = time.perf_counter()
    try:
        async with session.get(f"{BASE_URL}{path}", headers=headers,
                               timeout=aiohttp.ClientTimeout(total=15)) as resp:
            ms = (time.perf_counter() - t0) * 1000
            await resp.text()
            record(scenario, path, resp.status, ms)
            return resp.status, ms
    except Exception as e:
        ms = (time.perf_counter() - t0) * 1000
        record(scenario, path, 0, ms, str(e))
        return 0, ms

# ─── Setup: register + create player ─────────────────────────────────────────

async def register_user(session, username, password, email):
    s, _, body = await http_post(session, "setup", "/api/auth/register",
                                  {"username": username, "password": password, "email": email})
    if s == 200:
        return body.get("token"), body.get("user_id")
    return None, None

async def create_player(session, token):
    el  = random.choice(ELEMENTS)
    sfx = random_suffix(4)
    s, _, _ = await http_post(session, "setup", "/api/player/create",
                               {"element_type": el, "character_name": f"Hero{sfx}"}, token=token)
    return s in (200, 201)

async def setup_test_users(n=55):
    print(f"[Setup] Đang đăng ký {n} tài khoản test...")
    credentials = [(f"t_{random_suffix()}", f"T@{random_suffix()}1A") for _ in range(n)]
    users = []

    async with aiohttp.ClientSession(connector=aiohttp.TCPConnector(limit=20)) as session:
        reg_tasks = [register_user(session, u, p, f"{u}@lt.local") for u, p in credentials]
        reg_res   = await asyncio.gather(*reg_tasks)
        for (uname, pwd), (tok, uid) in zip(credentials, reg_res):
            if tok:
                users.append({"username": uname, "password": pwd, "token": tok, "user_id": uid})
        print(f"[Setup] Đăng ký thành công {len(users)}/{n}.")

        print(f"[Setup] Tạo character cho {len(users)} user...")
        cp_tasks = [create_player(session, u["token"]) for u in users]
        cp_res   = await asyncio.gather(*cp_tasks)
        print(f"[Setup] Character thành công: {sum(cp_res)}/{len(users)}.")

    return users

# ─── S1: Login baseline ───────────────────────────────────────────────────────

async def scenario_s1_baseline(users):
    """
    Đo latency login 1 user, tối đa 5 lần (rate-limit 5/60s/IP).
    Kết quả khoảng 200–350ms do BCrypt cost 12.
    """
    print("\n[S1] Baseline login latency (max 5 lần, 1 user)...")
    u = users[0]
    lats = []
    async with aiohttp.ClientSession(connector=aiohttp.TCPConnector(limit=5)) as session:
        for i in range(5):
            s, ms, body = await http_post(session, "S1_baseline", "/api/auth/login",
                                          {"username": u["username"], "password": u["password"]})
            ok = s == 200
            lats.append(ms) if ok else None
            print(f"  [{i+1}/5] {ms:.1f} ms {'OK' if ok else 'FAIL/rate-limited'}")
            await asyncio.sleep(0.1)

    if lats:
        print(f"  n={len(lats)}  min={fmt(min(lats))}  "
              f"mean={fmt(statistics.mean(lats))}  max={fmt(max(lats))}")
        print("  Ghi chú: ~200-350ms là bình thường – BCrypt cost 12 (thiết kế bảo mật).")
    return lats

# ─── S2: Concurrent authenticated API ────────────────────────────────────────

async def scenario_s2_concurrent(users, concurrency):
    """
    N users gọi 3 endpoint đồng thời bằng token từ đăng ký.
    Không bị rate-limit vì không qua login.
    """
    scenario = f"S2_c{concurrency}"
    subset = users[:concurrency]

    async def one_user(u):
        results = []
        s, ms = await http_get(session, scenario, "/api/leaderboard", token=u["token"])
        results.append(("leaderboard", ms, s))
        if u.get("user_id"):
            s, ms = await http_get(session, scenario,
                                   f"/api/player/{u['user_id']}/data", token=u["token"])
            results.append(("player_data", ms, s))
            s, ms = await http_get(session, scenario,
                                   f"/api/gene/{u['user_id']}", token=u["token"])
            results.append(("gene_list", ms, s))
        return results

    async with aiohttp.ClientSession(
            connector=aiohttp.TCPConnector(limit=concurrency + 10)) as session:
        t0 = time.perf_counter()
        all_rows = await asyncio.gather(*[one_user(u) for u in subset])
        wall = (time.perf_counter() - t0) * 1000

    by_ep = defaultdict(list)
    for rows in all_rows:
        for ep, ms, st in rows:
            if st in (200, 201, 204):
                by_ep[ep].append(ms)

    total_ok = sum(len(v) for v in by_ep.values())
    tps = total_ok / (wall / 1000) if wall > 0 else 0

    for ep, lats in by_ep.items():
        if lats:
            print(f"  [{ep}] n={len(lats)}  "
                  f"p50={fmt(percentile(lats,50))}  "
                  f"p95={fmt(percentile(lats,95))}  max={fmt(max(lats))}")
    print(f"  total_ok={total_ok}  wall={fmt(wall)}  throughput={tps:.1f} req/s")
    return by_ep, tps

# ─── S3: Ramp-up ──────────────────────────────────────────────────────────────

async def scenario_s3_rampup(users):
    print("\n[S3] Ramp-up concurrent authenticated API...")
    levels = [1, 5, 10, 20, 50]
    results = []
    for c in levels:
        if c > len(users):
            print(f"  [skip] c={c}")
            continue
        print(f"\n[S3] concurrency={c}")
        by_ep, tps = await scenario_s2_concurrent(users, c)
        all_lats = [ms for lats in by_ep.values() for ms in lats]
        results.append({
            "concurrency": c, "n": len(all_lats),
            "p50": percentile(all_lats, 50),
            "p95": percentile(all_lats, 95),
            "p99": percentile(all_lats, 99),
            "max": max(all_lats) if all_lats else 0,
            "rps": round(tps, 1),
        })
        await asyncio.sleep(0.3)
    return results

# ─── S4: Sustained load ───────────────────────────────────────────────────────

async def scenario_s4_sustained(users, concurrency=50, rounds=5):
    c = min(concurrency, len(users))
    print(f"\n[S4] Sustained load – {c} users × {rounds} vòng ({c*rounds} requests)...")
    subset = users[:c]
    all_lats = []

    async with aiohttp.ClientSession(
            connector=aiohttp.TCPConnector(limit=c + 10)) as session:
        t0 = time.perf_counter()
        for r in range(rounds):
            tasks = [http_get(session, "S4_sustained", "/api/leaderboard", token=u["token"])
                     for u in subset]
            res = await asyncio.gather(*tasks)
            all_lats.extend(ms for st, ms in res if st in (200, 204))
        wall = (time.perf_counter() - t0) * 1000

    tps = len(all_lats) / (wall / 1000) if wall > 0 else 0
    if all_lats:
        print(f"  n={len(all_lats)}  wall={fmt(wall)}")
        print(f"  p50={fmt(percentile(all_lats,50))}  "
              f"p95={fmt(percentile(all_lats,95))}  "
              f"p99={fmt(percentile(all_lats,99))}  rps={tps:.1f}")
    return all_lats, tps

# ─── Report ───────────────────────────────────────────────────────────────────

def write_report(s1_lats, s3_ramp, s4_lats, s4_tps):
    L = []
    sep = "=" * 65
    L += [sep, "   GAME SERVER API – LOAD TEST REPORT",
          f"   Thời điểm : {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}",
          f"   Base URL   : {BASE_URL}", sep]

    L.append("\n## S1 – Baseline login latency (serial, BCrypt cost 12)")
    if s1_lats:
        L += [f"   n     = {len(s1_lats)}",
              f"   min   = {fmt(min(s1_lats))}",
              f"   mean  = {fmt(statistics.mean(s1_lats))}",
              f"   max   = {fmt(max(s1_lats))}"]
        if len(s1_lats) > 1:
            L.append(f"   stdev = {fmt(statistics.stdev(s1_lats))}")
        L.append("   Ghi chú: 200-350 ms là bình thường với BCrypt cost 12.")
        L.append("   Login bị giới hạn 5 req/60s/IP (chống brute-force).")
    else:
        L.append("   Không lấy được mẫu (rate-limit đã đầy từ lần chạy trước).")

    L.append("\n## S3 – Ramp-up: authenticated endpoints (leaderboard + player_data + gene)")
    hdr = f"  {'Users':>6}  {'Requests':>9}  {'p50':>10}  {'p95':>10}  {'p99':>10}  {'max':>10}  {'RPS':>8}"
    L += [hdr, "  " + "-" * 68]
    for r in s3_ramp:
        L.append(f"  {r['concurrency']:>6}  {r['n']:>9}  "
                 f"{fmt(r['p50']):>10}  {fmt(r['p95']):>10}  "
                 f"{fmt(r['p99']):>10}  {fmt(r['max']):>10}  {r['rps']:>7.1f}")

    L.append(f"\n## S4 – Sustained load (50 users × 5 vòng, /api/leaderboard)")
    if s4_lats:
        L += [f"   n    = {len(s4_lats)}",
              f"   p50  = {fmt(percentile(s4_lats,50))}",
              f"   p95  = {fmt(percentile(s4_lats,95))}",
              f"   p99  = {fmt(percentile(s4_lats,99))}",
              f"   max  = {fmt(max(s4_lats))}",
              f"   RPS  = {s4_tps:.1f} req/s"]

    L += ["\n## Phương pháp đo",
          "  - Công cụ    : Python 3.14 / aiohttp (async HTTP)",
          "  - Môi trường : localhost loopback (không có WAN latency)",
          "  - Database   : MySQL/MariaDB local",
          "  - Đo         : kể từ khi gửi request đến khi nhận xong response body",
          "  - Rate-limit login 5/60s/IP là tính năng bảo mật, không phải giới hạn HN",
          "  - CSV thô    : " + CSV_FILE, sep]

    text = "\n".join(L)
    with open(REPORT_FILE, "w", encoding="utf-8") as f:
        f.write(text)
    return text

# ─── Main ─────────────────────────────────────────────────────────────────────

async def main():
    print(f"Game Server API Load Test  –  {BASE_URL}")
    print(f"Results: {RESULTS_DIR}\n")

    # Health check
    try:
        async with aiohttp.ClientSession() as sess:
            async with sess.get(f"{BASE_URL}/api/leaderboard",
                                timeout=aiohttp.ClientTimeout(total=5)) as resp:
                print(f"[Health] /api/leaderboard -> HTTP {resp.status}")
    except Exception as e:
        print(f"[ERROR] Không kết nối được: {e}")
        print("        dotnet run --project GameServerApi")
        sys.exit(1)

    users = await setup_test_users(n=55)
    if len(users) < 5:
        print("[ERROR] Không đủ user."); sys.exit(1)

    s1_lats           = await scenario_s1_baseline(users)
    s3_ramp           = await scenario_s3_rampup(users)
    s4_lats, s4_tps   = await scenario_s4_sustained(users)

    flush_csv()
    report = write_report(s1_lats, s3_ramp, s4_lats, s4_tps)

    print("\n" + report)
    print(f"\nCSV   : {CSV_FILE}")
    print(f"Report: {REPORT_FILE}")

if __name__ == "__main__":
    asyncio.run(main())
