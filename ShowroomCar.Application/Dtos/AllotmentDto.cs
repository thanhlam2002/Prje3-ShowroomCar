namespace ShowroomCar.Application.Dtos
{
    public class AllotmentReserveRequest
    {
        public long SoId { get; set; }        // sales_orders.so_id
        public long VehicleId { get; set; }   // vehicles.vehicle_id
    }

    public class AllotmentDto
    {
        public long AllotId { get; set; }     // allotments.allot_id
        public long SoId { get; set; }
        public long VehicleId { get; set; }
        public DateTime? ReservedAt { get; set; }
        public string Status { get; set; } = null!;
    }
}
