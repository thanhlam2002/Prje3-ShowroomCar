using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ShowroomCar.Infrastructure.Persistence.Entities;

public partial class GoodsReceipt
{
    public long GrId { get; set; }

    public string GrNo { get; set; } = null!;

    public long? PoId { get; set; }

    public DateOnly ReceiptDate { get; set; }

    public int WarehouseId { get; set; }

    public long? CreatedBy { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual User? CreatedByNavigation { get; set; }

    public virtual ICollection<GoodsReceiptItem> GoodsReceiptItems { get; set; } = new List<GoodsReceiptItem>();

    public virtual PurchaseOrder? Po { get; set; }

    public virtual Warehouse Warehouse { get; set; } = null!;
}
