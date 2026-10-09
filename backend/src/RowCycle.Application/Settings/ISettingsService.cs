using RowCycle.Application.Dtos;

namespace RowCycle.Application.Settings;

/// <summary>Admin-editable settings, read and written as typed values.</summary>
public interface ISettingsService
{
    Task<SettingsResponse> GetAsync(CancellationToken cancellationToken = default);

    /// <summary>Max submissions per member per day (FR-09). Used by the submission service.</summary>
    Task<int> GetDailySubmissionLimitAsync(CancellationToken cancellationToken = default);

    /// <summary>Saves the new values and audit-logs each changed setting with its old and new value (FR-19).</summary>
    Task<SettingsResponse> UpdateAsync(UpdateSettingsRequest request, Guid adminId, CancellationToken cancellationToken = default);
}
