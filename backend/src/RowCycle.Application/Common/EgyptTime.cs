namespace RowCycle.Application.Common;

/// <summary>
/// Row-Cycle's local time. Data is stored in UTC (rule 4); only "per day" limits are counted in Egypt's calendar day.
/// </summary>
public static class EgyptTime
{
    private static readonly TimeZoneInfo Zone = TimeZoneInfo.FindSystemTimeZoneById("Africa/Cairo");

    /// <summary>The UTC moment today started in Egypt (midnight Cairo time), for the given current time.</summary>
    public static DateTimeOffset StartOfDayUtc(DateTimeOffset now)
    {
        var local = TimeZoneInfo.ConvertTime(now, Zone);
        var midnight = local.Date;
        return new DateTimeOffset(midnight, Zone.GetUtcOffset(midnight)).ToUniversalTime();
    }
}
