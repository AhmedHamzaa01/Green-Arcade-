using RowCycle.Application.Common;
using RowCycle.Application.Dtos;

namespace RowCycle.Application.Submissions;

/// <summary>Photo-verified actions, the member side (F3).</summary>
public interface ISubmissionService
{
    Task<IReadOnlyList<SubmissionCategoryResponse>> GetCategoriesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves the photo (JPG/PNG/WEBP ≤ 10 MB, checked by content, FR-08) and creates a Pending submission,
    /// unless the member reached today's limit (FR-09). Days are counted in Egypt time.
    /// </summary>
    Task<SubmissionResponse> CreateAsync(
        Guid userId, CreateSubmissionRequest request, FileUpload photo, CancellationToken cancellationToken = default);

    Task<PagedResult<SubmissionResponse>> ListMineAsync(Guid userId, PageRequest page, CancellationToken cancellationToken = default);
}

/// <summary>The moderation queue (F3, PRD Moderator). Reviews are audit-logged.</summary>
public interface ISubmissionModerationService
{
    Task<PagedResult<SubmissionResponse>> SearchAsync(SubmissionQuery query, PageRequest page, CancellationToken cancellationToken = default);

    Task<SubmissionResponse> GetAsync(Guid submissionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets status, reviewer and time and writes the Earn ledger row in ONE transaction (FR-10).
    /// Only a Pending submission can be approved, so it pays out once.
    /// </summary>
    Task<SubmissionResponse> ApproveAsync(Guid submissionId, Guid moderatorId, CancellationToken cancellationToken = default);

    /// <summary>Requires a reason; writes no points.</summary>
    Task<SubmissionResponse> RejectAsync(
        Guid submissionId, RejectSubmissionRequest request, Guid moderatorId, CancellationToken cancellationToken = default);
}
