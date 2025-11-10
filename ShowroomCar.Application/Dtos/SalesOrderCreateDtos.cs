namespace ShowroomCar.Application.Dtos
{
    public class SalesOrderCreateRequest
    {
        public long CustomerId { get; set; }
        public List<SalesOrderItemCreateRequest> Items { get; set; } = new();
    }

    public class SalesOrderItemCreateRequest
    {
        public long VehicleId { get; set; }
        public decimal SellPrice { get; set; }
        public decimal Discount { get; set; }
        public decimal Tax { get; set; }
    }
}
