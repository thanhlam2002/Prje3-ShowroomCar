using System;

namespace ShowroomCar.Application.Dtos
{
    public class VehicleRequestCreateDto
    {
        public long? VehicleId { get; set; }
        public long? CustomerId { get; set; }   // nếu khách đã có account
        public string FullName { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string Content { get; set; }
        public long? RequestId { get; set; }
        public string PreferredColor { get; set; }
        // WEB | SHOWROOM | CALL
        public string Source { get; set; }
    }

    public class VehicleRequestListDto
    {
        public long RequestId { get; set; }
        public string FullName { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string Status { get; set; }
        public string PreferredColor { get; set; }
        public string ModelName { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class VehicleRequestDetailDto
    {
        public long RequestId { get; set; }
        public long? CustomerId { get; set; }
        public string FullName { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string Content { get; set; }
        public string VehicleStatus { get; set; }
        public string VehicleColor { get; set; }
        public string VehicleVin { get; set; }
        public string VehicleModelName { get; set; }
        public int ModelId { get; set; }
        public string ModelName { get; set; }
        public string PreferredColor { get; set; }
        public long? VehicleId { get; set; }

        public string Source { get; set; }
        public string Status { get; set; }

        public long? PoId { get; set; }
        public long? SoId { get; set; }

        public DateTime CreatedAt { get; set; }
        public long? ProcessedBy { get; set; }
        public DateTime? ProcessedAt { get; set; }
    }
}
