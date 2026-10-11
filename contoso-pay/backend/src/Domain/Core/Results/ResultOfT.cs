namespace Domain.Core.Results;

public class Result<TValue> : Result
{
    private readonly TValue? _value;

    public TValue? Value => _value;

    protected internal Result(TValue? value, bool isSuccess, string? successMessage = null, List<Error>? errors = null, ResultType type = ResultType.Undefined)
        : base(isSuccess, successMessage, errors, type)
        => _value = value;
}