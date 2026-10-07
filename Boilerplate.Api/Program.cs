using Boilerplate.Api.Infrastructure.DependencyInjection;
using Boilerplate.Api.Web.DependencyInjection;
using Boilerplate.Api.Web.Routing;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApiInfrastructure(
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is required."));
builder.Services.AddApiWeb();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapApiDefinition();
    app.UseSwaggerUi(options =>
    {
        options.Path = "/swagger";
        options.DocExpansion = "list";
        options.SwaggerRoutes.Add(new NSwag.AspNetCore.SwaggerUiRoute("v1", "/openapi/v1.yaml"));
    });
}

app.MapApiEndpoints();

app.Run();
