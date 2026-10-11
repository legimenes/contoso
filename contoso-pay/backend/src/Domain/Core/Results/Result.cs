namespace Domain.Core.Results;

public class Result
{
    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public string? SuccessMessage { get; }

    private readonly List<Error>? _errors;

    public IReadOnlyCollection<Error>? Errors
    {
        get
        {
            return _errors?.AsReadOnly();
        }
    }

    public ResultType Type { get; }

    private const ResultType DefaultSuccessResultType = ResultType.Ok;
    private const ResultType DefaultFailureResultType = ResultType.UnprocessableContent;

    protected internal Result(bool isSuccess, string? successMessage = null, List<Error>? errors = null, ResultType type = ResultType.Undefined)
    {
        IsSuccess = isSuccess;
        SuccessMessage = successMessage;
        _errors = errors;
        Type = type;
    }

    public static Result Success(ResultType type = DefaultSuccessResultType) => new(true, type: type);

    public static Result Success(string successMessage, ResultType type = DefaultSuccessResultType) => new(true, successMessage, type: type);

    public static Result<TValue> Success<TValue>(TValue value, ResultType type = DefaultSuccessResultType) => new(value, true, type: type);

    public static Result<TValue> Success<TValue>(TValue value, string successMessage, ResultType type = DefaultSuccessResultType) => new(value, true, successMessage, type: type);

    public static Result Failure(ResultType type = DefaultFailureResultType) => new(false, type: type);

    public static Result Failure(Error error, ResultType type = DefaultFailureResultType) => new(false, null, [error], type: type);

    public static Result Failure(List<Error> errors, ResultType type = DefaultFailureResultType) => new(false, null, errors, type: type);

    public static Result Failure(IDictionary<string, string[]> errors, ResultType type = DefaultFailureResultType)
    {
        List<Error>? errorList = ConvertDictionaryToErrorList(errors);
        return new(false, null, errorList, type: type);
    }

    public static Result<TValue> Failure<TValue>(Error error, ResultType type = DefaultFailureResultType) => new(default, false, null, [error], type: type);

    public static Result<TValue> Failure<TValue>(List<Error> errors, ResultType type = DefaultFailureResultType) => new(default, false, null, errors, type: type);

    public static Result<TValue> Failure<TValue>(IDictionary<string, string[]> errors, ResultType type = DefaultFailureResultType)
    {
        List<Error>? errorList = ConvertDictionaryToErrorList(errors);
        return new(default, false, null, errorList, type: type);
    }

    private static List<Error> ConvertDictionaryToErrorList(IDictionary<string, string[]> errors)
    {
        List<Error>? errorList = [];
        foreach (ICollection<string> firstLevelErrorMessages in errors.Values)
        {
            if (firstLevelErrorMessages.Count == 1)
                errorList.Add(Error.New(firstLevelErrorMessages.First()));
            else
                foreach (string secondLevelErrorMessages in firstLevelErrorMessages)
                    errorList.Add(Error.New(secondLevelErrorMessages));
        }
        return errorList;
    }
}