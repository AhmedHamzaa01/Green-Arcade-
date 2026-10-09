using System.Text.Json;
using FluentValidation;
using RowCycle.Application.Audit;
using RowCycle.Application.Common;
using RowCycle.Application.Dtos;
using RowCycle.Domain.Constants;
using RowCycle.Domain.Entities;

namespace RowCycle.Application.Settings;

internal sealed class SettingsService(
    ISettingsRepository settings,
    IAuditLogService audit,
    IUnitOfWork unitOfWork,
    IValidator<UpdateSettingsRequest> validator) : ISettingsService
{
    /// <summary>Used when a setting row is missing.</summary>
    public const int DefaultDailySubmissionLimit = 5;

    public async Task<SettingsResponse> GetAsync(CancellationToken cancellationToken = default) =>
        new(await GetDailySubmissionLimitAsync(cancellationToken));

    public Task<int> GetDailySubmissionLimitAsync(CancellationToken cancellationToken = default) =>
        GetIntAsync(SettingKeys.DailySubmissionLimit, DefaultDailySubmissionLimit, cancellationToken);

    public async Task<SettingsResponse> UpdateAsync(
        UpdateSettingsRequest request, Guid adminId, CancellationToken cancellationToken = default)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);

        await unitOfWork.ExecuteInTransactionAsync(
            ct => SetIntAsync(SettingKeys.DailySubmissionLimit, request.DailySubmissionLimit, DefaultDailySubmissionLimit, adminId, ct),
            cancellationToken);

        return await GetAsync(cancellationToken);
    }

    private async Task<int> GetIntAsync(string key, int defaultValue, CancellationToken cancellationToken)
    {
        var setting = await settings.FindAsync(key, cancellationToken);
        return setting is null ? defaultValue : JsonSerializer.Deserialize<int>(setting.Value);
    }

    /// <summary>Writes the value and an audit row, only if it actually changed.</summary>
    private async Task SetIntAsync(string key, int value, int defaultValue, Guid adminId, CancellationToken cancellationToken)
    {
        var setting = await settings.FindAsync(key, cancellationToken);
        var oldValue = setting is null ? defaultValue : JsonSerializer.Deserialize<int>(setting.Value);
        if (oldValue == value)
        {
            return;
        }

        if (setting is null)
        {
            settings.Add(new Setting { Key = key, Value = JsonSerializer.Serialize(value) });
        }
        else
        {
            setting.Value = JsonSerializer.Serialize(value);
        }

        audit.Record(adminId, AuditActions.SettingsUpdate, "settings", key, new { old = oldValue, @new = value });
    }
}

public sealed class UpdateSettingsRequestValidator : AbstractValidator<UpdateSettingsRequest>
{
    public UpdateSettingsRequestValidator() =>
        RuleFor(x => x.DailySubmissionLimit).InclusiveBetween(1, 50);
}
