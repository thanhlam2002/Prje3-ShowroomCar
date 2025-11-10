using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class GoodsReturnItem
{
    [Key]
    public long GrtItemId { get; set; }

    [ForeignKey(nameof(GoodsReturn))]
    public long GrtId { get; set; }

    public long VehicleId { get; set; }
    public string Reason { get; set; } = string.Empty;

    public virtual GoodsReturn GoodsReturn { get; set; } = null!;
}
