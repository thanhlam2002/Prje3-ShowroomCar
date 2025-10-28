namespace ShowroomCar.Application.Dtos
{
    public class PaymentAllocationDto
    {
        public long AllocId { get; set; }
        public long InvoiceId { get; set; }
        public string? InvoiceNo { get; set; }
        public decimal AmountApplied { get; set; }
    }
}
