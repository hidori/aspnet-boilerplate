using System.Data;
using Boilerplate.Api.Infrastructure.Data;
using Boilerplate.Api.Infrastructure.DependencyInjection;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Boilerplate.Api.Infrastructure.Tests.DependencyInjection;

public class ServiceCollectionExtensionsTests
{
    private const string ConnectionString =
        "Server=localhost;Database=Boilerplate;Integrated Security=True;Encrypt=True";

    [Fact]
    public void RegistersScopedSqlServerContextWithoutOpeningConnection()
    {
        var services = new ServiceCollection();

        Assert.Same(services, services.AddApiInfrastructure(ConnectionString));
        Assert.Equal(ServiceLifetime.Scoped,
            Assert.Single(services, service => service.ServiceType == typeof(ApiDbContext)).Lifetime);

        using var provider = services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApiDbContext>();

        Assert.Equal("Microsoft.EntityFrameworkCore.SqlServer", context.Database.ProviderName);
        var expected = new SqlConnectionStringBuilder(ConnectionString);
        var actual = new SqlConnectionStringBuilder(context.Database.GetConnectionString());
        Assert.Equal(expected.DataSource, actual.DataSource);
        Assert.Equal(expected.InitialCatalog, actual.InitialCatalog);
        Assert.Equal(expected.IntegratedSecurity, actual.IntegratedSecurity);
        Assert.Equal(expected.Encrypt, actual.Encrypt);
        Assert.Equal(ConnectionState.Closed, context.Database.GetDbConnection().State);
        Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<ApiDbContext>());
    }

    [Fact]
    public void SharesContextWithinScopeButNotAcrossScopes()
    {
        var services = new ServiceCollection();
        services.AddApiInfrastructure(ConnectionString);
        using var provider = services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
        using var firstScope = provider.CreateScope();
        using var secondScope = provider.CreateScope();

        var firstContext = firstScope.ServiceProvider.GetRequiredService<ApiDbContext>();

        Assert.Same(firstContext, firstScope.ServiceProvider.GetRequiredService<ApiDbContext>());
        Assert.NotSame(firstContext, secondScope.ServiceProvider.GetRequiredService<ApiDbContext>());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void RejectsMissingOrBlankConnectionString(string? connectionString)
    {
        var services = new ServiceCollection();

        var exception = Assert.ThrowsAny<ArgumentException>(
            () => services.AddApiInfrastructure(connectionString!));

        Assert.Equal("connectionString", exception.ParamName);
    }
}
