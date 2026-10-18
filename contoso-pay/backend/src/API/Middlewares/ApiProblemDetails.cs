using Domain.Core.Results;
using Microsoft.AspNetCore.Mvc;

namespace API.Middlewares;

public sealed class ApiProblemDetails : ProblemDetails
{
    public IReadOnlyCollection<Error>? Errors { get; init; }

    public string? TraceId { get; init; }

    public string? RequestId { get; init; }
}