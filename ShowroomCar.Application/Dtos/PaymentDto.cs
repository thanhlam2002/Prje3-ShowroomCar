namespace ShowroomCar.Application.Dtos
{
    public class PaymentDto
    {
        public long PaymentId { get; set; }
        public string ReceiptNo { get; set; } = string.Empty;
        public long CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public DateOnly PaymentDate { get; set; }
        public string Method { get; set; } = "CASH";
        public decimal Amount { get; set; }
        public string? Notes { get; set; }

        public List<PaymentAllocationDto> Allocations { get; set; } = new();
    }
}
