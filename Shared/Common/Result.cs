namespace FolioTrace.Common;

public sealed record Result<T>
{
    public T? Value { get; init; }

    public IReadOnlyList<string> ValidationErrors { get; init; } = [];

    public bool IsValid => ValidationErrors.Count == 0;

    public static Result<T> Success(T value) => new()
    {
        Value = value
    };

    public static Result<T> Invalid(IReadOnlyList<string> validationErrors) => new()
    {
        ValidationErrors = validationErrors
    };
}
