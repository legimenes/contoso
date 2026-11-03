using API.Endpoints;
using Asp.Versioning.Builder;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Reflection;

namespace API.Extensions;

internal static class EndpointExtensions
{
    internal static IServiceCollection AddEndpoints(this IServiceCollection services, Assembly assembly)
    {
        ServiceDescriptor[] serviceDescriptors = [.. assembly
            .DefinedTypes
            .Where(type => type is { IsAbstract: false, IsInterface: false } &&
                           type.IsAssignableTo(typeof(IEndpoint)))
            .Select(type => ServiceDescriptor.Transient(typeof(IEndpoint), type))];

        services.TryAddEnumerable(serviceDescriptors);
        return services;
    }

    internal static IApplicationBuilder MapEndpoints(this WebApplication app)
    {
        RouteGroupBuilder versionedGroup = RouteGroup.MapVersionedGroup(app);
        IVersionedEndpointRouteBuilder unversionedGroup = RouteGroup.MapUnversionedGroup(app);

        IEnumerable<IEndpoint> endpoints = app.Services.GetRequiredService<IEnumerable<IEndpoint>>();
        foreach (IEndpoint endpoint in endpoints)
        {
            if (endpoint is IUnversionedEndpoint)
            {
                endpoint.MapEndpoint(unversionedGroup);
                continue;
            }

            endpoint.MapEndpoint(versionedGroup);
        }
        return app;
    }
}