namespace RowCycle.Domain.Common;

/// <summary>
/// History that is never deleted: orders are cancelled, submissions rejected, points reversed (FR-05, FR-16, FR-19).
/// </summary>
public interface INonDeletable;
