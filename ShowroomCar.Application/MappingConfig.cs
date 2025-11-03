using System.Collections.Generic;
using Mapster;
using ShowroomCar.Infrastructure.Persistence.Entities;
using ShowroomCar.Application.Dtos;

namespace ShowroomCar.Application
{
    public static class MappingConfig
    {
        public static void Register()
        {
            var cfg = TypeAdapterConfig.GlobalSettings;
            cfg.Default.IgnoreNullValues(true);

            TypeAdapterConfig<Vehicle, VehicleDto>.NewConfig()
                .Map(d => d.ModelName, s => s.Model != null ? s.Model.Name : null);

            TypeAdapterConfig<Customer, CustomerDto>.NewConfig();

            TypeAdapterConfig<SalesOrder, SalesOrderDto>.NewConfig()
                .Map(d => d.CustomerName, s => s.Customer != null ? s.Customer.FullName : null)
                .Map(d => d.Items, s => s.SalesOrderItems != null
                                         ? s.SalesOrderItems.Adapt<List<SalesOrderItemDto>>()
                                         : new List<SalesOrderItemDto>());

            TypeAdapterConfig<SalesOrderItem, SalesOrderItemDto>.NewConfig()
                .Map(d => d.VehicleVin, s => s.Vehicle != null ? s.Vehicle.Vin : null);

            TypeAdapterConfig<Invoice, InvoiceDto>.NewConfig()
                .Map(d => d.CustomerName, s => s.Customer != null ? s.Customer.FullName : null)
                .Map(d => d.Items, s => s.InvoiceItems != null
                                         ? s.InvoiceItems.Adapt<List<InvoiceItemDto>>()
                                         : new List<InvoiceItemDto>());

            TypeAdapterConfig<InvoiceItem, InvoiceItemDto>.NewConfig()
                .Map(d => d.VehicleVin, s => s.Vehicle != null ? s.Vehicle.Vin : null);

            TypeAdapterConfig<Payment, PaymentDto>.NewConfig()
                .Map(d => d.CustomerName, s => s.Customer != null ? s.Customer.FullName : null)
                .Map(d => d.Allocations, s => s.PaymentAllocations != null
                                              ? s.PaymentAllocations.Adapt<List<PaymentAllocationDto>>()
                                              : new List<PaymentAllocationDto>());

            TypeAdapterConfig<PaymentAllocation, PaymentAllocationDto>.NewConfig()
                .Map(d => d.InvoiceNo, s => s.Invoice != null ? s.Invoice.InvoiceNo : null);
        }
    }
}
