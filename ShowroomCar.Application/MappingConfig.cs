using Mapster;
using Microsoft.Extensions.DependencyInjection;  // <— Thêm dòng này
using ShowroomCar.Infrastructure.Persistence.Entities; // Giữ nguyên
using ShowroomCar.Application.Dtos;

namespace ShowroomCar.Application
{
    public static class MappingConfig
    {
        public static void RegisterMappings(this IServiceCollection services)
        {
            TypeAdapterConfig<Vehicle, VehicleDto>.NewConfig()
                .Map(dest => dest.ModelName, src => src.Model.Name)
                .IgnoreNullValues(true);

            TypeAdapterConfig<Customer, CustomerDto>.NewConfig()
                .IgnoreNullValues(true);

            TypeAdapterConfig<SalesOrder, SalesOrderDto>.NewConfig()
                .Map(dest => dest.CustomerName, src => src.Customer.FullName)
                .Map(dest => dest.Items, src => src.SalesOrderItems.Adapt<List<SalesOrderItemDto>>())
                .IgnoreNullValues(true);

            TypeAdapterConfig<SalesOrderItem, SalesOrderItemDto>.NewConfig()
                .Map(dest => dest.VehicleVin, src => src.Vehicle.Vin)
                .IgnoreNullValues(true);

            TypeAdapterConfig<Invoice, InvoiceDto>.NewConfig()
                .Map(dest => dest.CustomerName, src => src.Customer.FullName)
                .Map(dest => dest.Items, src => src.InvoiceItems.Adapt<List<InvoiceItemDto>>())
                .IgnoreNullValues(true);

            TypeAdapterConfig<InvoiceItem, InvoiceItemDto>.NewConfig()
                .Map(dest => dest.VehicleVin, src => src.Vehicle.Vin)
                .IgnoreNullValues(true);

            TypeAdapterConfig<Payment, PaymentDto>.NewConfig()
                .Map(dest => dest.CustomerName, src => src.Customer.FullName)
                .Map(dest => dest.Allocations, src => src.PaymentAllocations.Adapt<List<PaymentAllocationDto>>())
                .IgnoreNullValues(true);

            TypeAdapterConfig<PaymentAllocation, PaymentAllocationDto>.NewConfig()
                .Map(dest => dest.InvoiceNo, src => src.Invoice.InvoiceNo)
                .IgnoreNullValues(true);

            services.AddMapster();
        }
    }
}
