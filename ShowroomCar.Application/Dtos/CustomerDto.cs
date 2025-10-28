namespace ShowroomCar.Application.Dtos
{
    public class CustomerDto
    {
        public long CustomerId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
    }
}
