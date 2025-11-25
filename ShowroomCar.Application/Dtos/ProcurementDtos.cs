using System.ComponentModel.DataAnnotations;

namespace ShowroomCar.Application.Dtos
{
    // ---- PO ----
    public class PoItemCreate
    {
        [Required] public int ModelId { get; set; }
        [Range(1, int.MaxValue)] public int Qty { get; set; }
        [Range(0, double.MaxValue)] public decimal UnitPrice { get; set; }
    }

    public class PurchaseOrderCreateRequest
    {
        [Required] public int SupplierId { get; set; }
        public DateOnly OrderDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
        public List<PoItemCreate> Items { get; set; } = new();
    }

    public class PurchaseOrderDto
    {
        public long PoId { get; set; }
        public string PoNo { get; set; } = null!;
        public int SupplierId { get; set; }
        public string SupplierName { get; set; } = null!;
        public DateOnly OrderDate { get; set; }
        public string Status { get; set; } = null!;
        public decimal TotalAmount { get; set; }
        public List<PurchaseOrderItemDto> Items { get; set; } = new();
    }

    public class PurchaseOrderItemDto
    {
        public long PoItemId { get; set; }
        public long PoId { get; set; }
        public int ModelId { get; set; }
        public int Qty { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal? LineTotal { get; set; }
    }

    // ---- GR ----
    public class GrVehicleCreate
    {
        [Required] public int ModelId { get; set; }
        [Required] public string Vin { get; set; } = null!;
        [Required] public string EngineNo { get; set; } = null!;
        public string? Color { get; set; }
        public int? Year { get; set; }
        public decimal LandedCost { get; set; } = 0;
    }

    public class GoodsReceiptCreateRequest
    {
        public long? PoId { get; set; }
        [Required] public int WarehouseId { get; set; }
        public DateOnly ReceiptDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
        public List<GrVehicleCreate> Vehicles { get; set; } = new();
    }

    public class GoodsReceiptDto
    {
        public long GrId { get; set; }
        public string GrNo { get; set; } = null!;
        public long? PoId { get; set; }
        public DateOnly ReceiptDate { get; set; }
        public int WarehouseId { get; set; }
        public List<GoodsReceiptItemDto> Items { get; set; } = new();
    }

    public class GoodsReceiptItemDto
    {
        public long GrItemId { get; set; }
        public long GrId { get; set; }
        public long VehicleId { get; set; }
        public string Vin { get; set; } = null!;
        public string EngineNo { get; set; } = null!;
        public decimal LandedCost { get; set; }
    }

    public class PurchaseOrderUpdateSupplierRequest
    {
        public int SupplierId { get; set; }
    }
}
