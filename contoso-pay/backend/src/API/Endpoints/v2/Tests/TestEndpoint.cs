namespace API.Endpoints.v2.Tests;

internal sealed class TestEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder v2 = app.MapVersionedGroup(2);

        v2.MapGet("/test", () => Results.Ok(new { message = "OK", version = "v2", timestamp = DateTime.UtcNow }))
            .MapToApiVersion(2, 0)
            .WithTags(Tags.Connect)
            .Produces(StatusCodes.Status200OK);
    }
}
