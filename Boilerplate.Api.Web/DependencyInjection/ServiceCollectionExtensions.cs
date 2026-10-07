using Boilerplate.Api.Web.Controllers.Health;
using Microsoft.Extensions.DependencyInjection;

namespace Boilerplate.Api.Web.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApiWeb(this IServiceCollection services)
    {
        services.AddSingleton<HealthController>();
        services.AddValidation();

        return services;
    }
}
