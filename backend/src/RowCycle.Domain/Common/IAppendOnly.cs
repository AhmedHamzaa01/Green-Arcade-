namespace RowCycle.Domain.Common;

/// <summary>Rows are written once and never updated or deleted (FR-05, FR-16, FR-19).</summary>
public interface IAppendOnly : INonDeletable;
