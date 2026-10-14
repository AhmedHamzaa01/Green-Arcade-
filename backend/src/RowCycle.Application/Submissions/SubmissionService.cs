using AutoMapper;
using FluentValidation;
using RowCycle.Application.Common;
using RowCycle.Application.Dtos;
using RowCycle.Application.Settings;
using RowCycle.Domain.Entities;

namespace RowCycle.Application.Submissions;

internal sealed class SubmissionService(
    ISubmissionRepository submissions,
    ISettingsService settings,
    IFileStorage fileStorage,
    IUnitOfWork unitOfWork,
    IValidator<CreateSubmissionRequest> createValidator,
    IValidator<PageRequest> pageValidator,
    IMapper mapper,
    TimeProvider timeProvider) : ISubmissionService
{
    private const string PhotoFolder = "submissions";

    public async Task<IReadOnlyList<SubmissionCategoryResponse>> GetCategoriesAsync(CancellationToken cancellationToken = default) =>
        mapper.Map<List<SubmissionCategoryResponse>>(await submissions.ListCategoriesAsync(activeOnly: true, cancellationToken));

    public async Task<SubmissionResponse> CreateAsync(
        Guid userId, CreateSubmissionRequest request, FileUpload photo, CancellationToken cancellationToken = default)
    {
        await createValidator.ValidateAndThrowAsync(request, cancellationToken);

        var category = await submissions.FindCategoryAsync(request.CategoryId, cancellationToken);
        if (category is null || category.DeletedAt is not null || !category.IsActive)
        {
            throw AppException.Validation("Unknown action.", "Choose one of the listed actions.");
        }

        if (request.WeightKg is not null && !category.TracksWeight)
        {
            throw AppException.Validation("Weight isn't recorded for this action.", $"Leave the weight empty for \"{category.Name}\".");
        }

        // FR-08: size and real content, not the file name.
        if (photo.Length is <= 0 or > ImageFileCheck.MaxBytes)
        {
            throw AppException.Validation("Photo is too large.", "Use a photo up to 10 MB.");
        }

        var extension = await ImageFileCheck.DetectExtensionAsync(photo.Content, cancellationToken)
            ?? throw AppException.Validation("Unsupported photo.", "Use a JPG, PNG or WEBP photo.");

        var limit = await settings.GetDailySubmissionLimitAsync(cancellationToken);
        var now = timeProvider.GetUtcNow();
        var submission = new Submission
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            CategoryId = category.Id,
            Category = category,
            Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim(),
            WeightKg = request.WeightKg,
            CreatedAt = now,
        };

        submission.PhotoUrl = await fileStorage.SaveAsync(photo.Content, PhotoFolder, extension, cancellationToken);
        try
        {
            await unitOfWork.ExecuteInTransactionAsync(async ct =>
            {
                // FR-09: counted under a lock on the member, so parallel uploads can't both slip under the limit.
                // Every submission today counts, rejected ones too, so re-uploading isn't a way around it.
                await submissions.LockMemberAsync(userId, ct);
                if (await submissions.CountSinceAsync(userId, EgyptTime.StartOfDayUtc(now), ct) >= limit)
                {
                    throw AppException.Conflict("Daily limit reached.", $"You can submit up to {limit} actions a day. Try again tomorrow.");
                }

                submissions.Add(submission);
            }, cancellationToken);
        }
        catch
        {
            // Don't leave a file nobody points to.
            await fileStorage.DeleteAsync(submission.PhotoUrl, CancellationToken.None);
            throw;
        }

        var saved = await submissions.FindAsync(submission.Id, cancellationToken);
        return mapper.Map<SubmissionResponse>(saved!);
    }

    public async Task<PagedResult<SubmissionResponse>> ListMineAsync(
        Guid userId, PageRequest page, CancellationToken cancellationToken = default)
    {
        await pageValidator.ValidateAndThrowAsync(page, cancellationToken);
        return (await submissions.SearchAsync(userId, null, page, cancellationToken)).Map(mapper.Map<SubmissionResponse>);
    }
}

public sealed class CreateSubmissionRequestValidator : AbstractValidator<CreateSubmissionRequest>
{
    /// <summary>A generous upper bound for one collection or clean-up.</summary>
    public const decimal MaxWeightKg = 10_000m;

    public CreateSubmissionRequestValidator()
    {
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.Note).MaximumLength(1000);
        RuleFor(x => x.WeightKg).InclusiveBetween(0m, MaxWeightKg).PrecisionScale(8, 2, ignoreTrailingZeros: true)
            .When(x => x.WeightKg is not null);
    }
}
