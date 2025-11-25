using System;
using System.Collections.Generic;

namespace ShowroomCar.Infrastructure.Persistence.Entities;

public partial class PurchaseOrder
{
    public long PoId { get; set; }

    public string PoNo { get; set; } = null!;

    public int SupplierId { get; set; }

    public string Status { get; set; } = null!;

    public DateOnly OrderDate { get; set; }

    public decimal TotalAmount { get; set; }

    public long? CreatedBy { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual User? CreatedByNavigation { get; set; }

    public virtual ICollection<GoodsReceipt> GoodsReceipts { get; set; } = new List<GoodsReceipt>();

    public virtual ICollection<PurchaseOrderItem> PurchaseOrderItems { get; set; } = new List<PurchaseOrderItem>();

    public virtual Supplier Supplier { get; set; } = null!;

    public long? CustomerId { get; set; }

    public long? RequestId { get; set; }


    public Customer Customer { get; set; }

    public VehicleRequest VehicleRequest { get; set; }

    public ICollection<VehicleReturn> VehicleReturns { get; set; }

}
