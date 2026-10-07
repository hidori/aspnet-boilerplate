using Microsoft.EntityFrameworkCore;

namespace Boilerplate.Api.Infrastructure.Data;

public sealed class ApiDbContext : DbContext
{
    public ApiDbContext(DbContextOptions<ApiDbContext> options)
        : base(options)
    {
    }
}
