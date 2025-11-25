-- Active: 1727270994892@@127.0.0.1@3306@showroom
CREATE TABLE IF NOT EXISTS vehicle_requests (
    request_id BIGINT AUTO_INCREMENT PRIMARY KEY,
    customer_id BIGINT NULL,
    full_name VARCHAR(200) NOT NULL,
    phone VARCHAR(30) NOT NULL,
    email VARCHAR(255),
    content TEXT,

    model_id INT NOT NULL,
    preferred_color VARCHAR(50),
    vehicle_id BIGINT NULL,

    source VARCHAR(20) NOT NULL, -- WEB|SHOWROOM|CALL
    status VARCHAR(20) NOT NULL, -- NEW|IN_PROGRESS|WAIT_STOCK|PO_CREATED|WAITING|CONVERTED|CANCELED

    po_id BIGINT NULL,
    so_id BIGINT NULL,

    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    processed_by BIGINT NULL,
    processed_at TIMESTAMP NULL
);

CREATE TABLE IF NOT EXISTS vehicle_returns (
    return_id BIGINT AUTO_INCREMENT PRIMARY KEY,
    vehicle_id BIGINT NOT NULL,
    po_id BIGINT NULL,
    gr_id BIGINT NULL,
    reason TEXT,
    status VARCHAR(20) NOT NULL, -- PENDING|SENT|CONFIRMED
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    created_by BIGINT NULL
);

ALTER TABLE vehicles 
    ADD COLUMN reserved_for_customer_id BIGINT NULL;

ALTER TABLE vehicles 
    ADD COLUMN reserved_request_id BIGINT NULL;

ALTER TABLE vehicle_requests
    ADD CONSTRAINT fk_vr_customer
        FOREIGN KEY (customer_id) REFERENCES customers(customer_id);

ALTER TABLE vehicle_requests
    ADD CONSTRAINT fk_vr_model
        FOREIGN KEY (model_id) REFERENCES vehicle_models(model_id);

ALTER TABLE vehicle_requests
    ADD CONSTRAINT fk_vr_vehicle
        FOREIGN KEY (vehicle_id) REFERENCES vehicles(vehicle_id);

ALTER TABLE vehicle_requests
    ADD CONSTRAINT fk_vr_po
        FOREIGN KEY (po_id) REFERENCES purchase_orders(po_id);

ALTER TABLE vehicle_requests
    ADD CONSTRAINT fk_vr_so
        FOREIGN KEY (so_id) REFERENCES sales_orders(so_id);

ALTER TABLE vehicle_requests
    ADD CONSTRAINT fk_vr_processed_by
        FOREIGN KEY (processed_by) REFERENCES users(user_id);

ALTER TABLE vehicles
    ADD CONSTRAINT fk_vehicle_reserved_customer
        FOREIGN KEY (reserved_for_customer_id) REFERENCES customers(customer_id);

ALTER TABLE vehicles
    ADD CONSTRAINT fk_vehicle_reserved_request
        FOREIGN KEY (reserved_request_id) REFERENCES vehicle_requests(request_id);

DESCRIBE purchase_orders;

ALTER TABLE purchase_orders 
    ADD COLUMN customer_id BIGINT NULL;

ALTER TABLE purchase_orders 
    ADD COLUMN request_id BIGINT NULL;

ALTER TABLE purchase_orders
    ADD CONSTRAINT fk_po_customer
        FOREIGN KEY (customer_id) REFERENCES customers(customer_id);

ALTER TABLE purchase_orders
    ADD CONSTRAINT fk_po_request
        FOREIGN KEY (request_id) REFERENCES vehicle_requests(request_id);

ALTER TABLE vehicle_returns
    ADD CONSTRAINT fk_vret_vehicle
        FOREIGN KEY (vehicle_id) REFERENCES vehicles(vehicle_id);

ALTER TABLE vehicle_returns
    ADD CONSTRAINT fk_vret_po
        FOREIGN KEY (po_id) REFERENCES purchase_orders(po_id);

ALTER TABLE vehicle_returns
    ADD CONSTRAINT fk_vret_gr
        FOREIGN KEY (gr_id) REFERENCES goods_receipts(gr_id);

ALTER TABLE vehicle_returns
    ADD CONSTRAINT fk_vret_created_by
        FOREIGN KEY (created_by) REFERENCES users(user_id);

CREATE INDEX ix_vr_status ON vehicle_requests(status);
CREATE INDEX ix_vr_model_color ON vehicle_requests(model_id, preferred_color);

CREATE INDEX ix_vehicle_reserved_customer
    ON vehicles(reserved_for_customer_id, status);

CREATE INDEX ix_po_request_id
    ON purchase_orders(request_id);
ALTER TABLE sales_orders
ADD COLUMN contract_confirmed_at DATETIME NULL;

INSERT INTO suppliers (code, name, phone, email, address)
VALUES ('UNKNOWN', 'Unknown Supplier', '', '', '');

INSERT INTO vehicle_models 
(name, model_no, brand_id, base_price, fuel_type, transmission, seat_no, active)
VALUES 
('Toyota Camry', 'CAMRY-2024', 1, 500000000, 'petrol', 'AT', 5, true);

INSERT INTO vehicles
(model_id, vin, engine_no, color, year, status, current_warehouse_id, acquired_at, updated_at)
VALUES
(1, 'VIN-TEST-001', 'ENG-TEST-001', 'Red', 2024, 'IN_STOCK', 1, NOW(), NOW());

INSERT INTO vehicles
(model_id, vin, engine_no, color, year, status, current_warehouse_id, acquired_at, updated_at)
VALUES
(1, 'VIN-SOLD-001', 'ENG-SOLD-001', 'Red', 2024, 'SOLD', 1, NOW(), NOW());

INSERT INTO vehicles
(model_id, vin, engine_no, color, year, status, current_warehouse_id, acquired_at, updated_at)
VALUES
(1, 'VIN-RES-001', 'ENG-RES-001', 'Red', 2024, 'RESERVED', 1, NOW(), NOW());

INSERT INTO vehicles
(model_id, vin, engine_no, color, year, status, current_warehouse_id, acquired_at, updated_at)
VALUES
(1, 'VIN-TRANSIT-001', 'ENG-TRANSIT-001', 'Red', 2024, 'IN_TRANSIT', 1, NOW(), NOW());
