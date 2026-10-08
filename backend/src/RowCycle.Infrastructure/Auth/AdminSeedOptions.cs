namespace RowCycle.Infrastructure.Auth;

/// <summary>Section <c>Admin</c>. When Email and Password are set, that admin user is created on startup.</summary>
public sealed class AdminSeedOptions
{
    public const string SectionName = "Admin";

    public string? Email { get; set; }
    public string? Password { get; set; }
    public string FullName { get; set; } = "Administrator";
}
