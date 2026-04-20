# System Specification

本文件說明目前系統實際功能與設計。

---

## 一、使用角色

- Employee（員工）
- Manager（主管）

---

## 二、核心流程

1. 員工建立 DailyTrip（每日差旅）
2. 新增費用明細（ExpenseRecord）
3. 勾選多筆差旅建立 Application
4. 送出申請
5. 主管審核（核准 / 駁回）

---

## 三、差旅資料設計

### DailyTrip

- 出差日期
- 出差事由

限制：

- 同一員工同一天不可重複建立差旅資料（未作廢情況下）

---

### ExpenseRecord

- 費用分類
- 費用項目
- 金額
- 起點 / 終點
- 距離
- 補充說明
- 碳排資料（透過 CarbonEmissionRecord 關聯）

---

## 四、地圖與距離計算

- 使用 OpenStreetMap + OSRM
- 提供 geocode / route-preview API
- 前端可先顯示距離結果
- 送出時以前端距離為主，必要時由後端補算

---

## 五、申請單（Application）

狀態：

- Draft
- Submitted
- Approved
- Rejected
- Voided

規則：

- 僅 Draft / Rejected 狀態可修改
- 駁回後可修改再送

---

## 六、駁回流程

- 主管駁回時保存快照
- 主管可查看駁回當下資料
- 員工可修改原資料重新送出

---

## 七、制度資料

- 費率與碳排係數採版本管理
- 依使用日期套用對應版本
- 不直接覆寫歷史資料

---

## 八、帳號與權限

- 使用 ASP.NET Core Identity
- 帳號停用以 LockoutEnd 搭配 SecurityStamp 更新處理

---

## 九、驗證規則

- 日期不可超過今天
- 日期不可早於 90 天前
- 必填欄位需檢查
- 國內交通需有起點、終點與有效距離

---

## 十、分析資料

- 僅統計已核准資料
- 以月份進行彙總
