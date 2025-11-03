using Mapster;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;

namespace ShowroomCar.Application
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection RegisterMappings(this IServiceCollection services)
        {
            MappingConfig.Register();                    // khởi tạo rule Mapster
            services.AddSingleton(TypeAdapterConfig.GlobalSettings);
            services.AddScoped<IMapper, ServiceMapper>();
            return services;
        }
    }
}
