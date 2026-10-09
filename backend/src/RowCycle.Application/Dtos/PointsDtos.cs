using RowCycle.Domain.Enums;

namespace RowCycle.Application.Dtos;

/// <summary>Result of a points change: the new ledger row and the balance after it.</summary>
public sealed record PointsChange(Guid EntryId, int Amount, int Balance);

public sealed record PointsEntryResponse(
    Guid Id,
    int Amount,
    PointsEntryType Type,
    PointsSourceType SourceType,
    Guid? SourceId,
    string? Reason,
    DateTimeOffset CreatedAt);

/// <summary><c>GET /me/points</c>: the balance plus one page of the history, newest first.</summary>
public sealed record PointsHistoryResponse(
    int Balance,
    IReadOnlyList<PointsEntryResponse> Items,
    int Page,
    int PageSize,
    int Total);
