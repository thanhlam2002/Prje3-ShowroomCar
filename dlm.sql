-- 0) Đảm bảo role ADMIN tồn tại
INSERT INTO roles (code, name)
VALUES ('ADMIN', 'Administrator')
ON DUPLICATE KEY UPDATE code = code;

-- 1) Tạo/cập nhật user superadmin (mật khẩu: Admin@123)
-- Hash BCrypt (cost=12): $2b$12$uBFEZByMgftsQjVy6Y0a9.jhwZD4tMg2VGDd5UjAhZfhAIz1.Kb52
INSERT INTO users (username, email, password_hash, active, created_at, updated_at)
VALUES ('superadmin', 'superadmin@example.com', '$2b$12$uBFEZByMgftsQjVy6Y0a9.jhwZD4tMg2VGDd5UjAhZfhAIz1.Kb52', 1, NOW(), NOW())
ON DUPLICATE KEY UPDATE
  email         = VALUES(email),
  password_hash = VALUES(password_hash),
  active        = 1,
  updated_at    = NOW();

-- 2) Gán quyền ADMIN cho superadmin (idempotent)
INSERT INTO user_roles (user_id, role_id)
SELECT u.user_id, r.role_id
FROM users AS u
JOIN roles AS r ON r.code = 'ADMIN'
WHERE u.username = 'superadmin'
ON DUPLICATE KEY UPDATE user_id = user_roles.user_id;


-- 3) Kiểm tra nhanh
SELECT username, email, active, LENGTH(password_hash) AS hash_len
FROM users WHERE username = 'superadmin';

SELECT invoice_no, status, grand_total FROM invoices;
SELECT * FROM payment_allocations;
SELECT * FROM payments;

-- Bảng phiếu trả hàng
CREATE TABLE goods_return (
    grt_id BIGINT AUTO_INCREMENT PRIMARY KEY,
    grt_no VARCHAR(50) NOT NULL,
    po_id BIGINT NOT NULL,
    supplier_id BIGINT NOT NULL,
    return_date DATE NOT NULL,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Bảng chi tiết phiếu trả hàng
CREATE TABLE IF NOT EXISTS goods_return_item (
    grt_item_id BIGINT AUTO_INCREMENT PRIMARY KEY,
    grt_id BIGINT NOT NULL,
    vehicle_id BIGINT NOT NULL,
    reason VARCHAR(255) NULL,
    CONSTRAINT fk_goods_return FOREIGN KEY (grt_id) REFERENCES goods_return(grt_id)
        ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT fk_goods_return_vehicle FOREIGN KEY (vehicle_id) REFERENCES vehicles(vehicle_id)
        ON DELETE RESTRICT ON UPDATE CASCADE
);

