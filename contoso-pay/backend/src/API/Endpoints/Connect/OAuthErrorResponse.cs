using System.Text.Json.Serialization;

namespace API.Endpoints.Connect;

internal sealed record OAuthErrorResponse(
    [property: JsonPropertyName("error")] string Error,
    [property: JsonPropertyName("error_description")] string ErrorDescription);