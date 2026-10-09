namespace RowCycle.Application.Dtos;

/// <summary>All admin-editable settings, as typed values.</summary>
public sealed record SettingsResponse(int DailySubmissionLimit);

public sealed record UpdateSettingsRequest(int DailySubmissionLimit);
