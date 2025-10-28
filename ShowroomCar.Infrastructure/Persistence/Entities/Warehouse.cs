using System;
using System.Collections.Generic;

namespace ShowroomCar.Infrastructure.Persistence.Entities;

public partial class Warehouse
{
    public int WarehouseId { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Address { get; set; }

    public virtual ICollection<GoodsReceipt> GoodsReceipts { get; set; } = new List<GoodsReceipt>();

    public virtual ICollection<InventoryMove> InventoryMoveFromWarehouses { get; set; } = new List<InventoryMove>();

    public virtual ICollection<InventoryMove> InventoryMoveToWarehouses { get; set; } = new List<InventoryMove>();

    public virtual ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
}
