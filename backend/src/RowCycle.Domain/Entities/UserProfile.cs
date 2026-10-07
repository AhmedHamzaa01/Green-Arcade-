namespace RowCycle.Domain.Entities;

/// <summary>App data for an Identity user; shares the user's id as its primary key.</summary>
public class UserProfile
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? PhotoUrl { get; set; }
    public string? Phone { get; set; }

    /// <summary>Cache of the ledger sum. Changed only by the points service, in the same transaction as the ledger row (FR-05, FR-06).</summary>
    public int PointsBalance { get; set; }

    public List<Address> Addresses { get; set; } = [];
}
