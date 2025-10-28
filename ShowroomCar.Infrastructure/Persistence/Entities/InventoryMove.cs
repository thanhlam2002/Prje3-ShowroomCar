using System;
using System.Collections.Generic;

namespace ShowroomCar.Infrastructure.Persistence.Entities;

public partial class InventoryMove
{
    public long MoveId { get; set; }

    public long VehicleId { get; set; }

    public int? FromWarehouseId { get; set; }

    public int? ToWarehouseId { get; set; }

    public string Reason { get; set; } = null!;

    public DateTime? MovedAt { get; set; }

    public long? MovedBy { get; set; }

    public virtual Warehouse? FromWarehouse { get; set; }

    public virtual User? MovedByNavigation { get; set; }

    public virtual Warehouse? ToWarehouse { get; set; }

    public virtual Vehicle Vehicle { get; set; } = null!;
}
