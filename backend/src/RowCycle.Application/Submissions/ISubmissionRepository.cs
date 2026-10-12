using RowCycle.Application.Common;
using RowCycle.Domain.Entities;
using RowCycle.Domain.Enums;

namespace RowCycle.Application.Submissions;

/// <summary>A submission with the name of the member who sent it, for lists and review.</summary>
public sealed record SubmissionView(Submission Submission, string MemberName);

public interface ISubmissionRepository
{
    /// <summary>Active categories, by name.</summary>
    Task<IReadOnlyList<SubmissionCategory>> ListActiveCategoriesAsync(CancellationToken cancellationToken = default);

    /// <summary>Includes soft-deleted categories, so an old submission can still be reviewed.</summary>
    Task<SubmissionCategory?> FindCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Locks the member's profile row until the transaction ends, so two uploads at the same moment
    /// are counted one after the other against the daily limit (FR-09).
    /// </summary>
    Task LockMemberAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>How many submissions the member made since <paramref name="fromUtc"/>, whatever their status.</summary>
    Task<int> CountSinceAsync(Guid userId, DateTimeOffset fromUtc, CancellationToken cancellationToken = default);

    void Add(Submission submission);

    /// <summary>Locks the submission's row until the transaction ends, so two reviews can't race. Tracked.</summary>
    Task<Submission?> LockAsync(Guid submissionId, CancellationToken cancellationToken = default);

    /// <summary>Read-only, with category and member name.</summary>
    Task<SubmissionView?> FindAsync(Guid submissionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// <paramref name="userId"/> limits it to one member's submissions (newest first);
    /// without it this is the moderation queue (oldest first, so the longest-waiting come first).
    /// </summary>
    Task<PagedResult<SubmissionView>> SearchAsync(
        Guid? userId, SubmissionStatus? status, PageRequest page, CancellationToken cancellationToken = default);
}
