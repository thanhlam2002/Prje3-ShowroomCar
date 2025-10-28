namespace ShowroomCar.Application.Dtos
{
    public class InvoiceDto
    {
        public long InvoiceId { get; set; }
        public string InvoiceNo { get; set; } = string.Empty;
        public long CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public long? SoId { get; set; }
        public DateOnly InvoiceDate { get; set; }
        public string Status { get; set; } = "DRAFT";
        public decimal Subtotal { get; set; }
        public decimal Discount { get; set; }
        public decimal Tax { get; set; }
        public decimal GrandTotal { get; set; }

        public List<InvoiceItemDto> Items { get; set; } = new();
    }
}
