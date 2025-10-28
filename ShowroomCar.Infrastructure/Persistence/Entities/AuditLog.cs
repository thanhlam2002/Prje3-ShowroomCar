using System;
using System.Collections.Generic;

namespace ShowroomCar.Infrastructure.Persistence.Entities;

public partial class AuditLog
{
    public long Id { get; set; }

    public string Entity { get; set; } = null!;

    public long EntityId { get; set; }

    public string Action { get; set; } = null!;

    public string? Changes { get; set; }

    public long? ActorUserId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual User? ActorUser { get; set; }
}
