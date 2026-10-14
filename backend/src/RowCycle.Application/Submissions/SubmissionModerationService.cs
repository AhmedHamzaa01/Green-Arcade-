using AutoMapper;
using FluentValidation;
using RowCycle.Application.Audit;
using RowCycle.Application.Badges;
using RowCycle.Application.Common;
using RowCycle.Application.Dtos;
using RowCycle.Application.Points;
using RowCycle.Domain.Entities;
using RowCycle.Domain.Enums;

namespace RowCycle.Application.Submissions;

internal sealed class SubmissionModerationService(
    ISubmissionRepository submissions,
    IPointsService points,
    BadgeAwarder awarder,
    IAuditLogService audit,
    IUnitOfWork unitOfWork,
    IValidator<SubmissionQuery> queryValidator,
    IValidator<RejectSubmissionRequest> rejectValidator,
    IValidator<PageRequest> pageValidator,
    IMapper mapper,
    TimeProvider timeProvider) : ISubmissionModerationService
{
    public async Task<PagedResult<SubmissionResponse>> SearchAsync(
        SubmissionQuery query, PageRequest page, CancellationToken cancellationToken = default)
    {
        await queryValidator.ValidateAndThrowAsync(query, cancellationToken);
        await pageValidator.ValidateAndThrowAsync(page, cancellationToken);
        return (await submissions.SearchAsync(null, query.Status, page, cancellationToken)).Map(mapper.Map<SubmissionResponse>);
    }

    public async Task<SubmissionResponse> GetAsync(Guid submissionId, CancellationToken cancellationToken = default) =>
        mapper.Map<SubmissionResponse>(await submissions.FindAsync(submissionId, cancellationToken)
            ?? throw AppException.NotFound("Submission not found."));

    public async Task<SubmissionResponse> ApproveAsync(Guid submissionId, Guid moderatorId, CancellationToken cancellationToken = default)
    {
        await unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var submission = await LockPendingAsync(submissionId, ct);
            var category = await submissions.FindCategoryAsync(submission.CategoryId, ct)
                ?? throw AppException.NotFound("Action category not found.");

            MarkReviewed(submission, SubmissionStatus.Approved, moderatorId);

            // FR-10: the points go in the same transaction; the ledger also allows only one Earn per submission.
            if (category.PointsReward > 0)
            {
                await points.EarnAsync(submission.UserId, category.PointsReward, PointsSourceType.Submission, submission.Id,
                    $"Action approved: {category.Name}", moderatorId, ct);
            }

            // FR-12: badges reached by this approval, in the same transaction.
            await awarder.AwardAsync(submission.UserId, BadgeTrigger.SubmissionApproved, ct);

            audit.Record(moderatorId, AuditActions.SubmissionApprove, "submission", submissionId.ToString(),
                new { submission.UserId, category = category.Name, points = category.PointsReward });
        }, cancellationToken);

        return await GetAsync(submissionId, cancellationToken);
    }

    public async Task<SubmissionResponse> RejectAsync(
        Guid submissionId, RejectSubmissionRequest request, Guid moderatorId, CancellationToken cancellationToken = default)
    {
        await rejectValidator.ValidateAndThrowAsync(request, cancellationToken);

        await unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var submission = await LockPendingAsync(submissionId, ct);
            MarkReviewed(submission, SubmissionStatus.Rejected, moderatorId);
            submission.RejectReason = request.Reason.Trim();

            audit.Record(moderatorId, AuditActions.SubmissionReject, "submission", submissionId.ToString(),
                new { submission.UserId, reason = submission.RejectReason });
        }, cancellationToken);

        return await GetAsync(submissionId, cancellationToken);
    }

    /// <summary>Only Pending submissions can be reviewed, so a second approve (or approve after reject) is refused.</summary>
    private async Task<Submission> LockPendingAsync(Guid submissionId, CancellationToken cancellationToken)
    {
        var submission = await submissions.LockAsync(submissionId, cancellationToken)
            ?? throw AppException.NotFound("Submission not found.");
        if (submission.Status != SubmissionStatus.Pending)
        {
            throw AppException.Conflict("Already reviewed.", $"This submission was already {submission.Status.ToString().ToLowerInvariant()}.");
        }

        return submission;
    }

    private void MarkReviewed(Submission submission, SubmissionStatus status, Guid moderatorId)
    {
        submission.Status = status;
        submission.ReviewedBy = moderatorId;
        submission.ReviewedAt = timeProvider.GetUtcNow();
    }
}

public sealed class SubmissionQueryValidator : AbstractValidator<SubmissionQuery>
{
    public SubmissionQueryValidator() => RuleFor(x => x.Status).IsInEnum().When(x => x.Status is not null);
}

public sealed class RejectSubmissionRequestValidator : AbstractValidator<RejectSubmissionRequest>
{
    public RejectSubmissionRequestValidator() =>
        RuleFor(x => x.Reason).NotEmpty().WithMessage("A reason is required.").MaximumLength(500);
}
