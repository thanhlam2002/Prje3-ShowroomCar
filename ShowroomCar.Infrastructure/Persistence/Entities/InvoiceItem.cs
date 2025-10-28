using System;
using System.Collections.Generic;

namespace ShowroomCar.Infrastructure.Persistence.Entities;

public partial class InvoiceItem
{
    public long InvItemId { get; set; }

    public long InvoiceId { get; set; }

    public long VehicleId { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal? Discount { get; set; }

    public decimal? Tax { get; set; }

    public decimal? LineTotal { get; set; }

    public virtual Invoice Invoice { get; set; } = null!;

    public virtual Vehicle Vehicle { get; set; } = null!;
}
