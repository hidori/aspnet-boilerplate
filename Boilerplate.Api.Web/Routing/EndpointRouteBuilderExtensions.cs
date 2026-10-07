using Boilerplate.Api.Web.Routing.Health;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Boilerplate.Api.Web.Routing;

public static class EndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapApiEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGroup("/health").MapHealthEndpoints();

        return endpoints;
    }

    public static IEndpointRouteBuilder MapApiDefinition(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/openapi/v1.yaml", () =>
            Results.File(Path.Combine(AppContext.BaseDirectory, "openapi.yaml"), "application/yaml"))
            .ExcludeFromDescription();

        return endpoints;
    }
}
