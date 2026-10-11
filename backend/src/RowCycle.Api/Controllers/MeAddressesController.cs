using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RowCycle.Application.Addresses;
using RowCycle.Application.Dtos;

namespace RowCycle.Api.Controllers;

/// <summary>The signed-in member's delivery addresses.</summary>
[ApiController]
[Route("me/addresses")]
[Authorize]
[Produces("application/json")]
public sealed class MeAddressesController(IAddressService addresses) : ControllerBase
{
    /// <summary>Default address first.</summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<AddressResponse>>(StatusCodes.Status200OK)]
    public Task<IReadOnlyList<AddressResponse>> List(CancellationToken cancellationToken) =>
        addresses.ListAsync(User.GetUserId(), cancellationToken);

    [HttpPost]
    [ProducesResponseType<AddressResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(SaveAddressRequest request, CancellationToken cancellationToken) =>
        StatusCode(StatusCodes.Status201Created, await addresses.CreateAsync(User.GetUserId(), request, cancellationToken));

    [HttpPut("{id:guid}")]
    [ProducesResponseType<AddressResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<AddressResponse> Update(Guid id, SaveAddressRequest request, CancellationToken cancellationToken) =>
        addresses.UpdateAsync(User.GetUserId(), id, request, cancellationToken);

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await addresses.DeleteAsync(User.GetUserId(), id, cancellationToken);
        return NoContent();
    }
}
