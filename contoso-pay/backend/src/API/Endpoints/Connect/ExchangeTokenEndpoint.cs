using Core.Abstractions.Identity;
using Domain.Core.Results;
using Domain.Resources;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using System.Security.Claims;

namespace API.Endpoints.Connect;

internal sealed class ExchangeTokenEndpoint : IEndpoint, IUnversionedEndpoint
{
    private const string FormUrlEncoded = "application/x-www-form-urlencoded";
    private const string InvalidClient = "invalid_client";
    private const string InvalidRequest = "invalid_request";
    private const string UnsupportedGrantType = "unsupported_grant_type";

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/connect/token", async (
            HttpContext context,
            IExchangeToken useCase,
            CancellationToken cancellationToken) =>
        {
            if (!IsFormUrlEncoded(context.Request.ContentType))
            {
                return OAuthError(InvalidRequest, Resource.Validation_Token_InvalidRequest);
            }

            IFormCollection form;
            try
            {
                form = await context.Request.ReadFormAsync(cancellationToken);
            }
            catch (InvalidDataException)
            {
                return OAuthError(InvalidRequest, Resource.Validation_Token_InvalidRequest);
            }

            string grantType = form["grant_type"].ToString();
            string clientId = form["client_id"].ToString();
            string clientSecret = form["client_secret"].ToString();

            if (string.IsNullOrWhiteSpace(grantType))
            {
                return OAuthError(InvalidRequest, Resource.Validation_Token_InvalidRequest);
            }

            if (!string.Equals(grantType, OpenIddictConstants.GrantTypes.ClientCredentials, StringComparison.Ordinal))
            {
                return OAuthError(UnsupportedGrantType, Resource.Validation_Token_UnsupportedGrantType);
            }

            if (string.IsNullOrWhiteSpace(clientId) ||
                string.IsNullOrWhiteSpace(clientSecret))
            {
                return OAuthError(InvalidRequest, Resource.Validation_Token_InvalidRequest);
            }

            Result<ClaimsPrincipal> result = await useCase.ExchangeAsync(clientId, clientSecret, cancellationToken);
            if (result.IsFailure || result.Value is null)
            {
                return OAuthError(InvalidClient, Resource.Validation_Token_InvalidClient);
            }

            return Results.SignIn(
                result.Value,
                authenticationScheme: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        })
        .AllowAnonymous()
        .WithTags(Tags.Connect)
        .Produces(StatusCodes.Status200OK)
        .Produces<OAuthErrorResponse>(StatusCodes.Status400BadRequest)
        .Produces<OAuthErrorResponse>(StatusCodes.Status401Unauthorized);
    }

    private static bool IsFormUrlEncoded(string? contentType)
    {
        if (contentType is null ||
            !contentType.StartsWith(FormUrlEncoded, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return contentType.Length == FormUrlEncoded.Length ||
               contentType[FormUrlEncoded.Length] == ';';
    }

    private static IResult OAuthError(string error, string description)
    {
        return Results.Json(
            new OAuthErrorResponse(error, description),
            statusCode: string.Equals(error, InvalidClient, StringComparison.Ordinal)
                ? StatusCodes.Status401Unauthorized
                : StatusCodes.Status400BadRequest);
    }
}