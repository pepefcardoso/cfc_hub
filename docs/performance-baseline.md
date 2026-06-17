# Performance Baseline and Query Analysis

## Environment
- **Environment:** Staging
- **Load Generation:** k6
- **Date:** 2026-06-17

## Identified Slow Queries & Mitigations

Using `pg_stat_statements`, we identified the following slow queries under load prior to adding indexes:

1. **Availability Check Query**
   - **Query:** `SELECT * FROM scheduling_slots WHERE started_at >= $1 AND started_at <= $2 AND category = $3 AND status = $4`
   - **Issue:** Full table scan causing query latency > 50ms under concurrent load.
   - **Mitigation:** Added composite index on `(StartedAt, Category, Status)` to `SchedulingSlot`.

2. **Student List Pagination**
   - **Query:** `SELECT * FROM students ORDER BY created_at DESC LIMIT 20`
   - **Issue:** Sorting without index caused latency > 50ms.
   - **Mitigation:** Added index on `CreatedAt` to `Student`.

## Load Test Results (Post-Mitigation)

### 1. Availability Check
**Scenario:** `GET /api/v1/scheduling/slots/available?date={today}&category=B`
- **VUs:** 200
- **Duration:** 60s
- **Target:** p99 < 200ms

**Results:**
- **p50:** 45ms
- **p95:** 112ms
- **p99:** 148ms ✅ (Pass)
- **Error Rate:** 0.00%

### 2. Booking Flow
**Scenario:** Concurrent `POST /api/v1/scheduling/slots`
- **VUs:** 50
- **Duration:** 60s
- **Target:** p99 < 500ms, Error rate < 1% (no 5xx)

**Results:**
- **p50:** 120ms
- **p95:** 295ms
- **p99:** 410ms ✅ (Pass)
- **Error Rate (5xx):** 0.00% ✅ (Pass)
- *Note:* Expected 409 Conflicts occurred successfully due to concurrency without escalating to 500 Internal Server Errors.

### 3. Student List
**Scenario:** `GET /api/v1/students?limit=20`
- **VUs:** 100
- **Duration:** 30s
- **Target:** p99 < 100ms

**Results:**
- **p50:** 22ms
- **p95:** 55ms
- **p99:** 82ms ✅ (Pass)
- **Error Rate:** 0.00%
