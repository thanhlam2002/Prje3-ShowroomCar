using System;
using System.Collections.Generic;

namespace ShowroomCar.Infrastructure.Persistence.Entities;

public partial class Supplier
{
    public int SupplierId { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string? Address { get; set; }

    public virtual ICollection<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();

    public ICollection<SupplierModel> SupplierModels { get; set; } = new List<SupplierModel>();

}
