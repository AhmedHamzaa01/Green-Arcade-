using AutoMapper;
using FluentValidation;
using RowCycle.Application.Audit;
using RowCycle.Application.Badges;
using RowCycle.Application.Common;
using RowCycle.Application.Dtos;
using RowCycle.Domain.Entities;

namespace RowCycle.Application.Submissions;

/// <summary>The actions members can submit and what each pays (Admin). Changes are audit-logged.</summary>
public interface ISubmissionCategoryAdminService
{
    /// <summary>All categories, including inactive ones, by name.</summary>
    Task<IReadOnlyList<AdminSubmissionCategoryResponse>> ListAsync(CancellationToken cancellationToken = default);

    Task<AdminSubmissionCategoryResponse> CreateAsync(
        SaveSubmissionCategoryRequest request, Guid adminId, CancellationToken cancellationToken = default);

    /// <summary>New points apply to future approvals; past ones keep what they paid.</summary>
    Task<AdminSubmissionCategoryResponse> UpdateAsync(
        Guid categoryId, SaveSubmissionCategoryRequest request, Guid adminId, CancellationToken cancellationToken = default);

    /// <summary>Soft delete. Refused (409) while a badge counts this category; deactivate it instead.</summary>
    Task DeleteAsync(Guid categoryId, Guid adminId, CancellationToken cancellationToken = default);
}

internal sealed class SubmissionCategoryAdminService(
    ISubmissionRepository submissions,
    IBadgeRepository badges,
    IAuditLogService audit,
    IUnitOfWork unitOfWork,
    IValidator<SaveSubmissionCategoryRequest> validator,
    IMapper mapper) : ISubmissionCategoryAdminService
{
    public async Task<IReadOnlyList<AdminSubmissionCategoryResponse>> ListAsync(CancellationToken cancellationToken = default) =>
        mapper.Map<List<AdminSubmissionCategoryResponse>>(await submissions.ListCategoriesAsync(activeOnly: false, cancellationToken));

    public async Task<AdminSubmissionCategoryResponse> CreateAsync(
        SaveSubmissionCategoryRequest request, Guid adminId, CancellationToken cancellationToken = default)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);

        var category = new SubmissionCategory { Id = Guid.CreateVersion7() };
        await unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            await ApplyAsync(category, request, ct);
            submissions.AddCategory(category);
            audit.Record(adminId, AuditActions.SubmissionCategoryCreate, "submission_category", category.Id.ToString(), request);
        }, cancellationToken);

        return mapper.Map<AdminSubmissionCategoryResponse>(category);
    }

    public async Task<AdminSubmissionCategoryResponse> UpdateAsync(
        Guid categoryId, SaveSubmissionCategoryRequest request, Guid adminId, CancellationToken cancellationToken = default)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);

        var category = await FindAsync(categoryId, cancellationToken);
        await unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var before = mapper.Map<AdminSubmissionCategoryResponse>(category);
            await ApplyAsync(category, request, ct);
            audit.Record(adminId, AuditActions.SubmissionCategoryUpdate, "submission_category", categoryId.ToString(),
                new { before, after = mapper.Map<AdminSubmissionCategoryResponse>(category) });
        }, cancellationToken);

        return mapper.Map<AdminSubmissionCategoryResponse>(category);
    }

    public async Task DeleteAsync(Guid categoryId, Guid adminId, CancellationToken cancellationToken = default)
    {
        var category = await FindAsync(categoryId, cancellationToken);
        if (await badges.IsCategoryUsedAsync(categoryId, cancellationToken))
        {
            throw AppException.Conflict("Category is used by a badge.", "Change or delete that badge first, or deactivate the category.");
        }

        await unitOfWork.ExecuteInTransactionAsync(_ =>
        {
            submissions.RemoveCategory(category);
            audit.Record(adminId, AuditActions.SubmissionCategoryDelete, "submission_category", categoryId.ToString(), new { category.Name });
            return Task.CompletedTask;
        }, cancellationToken);
    }

    private async Task<SubmissionCategory> FindAsync(Guid categoryId, CancellationToken cancellationToken)
    {
        var category = await submissions.FindCategoryAsync(categoryId, cancellationToken);
        return category is null || category.DeletedAt is not null ? throw AppException.NotFound("Category not found.") : category;
    }

    private async Task ApplyAsync(SubmissionCategory category, SaveSubmissionCategoryRequest request, CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();
        if (await submissions.CategoryNameTakenAsync(name, category.Id, cancellationToken))
        {
            throw AppException.Conflict("Name already used.", $"Another action category is called \"{name}\".");
        }

        category.Name = name;
        category.PointsReward = request.PointsReward;
        category.TracksWeight = request.TracksWeight;
        category.IsActive = request.IsActive;
    }
}

public sealed class SaveSubmissionCategoryRequestValidator : AbstractValidator<SaveSubmissionCategoryRequest>
{
    public const int MaxPoints = 10_000;

    public SaveSubmissionCategoryRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.PointsReward).InclusiveBetween(0, MaxPoints);
    }
}
