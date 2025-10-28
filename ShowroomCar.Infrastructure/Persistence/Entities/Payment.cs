using System;
using System.Collections.Generic;

namespace ShowroomCar.Infrastructure.Persistence.Entities;

public partial class Payment
{
    public long PaymentId { get; set; }

    public string ReceiptNo { get; set; } = null!;

    public long CustomerId { get; set; }

    public DateOnly PaymentDate { get; set; }

    public string Method { get; set; } = null!;

    public decimal Amount { get; set; }

    public string? Notes { get; set; }

    public virtual Customer Customer { get; set; } = null!;

    public virtual ICollection<PaymentAllocation> PaymentAllocations { get; set; } = new List<PaymentAllocation>();
}
