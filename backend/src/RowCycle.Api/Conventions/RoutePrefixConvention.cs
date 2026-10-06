using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace RowCycle.Api.Conventions;

/// <summary>Prefixes every controller route with a common base path, e.g. "api/v1".</summary>
public sealed class RoutePrefixConvention(string prefix) : IApplicationModelConvention
{
    private readonly AttributeRouteModel _prefix = new(new Microsoft.AspNetCore.Mvc.RouteAttribute(prefix));

    public void Apply(ApplicationModel application)
    {
        foreach (var selector in application.Controllers.SelectMany(c => c.Selectors))
        {
            selector.AttributeRouteModel = selector.AttributeRouteModel is null
                ? _prefix
                : AttributeRouteModel.CombineAttributeRouteModel(_prefix, selector.AttributeRouteModel);
        }
    }
}
