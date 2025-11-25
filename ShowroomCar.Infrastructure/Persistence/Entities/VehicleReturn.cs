using System;
namespace ShowroomCar.Infrastructure.Persistence.Entities;

public class VehicleReturn
{
    public long ReturnId { get; set; }
    public long VehicleId { get; set; }
    public long? PoId { get; set; }
    public long? GrId { get; set; }
    public string Reason { get; set; }
    public string Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public long? CreatedBy { get; set; }

    // Navigation
    public Vehicle Vehicle { get; set; }
    public PurchaseOrder PurchaseOrder { get; set; }
    public GoodsReceipt GoodsReceipt { get; set; }
    public User CreatedUser { get; set; }
}
