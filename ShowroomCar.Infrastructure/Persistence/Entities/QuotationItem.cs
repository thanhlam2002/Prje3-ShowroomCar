using System;
using System.Collections.Generic;

namespace ShowroomCar.Infrastructure.Persistence.Entities;

public partial class QuotationItem
{
    public long QuoteItemId { get; set; }

    public long QuoteId { get; set; }

    public int ModelId { get; set; }

    public int Qty { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal? LineTotal { get; set; }

    public virtual VehicleModel Model { get; set; } = null!;

    public virtual Quotation Quote { get; set; } = null!;
}
