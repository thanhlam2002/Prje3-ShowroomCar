-- Migration script để thêm các cột mới vào bảng service_orders
-- Chạy script này trực tiếp trong MySQL/MariaDB

USE showroom;

-- Thêm cột po_id (nullable vì có thể không có PO)
ALTER TABLE `service_orders`
ADD COLUMN `po_id` BIGINT(20) NULL AFTER `vehicle_id`;

-- Thêm cột gr_id (nullable vì có thể không có GR)
ALTER TABLE `service_orders`
ADD COLUMN `gr_id` BIGINT(20) NULL AFTER `po_id`;

-- Thêm cột model_id (required, foreign key đến vehicle_models)
-- Tạm thời nullable để có thể cập nhật dữ liệu sau
ALTER TABLE `service_orders`
ADD COLUMN `model_id` INT(11) NULL AFTER `gr_id`;

-- Thêm cột quantity_expected
ALTER TABLE `service_orders`
ADD COLUMN `quantity_expected` INT(11) NOT NULL DEFAULT 1 AFTER `model_id`;

-- Thêm cột updated_at
ALTER TABLE `service_orders`
ADD COLUMN `updated_at` DATETIME NULL AFTER `created_at`;

-- Thêm index cho po_id
ALTER TABLE `service_orders`
ADD INDEX `fk_svc_po` (`po_id`);

-- Thêm index cho gr_id
ALTER TABLE `service_orders`
ADD INDEX `fk_svc_gr` (`gr_id`);

-- Thêm index cho model_id (nếu chưa có)
ALTER TABLE `service_orders`
ADD INDEX `fk_svc_model` (`model_id`);

-- Thêm foreign key constraint cho model_id
ALTER TABLE `service_orders`
ADD CONSTRAINT `fk_svc_model` 
FOREIGN KEY (`model_id`) 
REFERENCES `vehicle_models` (`model_id`) 
ON DELETE RESTRICT 
ON UPDATE CASCADE;

-- ⚠️ QUAN TRỌNG: Cập nhật dữ liệu hiện có TRƯỚC KHI thêm foreign key constraints

-- 1. Cập nhật model_id từ vehicle (nếu có dữ liệu cũ)
UPDATE `service_orders` so
INNER JOIN `vehicles` v ON so.vehicle_id = v.vehicle_id
SET so.model_id = v.model_id
WHERE so.model_id IS NULL;

-- 2. Đặt model_id là NOT NULL sau khi đã cập nhật dữ liệu
ALTER TABLE `service_orders`
MODIFY COLUMN `model_id` INT(11) NOT NULL;

-- 3. Cập nhật po_id thành NULL nếu giá trị không tồn tại trong purchase_orders
UPDATE `service_orders` so
LEFT JOIN `purchase_orders` po ON so.po_id = po.po_id
SET so.po_id = NULL
WHERE so.po_id IS NOT NULL AND po.po_id IS NULL;

-- 4. Cập nhật gr_id thành NULL nếu giá trị không tồn tại trong goods_receipts
UPDATE `service_orders` so
LEFT JOIN `goods_receipts` gr ON so.gr_id = gr.gr_id
SET so.gr_id = NULL
WHERE so.gr_id IS NOT NULL AND gr.gr_id IS NULL;

-- 5. Bây giờ mới thêm foreign key constraints (sau khi đã clean data)

-- Thêm foreign key constraint cho po_id (optional - có thể bỏ qua nếu có lỗi)
-- Nếu có lỗi ở đây, có nghĩa là vẫn còn dữ liệu vi phạm, bỏ qua constraint này
ALTER TABLE `service_orders`
ADD CONSTRAINT `fk_svc_po` 
FOREIGN KEY (`po_id`) 
REFERENCES `purchase_orders` (`po_id`) 
ON DELETE SET NULL 
ON UPDATE CASCADE;

-- Thêm foreign key constraint cho gr_id (optional - có thể bỏ qua nếu có lỗi)
ALTER TABLE `service_orders`
ADD CONSTRAINT `fk_svc_gr` 
FOREIGN KEY (`gr_id`) 
REFERENCES `goods_receipts` (`gr_id`) 
ON DELETE SET NULL 
ON UPDATE CASCADE;

-- Xóa các cột mặc định nếu không cần
-- ALTER TABLE `service_orders` ALTER COLUMN `po_id` DROP DEFAULT;
-- ALTER TABLE `service_orders` ALTER COLUMN `gr_id` DROP DEFAULT;
-- ALTER TABLE `service_orders` ALTER COLUMN `model_id` DROP DEFAULT;

