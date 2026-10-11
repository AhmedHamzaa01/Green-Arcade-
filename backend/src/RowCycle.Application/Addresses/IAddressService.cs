using RowCycle.Application.Dtos;

namespace RowCycle.Application.Addresses;

/// <summary>A member's delivery addresses (F1). Exactly one is the default once there is any.</summary>
public interface IAddressService
{
    Task<IReadOnlyList<AddressResponse>> ListAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<AddressResponse> CreateAsync(Guid userId, SaveAddressRequest request, CancellationToken cancellationToken = default);
    Task<AddressResponse> UpdateAsync(Guid userId, Guid addressId, SaveAddressRequest request, CancellationToken cancellationToken = default);

    /// <summary>Soft delete. If it was the default, the next address becomes the default.</summary>
    Task DeleteAsync(Guid userId, Guid addressId, CancellationToken cancellationToken = default);
}
