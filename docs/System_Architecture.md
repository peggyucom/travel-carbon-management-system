# System Architecture

本系統採用 ASP.NET Core MVC 架構，並依目前專案實作方式，將畫面、流程控制、商業邏輯、資料存取與外部服務進行分層。

設計上以支援差旅報支與審核流程為主，並在可行範圍內維持結構清楚，讓後續功能擴充與維護較容易進行。

---

## 一、整體架構概念

系統主要分為以下幾個部分：

- 畫面層（Presentation Layer）
- 控制層（Controller Layer）
- 商業邏輯層（Service Layer）
- 資料存取（Data Layer）
- 外部服務整合（External Service）

---

## 二、各層說明

### 1. 畫面層（Presentation Layer）

畫面層由 Razor Views 與前端 JavaScript 組成，負責使用者操作介面與互動。

目前使用技術包含：

- Razor Views：負責頁面結構與資料呈現
- JavaScript / jQuery / fetch：處理部分互動與資料更新
- Bootstrap：基本版面與 UI 元件
- Chart.js：Dashboard 圖表呈現

主要職責：

- 顯示表單與資料
- 接收使用者輸入
- 呼叫 API 取得即時資料（例如距離試算）
- 更新畫面內容與圖表

---

### 2. 控制層（Controller Layer）

Controller 負責接收請求、控制頁面流程，並將資料交由 Service 或 View 處理。

目前系統分為兩種使用方式：

#### (1) MVC Controller

例如：

- ExpenseController
- ApplicationController
- DashboardController
- AdminController
- FaqController
- AccountController

主要負責：

- 回傳 View
- 接收表單資料
- 控制新增、編輯、送出與審核流程
- 進行基本權限判斷與資料準備

補充說明：

目前部分驗證與流程判斷仍會直接寫在 Controller，例如：

- 差旅日期不可超過今天
- 差旅日期不可早於 90 天前
- 部分欄位驗證
- 距離與費用的初步檢查

在目前專案規模下，這樣的做法可以維持開發效率與可讀性，因此沒有強制全部抽到 Service。

---

#### (2) API 路由（JSON 回傳）

例如：

- ExpenseApiController
- Dashboard 相關 API 路由

主要用途：

- 地址轉換（geocode）
- 路線距離試算（route-preview）
- Dashboard 資料查詢
- 差旅明細資料查詢

這樣的設計讓畫面流程與資料取得可以分開處理，避免單一 Controller 過於複雜。

---

### 3. 商業邏輯層（Service Layer）

Service 層負責系統主要商業邏輯，目前是邏輯集中處。

主要 Service 包含：

- ApplicationService
- ExpenseService
- DailyTripService
- CarbonService
- ReportService
- SystemConfigService
- FaqService

主要負責：

- 申請單建立、送出、核准、駁回與作廢流程
- 狀態判斷與操作限制
- 駁回快照建立與查詢
- 費率與制度資料查詢
- Dashboard 分析資料整理
- 碳排計算處理

設計上會盡量將流程規則集中在 Service，避免分散在各個 Controller。

---

### 4. 資料存取（Data Layer）

資料存取透過 Entity Framework Core 與 ApplicationDbContext 處理，資料庫使用 SQL Server。

主要方式：

- 使用 DbContext 管理資料表與關聯
- 透過 EF Core 進行查詢與更新
- 使用 migration 管理資料結構

核心資料包含：

- DailyTrips
- ExpenseRecords
- Applications
- ApprovalHistories
- ReportSnapshots
- CarbonEmissionRecords
- 各類費率與碳排係數資料

補充說明：

目前沒有另外設計 Repository 層，資料存取主要由 Service 透過 DbContext 處理，部分畫面資料準備也會由 Controller 直接查詢資料庫。

考量本專案規模，這樣的設計已能維持可讀性與開發效率，因此沒有額外增加抽象層。

---

### 5. 外部服務整合（External Service）

目前系統整合地圖與路線服務，用於地址查詢與距離計算。

使用服務：

- OpenStreetMap
- OSRM

封裝方式：

- IDistanceService
- OpenStreetMapDistanceService

主要負責：

- 地址轉經緯度
- 路線距離計算
- 提供前端距離資料

設計上透過介面抽象化外部服務，避免 Controller 直接依賴外部 API。

未來若需要替換地圖服務（例如 Google Maps），可降低修改範圍。

---

## 三、依賴注入（Dependency Injection）

系統透過 ASP.NET Core 內建 DI 容器管理服務。

目前使用方式：

- Controller 注入 Service
- Service 注入 DbContext 與外部服務
- 在 Program.cs 中統一註冊

優點：

- 降低模組耦合
- 提升可維護性
- 方便後續替換實作

---

## 四、資料流程（簡化）

以距離計算與費用建立為例：

1. 使用者輸入起點與終點
2. 前端呼叫 API 取得距離資料
3. API 呼叫外部地圖服務
4. 回傳距離結果
5. 使用者送出表單
6. Controller 處理流程與基本驗證
7. Service 處理邏輯並寫入資料
8. 透過 EF Core 將資料存入資料庫

---

## 五、架構設計重點

- 採用 MVC 架構區分畫面與流程
- 商業邏輯主要集中於 Service 層
- API 與畫面流程有一定程度分離
- 外部服務透過介面封裝
- 使用 DI 管理相依性

---

## 六、設計考量

本專案在設計上：

- 以可理解與可維護為優先
- 保留基本分層，但不過度設計
- 部分邏輯仍保留在 Controller
- 未額外拆出 Repository 層

在目前專案規模與開發時間下，這樣的設計較容易維護，也較符合實作需求。