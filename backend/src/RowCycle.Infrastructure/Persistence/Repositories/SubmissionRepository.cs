using Microsoft.EntityFrameworkCore;
using RowCycle.Application.Common;
using RowCycle.Application.Submissions;
using RowCycle.Domain.Entities;
using RowCycle.Domain.Enums;

namespace RowCycle.Infrastructure.Persistence.Repositories;

internal sealed class SubmissionRepository(AppDbContext db) : ISubmissionRepository
{
    public async Task<IReadOnlyList<SubmissionCategory>> ListCategoriesAsync(bool activeOnly, CancellationToken cancellationToken = default) =>
        await db.SubmissionCategories.AsNoTracking()
            .Where(c => !activeOnly || c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);

    public Task<SubmissionCategory?> FindCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default) =>
        db.SubmissionCategories.IgnoreQueryFilters().SingleOrDefaultAsync(c => c.Id == categoryId, cancellationToken);

    public Task<bool> CategoryNameTakenAsync(string name, Guid exceptId, CancellationToken cancellationToken = default) =>
        db.SubmissionCategories.AnyAsync(c => c.Id != exceptId && c.Name.ToLower() == name.ToLower(), cancellationToken);

    public void AddCategory(SubmissionCategory category) => db.SubmissionCategories.Add(category);

    public void RemoveCategory(SubmissionCategory category) => db.SubmissionCategories.Remove(category);

    public async Task LockMemberAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        EnsureTransaction();

        // Plain SQL because LINQ can't say FOR UPDATE. Only the lock matters, so nothing is loaded.
        var locked = await db.Database
            .SqlQuery<Guid>($"SELECT user_id AS \"Value\" FROM user_profiles WHERE user_id = {userId} FOR UPDATE")
            .ToListAsync(cancellationToken);
        if (locked.Count == 0)
        {
            throw AppException.NotFound("User not found.");
        }
    }

    public Task<int> CountSinceAsync(Guid userId, DateTimeOffset fromUtc, CancellationToken cancellationToken = default) =>
        db.Submissions.CountAsync(s => s.UserId == userId && s.CreatedAt >= fromUtc, cancellationToken);

    public void Add(Submission submission) => db.Submissions.Add(submission);

    public async Task<Submission?> LockAsync(Guid submissionId, CancellationToken cancellationToken = default)
    {
        EnsureTransaction();
        return await db.Submissions
            .FromSql($"SELECT * FROM submissions WHERE id = {submissionId} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);
    }

    public Task<SubmissionView?> FindAsync(Guid submissionId, CancellationToken cancellationToken = default) =>
        Rows()
            .Where(r => r.Submission.Id == submissionId)
            .Select(r => new SubmissionView(r.Submission, r.MemberName))
            .SingleOrDefaultAsync(cancellationToken);

    public Task<PagedResult<SubmissionView>> SearchAsync(
        Guid? userId, SubmissionStatus? status, PageRequest page, CancellationToken cancellationToken = default)
    {
        var rows = Rows();

        if (userId is { } id)
        {
            rows = rows.Where(r => r.Submission.UserId == id);
        }

        if (status is { } wanted)
        {
            rows = rows.Where(r => r.Submission.Status == wanted);
        }

        // A member sees their latest first; the queue shows who has waited longest first.
        rows = userId is null
            ? rows.OrderBy(r => r.Submission.CreatedAt).ThenBy(r => r.Submission.Id)
            : rows.OrderByDescending(r => r.Submission.CreatedAt).ThenByDescending(r => r.Submission.Id);

        return rows.Select(r => new SubmissionView(r.Submission, r.MemberName)).ToPagedResultAsync(page, cancellationToken);
    }

    /// <summary>
    /// Read-only submissions with their category (even if deleted since) and the member's name.
    /// A class with settable properties, not a record, so EF can still filter and order on it.
    /// </summary>
    private IQueryable<Row> Rows() =>
        from s in db.Submissions.AsNoTracking().IgnoreQueryFilters().Include(s => s.Category)
        join p in db.UserProfiles on s.UserId equals p.UserId
        select new Row { Submission = s, MemberName = p.FullName };

    private sealed class Row
    {
        public required Submission Submission { get; init; }
        public required string MemberName { get; init; }
    }

    private void EnsureTransaction()
    {
        if (db.Database.CurrentTransaction is null)
        {
            throw new InvalidOperationException("Rows can only be locked inside a transaction.");
        }
    }
}
