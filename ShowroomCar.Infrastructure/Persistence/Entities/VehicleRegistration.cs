using System;
using System.Collections.Generic;

namespace ShowroomCar.Infrastructure.Persistence.Entities;

public partial class VehicleRegistration
{
    public long RegId { get; set; }

    public long VehicleId { get; set; }

    public string? RegNo { get; set; }

    public DateOnly? RegDate { get; set; }

    public string? OwnerName { get; set; }

    public string? Address { get; set; }

    public string? Fields { get; set; }

    public virtual Vehicle Vehicle { get; set; } = null!;
}
