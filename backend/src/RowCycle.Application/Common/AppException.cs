namespace RowCycle.Application.Common;

/// <summary>
/// An expected business error (wrong password, duplicate email, ...). The API turns it into a Problem Details response.
/// </summary>
public sealed class AppException(ErrorKind kind, string title, string? detail = null) : Exception(detail ?? title)
{
    public ErrorKind Kind { get; } = kind;
    public string Title { get; } = title;
    public string? Detail { get; } = detail;

    public static AppException Validation(string title, string? detail = null) => new(ErrorKind.Validation, title, detail);
    public static AppException Unauthorized(string title, string? detail = null) => new(ErrorKind.Unauthorized, title, detail);
    public static AppException NotFound(string title, string? detail = null) => new(ErrorKind.NotFound, title, detail);
    public static AppException Conflict(string title, string? detail = null) => new(ErrorKind.Conflict, title, detail);
}
