namespace RowCycle.Domain.Entities;

public class Setting
{
    public string Key { get; set; } = string.Empty;

    /// <summary>JSON text (stored as jsonb).</summary>
    public string Value { get; set; } = "null";
}
