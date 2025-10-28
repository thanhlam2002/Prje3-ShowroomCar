using System;
using System.Collections.Generic;

namespace ShowroomCar.Infrastructure.Persistence.Entities;

public partial class SalesOrderItem
{
    public long SoItemId { get; set; }

    public long SoId { get; set; }

    public long VehicleId { get; set; }

    public decimal SellPrice { get; set; }

    public decimal? Discount { get; set; }

    public decimal? Tax { get; set; }

    public decimal? LineTotal { get; set; }

    public virtual SalesOrder So { get; set; } = null!;

    public virtual Vehicle Vehicle { get; set; } = null!;
}
