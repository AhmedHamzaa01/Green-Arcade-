using RowCycle.Application.Common;
using RowCycle.Domain.Entities;

namespace RowCycle.Application.Addresses;

public interface IAddressRepository : IRepository<Address>
{
    /// <summary>The user's addresses (tracked), default first.</summary>
    Task<IReadOnlyList<Address>> ListAsync(Guid userId, CancellationToken cancellationToken = default);
}
