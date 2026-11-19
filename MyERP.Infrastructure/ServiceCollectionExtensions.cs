using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MyERP.Application;

namespace MyERP.Infrastructure
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddErpInfrastructure(this IServiceCollection services, string connectionString)
        {
            services.AddDbContext<ErpDbContext>(options => options.UseSqlServer(connectionString));
            services.AddScoped<IErpDbContext>(provider => provider.GetRequiredService<ErpDbContext>());
            return services;
        }
    }
}
