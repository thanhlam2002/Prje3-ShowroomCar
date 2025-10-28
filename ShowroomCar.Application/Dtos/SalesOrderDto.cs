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
}
