namespace ShowroomCar.Application.Dtos
{
    public class VehicleDto
    {
        public long VehicleId { get; set; }
        public string Vin { get; set; } = string.Empty;
        public string EngineNo { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public int Year { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? ModelName { get; set; }
    }
}
