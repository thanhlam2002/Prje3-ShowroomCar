namespace ShowroomCar.Application.Dtos
{
    public class SalesOrderDto
    {
        public long SoId { get; set; }
        public string SoNo { get; set; } = string.Empty;
        public long CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public DateOnly OrderDate { get; set; }
        public string Status { get; set; } = "DRAFT";
        public decimal Subtotal { get; set; }
        public decimal Discount { get; set; }
        public decimal Tax { get; set; }
        public decimal GrandTotal { get; set; }

        public List<SalesOrderItemDto> Items { get; set; } = new();
    }

    public class SalesOrderContractDto
    {
        public long SoId { get; set; }
        public string SoNo { get; set; } = string.Empty;

        public long CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;

        // Xe
        public string VehicleVin { get; set; } = string.Empty;
        public string VehicleModel { get; set; } = string.Empty;
        public string VehicleColor { get; set; } = string.Empty;

        // Giá
        public decimal Price { get; set; }
        public decimal Discount { get; set; }
        public decimal Tax { get; set; }
        public decimal GrandTotal { get; set; }

        // Điều khoản hợp đồng
        public string Terms { get; set; } = string.Empty;

        // Thời điểm xác nhận hợp đồng
        public DateTime? ContractConfirmedAt { get; set; }
    }
}
