using AutoMapper;
using FluentValidation;
using RowCycle.Application.Common;
using RowCycle.Application.Dtos;
using RowCycle.Domain.Entities;

namespace RowCycle.Application.Addresses;

internal sealed class AddressService(
    IAddressRepository addresses,
    IUnitOfWork unitOfWork,
    IValidator<SaveAddressRequest> validator,
    IMapper mapper) : IAddressService
{
    public async Task<IReadOnlyList<AddressResponse>> ListAsync(Guid userId, CancellationToken cancellationToken = default) =>
        mapper.Map<List<AddressResponse>>(await addresses.ListAsync(userId, cancellationToken));

    public async Task<AddressResponse> CreateAsync(Guid userId, SaveAddressRequest request, CancellationToken cancellationToken = default)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);

        var address = new Address { Id = Guid.CreateVersion7(), UserId = userId };
        await unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var existing = await addresses.ListAsync(userId, ct);
            Apply(address, request);
            // The first address is always the default.
            address.IsDefault = request.IsDefault || existing.Count == 0;
            if (address.IsDefault)
            {
                ClearDefault(existing);
            }

            addresses.Add(address);
        }, cancellationToken);

        return mapper.Map<AddressResponse>(address);
    }

    public async Task<AddressResponse> UpdateAsync(
        Guid userId, Guid addressId, SaveAddressRequest request, CancellationToken cancellationToken = default)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);

        Address? address = null;
        await unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var all = await addresses.ListAsync(userId, ct);
            address = all.SingleOrDefault(a => a.Id == addressId) ?? throw AppException.NotFound("Address not found.");
            Apply(address, request);
            if (request.IsDefault)
            {
                ClearDefault(all);
                address.IsDefault = true;
            }

            // Un-ticking "default" on the only default address keeps it as the default.
        }, cancellationToken);

        return mapper.Map<AddressResponse>(address);
    }

    public async Task DeleteAsync(Guid userId, Guid addressId, CancellationToken cancellationToken = default)
    {
        await unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var all = await addresses.ListAsync(userId, ct);
            var address = all.SingleOrDefault(a => a.Id == addressId) ?? throw AppException.NotFound("Address not found.");
            addresses.Remove(address);

            if (address.IsDefault && all.FirstOrDefault(a => a.Id != addressId) is { } next)
            {
                address.IsDefault = false;
                next.IsDefault = true;
            }
        }, cancellationToken);
    }

    private static void Apply(Address address, SaveAddressRequest request)
    {
        address.FullName = request.FullName.Trim();
        address.Phone = request.Phone.Trim();
        address.City = request.City.Trim();
        address.Area = request.Area.Trim();
        address.Street = request.Street.Trim();
        address.Building = request.Building.Trim();
        address.Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();
    }

    private static void ClearDefault(IEnumerable<Address> all)
    {
        foreach (var other in all)
        {
            other.IsDefault = false;
        }
    }
}

public sealed class SaveAddressRequestValidator : AbstractValidator<SaveAddressRequest>
{
    public SaveAddressRequestValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(32).Matches(@"^\+?[0-9 ]{7,20}$")
            .WithMessage("Enter a phone number with digits only, e.g. 01012345678.");
        RuleFor(x => x.City).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Area).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Street).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Building).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Notes).MaximumLength(500);
    }
}
