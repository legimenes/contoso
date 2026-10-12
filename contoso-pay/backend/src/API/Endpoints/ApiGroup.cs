using Asp.Versioning;

namespace API.Endpoints;

internal static class ApiGroup
{
    internal const string Prefix = "/v{version:apiVersion}";

    internal static RouteGroupBuilder MapVersionedGroup(this IEndpointRouteBuilder app, int major, int minor = 0)
    {
        ApiVersion version = new(major, minor);
        return app.NewVersionedApi().MapGroup(Prefix).HasApiVersion(version);
    }
}
