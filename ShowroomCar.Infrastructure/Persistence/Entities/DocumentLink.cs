using System;
using System.Collections.Generic;

namespace ShowroomCar.Infrastructure.Persistence.Entities;

public partial class DocumentLink
{
    public long LinkId { get; set; }

    public long DocId { get; set; }

    public string EntityTable { get; set; } = null!;

    public long EntityId { get; set; }

    public virtual Document Doc { get; set; } = null!;
}
