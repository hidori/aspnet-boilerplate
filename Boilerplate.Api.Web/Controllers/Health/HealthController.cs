using Boilerplate.Api.Web.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Boilerplate.Api.Web.Controllers.Health;

public sealed class HealthController
{
    public Ok<HealthResponse> GetStatus()
    {
        return TypedResults.Ok(new HealthResponse { Status = "ok" });
    }
}
