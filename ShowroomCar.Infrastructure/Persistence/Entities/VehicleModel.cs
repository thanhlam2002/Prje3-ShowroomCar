using System;
using System.Collections.Generic;

namespace ShowroomCar.Infrastructure.Persistence.Entities;

public partial class VehicleModel
{
    public int ModelId { get; set; }

    public string ModelNo { get; set; } = null!;

    public string Name { get; set; } = null!;

    public int BrandId { get; set; }

    public decimal BasePrice { get; set; }

    public string? FuelType { get; set; }

    public string? Transmission { get; set; }

    public int? SeatNo { get; set; }

    public string? Specs { get; set; }

    public bool? Active { get; set; }

    public virtual Brand Brand { get; set; } = null!;

    public virtual ICollection<PurchaseOrderItem> PurchaseOrderItems { get; set; } = new List<PurchaseOrderItem>();

    public virtual ICollection<QuotationItem> QuotationItems { get; set; } = new List<QuotationItem>();

    public virtual ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();

    public virtual ICollection<WaitlistEntry> WaitlistEntries { get; set; } = new List<WaitlistEntry>();

    public ICollection<SupplierModel> SupplierModels { get; set; } = new List<SupplierModel>();


}
