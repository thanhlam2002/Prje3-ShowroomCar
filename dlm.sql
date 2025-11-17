-- =====================================================
-- ShowroomCar - MySQL/MariaDB Schema for XAMPP
-- Engine: InnoDB, Charset: utf8mb4
-- =====================================================

SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;

-- Tạo database và sử dụng
CREATE DATABASE IF NOT EXISTS showroom
  CHARACTER SET utf8mb4
  COLLATE utf8mb4_unicode_ci;
USE showroom;

-- =====================================================
-- 1) Identity & Access
-- =====================================================
DROP TABLE IF EXISTS user_roles;
DROP TABLE IF EXISTS users;
DROP TABLE IF EXISTS roles;

CREATE TABLE roles (
  role_id INT AUTO_INCREMENT PRIMARY KEY,
  code VARCHAR(50) NOT NULL UNIQUE,
  name VARCHAR(100) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE users (
  user_id BIGINT AUTO_INCREMENT PRIMARY KEY,
  username VARCHAR(100) NOT NULL UNIQUE,
  email VARCHAR(255) NOT NULL UNIQUE,
  password_hash VARCHAR(255) NOT NULL,
  active TINYINT(1) NOT NULL DEFAULT 1,
  created_at DATETIME NOT NULL,
  updated_at DATETIME NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE user_roles (
  user_id BIGINT NOT NULL,
  role_id INT NOT NULL,
  PRIMARY KEY (user_id, role_id),
  CONSTRAINT fk_user_roles_user FOREIGN KEY (user_id) REFERENCES users(user_id),
  CONSTRAINT fk_user_roles_role FOREIGN KEY (role_id) REFERENCES roles(role_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- =====================================================
-- 2) Parties
-- =====================================================
DROP TABLE IF EXISTS customers;
DROP TABLE IF EXISTS suppliers;

CREATE TABLE customers (
  customer_id BIGINT AUTO_INCREMENT PRIMARY KEY,
  code VARCHAR(50) UNIQUE,
  full_name VARCHAR(200) NOT NULL,
  phone VARCHAR(30),
  email VARCHAR(255),
  address TEXT,
  id_no VARCHAR(50),
  created_at DATETIME NOT NULL,
  updated_at DATETIME NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE suppliers (
  supplier_id INT AUTO_INCREMENT PRIMARY KEY,
  code VARCHAR(50) NOT NULL UNIQUE,
  name VARCHAR(200) NOT NULL,
  phone VARCHAR(30),
  email VARCHAR(255),
  address TEXT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- =====================================================
-- 3) Catalog & Inventory
-- =====================================================
DROP TABLE IF EXISTS vehicle_images;
DROP TABLE IF EXISTS inventory_moves;
DROP TABLE IF EXISTS vehicles;
DROP TABLE IF EXISTS warehouses;
DROP TABLE IF EXISTS vehicle_models;
DROP TABLE IF EXISTS brands;

CREATE TABLE brands (
  brand_id INT AUTO_INCREMENT PRIMARY KEY,
  name VARCHAR(100) NOT NULL UNIQUE,
  country VARCHAR(80)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE vehicle_models (
  model_id INT AUTO_INCREMENT PRIMARY KEY,
  model_no VARCHAR(50) NOT NULL UNIQUE,
  name VARCHAR(150) NOT NULL,
  brand_id INT NOT NULL,
  base_price DECIMAL(14,2) NOT NULL DEFAULT 0,
  fuel_type VARCHAR(20),
  transmission VARCHAR(10),
  seat_no INT,
  specs TEXT,        -- Đổi từ JSON sang TEXT
  active TINYINT(1) NOT NULL DEFAULT 1,
  CONSTRAINT fk_vehicle_models_brand FOREIGN KEY (brand_id) REFERENCES brands(brand_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE warehouses (
  warehouse_id INT AUTO_INCREMENT PRIMARY KEY,
  code VARCHAR(50) NOT NULL UNIQUE,
  name VARCHAR(150) NOT NULL,
  address TEXT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE vehicles (
  vehicle_id BIGINT AUTO_INCREMENT PRIMARY KEY,
  model_id INT NOT NULL,
  vin VARCHAR(64) NOT NULL UNIQUE,
  engine_no VARCHAR(64) NOT NULL UNIQUE,
  color VARCHAR(50),
  year INT,
  status VARCHAR(20) NOT NULL,
  current_warehouse_id INT,
  acquired_at DATETIME NULL,
  updated_at DATETIME NULL,
  INDEX ix_vehicles_status_wh (status, current_warehouse_id),
  INDEX ix_vehicles_model_color (model_id, color),
  CONSTRAINT fk_vehicles_model FOREIGN KEY (model_id) REFERENCES vehicle_models(model_id),
  CONSTRAINT fk_vehicles_wh FOREIGN KEY (current_warehouse_id) REFERENCES warehouses(warehouse_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE vehicle_images (
  image_id BIGINT AUTO_INCREMENT PRIMARY KEY,
  vehicle_id BIGINT NOT NULL,
  url TEXT NOT NULL,
  kind VARCHAR(20),
  sort_order INT DEFAULT 0,
  created_at DATETIME NULL,
  CONSTRAINT fk_vehicle_images_vehicle FOREIGN KEY (vehicle_id) REFERENCES vehicles(vehicle_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE inventory_moves (
  move_id BIGINT AUTO_INCREMENT PRIMARY KEY,
  vehicle_id BIGINT NOT NULL,
  from_warehouse_id INT NULL,
  to_warehouse_id INT NULL,
  reason VARCHAR(20) NOT NULL,
  moved_at DATETIME NULL,
  moved_by BIGINT NULL,
  CONSTRAINT fk_inventory_moves_vehicle FOREIGN KEY (vehicle_id) REFERENCES vehicles(vehicle_id),
  CONSTRAINT fk_inventory_moves_from_wh FOREIGN KEY (from_warehouse_id) REFERENCES warehouses(warehouse_id),
  CONSTRAINT fk_inventory_moves_to_wh FOREIGN KEY (to_warehouse_id) REFERENCES warehouses(warehouse_id),
  CONSTRAINT fk_inventory_moves_user FOREIGN KEY (moved_by) REFERENCES users(user_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- =====================================================
-- 4) Procurement (PO -> GR)
-- =====================================================
DROP TABLE IF EXISTS goods_receipt_items;
DROP TABLE IF EXISTS goods_receipts;
DROP TABLE IF EXISTS purchase_order_items;
DROP TABLE IF EXISTS purchase_orders;

CREATE TABLE purchase_orders (
  po_id BIGINT AUTO_INCREMENT PRIMARY KEY,
  po_no VARCHAR(50) NOT NULL UNIQUE,
  supplier_id INT NOT NULL,
  status VARCHAR(20) NOT NULL,
  order_date DATE NOT NULL,
  total_amount DECIMAL(14,2) NOT NULL DEFAULT 0,
  created_by BIGINT NULL,
  created_at DATETIME NULL,
  CONSTRAINT fk_po_supplier FOREIGN KEY (supplier_id) REFERENCES suppliers(supplier_id),
  CONSTRAINT fk_po_user FOREIGN KEY (created_by) REFERENCES users(user_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE purchase_order_items (
  po_item_id BIGINT AUTO_INCREMENT PRIMARY KEY,
  po_id BIGINT NOT NULL,
  model_id INT NOT NULL,
  qty INT NOT NULL,
  unit_price DECIMAL(14,2) NOT NULL,
  line_total DECIMAL(14,2) NULL,
  CONSTRAINT fk_poi_po FOREIGN KEY (po_id) REFERENCES purchase_orders(po_id),
  CONSTRAINT fk_poi_model FOREIGN KEY (model_id) REFERENCES vehicle_models(model_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE goods_receipts (
  gr_id BIGINT AUTO_INCREMENT PRIMARY KEY,
  gr_no VARCHAR(50) NOT NULL UNIQUE,
  po_id BIGINT NULL,
  receipt_date DATE NOT NULL,
  warehouse_id INT NOT NULL,
  created_by BIGINT NULL,
  created_at DATETIME NULL,
  CONSTRAINT fk_gr_po FOREIGN KEY (po_id) REFERENCES purchase_orders(po_id),
  CONSTRAINT fk_gr_wh FOREIGN KEY (warehouse_id) REFERENCES warehouses(warehouse_id),
  CONSTRAINT fk_gr_user FOREIGN KEY (created_by) REFERENCES users(user_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE goods_receipt_items (
  gr_item_id BIGINT AUTO_INCREMENT PRIMARY KEY,
  gr_id BIGINT NOT NULL,
  vehicle_id BIGINT NOT NULL UNIQUE,
  landed_cost DECIMAL(14,2) DEFAULT 0,
  CONSTRAINT fk_gri_gr FOREIGN KEY (gr_id) REFERENCES goods_receipts(gr_id),
  CONSTRAINT fk_gri_vehicle FOREIGN KEY (vehicle_id) REFERENCES vehicles(vehicle_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- =====================================================
-- 5) Sales (Quotation -> SO -> Service -> Invoice)
-- =====================================================
DROP TABLE IF EXISTS service_orders;
DROP TABLE IF EXISTS allotments;
DROP TABLE IF EXISTS sales_order_items;
DROP TABLE IF EXISTS sales_orders;
DROP TABLE IF EXISTS quotation_items;
DROP TABLE IF EXISTS quotations;

CREATE TABLE quotations (
  quote_id BIGINT AUTO_INCREMENT PRIMARY KEY,
  quote_no VARCHAR(50) NOT NULL UNIQUE,
  customer_id BIGINT NOT NULL,
  quote_date DATE NOT NULL,
  status VARCHAR(20) NOT NULL,
  subtotal DECIMAL(14,2) NOT NULL DEFAULT 0,
  discount DECIMAL(14,2) NOT NULL DEFAULT 0,
  tax DECIMAL(14,2) NOT NULL DEFAULT 0,
  grand_total DECIMAL(14,2) NOT NULL DEFAULT 0,
  created_by BIGINT NULL,
  CONSTRAINT fk_quote_customer FOREIGN KEY (customer_id) REFERENCES customers(customer_id),
  CONSTRAINT fk_quote_user FOREIGN KEY (created_by) REFERENCES users(user_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE quotation_items (
  quote_item_id BIGINT AUTO_INCREMENT PRIMARY KEY,
  quote_id BIGINT NOT NULL,
  model_id INT NOT NULL,
  qty INT NOT NULL,
  unit_price DECIMAL(14,2) NOT NULL,
  line_total DECIMAL(14,2) NULL,
  CONSTRAINT fk_qi_quote FOREIGN KEY (quote_id) REFERENCES quotations(quote_id),
  CONSTRAINT fk_qi_model FOREIGN KEY (model_id) REFERENCES vehicle_models(model_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE sales_orders (
  so_id BIGINT AUTO_INCREMENT PRIMARY KEY,
  so_no VARCHAR(50) NOT NULL UNIQUE,
  customer_id BIGINT NOT NULL,
  order_date DATE NOT NULL,
  status VARCHAR(20) NOT NULL,
  created_by BIGINT NULL,
  assigned_to BIGINT NULL,
  subtotal DECIMAL(14,2) NOT NULL DEFAULT 0,
  discount DECIMAL(14,2) NOT NULL DEFAULT 0,
  tax DECIMAL(14,2) NOT NULL DEFAULT 0,
  grand_total DECIMAL(14,2) NOT NULL DEFAULT 0,
  INDEX ix_so_cust_status_date (customer_id, status, order_date),
  CONSTRAINT fk_so_customer FOREIGN KEY (customer_id) REFERENCES customers(customer_id),
  CONSTRAINT fk_so_created_by FOREIGN KEY (created_by) REFERENCES users(user_id),
  CONSTRAINT fk_so_assigned_to FOREIGN KEY (assigned_to) REFERENCES users(user_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE sales_order_items (
  so_item_id BIGINT AUTO_INCREMENT PRIMARY KEY,
  so_id BIGINT NOT NULL,
  vehicle_id BIGINT NOT NULL UNIQUE,
  sell_price DECIMAL(14,2) NOT NULL,
  discount DECIMAL(14,2) DEFAULT 0,
  tax DECIMAL(14,2) DEFAULT 0,
  line_total DECIMAL(14,2) NULL,
  CONSTRAINT fk_soi_so FOREIGN KEY (so_id) REFERENCES sales_orders(so_id),
  CONSTRAINT fk_soi_vehicle FOREIGN KEY (vehicle_id) REFERENCES vehicles(vehicle_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE allotments (
  allot_id BIGINT AUTO_INCREMENT PRIMARY KEY,
  so_id BIGINT NOT NULL,
  vehicle_id BIGINT NOT NULL UNIQUE,
  reserved_at DATETIME NULL,
  status VARCHAR(20) NOT NULL,
  CONSTRAINT fk_allot_so FOREIGN KEY (so_id) REFERENCES sales_orders(so_id),
  CONSTRAINT fk_allot_vehicle FOREIGN KEY (vehicle_id) REFERENCES vehicles(vehicle_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE service_orders (
  svc_id BIGINT AUTO_INCREMENT PRIMARY KEY,
  svc_no VARCHAR(50) NOT NULL UNIQUE,
  vehicle_id BIGINT NOT NULL,
  scheduled_date DATE NULL,
  status VARCHAR(20) NOT NULL,
  notes TEXT,
  created_by BIGINT NULL,
  created_at DATETIME NULL,
  CONSTRAINT fk_svc_vehicle FOREIGN KEY (vehicle_id) REFERENCES vehicles(vehicle_id),
  CONSTRAINT fk_svc_user FOREIGN KEY (created_by) REFERENCES users(user_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- =====================================================
-- 6) Registration & Documents
-- =====================================================
DROP TABLE IF EXISTS document_links;
DROP TABLE IF EXISTS documents;
DROP TABLE IF EXISTS vehicle_registrations;

CREATE TABLE vehicle_registrations (
  reg_id BIGINT AUTO_INCREMENT PRIMARY KEY,
  vehicle_id BIGINT NOT NULL UNIQUE,
  reg_no VARCHAR(50),
  reg_date DATE,
  owner_name VARCHAR(200),
  address TEXT,
  fields TEXT,      -- Đổi từ JSON sang TEXT
  CONSTRAINT fk_reg_vehicle FOREIGN KEY (vehicle_id) REFERENCES vehicles(vehicle_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE documents (
  doc_id BIGINT AUTO_INCREMENT PRIMARY KEY,
  doc_no VARCHAR(50) NOT NULL UNIQUE,
  doc_type VARCHAR(20) NOT NULL,
  doc_date DATE NOT NULL,
  customer_id BIGINT NULL,
  related_id BIGINT NULL,
  related_table VARCHAR(50) NULL,
  storage_url TEXT NOT NULL,
  created_by BIGINT NULL,
  created_at DATETIME NULL,
  CONSTRAINT fk_doc_customer FOREIGN KEY (customer_id) REFERENCES customers(customer_id),
  CONSTRAINT fk_doc_user FOREIGN KEY (created_by) REFERENCES users(user_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE document_links (
  link_id BIGINT AUTO_INCREMENT PRIMARY KEY,
  doc_id BIGINT NOT NULL,
  entity_table VARCHAR(50) NOT NULL,
  entity_id BIGINT NOT NULL,
  CONSTRAINT fk_doclink_doc FOREIGN KEY (doc_id) REFERENCES documents(doc_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- =====================================================
-- 7) Invoicing & Payments
-- =====================================================
DROP TABLE IF EXISTS payment_allocations;
DROP TABLE IF EXISTS payments;
DROP TABLE IF EXISTS invoice_items;
DROP TABLE IF EXISTS invoices;

CREATE TABLE invoices (
  invoice_id BIGINT AUTO_INCREMENT PRIMARY KEY,
  invoice_no VARCHAR(50) NOT NULL UNIQUE,
  so_id BIGINT NULL,
  customer_id BIGINT NOT NULL,
  invoice_date DATE NOT NULL,
  status VARCHAR(20) NOT NULL,
  subtotal DECIMAL(14,2) NOT NULL DEFAULT 0,
  discount DECIMAL(14,2) NOT NULL DEFAULT 0,
  tax DECIMAL(14,2) NOT NULL DEFAULT 0,
  grand_total DECIMAL(14,2) NOT NULL DEFAULT 0,
  created_by BIGINT NULL,
  created_at DATETIME NULL,
  CONSTRAINT fk_inv_so FOREIGN KEY (so_id) REFERENCES sales_orders(so_id),
  CONSTRAINT fk_inv_customer FOREIGN KEY (customer_id) REFERENCES customers(customer_id),
  CONSTRAINT fk_inv_user FOREIGN KEY (created_by) REFERENCES users(user_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE invoice_items (
  inv_item_id BIGINT AUTO_INCREMENT PRIMARY KEY,
  invoice_id BIGINT NOT NULL,
  vehicle_id BIGINT NOT NULL,
  unit_price DECIMAL(14,2) NOT NULL,
  discount DECIMAL(14,2) DEFAULT 0,
  tax DECIMAL(14,2) DEFAULT 0,
  line_total DECIMAL(14,2) NULL,
  CONSTRAINT fk_invi_inv FOREIGN KEY (invoice_id) REFERENCES invoices(invoice_id),
  CONSTRAINT fk_invi_vehicle FOREIGN KEY (vehicle_id) REFERENCES vehicles(vehicle_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE payments (
  payment_id BIGINT AUTO_INCREMENT PRIMARY KEY,
  receipt_no VARCHAR(50) NOT NULL UNIQUE,
  customer_id BIGINT NOT NULL,
  payment_date DATE NOT NULL,
  method VARCHAR(20) NOT NULL,
  amount DECIMAL(14,2) NOT NULL,
  notes TEXT,
  CONSTRAINT fk_pay_customer FOREIGN KEY (customer_id) REFERENCES customers(customer_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE payment_allocations (
  alloc_id BIGINT AUTO_INCREMENT PRIMARY KEY,
  payment_id BIGINT NOT NULL,
  invoice_id BIGINT NOT NULL,
  amount_applied DECIMAL(14,2) NOT NULL,
  CONSTRAINT fk_alloc_pay FOREIGN KEY (payment_id) REFERENCES payments(payment_id),
  CONSTRAINT fk_alloc_invoice FOREIGN KEY (invoice_id) REFERENCES invoices(invoice_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- =====================================================
-- 8) Waitlist
-- =====================================================
DROP TABLE IF EXISTS waitlist_entries;
DROP TABLE IF EXISTS waitlists;

CREATE TABLE waitlists (
  waitlist_id INT AUTO_INCREMENT PRIMARY KEY,
  name VARCHAR(150) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE waitlist_entries (
  entry_id BIGINT AUTO_INCREMENT PRIMARY KEY,
  waitlist_id INT NOT NULL,
  customer_id BIGINT NOT NULL,
  model_id INT NOT NULL,
  preferred_color VARCHAR(50),
  requested_date DATE,
  status VARCHAR(20) NOT NULL,
  CONSTRAINT fk_wle_waitlist FOREIGN KEY (waitlist_id) REFERENCES waitlists(waitlist_id),
  CONSTRAINT fk_wle_customer FOREIGN KEY (customer_id) REFERENCES customers(customer_id),
  CONSTRAINT fk_wle_model FOREIGN KEY (model_id) REFERENCES vehicle_models(model_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- =====================================================
-- 9) Audit
-- =====================================================
DROP TABLE IF EXISTS audit_logs;

CREATE TABLE audit_logs (
  id BIGINT AUTO_INCREMENT PRIMARY KEY,
  entity VARCHAR(50) NOT NULL,
  entity_id BIGINT NOT NULL,
  action VARCHAR(20) NOT NULL,
  changes TEXT,     -- Đổi từ JSON sang TEXT
  actor_user_id BIGINT NULL,
  created_at DATETIME NULL,
  CONSTRAINT fk_audit_user FOREIGN KEY (actor_user_id) REFERENCES users(user_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

SET FOREIGN_KEY_CHECKS = 1;

-- =====================================================
-- Sample Data (1–2 dòng cho mỗi bảng)
-- =====================================================

INSERT INTO roles (code, name) VALUES ('ADMIN', 'Administrator');

INSERT INTO users (username, email, password_hash, active, created_at)
VALUES ('admin', 'admin@example.com', '$2a$12$examplehashedpassword', 1, NOW());

INSERT INTO user_roles (user_id, role_id) VALUES (1, 1);

INSERT INTO customers (code, full_name, phone, email, address, id_no, created_at) 
VALUES ('CUST001', 'John Doe', '0900000000', 'john@example.com', '123 Main St', 'ID123', NOW());

INSERT INTO suppliers (code, name, phone, email, address)
VALUES ('SUP001', 'ACME Motors', '0900111222', 'acme@example.com', 'Supplier Address');

INSERT INTO brands (name, country) VALUES ('Toyota', 'Japan');

INSERT INTO vehicle_models (model_no, name, brand_id, base_price, fuel_type, transmission, seat_no, specs, active)
VALUES ('CAMRY-25', 'Camry 2.5', 1, 35000.00, 'petrol', 'AT', 5, '{"hp":203}', 1);

INSERT INTO warehouses (code, name, address)
VALUES ('WH01', 'Main Warehouse', 'Lot A, City');

INSERT INTO vehicles (model_id, vin, engine_no, color, year, status, current_warehouse_id, acquired_at, updated_at)
VALUES (1, 'VIN000000000001', 'ENG000000000001', 'Black', 2024, 'IN_STOCK', 1, NOW(), NOW());

INSERT INTO vehicle_images (vehicle_id, url, kind, sort_order, created_at)
VALUES (1, 'https://example.com/img1.jpg', 'exterior', 1, NOW());

INSERT INTO inventory_moves (vehicle_id, from_warehouse_id, to_warehouse_id, reason, moved_at, moved_by)
VALUES (1, NULL, 1, 'RECEIVE', NOW(), 1);

INSERT INTO purchase_orders (po_no, supplier_id, status, order_date, total_amount, created_by, created_at)
VALUES ('PO-0001', 1, 'APPROVED', CURDATE(), 35000.00, 1, NOW());

INSERT INTO purchase_order_items (po_id, model_id, qty, unit_price, line_total)
VALUES (1, 1, 1, 35000.00, 35000.00);

INSERT INTO goods_receipts (gr_no, po_id, receipt_date, warehouse_id, created_by, created_at)
VALUES ('GR-0001', 1, CURDATE(), 1, 1, NOW());

INSERT INTO goods_receipt_items (gr_id, vehicle_id, landed_cost)
VALUES (1, 1, 35000.00);

INSERT INTO quotations (quote_no, customer_id, quote_date, status, subtotal, discount, tax, grand_total, created_by)
VALUES ('Q-0001', 1, CURDATE(), 'SENT', 35000.00, 0.00, 0.00, 35000.00, 1);

INSERT INTO quotation_items (quote_id, model_id, qty, unit_price, line_total)
VALUES (1, 1, 1, 35000.00, 35000.00);

INSERT INTO sales_orders (so_no, customer_id, order_date, status, created_by, assigned_to, subtotal, discount, tax, grand_total)
VALUES ('SO-0001', 1, CURDATE(), 'CONFIRMED', 1, 1, 35000.00, 0.00, 0.00, 35000.00);

INSERT INTO sales_order_items (so_id, vehicle_id, sell_price, discount, tax, line_total)
VALUES (1, 1, 35000.00, 0.00, 0.00, 35000.00);

INSERT INTO allotments (so_id, vehicle_id, reserved_at, status)
VALUES (1, 1, NOW(), 'RESERVED');

INSERT INTO service_orders (svc_no, vehicle_id, scheduled_date, status, notes, created_by, created_at)
VALUES ('SVC-0001', 1, CURDATE(), 'PLANNED', 'Pre-delivery inspection', 1, NOW());

INSERT INTO vehicle_registrations (vehicle_id, reg_no, reg_date, owner_name, address, fields)
VALUES (1, 'REG-0001', CURDATE(), 'John Doe', '123 Main St', '{"note":"first registration"}');

INSERT INTO documents (doc_no, doc_type, doc_date, customer_id, related_id, related_table, storage_url, created_by, created_at)
VALUES ('DOC-0001', 'SO', CURDATE(), 1, 1, 'sales_orders', 'https://example.com/so-0001.pdf', 1, NOW());

INSERT INTO document_links (doc_id, entity_table, entity_id)
VALUES (1, 'sales_orders', 1);

INSERT INTO invoices (invoice_no, so_id, customer_id, invoice_date, status, subtotal, discount, tax, grand_total, created_by, created_at)
VALUES ('INV-0001', 1, 1, CURDATE(), 'ISSUED', 35000.00, 0.00, 0.00, 35000.00, 1, NOW());

INSERT INTO invoice_items (invoice_id, vehicle_id, unit_price, discount, tax, line_total)
VALUES (1, 1, 35000.00, 0.00, 0.00, 35000.00);

INSERT INTO payments (receipt_no, customer_id, payment_date, method, amount, notes)
VALUES ('RCPT-0001', 1, CURDATE(), 'BANK', 35000.00, 'Full payment');

INSERT INTO payment_allocations (payment_id, invoice_id, amount_applied)
VALUES (1, 1, 35000.00);

INSERT INTO waitlists (name) VALUES ('EV Priority Queue');

INSERT INTO waitlist_entries (waitlist_id, customer_id, model_id, preferred_color, requested_date, status)
VALUES (1, 1, 1, 'White', CURDATE(), 'WAITING');

INSERT INTO audit_logs (entity, entity_id, action, changes, actor_user_id, created_at)
VALUES ('vehicles', 1, 'CREATE', '{"vin":"VIN000000000001"}', 1, NOW());

-- Done.
