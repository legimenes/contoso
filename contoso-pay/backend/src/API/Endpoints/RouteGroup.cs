using Asp.Versioning;
using Asp.Versioning.Builder;

namespace API.Endpoints;

internal static class RouteGroup
{
    internal const string Prefix = "/v{version:apiVersion}";

    internal static readonly ApiVersion[] ApiVersions =
    [
        new(1, 0),
        new(2, 0)
    ];

    internal static RouteGroupBuilder MapVersionedGroup(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.NewVersionedApi().MapGroup(Prefix);

        foreach (ApiVersion version in ApiVersions)
        {
            group.HasApiVersion(version);
        }

        return group;
    }

    internal static IVersionedEndpointRouteBuilder MapUnversionedGroup(this IEndpointRouteBuilder app)
    {
        IVersionedEndpointRouteBuilder group = app.NewVersionedApi().HasApiVersion(new ApiVersion(1, 0));
        return group;
    }
}