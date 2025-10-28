using System;
using System.Collections.Generic;

namespace ShowroomCar.Infrastructure.Persistence.Entities;

public partial class VehicleImage
{
    public long ImageId { get; set; }

    public long VehicleId { get; set; }

    public string Url { get; set; } = null!;

    public string? Kind { get; set; }

    public int? SortOrder { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Vehicle Vehicle { get; set; } = null!;
}
