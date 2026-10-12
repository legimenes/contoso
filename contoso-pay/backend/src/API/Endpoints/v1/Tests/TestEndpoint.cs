namespace API.Endpoints.v1.Tests;

internal sealed class TestEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder v1 = app.MapVersionedGroup(1);

        v1.MapGet("/test", () => Results.Ok(new { message = "OK", version = "v1" }))
            .MapToApiVersion(1, 0)
            .WithTags(Tags.Connect)
            .Produces(StatusCodes.Status200OK);
    }
}
