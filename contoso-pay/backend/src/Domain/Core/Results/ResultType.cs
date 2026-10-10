namespace Domain.Core.Results;

public enum ResultType
{
    Undefined = 0,
    Ok = 200,
    BadRequest = 400,
    NotFound = 404,
    Conflict = 409,
    UnprocessableContent = 422,
    InternalError = 500
}