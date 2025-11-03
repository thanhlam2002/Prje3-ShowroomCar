namespace ShowroomCar.Application.Dtos
{
    // Tạo phiếu thu
    public class PaymentCreateRequest
    {
        public long     CustomerId   { get; set; }
        public DateOnly PaymentDate  { get; set; }  // MySQL DATE
        public string   Method       { get; set; } = null!;
        public decimal  Amount       { get; set; }  // tổng tiền thu
        public string?  Notes        { get; set; }
    }

    // Một dòng phân bổ vào hóa đơn
    public class PaymentAllocationLine
    {
        public long    InvoiceId { get; set; }
        public decimal Amount    { get; set; }
    }

    // Yêu cầu phân bổ: nhiều dòng
    public class PaymentAllocateRequest
    {
        public List<PaymentAllocationLine> Allocations { get; set; } = new();
    }

    // Tóm tắt nhanh phiếu thu sau khi tạo/phân bổ
    public class PaymentSummaryDto
    {
        public long     PaymentId   { get; set; }
        public string   ReceiptNo   { get; set; } = null!;
        public long     CustomerId  { get; set; }
        public DateOnly PaymentDate { get; set; }
        public string   Method      { get; set; } = null!;
        public decimal  Amount      { get; set; }
        public decimal  Allocated   { get; set; }
        public decimal  Remaining   { get; set; }
    }

    // Tồn nợ của hóa đơn
    public class InvoiceBalanceDto
    {
        public long    InvoiceId { get; set; }
        public string  InvoiceNo { get; set; } = null!;
        public decimal GrandTotal { get; set; }
        public decimal Allocated  { get; set; }
        public decimal AmountDue  { get; set; }
        public string  Status     { get; set; } = null!;
    }
}
