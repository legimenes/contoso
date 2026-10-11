using Domain.Core.Results;
using Microsoft.AspNetCore.Mvc;
using System.Net;

internal abstract class EndpointBase
{
    internal static IResult CreateResult<T>(Result<T> result)
    {
        return result.Type switch
        {
            ResultType.Ok => Results.Ok(result.Value),
            ResultType.BadRequest => CreateProblemDetails(StatusCodes.Status400BadRequest, result.Errors),
            ResultType.NotFound => CreateProblemDetails(StatusCodes.Status404NotFound, result.Errors),
            ResultType.Conflict => CreateProblemDetails(StatusCodes.Status409Conflict, result.Errors),
            ResultType.UnprocessableContent =>CreateProblemDetails(StatusCodes.Status422UnprocessableEntity, result.Errors),
            ResultType.InternalError => CreateProblemDetails(StatusCodes.Status500InternalServerError, result.Errors),
            _ => CreateProblemDetails(StatusCodes.Status500InternalServerError, result.Errors)
        };
    }

    private static IResult CreateProblemDetails(
        int statusCode,
        IEnumerable<Error>? errors)
    {
        ProblemDetails problemDetails = new()
        {
            Type = GetType(statusCode),
            Status = statusCode
        };

        errors ??=
            [
                Error.New("An unexpected error occurred.")
            ];

        problemDetails.Extensions["errors"] = errors.Select(error => new
        {
            error.Title,
            error.Detail,
            error.Code
        });

        return Results.Problem(problemDetails);
    }

    private static string GetType(int statusCode)
    {
        return statusCode switch
        {
            StatusCodes.Status400BadRequest => HttpStatusCode.BadRequest.ToString(),
            StatusCodes.Status404NotFound => HttpStatusCode.NotFound.ToString(),
            StatusCodes.Status409Conflict => HttpStatusCode.Conflict.ToString(),
            StatusCodes.Status422UnprocessableEntity => HttpStatusCode.UnprocessableEntity.ToString(),
            StatusCodes.Status500InternalServerError => HttpStatusCode.InternalServerError.ToString(),
            _ => HttpStatusCode.InternalServerError.ToString()
        };
    }
}