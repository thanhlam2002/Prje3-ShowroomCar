using System;
using System.Collections.Generic;

namespace ShowroomCar.Infrastructure.Persistence.Entities;

public partial class PaymentAllocation
{
    public long AllocId { get; set; }

    public long PaymentId { get; set; }

    public long InvoiceId { get; set; }

    public decimal AmountApplied { get; set; }

    public virtual Invoice Invoice { get; set; } = null!;

    public virtual Payment Payment { get; set; } = null!;
}
