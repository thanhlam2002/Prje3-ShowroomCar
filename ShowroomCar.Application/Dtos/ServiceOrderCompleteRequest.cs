namespace ShowroomCar.Application.Dtos
{
    public class ServiceOrderCompleteRequest
    {
        public List<long> PassedVehicles { get; set; } = new();
        public List<long> FailedVehicles { get; set; } = new();
    }
}
