using System;
using System.Collections.Generic;
namespace ShowroomCar.Infrastructure.Persistence.Entities;

public class VehicleRequest
{
    public long RequestId { get; set; }
    public long? CustomerId { get; set; }
    public string FullName { get; set; }
    public string Phone { get; set; }
    public string Email { get; set; }
    public string Content { get; set; }
    public int ModelId { get; set; }
    public string PreferredColor { get; set; }
    public long? VehicleId { get; set; }
    public string Source { get; set; }
    public string Status { get; set; }
    public long? PoId { get; set; }
    public long? SoId { get; set; }
    public DateTime? PickupAppointment { get; set; }
    public DateTime CreatedAt { get; set; }
    public long? ProcessedBy { get; set; }
    public DateTime? ProcessedAt { get; set; }
    // Navigation
    public Customer Customer { get; set; }
    public VehicleModel Model { get; set; }
    public Vehicle Vehicle { get; set; }
    public PurchaseOrder PurchaseOrder { get; set; }
    public SalesOrder SalesOrder { get; set; }
    public User ProcessedUser { get; set; }

    public ICollection<Vehicle> ReservedVehicles { get; set; }
}
