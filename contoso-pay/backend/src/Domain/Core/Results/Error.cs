namespace Domain.Core.Results;

public class Error
{
    public string Title { get; }
    public string? Detail { get; }
    public string? Code { get; }

    private Error(string title, string? detail, string? code)
    {
        Title = title;
        Detail = detail;
        Code = code;
    }

    public static Error New(string title, string? detail = null, string? code = null)
        => new (title, detail, code);
}