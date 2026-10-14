using System.Text.Json;
using System.Text.Json.Serialization;

namespace RowCycle.Tests.Infrastructure;

public static class TestJson
{
    /// <summary>The API sends enums as text ("Pending", "FirstOrder").</summary>
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };
}
