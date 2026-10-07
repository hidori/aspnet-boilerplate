using System.Reflection;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json;
using Boilerplate.Api.Web.Controllers.Health;
using Boilerplate.Api.Web.DependencyInjection;
using Boilerplate.Api.Web.Routing;
using Boilerplate.Api.Web.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Boilerplate.Api.Web.Tests.Controllers.Health;

public class HealthControllerTests
{
    [Fact]
    public async Task MapApiEndpointsDefinesHealthContract()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddApiWeb();
        await using var app = builder.Build();
        var endpoints = (IEndpointRouteBuilder)app;

        Assert.Empty(endpoints.DataSources.SelectMany(source => source.Endpoints));
        Assert.Same(endpoints, endpoints.MapApiEndpoints());

        var endpoint = Assert.IsType<RouteEndpoint>(
            Assert.Single(endpoints.DataSources.SelectMany(source => source.Endpoints)));
        var handler = endpoint.Metadata.GetMetadata<MethodInfo>();
        Assert.NotNull(handler);
        Assert.Equal(typeof(HealthController), handler.DeclaringType);
        Assert.Equal(nameof(HealthController.GetStatus), handler.Name);
        Assert.Equal(typeof(Ok<HealthResponse>), handler.ReturnType);
        Assert.Equal("/health/", endpoint.RoutePattern.RawText);
        Assert.Equal("GetHealth", endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName);
        Assert.Equal(["GET"], endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()?.HttpMethods);
        Assert.Equal("Check API readiness",
            endpoint.Metadata.GetMetadata<IEndpointSummaryMetadata>()?.Summary);
        Assert.Equal("Checks the API process only, not SQL Server connectivity.",
            endpoint.Metadata.GetMetadata<IEndpointDescriptionMetadata>()?.Description);
        Assert.Contains(endpoint.Metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>(),
            response => response.StatusCode == StatusCodes.Status200OK && response.Type == typeof(HealthResponse));
        Assert.Equal(["Health"], endpoint.Metadata.GetMetadata<ITagsMetadata>()?.Tags);
    }

    [Theory]
    [InlineData("")]
    [InlineData("/api/v1")]
    public async Task HealthEndpointReturnsOkJson(string prefix)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddApiWeb();
        await using var app = builder.Build();
        app.MapGroup(prefix).MapApiEndpoints();
        await app.StartAsync();
        var links = app.Services.GetRequiredService<LinkGenerator>();
        Assert.Equal($"{prefix}/health", links.GetPathByName("GetHealth", values: null));
        var addresses = app.Services.GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>();
        Assert.NotNull(addresses);
        using var client = new HttpClient { BaseAddress = new Uri(Assert.Single(addresses.Addresses)) };
        using var response = await client.GetAsync($"{prefix}/health");

        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);
        Assert.Equal("application/json; charset=utf-8", response.Content.Headers.ContentType?.ToString());
        await using var body = await response.Content.ReadAsStreamAsync();
        using var json = await JsonDocument.ParseAsync(body);
        var property = Assert.Single(json.RootElement.EnumerateObject());
        Assert.Equal("status", property.Name);
        Assert.Equal("ok", property.Value.GetString());
    }

    [Fact]
    public void HealthControllerIsSharedAcrossScopes()
    {
        var services = new ServiceCollection();
        services.AddApiWeb();
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        using var firstScope = provider.CreateScope();
        using var secondScope = provider.CreateScope();

        var controller = provider.GetRequiredService<HealthController>();
        Assert.Same(controller, firstScope.ServiceProvider.GetRequiredService<HealthController>());
        Assert.Same(controller, secondScope.ServiceProvider.GetRequiredService<HealthController>());
        Assert.Equal("ok", controller.GetStatus().Value?.Status);
    }

    [Fact]
    public async Task ApiDefinitionServesTheAuthoredSpecification()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddApiWeb();
        await using var app = builder.Build();
        app.MapApiDefinition();
        await app.StartAsync();
        var addresses = app.Services.GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>();
        Assert.NotNull(addresses);
        using var client = new HttpClient { BaseAddress = new Uri(Assert.Single(addresses.Addresses)) };
        using var response = await client.GetAsync("/openapi/v1.yaml");

        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);
        Assert.Equal("application/yaml", response.Content.Headers.ContentType?.ToString());
        Assert.Equal(await File.ReadAllBytesAsync(Path.Combine(AppContext.BaseDirectory, "openapi.yaml")),
            await response.Content.ReadAsByteArrayAsync());
    }

    [Fact]
    public void GeneratedModelUsesSharedNamespaceJsonNameAndRequiredValidation()
    {
        Assert.Equal("Boilerplate.Api.Web.Models", typeof(HealthResponse).Namespace);
        var property = typeof(HealthResponse).GetProperty(nameof(HealthResponse.Status));
        Assert.NotNull(property);
        Assert.Equal("status",
            property.GetCustomAttribute<System.Text.Json.Serialization.JsonPropertyNameAttribute>()?.Name);
        var required = property.GetCustomAttribute<RequiredAttribute>();
        Assert.NotNull(required);
        Assert.False(required.AllowEmptyStrings);
    }

    [Theory]
    [InlineData("""{"status":"ok"}""", StatusCodes.Status200OK)]
    [InlineData("""{"status":""}""", StatusCodes.Status400BadRequest)]
    [InlineData("""{"status":null}""", StatusCodes.Status400BadRequest)]
    [InlineData("{}", StatusCodes.Status400BadRequest)]
    public async Task StandardValidationChecksGeneratedModelBeforeHandler(string request, int expectedStatus)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddApiWeb();
        builder.Services.AddValidation();
        await using var app = builder.Build();
        var calls = 0;
        app.MapPost("/validate-model", (HealthResponse body) =>
        {
            calls++;
            return TypedResults.Ok(body);
        });
        await app.StartAsync();
        var addresses = app.Services.GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>();
        Assert.NotNull(addresses);
        using var client = new HttpClient { BaseAddress = new Uri(Assert.Single(addresses.Addresses)) };
        using var content = new StringContent(request, Encoding.UTF8, "application/json");
        using var response = await client.PostAsync("/validate-model", content);

        Assert.Equal(expectedStatus, (int)response.StatusCode);
        Assert.Equal(expectedStatus == StatusCodes.Status200OK ? 1 : 0, calls);
    }
}
