using System.ComponentModel.DataAnnotations;

namespace RowCycle.Application.Common;

/// <summary>Section <c>App</c>.</summary>
public sealed class AppOptions
{
    public const string SectionName = "App";

    /// <summary>Base URL of the Angular app; used to build links in emails.</summary>
    [Required, Url]
    public string WebBaseUrl { get; set; } = string.Empty;
}
