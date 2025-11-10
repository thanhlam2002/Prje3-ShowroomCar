# 🚗 API Test Samples - ShowroomCar

Các JSON mẫu để test toàn bộ luồng nghiệp vụ ShowroomCar.

## 📋 Mục lục
1. [Authentication](#1-authentication)
2. [Purchase Order (Đơn mua hàng)](#2-purchase-order-đơn-mua-hàng)
3. [Goods Receipt (Phiếu nhập kho)](#3-goods-receipt-phiếu-nhập-kho)
4. [Service Order (Kiểm định)](#4-service-order-kiểm-định)
5. [Sales Order (Đơn bán hàng)](#5-sales-order-đơn-bán-hàng)
6. [Payment (Thanh toán)](#6-payment-thanh-toán)

---

## 1. Authentication

### 1.1. Login
**POST** `/api/auth/login`

```json
{
  "usernameOrEmail": "admin",
  "password": "admin123"
}
```

**Response:**
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "username": "admin",
  "roles": ["ADMIN"]
}
```

**Lưu token để dùng cho các request sau:**
```
Authorization: Bearer {accessToken}
```

---

## 2. Purchase Order (Đơn mua hàng)

### 2.1. Tạo Purchase Order
**POST** `/api/purchaseorders`  
**Auth:** `RequireAdmin`

```json
{
  "supplierId": 1,
  "orderDate": "2024-01-15",
  "items": [
    {
      "modelId": 1,
      "qty": 3,
      "unitPrice": 500000000
    },
    {
      "modelId": 2,
      "qty": 2,
      "unitPrice": 600000000
    }
  ]
}
```

**Response:**
```json
{
  "poId": 1,
  "poNo": "PO-20240115120000000"
}
```

### 2.2. Gửi PO cho Supplier (gửi email)
**POST** `/api/purchaseorders/{poId}/send`  
**Auth:** `RequireAdmin`

**Request body:** (không cần body)

**Response:**
```json
{
  "message": "PO sent successfully",
  "poNo": "PO-20240115120000000",
  "status": "RECEIVING"
}
```

### 2.3. Xem danh sách PO
**GET** `/api/purchaseorders`  
**Auth:** `RequireAdmin`

### 2.4. Xem chi tiết PO
**GET** `/api/purchaseorders/{poId}`  
**Auth:** `RequireAdmin`

---

## 3. Goods Receipt (Phiếu nhập kho)

### 3.1. Tạo Goods Receipt (Nhập hàng từ PO)
**POST** `/api/goodsreceipts`  
**Auth:** `RequireAdmin`

```json
{
  "poId": 7,
  "warehouseId": 1,
  "receiptDate": "2024-01-20",
  "vehicles": [
    {
      "modelId": 1,
      "vin": "VIN-001-ABC123",
      "engineNo": "ENG-001-XYZ789",
      "color": "Trắng",
      "year": 2024,
      "landedCost": 520000000
    },
    {
      "modelId": 1,
      "vin": "VIN-002-DEF456",
      "engineNo": "ENG-002-UVW012",
      "color": "Đen",
      "year": 2024,
      "landedCost": 520000000
    },
    {
      "modelId": 1,
      "vin": "VIN-003-GHI789",
      "engineNo": "ENG-003-RST345",
      "color": "Bạc",
      "year": 2024,
      "landedCost": 520000000
    },
    {
      "modelId": 1,
      "vin": "VIN-004-JKL012",
      "engineNo": "ENG-004-MNO678",
      "color": "Đỏ",
      "year": 2024,
      "landedCost": 620000000
    },
    {
      "modelId": 1,
      "vin": "VIN-005-PQR345",
      "engineNo": "ENG-005-STU901",
      "color": "Xanh",
      "year": 2024,
      "landedCost": 620000000
    }
  ]
}
```

**Response:**
```json
{
  "grId": 1,
  "grNo": "GR-20240120120000000",
  "vehicles": 5,
  "serviceOrders": 5
}
```

**Lưu ý:** Hệ thống tự động tạo ServiceOrder cho từng xe với status "PLANNED"

### 3.2. Xem chi tiết Goods Receipt
**GET** `/api/goodsreceipts/{grId}`  
**Auth:** `RequireAdmin`

---

## 4. Service Order (Kiểm định)

### 4.1. Xem danh sách Service Orders
**GET** `/api/serviceorders`  
**Query params:** `?vehicleId=1&status=PLANNED&fromDate=2024-01-20&toDate=2024-01-25`  
**Auth:** `RequireEmployee`

### 4.2. Xem chi tiết Service Order
**GET** `/api/serviceorders/{svcId}`  
**Auth:** `RequireEmployee`

### 4.3. Tạo Service Order thủ công (nếu cần)
**POST** `/api/serviceorders`  
**Auth:** `RequireEmployee`

```json
{
  "vehicleId": 1,
  "scheduledDate": "2024-01-21",
  "notes": "Kiểm định xe mới nhập"
}
```

### 4.4. Bắt đầu kiểm định
**POST** `/api/serviceorders/{svcId}/start`  
**Auth:** `RequireEmployee`

**Request body:** (không cần body)

**Response:**
```json
{
  "message": "Service SVC-INSP-20240120120000000 started."
}
```

### 4.5. Hoàn tất kiểm định (QUAN TRỌNG)
**POST** `/api/serviceorders/{svcId}/complete`  
**Auth:** `RequireEmployee`

```json
{
  "passedVehicles": [1, 2, 3, 4],
  "failedVehicles": [5]
}
```

**Giải thích:**
- `passedVehicles`: Danh sách VehicleId đạt kiểm định → Status = "IN_STOCK"
- `failedVehicles`: Danh sách VehicleId trượt kiểm định → Tạo GoodsReturn + Status = "RETURNED"

**Response:**
```json
{
  "svcId": 1,
  "svcNo": "SVC-INSP-20240120120000000",
  "status": "DONE",
  "passed": 4,
  "failed": 1
}
```

**Lưu ý:** 
- Xe đạt sẽ được chuyển status = "IN_STOCK" và có thể bán
- Xe trượt sẽ tạo GoodsReturn tự động
- Nếu tất cả xe trong PO đã kiểm định xong, PO sẽ tự động chuyển status = "CLOSED"

### 4.6. Cập nhật Service Order
**PUT** `/api/serviceorders/{svcId}`  
**Auth:** `RequireEmployee`

```json
{
  "scheduledDate": "2024-01-22",
  "notes": "Cập nhật lịch kiểm định"
}
```

### 4.7. Hủy Service Order
**POST** `/api/serviceorders/{svcId}/cancel`  
**Auth:** `RequireEmployee`

---

## 5. Sales Order (Đơn bán hàng)

### 5.1. Tạo Sales Order
**POST** `/api/salesorders`  
**Auth:** `RequireEmployee`

```json
{
  "customerId": 1,
  "items": [
    {
      "vehicleId": 1,
      "sellPrice": 550000000,
      "discount": 10000000,
      "tax": 55000000
    },
    {
      "vehicleId": 2,
      "sellPrice": 550000000,
      "discount": 0,
      "tax": 55000000
    }
  ]
}
```

**Response:**
```json
{
  "soId": 1,
  "soNo": "SO-20240122120000000"
}
```

**Lưu ý:** 
- Xe sẽ chuyển status từ "IN_STOCK" → "ALLOCATED"
- Sales Order status = "DRAFT"

### 5.2. Xem danh sách Sales Orders
**GET** `/api/salesorders`  
**Auth:** `RequireEmployee`

### 5.3. Xem chi tiết Sales Order
**GET** `/api/salesorders/{soId}`  
**Auth:** `RequireEmployee`

### 5.4. Xác nhận Sales Order (Tạo Invoice)
**POST** `/api/salesorders/{soId}/confirm`  
**Auth:** `RequireEmployee`

**Request body:** (không cần body)

**Response:**
```json
{
  "message": "Sales order confirmed and invoice created.",
  "soId": 1,
  "invoiceNo": "INV-20240122120000000"
}
```

**Lưu ý:**
- Xe chuyển status từ "ALLOCATED" → "SOLD"
- Sales Order status = "COMPLETED"
- Tự động tạo Invoice và InvoiceItems

### 5.5. Xóa Sales Order (chỉ khi DRAFT)
**DELETE** `/api/salesorders/{soId}`  
**Auth:** `RequireEmployee`

---

## 6. Payment (Thanh toán)

### 6.1. Xem danh sách Invoices của Customer
**GET** `/api/invoices?customerId=1`  
**Auth:** `RequireEmployee`

### 6.2. Tạo Payment (Phiếu thu)
**POST** `/api/payments`  
**Auth:** `RequireEmployee`

```json
{
  "customerId": 1,
  "paymentDate": "2024-01-25",
  "method": "CASH",
  "amount": 1000000000,
  "notes": "Thanh toán một phần"
}
```

**Response:**
```json
{
  "paymentId": 1,
  "receiptNo": "RCP-20240125120000000"
}
```

**Methods có thể dùng:** `CASH`, `BANK_TRANSFER`, `CREDIT_CARD`, `CHEQUE`

### 6.3. Phân bổ Payment vào Invoice
**POST** `/api/payments/{paymentId}/allocate`  
**Auth:** `RequireEmployee`

```json
{
  "allocations": [
    {
      "invoiceId": 1,
      "amount": 1000000000
    }
  ]
}
```

**Response:**
```json
{
  "paymentId": 1,
  "receiptNo": "RCP-20240125120000000",
  "allocated": 1000000000,
  "remaining": 0
}
```

### 6.4. Xem danh sách Payments
**GET** `/api/payments?customerId=1`  
**Auth:** `RequireEmployee`

---

## 📝 Luồng Test Hoàn Chỉnh

### Test Case 1: Luồng mua hàng và kiểm định

1. **Login** → Lấy token
2. **Tạo PO** → `poId = 1`
3. **Gửi PO** → Status = "RECEIVING"
4. **Tạo GR** với 5 xe → Tự động tạo 5 ServiceOrders
5. **Start ServiceOrder** cho từng xe (hoặc theo lô)
6. **Complete ServiceOrder** → 4 xe đạt, 1 xe trượt
7. **Kiểm tra:** 4 xe status = "IN_STOCK", 1 xe status = "RETURNED"

### Test Case 2: Luồng bán hàng

1. **Tạo SalesOrder** với 2 xe IN_STOCK → Xe chuyển "ALLOCATED"
2. **Confirm SalesOrder** → Tạo Invoice, xe chuyển "SOLD"
3. **Tạo Payment** → Thu tiền
4. **Allocate Payment** → Phân bổ vào Invoice

### Test Case 3: Luồng đầy đủ

1. Login
2. Tạo PO (3 xe model 1, 2 xe model 2)
3. Gửi PO
4. Tạo GR (nhập 5 xe)
5. Start tất cả ServiceOrders
6. Complete ServiceOrders (4 đạt, 1 trượt)
7. Tạo SalesOrder với 2 xe đạt
8. Confirm SalesOrder → Tạo Invoice
9. Tạo Payment
10. Allocate Payment vào Invoice

---

## 🔧 Các Endpoint Khác

### Customers
- **GET** `/api/customers` - Danh sách khách hàng
- **POST** `/api/customers` - Tạo khách hàng mới
- **GET** `/api/customers/{id}` - Chi tiết khách hàng

### Vehicles
- **GET** `/api/vehicles?status=IN_STOCK` - Danh sách xe
- **GET** `/api/vehicles/{id}` - Chi tiết xe

### Warehouses
- **GET** `/api/warehouses` - Danh sách kho

### Reports
- **GET** `/api/reports/sales?fromDate=2024-01-01&toDate=2024-01-31` - Báo cáo bán hàng

---

## ⚠️ Lưu ý

1. **Date Format:** Sử dụng `YYYY-MM-DD` cho DateOnly
2. **Authorization:** Tất cả endpoint (trừ login) cần header:
   ```
   Authorization: Bearer {token}
   ```
3. **Roles:**
   - `ADMIN`: Toàn quyền
   - `EMPLOYEE`: Quyền nhân viên (không tạo PO, GR)
4. **Status Flow:**
   - Vehicle: `UNDER_INSPECTION` → `IN_STOCK` → `ALLOCATED` → `SOLD`
   - ServiceOrder: `PLANNED` → `IN_PROGRESS` → `DONE`
   - SalesOrder: `DRAFT` → `COMPLETED`
   - PO: `PENDING` → `RECEIVING` → `CLOSED`

---

## 🧪 Test với Postman/Thunder Client

1. Import collection từ file này
2. Set biến `{{baseUrl}}` = `https://localhost:5001` hoặc URL của bạn
3. Set biến `{{token}}` sau khi login
4. Chạy các request theo thứ tự luồng

---

**Chúc bạn test thành công! 🚀**

