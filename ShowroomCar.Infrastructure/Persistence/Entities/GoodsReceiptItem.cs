using System;
using System.Collections.Generic;

namespace ShowroomCar.Infrastructure.Persistence.Entities;

public partial class GoodsReceiptItem
{
    public long GrItemId { get; set; }

    public long GrId { get; set; }

    public long VehicleId { get; set; }

    public decimal? LandedCost { get; set; }

    public virtual GoodsReceipt Gr { get; set; } = null!;

    public virtual Vehicle Vehicle { get; set; } = null!;
}
