namespace ShowroomCar.Application.Dtos
{
    public class SalesOrderItemDto
    {
        public long SoItemId { get; set; }
        public long VehicleId { get; set; }
        public decimal SellPrice { get; set; }
        public decimal Discount { get; set; }
        public decimal Tax { get; set; }
        public decimal LineTotal { get; set; }
        public string? VehicleVin { get; set; }
    }
}
