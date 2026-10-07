using Boilerplate.Admin.Web.Controllers;
using Microsoft.Extensions.DependencyInjection;

namespace Boilerplate.Admin.Web;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAdminWeb(this IServiceCollection services)
    {
        services.AddControllersWithViews()
            .AddApplicationPart(typeof(HomeController).Assembly);

        return services;
    }
}
