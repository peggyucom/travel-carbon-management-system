# Travel & Carbon Management System

本專題為一套差旅與碳排管理系統，主要解決員工在申報差旅與自用車里程補助時，需要查地圖並手動計算金額的問題，流程繁瑣且容易出錯。

---

## 核心功能

- 差旅報支管理（DailyTrip + Application）
- 地圖距離計算（整合 OpenStreetMap / OSRM）
- 自用車補助自動計算
- 主管審核、駁回與快照保存
- 費率與碳排係數版本管理
- 差旅費用與碳排分析

---

## 系統設計重點

- 將原本「月報支」流程，改為「明細建立 + 組成申請單」
- 由 `Application` 統一管理流程狀態，避免多狀態造成邏輯不一致
- 駁回時保存快照，確保主管與員工檢視資料基準一致
- 制度資料採版本管理，避免新設定影響歷史資料
- 使用 Controller、Service 與資料存取分層，提升維護性
- 使用 AJAX / fetch 處理部分互動與資料更新
- 距離計算服務透過 DI 注入，方便後續替換外部地圖服務

---

## 技術

- Backend: ASP.NET Core MVC / C#
- ORM: Entity Framework Core
- Database: SQL Server
- Frontend: Razor Views / JavaScript / Bootstrap
- Authentication: ASP.NET Core Identity
- Map Services: OpenStreetMap / OSRM
- Charts: Chart.js

---

## Configuration

本專案僅作為作品集與功能展示用途。

專案中未使用任何正式環境的真實敏感資訊。

示範資料僅用於展示系統流程，不對應任何真實使用者或正式環境。

---

## Demo Account

本系統提供測試帳號，方便快速體驗主要功能：

- Manager（主管）
- Employee（員工）

所有帳號與密碼皆為示範用途，未包含任何實際敏感資料。

---

## 專題文件

- [Initial Idea (from Excel)](./docs/Initial_Idea_from_Excel.md)