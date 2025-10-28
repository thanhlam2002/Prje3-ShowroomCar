using System;
using System.Collections.Generic;

namespace ShowroomCar.Infrastructure.Persistence.Entities;

public partial class ServiceOrder
{
    public long SvcId { get; set; }

    public string SvcNo { get; set; } = null!;

    public long VehicleId { get; set; }

    public DateOnly? ScheduledDate { get; set; }

    public string Status { get; set; } = null!;

    public string? Notes { get; set; }

    public long? CreatedBy { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual User? CreatedByNavigation { get; set; }

    public virtual Vehicle Vehicle { get; set; } = null!;
}
