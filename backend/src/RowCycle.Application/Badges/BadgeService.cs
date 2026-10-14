using FluentValidation;
using RowCycle.Application.Audit;
using RowCycle.Application.Common;
using RowCycle.Application.Dtos;
using RowCycle.Application.Submissions;
using RowCycle.Domain.Constants;
using RowCycle.Domain.Entities;
using RowCycle.Domain.Enums;

namespace RowCycle.Application.Badges;

internal sealed class BadgeService(
    IBadgeRepository badges,
    ISubmissionRepository submissions,
    IAuditLogService audit,
    IUnitOfWork unitOfWork,
    IValidator<SaveBadgeRequest> validator) : IBadgeService
{
    public async Task<IReadOnlyList<MyBadgeResponse>> ListMineAsync(Guid userId, CancellationToken cancellationToken = default) =>
        (await badges.ListEarnedAsync(userId, cancellationToken))
            .Select(e => new MyBadgeResponse(e.Badge.Id, e.Badge.Name, e.Badge.Icon, e.AwardedAt))
            .ToList();

    public async Task<IReadOnlyList<BadgeResponse>> ListAsync(CancellationToken cancellationToken = default) =>
        (await badges.ListAsync(cancellationToken)).Select(v => ToResponse(v.Badge, v.RuleCategoryName)).ToList();

    public IReadOnlyList<string> GetIcons() => BadgeIcons.All;

    public async Task<BadgeResponse> CreateAsync(SaveBadgeRequest request, Guid adminId, CancellationToken cancellationToken = default)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);

        var badge = new Badge { Id = Guid.CreateVersion7() };
        string? categoryName = null;
        await unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            categoryName = await ApplyAsync(badge, request, ct);
            badges.Add(badge);
            audit.Record(adminId, AuditActions.BadgeCreate, "badge", badge.Id.ToString(), request);
        }, cancellationToken);

        return ToResponse(badge, categoryName);
    }

    public async Task<BadgeResponse> UpdateAsync(
        Guid badgeId, SaveBadgeRequest request, Guid adminId, CancellationToken cancellationToken = default)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);

        var badge = await badges.FindAsync(badgeId, cancellationToken) ?? throw AppException.NotFound("Badge not found.");
        string? categoryName = null;
        await unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var before = new { badge.Name, badge.Icon, badge.RuleType, badge.RuleCategoryId, badge.Threshold };
            categoryName = await ApplyAsync(badge, request, ct);
            audit.Record(adminId, AuditActions.BadgeUpdate, "badge", badgeId.ToString(), new { before, after = request });
        }, cancellationToken);

        return ToResponse(badge, categoryName);
    }

    public async Task DeleteAsync(Guid badgeId, Guid adminId, CancellationToken cancellationToken = default)
    {
        var badge = await badges.FindAsync(badgeId, cancellationToken) ?? throw AppException.NotFound("Badge not found.");
        await unitOfWork.ExecuteInTransactionAsync(_ =>
        {
            badges.Remove(badge);
            audit.Record(adminId, AuditActions.BadgeDelete, "badge", badgeId.ToString(), new { badge.Name });
            return Task.CompletedTask;
        }, cancellationToken);
    }

    /// <summary>Checks what the validator can't (unique name, existing category) and copies the fields. Returns the category's name.</summary>
    private async Task<string?> ApplyAsync(Badge badge, SaveBadgeRequest request, CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();
        if (await badges.NameTakenAsync(name, badge.Id, cancellationToken))
        {
            throw AppException.Conflict("Name already used.", $"Another badge is called \"{name}\".");
        }

        string? categoryName = null;
        if (request.RuleType == BadgeRuleType.CategoryApproved)
        {
            var category = await submissions.FindCategoryAsync(request.RuleCategoryId!.Value, cancellationToken);
            if (category is null || category.DeletedAt is not null)
            {
                throw AppException.Validation("Unknown action category.", "Choose one of the listed action categories.");
            }

            categoryName = category.Name;
        }

        badge.Name = name;
        badge.Icon = request.Icon;
        badge.RuleType = request.RuleType;
        badge.RuleCategoryId = request.RuleType == BadgeRuleType.CategoryApproved ? request.RuleCategoryId : null;
        // "First order" has no count; 1 keeps the column meaningful.
        badge.Threshold = request.RuleType == BadgeRuleType.FirstOrder ? 1 : request.Threshold;
        return categoryName;
    }

    private static BadgeResponse ToResponse(Badge badge, string? categoryName) =>
        new(badge.Id, badge.Name, badge.Icon, badge.RuleType, badge.RuleCategoryId, categoryName, badge.Threshold);
}

public sealed class SaveBadgeRequestValidator : AbstractValidator<SaveBadgeRequest>
{
    public const int MaxThreshold = 10_000;

    public SaveBadgeRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Icon).Must(icon => BadgeIcons.All.Contains(icon)).WithMessage("Choose one of the listed icons.");
        RuleFor(x => x.RuleType).IsInEnum();
        RuleFor(x => x.RuleCategoryId).NotEmpty().When(x => x.RuleType == BadgeRuleType.CategoryApproved)
            .WithMessage("Choose the action category this badge counts.");
        RuleFor(x => x.RuleCategoryId).Empty().When(x => x.RuleType != BadgeRuleType.CategoryApproved)
            .WithMessage("Only the \"approved in a category\" rule uses a category.");
        RuleFor(x => x.Threshold).InclusiveBetween(1, MaxThreshold).When(x => x.RuleType != BadgeRuleType.FirstOrder);
    }
}
