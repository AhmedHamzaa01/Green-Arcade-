using RowCycle.Domain.Enums;

namespace RowCycle.Application.Dtos;

/// <summary>An action members can submit, and the points an approval gives.</summary>
public sealed record SubmissionCategoryResponse(Guid Id, string Name, int PointsReward, bool TracksWeight);

/// <summary>The fields of <c>POST /submissions</c> besides the photo. <c>WeightKg</c> only for categories that track weight.</summary>
public sealed record CreateSubmissionRequest(Guid CategoryId, string? Note, decimal? WeightKg);

public sealed record SubmissionResponse(
    Guid Id,
    Guid UserId,
    string MemberName,
    Guid CategoryId,
    string CategoryName,
    int CategoryPoints,
    string PhotoUrl,
    string? Note,
    decimal? WeightKg,
    SubmissionStatus Status,
    Guid? ReviewedBy,
    DateTimeOffset? ReviewedAt,
    string? RejectReason,
    DateTimeOffset CreatedAt);

public sealed record RejectSubmissionRequest(string Reason);

/// <summary>Filters for <c>GET /admin/submissions</c>.</summary>
public sealed record SubmissionQuery(SubmissionStatus? Status = null);
