using Domain.Core.Results;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Security.Claims;

namespace API.Endpoints;

internal abstract class EndpointBase
{
    private const string MerchantIdClaim = "merchant_id";

    internal static bool TryGetMerchantId(ClaimsPrincipal principal, out Guid merchantId)
    {
        merchantId = Guid.Empty;
        string? value = principal.FindFirst(MerchantIdClaim)?.Value;
        return Guid.TryParse(value, out merchantId) && merchantId != Guid.Empty;
    }

    internal static IResult CreateResult(Result result)
    {
        return result.Type switch
        {
            ResultType.Ok => Results.Ok(),
            ResultType.NoContent => Results.NoContent(),
            _ => CreateFailureResult(result)
        };
    }

    internal static IResult CreateResult<T>(Result<T> result, string? uri = null)
    {
        return result.Type switch
        {
            ResultType.Ok => Results.Ok(result.Value),
            ResultType.Created => Results.Created(uri, result.Value),
            ResultType.NoContent => Results.NoContent(),
            _ => CreateFailureResult(result)
        };
    }

    private static IResult CreateFailureResult(Result result)
    {
        return result.Type switch
        {
            ResultType.BadRequest => CreateProblemDetails(StatusCodes.Status400BadRequest, result.Errors),
            ResultType.Unauthorized => CreateProblemDetails(StatusCodes.Status401Unauthorized, result.Errors),
            ResultType.NotFound => CreateProblemDetails(StatusCodes.Status404NotFound, result.Errors),
            ResultType.Conflict => CreateProblemDetails(StatusCodes.Status409Conflict, result.Errors),
            ResultType.UnprocessableContent => CreateProblemDetails(StatusCodes.Status422UnprocessableEntity, result.Errors),
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
            StatusCodes.Status401Unauthorized => HttpStatusCode.Unauthorized.ToString(),
            StatusCodes.Status404NotFound => HttpStatusCode.NotFound.ToString(),
            StatusCodes.Status409Conflict => HttpStatusCode.Conflict.ToString(),
            StatusCodes.Status422UnprocessableEntity => HttpStatusCode.UnprocessableEntity.ToString(),
            StatusCodes.Status500InternalServerError => HttpStatusCode.InternalServerError.ToString(),
            _ => HttpStatusCode.InternalServerError.ToString()
        };
    }
}