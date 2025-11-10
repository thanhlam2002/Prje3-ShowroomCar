namespace ShowroomCar.Application.Dtos;

public record class BrandDto(int Id, string Name, string? Code);
public record class BrandCreateDto(string Name, string? Code);
public record class BrandUpdateDto(string Name, string? Code);
