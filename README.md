# ASP.NET Core: Boilerplate

Boilerplate project for building ASP.NET Core applications on .NET 10.

The API uses **specification-first models with Minimal API execution**.
[openapi.yaml](./Boilerplate.Api.Web/openapi.yaml) is the authored API contract.
NSwag produces models; route registration, shared controller
instances, and `TypedResults` handlers remain handwritten.
The API design policies below do not change the administration application's MVC
configuration.

## Running the API

Run locally with the .NET 10 SDK after configuring
`ConnectionStrings__DefaultConnection` in the process environment:

```bash
dotnet run --project Boilerplate.Api
```

Alternatively, start the API and its database dependency using
[Docker Compose](./compose.yaml). Set `MSSQL_SA_PASSWORD` in `.env` first:

```bash
docker compose up -d api
```

| Endpoint | URL | Availability |
| --- | --- | --- |
| Health check | http://localhost:8080/health | All environments |
| Swagger UI | http://localhost:8080/swagger | Development only |
| OpenAPI YAML | http://localhost:8080/openapi/v1.yaml | Development only |

The health check returns `{"status":"ok"}` for API process readiness; it does not
check database connectivity. Swagger UI displays the authored OpenAPI YAML,
not a contract inferred from C# types.

### API database dependency

The API host registers
[ApiDbContext](./Boilerplate.Api.Infrastructure/Data/ApiDbContext.cs) through
[AddApiInfrastructure](./Boilerplate.Api.Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs),
using EF Core's SQL Server provider and the `DefaultConnection` connection
string. Compose supplies this as `ConnectionStrings__DefaultConnection`;
local runs must supply it through environment variables or another ASP.NET Core
configuration provider. Missing or blank connection strings fail startup
explicitly.

The context is scoped: one instance per request/service scope, not a singleton.
Do not constructor-inject it into the existing singleton controllers. Register
database-dependent application services with scoped lifetimes and resolve them
through Minimal API handler parameters.

There are no entities or migrations yet. Registration does not open a database
connection, create a database, or apply migrations. The health endpoint still
checks the API process only.

### Generating API models

After changing the OpenAPI specification, run explicitly:

```bash
make generate
```

The command restores the repository-local NSwag tool and generates all models
into [Models/Models.Generated.cs](./Boilerplate.Api.Web/Models/Models.Generated.cs),
using the namespace `Boilerplate.Api.Web.Models`. Commit that file and the
specification together. Normal builds, tests, and application startup do not run
generation or require the tool. Handwritten partial classes and extension
methods use ordinary `.cs` filenames and are not overwritten. Regenerating the
single output file also removes models for deleted schemas.

Models follow NSwag's standard single-file output. Request, response, and shared
models coexist in that file; there are no feature or `Common/` directories and
no separate model project.

NSwag's `openapi2csclient` command is used with client classes, client interfaces,
and exception classes disabled: only models and their standard serialization
support are generated. It does not generate MVC controllers, routes, startup,
or project files. Models use System.Text.Json, nullable reference annotations,
and standard data annotations checked by .NET's `AddValidation()` for request
models. For the health schema, `required` and `minLength: 1` are enforced by
`RequiredAttribute`. No custom templates or type resolvers are used.

Model generation does not connect operations to handlers: routes must still
be updated manually to match the specification. Monetary values should be
specified as decimal strings; generated DTOs keep them as strings, with domain
conversion performed explicitly at the application boundary.

Custom JSON converters control runtime serialization and deserialization; they
do not define bidirectional generator type mappings. NSwag's standard C# type
settings apply to matching schema types/formats throughout nested models.
Mapping schemas to arbitrary existing CLR types would require a custom type
resolver and is not implemented here.

NSwag.ConsoleCore is version-pinned in the standard
[local tool manifest](./.config/dotnet-tools.json). `dotnet tool restore` installs that
version; no global tool installation, Java, or generator container image is
required. The manifest uses `.config/` for Dependabot's local-tool discovery;
Dependabot checks all repository NuGet dependencies, including local tools,
and the base images in [.devcontainer/Dockerfile](./.devcontainer/Dockerfile)
weekly. Rebuild the Dev Container to remove the previous OpenAPI Generator/Java
image configuration.

### Dev Container and host permissions

Host and container Git operations use the same filesystem UID/GID. VS Code runs
as the non-root `app` user, and container startup aligns that user with the bind
mounted workspace owner. The API and Admin processes also run as `app`.

Startup uses root only to set up the user and repair ownership of dedicated NuGet
and build-output volumes, then drops privileges before running the service.
The `dev` service also enables `REPAIR_GIT_OWNERSHIP` to repair existing `.git`
ownership before dropping privileges. This repairs metadata left by older
root-based containers, including object directories that can otherwise cause
intermittent commit failures. This repair is not enabled in `api` / `admin`,
where the workspace is read-only.
Source is writable in `dev` and read-only in `api` / `admin`; build outputs remain
in each service's separate `.artifacts` volume.

The `dev` image includes GNU Make for `make build`, `make test`, and `make clean`,
`less`, `nano`, `vim`, the OpenSSH client for SSH Git remotes, and passwordless
`sudo` for `app`.
Additional development tools can be installed from the VS Code terminal:

```bash
sudo apt-get update
sudo apt-get install <package>
```

VS Code forwards the host SSH agent. Do not mount the host `~/.ssh` directory
or copy private keys into the container. Load the required key into the host
agent before connecting.

When the agent holds keys for multiple accounts, transfer only the desired
OpenSSH **public** key (`.pub`) to the container, then run:

```bash
sh .devcontainer/configure-git-ssh.sh /path/to/key.pub
```

[configure-git-ssh.sh](./.devcontainer/configure-git-ssh.sh) uses that public key
with `IdentitiesOnly=yes` to select the matching private key in the forwarded
agent. No account name or developer key is committed to repository settings.
The selected public key and Git configuration persist in `/home/app/.copilot`;
the Dev Container reapplies the workspace-specific Git include on attach.
This does not modify `.git/config`, host Git configuration, or other workspaces.
Without a selected public key, the script reports that selection is not configured
and leaves normal agent forwarding unchanged. Host `includeIf` paths and SSH
configuration are not automatically translated into container paths.

Verify the selected identity with
`ssh -o IdentitiesOnly=yes -i "$HOME/.copilot/git-ssh/identity.pub" -T git@github.com`
(GitHub normally returns exit code 1 even on successful authentication), then
check read access with `git ls-remote origin` as `app`. Read access alone does not
prove push permission. Do not use force push as a connectivity test.
The selection script can be run in an existing container without rebuilding.

Use `sudo` only for system administration, not Git, builds, or editing workspace
files; running those as root can recreate host/container ownership conflicts.
Passwordless `sudo` grants full root access in `dev`. The `api` and `admin`
services use the `runtime` build target, which does not add `sudo` or this grant.
Manual package installations are lost when the container is recreated; add
tools that must persist to the `dev` stage in [.devcontainer/Dockerfile](./.devcontainer/Dockerfile).
Rebuild the Dev Container to apply these image changes; `apt-get` without
`sudo` still requires root and will fail as `app`.

The `dev` service also persists VS Code Server data (`/home/app/.vscode-server/data`)
and Copilot state (`/home/app/.copilot`) in the `dev-vscode-data` and
`dev-copilot-state` named volumes. This keeps remote chat history, agent sessions,
workspace state, and extension data across container rebuilds. Startup repairs
ownership of these volumes before VS Code connects. The API and Admin services
do not mount or repair this state.

### Preserving existing chat sessions on the first rebuild

Adding volumes does not migrate files from an existing container: the new mounts
hide the old container directories. Before the first rebuild with this configuration,
finish active agent work and run the following in the **existing Dev Container**:

```bash
umask 077
tar -czf /workspaces/aspnet-boilerplate/.artifacts/devcontainer-ai-state-before-persistence.tar.gz \
  --exclude=.vscode-server/data/logs -C /home/app .vscode-server/data .copilot
```

The archive excludes runtime logs and is stored in the already-persistent
`dev-artifacts` volume, outside version control.
It can contain private conversations and authentication data;
do not share or commit it. If either source directory does not exist because the
corresponding tool has never been used, omit that directory from the command.
Ensure the backup command succeeds before proceeding.

Close the remote VS Code window, then run these commands from the repository
root on the **host**, before reconnecting VS Code:

```bash
docker compose up -d --build dev
docker compose exec --user app dev tar --no-same-owner \
  -xzf /workspaces/aspnet-boilerplate/.artifacts/devcontainer-ai-state-before-persistence.tar.gz \
  -C /home/app
```

Reopen the project in the Dev Container and check that the sessions are present.
After checking, remove the backup archive. Subsequent **Dev Containers: Rebuild
Container** operations preserve the state without this manual migration.
This protects state stored in the container; chat UI availability still depends
on the client and extension using the same workspace and account.

Do not run `docker compose down --volumes` / `docker compose down -v` or delete
these volumes if you want to keep the history. These commands also delete the
database and other Compose-managed volumes. Keep the same Compose project name
when reopening the workspace; a different project name selects different volumes.

### Applying container configuration changes

For manual Compose shell commands, specify the user because `exec` bypasses the
startup entrypoint:

```bash
docker compose exec --user app dev bash
```

After changing this configuration:

1. Rebuild the API and Admin services from the host:
   `docker compose up -d --build api admin`.
2. Run **Dev Containers: Rebuild Container** in VS Code.
3. Verify `id -u` / `id -g` match between the host and the Dev Container.

Until the updated image has been rebuilt and the Dev Container recreated, repair
existing root-owned Git metadata once on the host. Run from the repository root:

```bash
sudo chown -R "$(id -u):$(id -g)" .git
```

Do not run normal Git commands with `sudo`. Rebuilding is necessary to stop an
existing root-based container from creating more root-owned files and to enable
the startup repair. Repair any affected source files separately on the host.

## API architecture

### Project responsibilities

| Project | Responsibility |
| --- | --- |
| [Boilerplate.Api](./Boilerplate.Api/) | Startup and composition root |
| [Boilerplate.Api.Web](./Boilerplate.Api.Web/) | HTTP controllers, request/response models, service and route registration |
| [Boilerplate.Api.Application](./Boilerplate.Api.Application/) | Use cases and interfaces for external dependencies |
| [Boilerplate.Domain](./Boilerplate.Domain/) | Domain models and business rules |
| [Boilerplate.Api.Infrastructure](./Boilerplate.Api.Infrastructure/) | API infrastructure implementations, including the EF Core SQL Server context and DI registration |

Controllers translate HTTP inputs and outputs and invoke application use cases.
Application and domain logic must not depend on ASP.NET Core HTTP types.

### Folder organization

Organize directories by responsibility: controllers by feature, route
registration under `Routing/`, and service registration under
`DependencyInjection/`. Classify registration entry points by their intended
scope, not just the services currently registered. `AddApiWeb` composes Web-layer
services and is not restricted to routing. Generated HTTP models remain in the
shared model file:

```text
Boilerplate.Api.Web/
  Controllers/
    Health/
      HealthController.cs
  Models/
    Models.Generated.cs
  Routing/
    EndpointRouteBuilderExtensions.cs
    Health/
      HealthRouteGroupBuilderExtensions.cs
  DependencyInjection/
    ServiceCollectionExtensions.cs
```

Use feature paths and namespaces under `Controllers/<Feature>/`. Larger domains
can add `<Domain>/<Feature>/` hierarchies for controllers. Route registration uses
the `Boilerplate.Api.Web.Routing` namespace; Web-layer service registration uses
`Boilerplate.Api.Web.DependencyInjection`. API infrastructure service
registration resides under `Boilerplate.Api.Infrastructure/DependencyInjection/`.
Generated models keep the shared `Boilerplate.Api.Web.Models` namespace.
Controller tests mirror the controller paths under
[Boilerplate.Api.Web.Tests/Controllers](./Boilerplate.Api.Web.Tests/Controllers/).

### Routing and HTTP responses

Controllers are plain classes, without `ControllerBase` or routing attributes.
Register dependencies in
[ServiceCollectionExtensions.cs](./Boilerplate.Api.Web/DependencyInjection/ServiceCollectionExtensions.cs)
and compose feature groups in
[EndpointRouteBuilderExtensions.cs](./Boilerplate.Api.Web/Routing/EndpointRouteBuilderExtensions.cs):

```csharp
endpoints.MapGroup("/health").MapHealthEndpoints();
```

Keep HTTP methods, relative paths, and endpoint metadata in the corresponding
feature's registration file, such as
[HealthRouteGroupBuilderExtensions.cs](./Boilerplate.Api.Web/Routing/Health/HealthRouteGroupBuilderExtensions.cs).
Feature-specific route registration uses `Routing/<Feature>/` directories and
matching namespaces; the top-level routing file only composes feature groups.
Resolve controllers once during startup and register their instance methods
directly, rather than wrapping them in route-handler lambdas. Set shared tags,
authorization, and filters on the route group as needed.

The empty relative path maps the group root. ASP.NET Core represents its route
pattern as `/health/`, but `GET /health` remains supported and the named route
generates `/health`.
Route groups can be nested when an API-wide prefix is needed.

Controller methods return Minimal API result types constructed with `TypedResults`:

- Single result: `Ok<Response>`.
- Multiple results: `Results<Ok<Response>, NotFound>`.
- Async operations: the corresponding `Task<T>` when needed.

Do not use MVC `ActionResult` / `IActionResult`, `AddControllers()`, or
`MapControllers()` in the API. Use ASP.NET Core's standard parameter binding,
authorization, endpoint filters, and middleware. `AddApiWeb()` enables .NET 10
Minimal API validation through `AddValidation()`. Request model DataAnnotations
are validated before the handler executes; invalid requests receive HTTP 400.
Validation metadata is generated in the assembly that calls `AddValidation()`.
If request models move to another assembly, register validation there as well.

## Instance lifetime and concurrency

These rules apply to all API features:

- Design controllers, use cases, application services, and repositories for
  concurrent use and register them as singletons by default. Do not rebuild their
  dependency graph for each request.
- Pass request data, user identity, cancellation tokens, and operation-specific
  state as arguments, or keep them in local variables, not shared instance fields.
- Shared dependencies must support concurrent use and the singleton lifetime.
  Configuration, thread-safe clients, and resource factories may be retained.
- Share database connection pools. Acquire and release connections for each
  operation; never share an active connection across concurrent requests.
- If EF Core is adopted, acquire a `DbContext` per unit of work through a factory,
  pooled when appropriate. Do not retain it in a singleton.

## Transaction ownership

**Use cases own transaction boundaries**, not controllers or repositories:

```text
Controller
  -> UseCase
       -> Begin transaction
       -> Repository operations using the same transaction
       -> Commit on success / Rollback on failure
```

- Define transaction / unit-of-work interfaces in Application and implement
  database-specific behavior in Infrastructure.
- Pass the operation's transaction explicitly to repository methods, or use
  repositories bound to that unit of work.
- Repositories must not independently commit a use-case transaction.
- Release transaction resources when the operation finishes. Never store the
  current transaction in a shared controller, use case, or repository field.

Application use cases and API database access are not implemented yet. These are
the design rules for future implementations, not existing database functionality.

## API verification

```bash
dotnet test Boilerplate.Api.Web.Tests/Boilerplate.Api.Web.Tests.csproj
dotnet build Boilerplate.Api/Boilerplate.Api.csproj
```

The current tests cover direct controller-method registration, route and response
metadata, the health JSON response, and shared controller identity across scopes.
For manual HTTP requests, use
[Boilerplate.Api.http](./Boilerplate.Api/Boilerplate.Api.http).
