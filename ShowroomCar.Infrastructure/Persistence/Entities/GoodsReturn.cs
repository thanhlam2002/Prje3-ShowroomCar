using System;
using System.ComponentModel.DataAnnotations; // thêm dòng này
using System.Collections.Generic;

public class GoodsReturn
{
    [Key]   
    public long GrtId { get; set; }
    public string GrtNo { get; set; } = string.Empty;
    public long PoId { get; set; }
    public long SupplierId { get; set; }
    public DateOnly ReturnDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual ICollection<GoodsReturnItem> GoodsReturnItems { get; set; } = new List<GoodsReturnItem>();
}
