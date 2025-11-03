using System.ComponentModel.DataAnnotations;

namespace ShowroomCar.Application.Dtos
{
    public class QuotationItemUpsert
    {
        [Required] public int ModelId { get; set; }
        [Range(1,int.MaxValue)] public int Qty { get; set; } = 1;
        [Range(0,double.MaxValue)] public decimal UnitPrice { get; set; }
    }

    public class QuotationCreateRequest
    {
        [Required] public long CustomerId { get; set; }
        public decimal Discount { get; set; } = 0;
        public decimal Tax      { get; set; } = 0;
        public List<QuotationItemUpsert> Items { get; set; } = new();
    }

    public class QuotationDto
    {
        public long     QuoteId    { get; set; }
        public string   QuoteNo    { get; set; } = null!;
        public long     CustomerId { get; set; }
        public DateOnly QuoteDate  { get; set; }     // <— DateOnly
        public string   Status     { get; set; } = null!;
        public decimal  Subtotal   { get; set; }
        public decimal  Discount   { get; set; }
        public decimal  Tax        { get; set; }
        public decimal  GrandTotal { get; set; }
        public List<QuotationItemDto> Items { get; set; } = new();
    }

    public class QuotationItemDto
    {
        public long QuoteItemId { get; set; }
        public long QuoteId     { get; set; }
        public int  ModelId     { get; set; }
        public int  Qty         { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }
    }

    public class QuotationConfirmOptions
    {
        public bool AutoAllocateVehicles { get; set; } = true;
    }
}
