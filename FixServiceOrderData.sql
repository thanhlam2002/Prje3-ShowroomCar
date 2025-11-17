-- Script cleanup dữ liệu service_orders trước khi thêm foreign key constraints
-- Chạy script này NẾU bạn đã chạy AddServiceOrderColumns.sql một phần và gặp lỗi

USE showroom;

-- 1. Cập nhật model_id từ vehicle (nếu có dữ liệu cũ)
UPDATE `service_orders` so
INNER JOIN `vehicles` v ON so.vehicle_id = v.vehicle_id
SET so.model_id = v.model_id
WHERE so.model_id IS NULL OR so.model_id = 0;

-- 2. Cập nhật po_id thành NULL nếu giá trị không tồn tại trong purchase_orders
UPDATE `service_orders` so
LEFT JOIN `purchase_orders` po ON so.po_id = po.po_id
SET so.po_id = NULL
WHERE (so.po_id IS NOT NULL AND so.po_id != 0 AND po.po_id IS NULL)
   OR so.po_id = 0;

-- 3. Cập nhật gr_id thành NULL nếu giá trị không tồn tại trong goods_receipts
UPDATE `service_orders` so
LEFT JOIN `goods_receipts` gr ON so.gr_id = gr.gr_id
SET so.gr_id = NULL
WHERE (so.gr_id IS NOT NULL AND so.gr_id != 0 AND gr.gr_id IS NULL)
   OR so.gr_id = 0;

-- 4. Đảm bảo model_id không NULL (nếu có dữ liệu)
-- Nếu vẫn còn NULL, cần xử lý thủ công
UPDATE `service_orders` so
INNER JOIN `vehicles` v ON so.vehicle_id = v.vehicle_id
SET so.model_id = v.model_id
WHERE so.model_id IS NULL;

-- 5. Sau khi cleanup, thử thêm foreign key constraints lại
-- (Chạy phần này từ AddServiceOrderColumns.sql)


