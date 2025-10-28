using System;
using System.Collections.Generic;

namespace ShowroomCar.Infrastructure.Persistence.Entities;

public partial class Allotment
{
    public long AllotId { get; set; }

    public long SoId { get; set; }

    public long VehicleId { get; set; }

    public DateTime? ReservedAt { get; set; }

    public string Status { get; set; } = null!;

    public virtual SalesOrder So { get; set; } = null!;

    public virtual Vehicle Vehicle { get; set; } = null!;
}
