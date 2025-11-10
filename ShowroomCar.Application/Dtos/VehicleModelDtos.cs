namespace ShowroomCar.Application.Dtos;

public record class VehicleModelDto(int Id, int BrandId, string Name);
public record class VehicleModelCreateDto(int BrandId, string Name);
public record class VehicleModelUpdateDto(int BrandId, string Name);
