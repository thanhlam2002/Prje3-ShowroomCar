using System;

namespace ShowroomCar.Application.Dtos
{
    public class ServiceOrderCreateRequest
    {
        public long VehicleId { get; set; }
        public DateOnly? ScheduledDate { get; set; }
        public string? Notes { get; set; }
    }

    public class ServiceOrderUpdateRequest
    {
        public DateOnly? ScheduledDate { get; set; }
        public string? Notes { get; set; }
    }

    public class ServiceOrderDto
    {
        public long SvcId { get; set; }
        public string SvcNo { get; set; } = null!;
        public long VehicleId { get; set; }
        public string VehicleVin { get; set; } = "";
        public DateOnly? ScheduledDate { get; set; }
        public string Status { get; set; } = null!;
        public string? Notes { get; set; }
    }
}
