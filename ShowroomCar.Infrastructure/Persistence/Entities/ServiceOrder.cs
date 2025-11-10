using System;
using System.Collections.Generic;

namespace ShowroomCar.Infrastructure.Persistence.Entities;

public partial class ServiceOrder
{
    public long SvcId { get; set; }

    public string SvcNo { get; set; } = null!;

    public long VehicleId { get; set; }

    public DateOnly? ScheduledDate { get; set; }

    public string? Notes { get; set; }

    public long? CreatedBy { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual User? CreatedByNavigation { get; set; }

    public virtual Vehicle Vehicle { get; set; } = null!;

    public long? PoId { get; set; }       // phiếu PO gốc (nullable vì có thể không có PO)
    public long? GrId { get; set; }       // phiếu nhập lô (nullable vì có thể không có GR)
    public int ModelId { get; set; }    // model kiểm định (phải là int để khớp với VehicleModel.ModelId)
    public int QuantityExpected { get; set; }
    public string Status { get; set; } = "PLANNED";  // PLANNED / IN_PROGRESS / APPROVED / REJECTED
    public DateTime? UpdatedAt { get; set; }
    public virtual VehicleModel Model { get; set; } = null!;

}
