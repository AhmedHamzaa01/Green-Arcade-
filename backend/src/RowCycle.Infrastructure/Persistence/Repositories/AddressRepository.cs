using Microsoft.EntityFrameworkCore;
using RowCycle.Application.Addresses;
using RowCycle.Domain.Entities;

namespace RowCycle.Infrastructure.Persistence.Repositories;

internal sealed class AddressRepository(AppDbContext db) : Repository<Address>(db), IAddressRepository
{
    public async Task<IReadOnlyList<Address>> ListAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await Db.Addresses
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.IsDefault).ThenBy(a => a.City).ThenBy(a => a.Id)
            .ToListAsync(cancellationToken);
}
