namespace RowCycle.Application.Common;

/// <summary>What went wrong, independent of HTTP. The API maps each kind to a status code.</summary>
public enum ErrorKind
{
    Validation,
    Unauthorized,
    Forbidden,
    NotFound,
    Conflict,
}
