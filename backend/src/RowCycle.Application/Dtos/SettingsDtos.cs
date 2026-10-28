namespace RowCycle.Application.Dtos;

/// <summary>All admin-editable settings, as typed values.</summary>
public sealed record SettingsResponse(int DailySubmissionLimit, int LowStockThreshold);

public sealed record UpdateSettingsRequest(int DailySubmissionLimit, int LowStockThreshold);
