using System;
using System.Collections.Generic;

namespace ShowroomCar.Infrastructure.Persistence.Entities;

public partial class Vehicle
{
    public long VehicleId { get; set; }

    public int ModelId { get; set; }

    public string Vin { get; set; } = null!;

    public string EngineNo { get; set; } = null!;

    public string? Color { get; set; }

    public int? Year { get; set; }

    public string Status { get; set; } = null!;

    public int? CurrentWarehouseId { get; set; }

    public DateTime? AcquiredAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Allotment? Allotment { get; set; }

    public virtual Warehouse? CurrentWarehouse { get; set; }

    public virtual GoodsReceiptItem? GoodsReceiptItem { get; set; }

    public virtual ICollection<InventoryMove> InventoryMoves { get; set; } = new List<InventoryMove>();

    public virtual ICollection<InvoiceItem> InvoiceItems { get; set; } = new List<InvoiceItem>();

    public virtual VehicleModel Model { get; set; } = null!;

    public virtual SalesOrderItem? SalesOrderItem { get; set; }

    public virtual ICollection<ServiceOrder> ServiceOrders { get; set; } = new List<ServiceOrder>();

    public virtual ICollection<VehicleImage> VehicleImages { get; set; } = new List<VehicleImage>();

    public virtual VehicleRegistration? VehicleRegistration { get; set; }
}
