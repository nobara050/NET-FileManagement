# Hướng Dẫn Tạo Grafana Dashboard (v11) — Từng Bước Chi Tiết

> **Grafana**: `http://localhost:3000` | Login: `admin` / `admin`
> **Phiên bản UI**: Grafana v11+ (giao diện mới với sidebar "Add" bên phải)

---

## Mục Lục

1. [Kiểm tra hệ thống & debug "No Data"](#1-kiểm-tra-hệ-thống--debug-no-data)
2. [Khởi tạo Dashboard mới](#2-khởi-tạo-dashboard-mới)
3. [Cách thêm panel & nhập query (QUAN TRỌNG)](#3-cách-thêm-panel--nhập-query)
4. [Panel 1 — HTTP Request Rate (Prometheus)](#4-panel-1--http-request-rate)
5. [Panel 2 — p95 Latency](#5-panel-2--p95-latency)
6. [Panel 3 — Error Rate %](#6-panel-3--error-rate-)
7. [Panel 4 — Live Application Logs (Loki)](#7-panel-4--live-application-logs)
8. [Panel 5 — Log Volume by Severity (Loki)](#8-panel-5--log-volume-by-severity)
9. [Panel 6 — Recent Traces (Tempo)](#9-panel-6--recent-traces)
10. [Lưu Dashboard](#10-lưu-dashboard)
11. [Demo Flow End-to-End](#11-demo-flow-end-to-end)
12. [Import nhanh bằng JSON](#12-import-nhanh-bằng-json)

---

## 1. Kiểm tra hệ thống & debug "No Data"

> ⚠️ **Làm phần này trước** — nếu bỏ qua, tạo panel xong cũng không thấy data.

### 1.1 Kiểm tra Prometheus scrape được backend

1. Mở trình duyệt → `http://localhost:9090`
2. Bấm menu **Status** → **Targets**
3. Tìm dòng `job="drive-api"` → cột **State** phải là **UP** (xanh lá)

**Nếu State = DOWN**: chạy lệnh sau kiểm tra backend
```
curl http://localhost:5213/metrics
```
Nếu thấy text chứa `http_requests_received_total` → backend OK, lỗi là Docker network.

### 1.2 Kiểm tra metric có data chưa

1. Tại `http://localhost:9090` → bấm **Graph** (menu trên)
2. Paste vào ô search: `http_requests_received_total`
3. Bấm **Execute**
4. ✅ Thấy bảng kết quả → OK. ❌ Không thấy gì → gọi vài API từ Swagger rồi Execute lại.

### 1.3 Kiểm tra Datasource trong Grafana

1. Menu trái → **Connections** → **Data sources**
2. Ba datasource phải xanh ✅:

| Datasource | URL |
|---|---|
| `Prometheus` | `http://prometheus:9090` |
| `Loki` | `http://loki:3100` |
| `Tempo` | `http://tempo:3200` |

3. Nếu đỏ ❌: bấm vào datasource → cuộn xuống → **Save & test**

---

## 2. Khởi tạo Dashboard mới

1. Menu trái → bấm icon **Dashboards** (4 ô vuông)
2. Góc trên phải → bấm **New** → **New dashboard**
3. Dashboard mới mở ra, đồng thời **sidebar "Add" xuất hiện bên phải** với các lựa chọn:
   - **Panel** — Drag or click to add a panel
   - **Group layouts**: Add row, Add tab
   - **Dashboard controls**: Filter, Variable...

---

## 3. Cách thêm panel & nhập query

> 📌 **Đọc kỹ phần này** — đây là cơ chế chung dùng lại cho tất cả các panel bên dưới.

### Bước A — Mở panel editor

Trong sidebar "Add" bên phải:
- Nhìn vào phần **Panel** (đầu tiên trong sidebar)
- Bấm vào hình vuông lớn có dấu **`+`** (hoặc kéo thả nó ra vùng canvas trái)
- Panel editor mở ra, chia làm 2 vùng:
  - **Trên**: preview biểu đồ (trống)
  - **Dưới**: khu vực cấu hình query

> Nếu sidebar "Add" đã đóng, bấm nút **`+`** nhỏ ở góc trên trái dashboard để mở lại.

### Bước B — Chọn datasource

Trong khu vực query phía dưới:
- Tìm dòng **"Data source"** → bấm dropdown → chọn `Prometheus` / `Loki` / `Tempo` tùy panel

### Bước C — Chuyển sang chế độ nhập code (Code mode)

Nhìn vào khu vực query, góc phải có 2 nút:
```
[Builder]  [Code]
```
→ **Bấm `Code`** để chuyển sang ô nhập text thuần

### Bước D — Nhập query & chạy

1. **Paste query** vào ô text lớn hiện ra
2. Bấm nút **`Run queries`** (màu xanh lam, góc phải)
3. Biểu đồ preview phía trên hiển thị kết quả

### Bước E — Cấu hình tên và loại biểu đồ

Cột bên phải panel editor có các mục:
- **Panel options → Title**: nhập tên panel
- Phần đầu cột phải: tên loại biểu đồ hiện tại (ví dụ "Time series") → bấm vào để đổi

### Bước F — Lưu panel về dashboard

Bấm **`Apply`** (nút xanh lá, góc trên phải) → quay về dashboard.

---

## 4. Panel 1 — HTTP Request Rate

**Mục đích**: Đếm request/giây theo từng controller và mã HTTP status.

1. Sidebar **Add** → bấm **`+`** trong mục Panel → panel editor mở
2. **Data source** → chọn **`Prometheus`**
3. Bấm **`Code`** (góc phải khu vực query)
4. Paste query:
   ```promql
   sum(rate(http_requests_received_total[1m])) by (controller, code)
   ```
5. Bấm **`Run queries`** → kiểm tra preview có đường biểu đồ
6. Cột phải:
   - **Title**: `HTTP Request Rate (req/s)`
   - **Visualization**: đảm bảo là `Time series`
   - **Standard options → Unit**: tìm chọn `Throughput → requests/sec (rps)`
7. Bấm **`Apply`**

> ❓ **Không thấy data?** → Mở Swagger (`http://localhost:5213/swagger`) gọi vài API rồi Run queries lại.

---

## 5. Panel 2 — p95 Latency

**Mục đích**: Đo độ trễ phân vị 95 — phát hiện request chậm.

1. Sidebar **Add** → bấm **`+`** → panel editor
2. **Data source** → **`Prometheus`** → bấm **`Code`**
3. Paste query:
   ```promql
   histogram_quantile(0.95, sum(rate(http_request_duration_seconds_bucket[1m])) by (le, controller))
   ```
4. Bấm **`Run queries`**
5. Cột phải:
   - **Title**: `95th Percentile Latency (p95)`
   - **Visualization**: `Time series`
   - **Standard options → Unit**: `Time → seconds (s)`
6. Bấm **`Apply`**

---

## 6. Panel 3 — Error Rate %

**Mục đích**: Gauge hiển thị % request lỗi 4xx/5xx.

1. Sidebar **Add** → bấm **`+`** → panel editor
2. **Data source** → **`Prometheus`** → bấm **`Code`**
3. Paste query:
   ```promql
   (sum(rate(http_requests_received_total{code=~"4..|5.."}[1m])) or vector(0))
   / (sum(rate(http_requests_received_total[1m])) > 0) * 100
   ```
4. Bấm **`Run queries`**
5. Cột phải — đổi loại biểu đồ:
   - Bấm vào tên visualization hiện tại (ví dụ `Time series`)
   - Tìm và chọn **`Gauge`**
6. Cấu hình tiếp:
   - **Title**: `Error Rate (%)`
   - **Standard options → Unit**: `Misc → Percent (0-100)`
   - **Standard options → Min**: `0` | **Max**: `100`
   - **Thresholds**: Xanh=0, **+ Add threshold** → Cam=5, **+ Add threshold** → Đỏ=10
7. Bấm **`Apply`**

---

## 7. Panel 4 — Live Application Logs

**Mục đích**: Xem log real-time, click TraceId để nhảy sang Tempo.

1. Sidebar **Add** → bấm **`+`** → panel editor
2. **Data source** → **`Loki`** → bấm **`Code`**
3. Paste query:
   ```logql
   {app="drive-api"}
   ```
4. Bấm **`Run queries`**
5. Cột phải — đổi loại biểu đồ sang **`Logs`**:
   - Bấm vào `Time series` → tìm chọn **`Logs`**
6. Cấu hình tiếp:
   - **Title**: `Live Application Logs`
   - **Options → Wrap lines**: bật ✅
   - **Options → Enable log details**: bật ✅
7. Bấm **`Apply`**

> ❓ **Không thấy log?** → Chạy API từ Swagger để tạo log, chờ 5 giây rồi Run queries lại.

---

## 8. Panel 5 — Log Volume by Severity

**Mục đích**: Bar chart thống kê số log theo cấp độ mỗi phút.

1. Sidebar **Add** → bấm **`+`** → panel editor
2. **Data source** → **`Loki`** → bấm **`Code`**
3. Paste query:
   ```logql
   sum by (level) (count_over_time({app="drive-api"}[1m]))
   ```
4. Bấm **`Run queries`**
5. Cột phải:
   - Đổi visualization → **`Bar chart`**
   - **Title**: `Log Volume by Severity`
6. Bấm **`Apply`**

---

## 9. Panel 6 — Recent Traces

**Mục đích**: Danh sách trace gần nhất, click để xem waterfall.

1. Sidebar **Add** → bấm **`+`** → panel editor
2. **Data source** → **`Tempo`**
3. *(Tempo dùng Builder mặc định, không cần bấm Code)*
4. Trong query editor:
   - **Query type**: chọn `Search`
   - **Service Name**: nhập `drive-api`
5. Bấm **`Run queries`**
6. Cột phải:
   - Đổi visualization → **`Traces`**
   - **Title**: `Recent API Traces`
7. Bấm **`Apply`**

---

## 10. Lưu Dashboard

1. Góc trên phải dashboard → bấm **`Save`** (nút xanh dương)
2. **Dashboard name**: `Drive - Unified Observability`
3. Bấm **`Save`**

**Cài auto-refresh**:
- Dropdown thời gian (hiện `Last 6 hours`) → chọn **`Last 15 minutes`**
- Bấm mũi tên xuống cạnh **`Refresh`** → chọn **`5s`**

---

## 11. Demo Flow End-to-End

### Bước 1 — Sinh traffic thực tế

Mở Swagger `http://localhost:5213/swagger` → gọi **5–10 API request** bất kỳ.

### Bước 2 — Quan sát Metrics (Prometheus)

- **HTTP Request Rate**: thấy đường tăng với label `controller=...`
- **p95 Latency**: thấy thời gian xử lý (ví dụ ~30ms)
- **Error Rate**: gọi API không có token → gauge đổi sang cam/đỏ

### Bước 3 — Đọc Log (Loki)

1. Panel **Live Application Logs** → thấy dòng log mới
2. Bấm vào một dòng log → mở chi tiết
3. Tìm field **`TraceId`** → bên cạnh có badge **`Tempo`** màu xanh → bấm vào

### Bước 4 — Xem Trace Waterfall (Tempo)

Grafana mở **Split View**: Log trái, Waterfall phải.

```
GET /api/...                     [45ms] ━━━━━━━━━━━━━━━━
  ├─ ASP.NET Middleware & Auth   [3ms]  ━
  ├─ SQL: SELECT DriveItems      [12ms]   ━━━━━
  └─ HTTP: S3 PreSignedUrl       [20ms]         ━━━━━━
```

### Bước 5 — Trace → Log

- Click vào một span trong waterfall
- Bấm **"Logs for this span"** trong popup

---

## 12. Import nhanh bằng JSON

Thay vì tạo từng panel thủ công:

1. Menu trái → **Dashboards** → **New** → **Import**
2. Paste JSON bên dưới vào ô **"Import via panel json"**
3. Bấm **Load** → **Import**

<details>
<summary><b>▶ Click để mở JSON Dashboard</b></summary>

```json
{
  "annotations": { "list": [] },
  "editable": true,
  "graphTooltip": 1,
  "panels": [
    {
      "collapsed": false,
      "gridPos": { "h": 1, "w": 24, "x": 0, "y": 0 },
      "id": 100,
      "title": "📊 HTTP Performance Metrics (Prometheus)",
      "type": "row"
    },
    {
      "datasource": { "type": "prometheus", "uid": "PBFA97CFB590B2093" },
      "fieldConfig": { "defaults": { "unit": "reqps" }, "overrides": [] },
      "gridPos": { "h": 8, "w": 12, "x": 0, "y": 1 },
      "id": 1,
      "title": "HTTP Request Rate (req/s)",
      "type": "timeseries",
      "targets": [{ "expr": "sum(rate(http_requests_received_total[1m])) by (controller, code)", "legendFormat": "{{controller}} ({{code}})" }]
    },
    {
      "datasource": { "type": "prometheus", "uid": "PBFA97CFB590B2093" },
      "fieldConfig": { "defaults": { "unit": "s" }, "overrides": [] },
      "gridPos": { "h": 8, "w": 8, "x": 12, "y": 1 },
      "id": 2,
      "title": "95th Percentile Latency (p95)",
      "type": "timeseries",
      "targets": [{ "expr": "histogram_quantile(0.95, sum(rate(http_request_duration_seconds_bucket[1m])) by (le, controller))", "legendFormat": "p95: {{controller}}" }]
    },
    {
      "datasource": { "type": "prometheus", "uid": "PBFA97CFB590B2093" },
      "fieldConfig": {
        "defaults": {
          "max": 100, "min": 0, "unit": "percent",
          "thresholds": { "mode": "absolute", "steps": [{ "color": "green", "value": null }, { "color": "orange", "value": 5 }, { "color": "red", "value": 10 }] }
        }
      },
      "gridPos": { "h": 8, "w": 4, "x": 20, "y": 1 },
      "id": 3,
      "title": "Error Rate (%)",
      "type": "gauge",
      "targets": [{ "expr": "(sum(rate(http_requests_received_total{code=~\"4..|5..\"}[1m])) or vector(0)) / (sum(rate(http_requests_received_total[1m])) > 0) * 100" }]
    },
    {
      "collapsed": false,
      "gridPos": { "h": 1, "w": 24, "x": 0, "y": 9 },
      "id": 101,
      "title": "📋 Application Logs (Loki)",
      "type": "row"
    },
    {
      "datasource": { "type": "loki", "uid": "Loki" },
      "gridPos": { "h": 9, "w": 16, "x": 0, "y": 10 },
      "id": 4,
      "options": { "enableLogDetails": true, "prettifyLogMessage": true, "showLabels": false, "sortOrder": "Descending", "wrapLogMessage": true },
      "title": "Live Application Logs",
      "type": "logs",
      "targets": [{ "expr": "{app=\"drive-api\"}" }]
    },
    {
      "datasource": { "type": "loki", "uid": "Loki" },
      "gridPos": { "h": 9, "w": 8, "x": 16, "y": 10 },
      "id": 5,
      "title": "Log Volume by Severity",
      "type": "barchart",
      "targets": [{ "expr": "sum by (level) (count_over_time({app=\"drive-api\"}[1m]))" }]
    },
    {
      "collapsed": false,
      "gridPos": { "h": 1, "w": 24, "x": 0, "y": 19 },
      "id": 102,
      "title": "🔍 Distributed Tracing (Tempo)",
      "type": "row"
    },
    {
      "datasource": { "type": "tempo", "uid": "Tempo" },
      "gridPos": { "h": 10, "w": 24, "x": 0, "y": 20 },
      "id": 6,
      "title": "Recent API Traces",
      "type": "traces",
      "targets": [{ "query": "drive-api", "queryType": "search" }]
    }
  ],
  "refresh": "5s",
  "schemaVersion": 39,
  "style": "dark",
  "tags": ["drive", "observability"],
  "time": { "from": "now-15m", "to": "now" },
  "timezone": "browser",
  "title": "Drive - Unified Observability",
  "uid": "drive-unified-obs",
  "version": 1
}
```
</details>
