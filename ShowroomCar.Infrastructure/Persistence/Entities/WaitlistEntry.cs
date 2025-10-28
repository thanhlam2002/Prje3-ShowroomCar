using System;
using System.Collections.Generic;

namespace ShowroomCar.Infrastructure.Persistence.Entities;

public partial class WaitlistEntry
{
    public long EntryId { get; set; }

    public int WaitlistId { get; set; }

    public long CustomerId { get; set; }

    public int ModelId { get; set; }

    public string? PreferredColor { get; set; }

    public DateOnly? RequestedDate { get; set; }

    public string Status { get; set; } = null!;

    public virtual Customer Customer { get; set; } = null!;

    public virtual VehicleModel Model { get; set; } = null!;

    public virtual Waitlist Waitlist { get; set; } = null!;
}
