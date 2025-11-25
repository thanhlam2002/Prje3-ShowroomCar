namespace ShowroomCar.Application.Dtos
{
    public class PurchaseOrderReceiveRequest
    {
        /// <summary>
        /// Danh sách xe nhận thực tế
        /// </summary>
        public List<ReceivedVehicleDto> Vehicles { get; set; } = new();

        /// <summary>
        /// Kho nhập xe
        /// </summary>
        public int WarehouseId { get; set; }

        /// <summary>
        /// Năm sản xuất (optional)
        /// </summary>
        public int? Year { get; set; }

        /// <summary>
        /// Màu mặc định nếu xe không gửi màu riêng
        /// </summary>
        public string? DefaultColor { get; set; }
    }

    public class ReceivedVehicleDto
    {
        public int ModelId { get; set; }
        public string Vin { get; set; }
        public string EngineNo { get; set; }
        public string Color { get; set; }
        public int Year { get; set; }
    }
}
