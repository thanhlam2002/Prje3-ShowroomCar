namespace ShowroomCar.Application.Dtos
{
    public class SalesOrderCreateRequest
    {
        public long CustomerId { get; set; }

        // Nếu tạo SO từ VehicleRequest thì FE truyền RequestId;
        // nếu khách mua trực tiếp thì để null.
        public long? RequestId { get; set; }
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
