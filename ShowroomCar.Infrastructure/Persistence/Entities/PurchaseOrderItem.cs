using System;
using System.Collections.Generic;

namespace ShowroomCar.Infrastructure.Persistence.Entities;

public partial class PurchaseOrderItem
{
    public long PoItemId { get; set; }

    public long PoId { get; set; }

    public int ModelId { get; set; }

    public int Qty { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal? LineTotal { get; set; }

    public virtual VehicleModel Model { get; set; } = null!;

    public virtual PurchaseOrder Po { get; set; } = null!;
}
