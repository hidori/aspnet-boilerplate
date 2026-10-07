using Boilerplate.Api.Web.Controllers.Health;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Boilerplate.Api.Web.Routing.Health;

public static class HealthRouteGroupBuilderExtensions
{
    public static RouteGroupBuilder MapHealthEndpoints(this RouteGroupBuilder group)
    {
        var healthController = ((IEndpointRouteBuilder)group).ServiceProvider
            .GetRequiredService<HealthController>();

        group.WithTags("Health");
        group.MapGet("", healthController.GetStatus)
            .WithName("GetHealth")
            .WithSummary("Check API readiness")
            .WithDescription("Checks the API process only, not SQL Server connectivity.");

        return group;
    }
}
