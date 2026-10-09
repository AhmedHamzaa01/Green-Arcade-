using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using RowCycle.Tests.Infrastructure;

namespace RowCycle.Tests;

[Collection(ApiCollection.Name)]
public sealed class MappingTests(ApiFactory factory)
{
    /// <summary>Fails if any DTO field has no source, so a broken mapping shows up here instead of at runtime.</summary>
    [Fact]
    public void All_mappings_are_valid()
    {
        var mapper = factory.Services.GetRequiredService<IMapper>();

        mapper.ConfigurationProvider.AssertConfigurationIsValid();
    }
}
