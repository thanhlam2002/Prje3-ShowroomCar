using System;
using System.Collections.Generic;

namespace ShowroomCar.Infrastructure.Persistence.Entities;

public partial class Document
{
    public long DocId { get; set; }

    public string DocNo { get; set; } = null!;

    public string DocType { get; set; } = null!;

    public DateOnly DocDate { get; set; }

    public long? CustomerId { get; set; }

    public long? RelatedId { get; set; }

    public string? RelatedTable { get; set; }

    public string StorageUrl { get; set; } = null!;

    public long? CreatedBy { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual User? CreatedByNavigation { get; set; }

    public virtual Customer? Customer { get; set; }

    public virtual ICollection<DocumentLink> DocumentLinks { get; set; } = new List<DocumentLink>();
}
