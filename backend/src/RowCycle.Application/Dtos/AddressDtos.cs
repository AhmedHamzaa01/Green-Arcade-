namespace RowCycle.Application.Dtos;

public sealed record AddressResponse(
    Guid Id,
    string FullName,
    string Phone,
    string City,
    string Area,
    string Street,
    string Building,
    string? Notes,
    bool IsDefault);

/// <summary>Create or edit a delivery address. Making it the default un-defaults the others.</summary>
public sealed record SaveAddressRequest(
    string FullName,
    string Phone,
    string City,
    string Area,
    string Street,
    string Building,
    string? Notes,
    bool IsDefault);
