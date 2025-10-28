namespace ShowroomCar.Application.Dtos
{
    public class InvoiceItemDto
    {
        public long InvItemId { get; set; }
        public long VehicleId { get; set; }
        public string? VehicleVin { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Discount { get; set; }
        public decimal Tax { get; set; }
        public decimal LineTotal { get; set; }
    }
}
