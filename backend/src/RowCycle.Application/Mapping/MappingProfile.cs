using System.Text.Json;
using AutoMapper;
using RowCycle.Application.Auth;
using RowCycle.Application.Dtos;
using RowCycle.Domain.Entities;

namespace RowCycle.Application.Mapping;

/// <summary>
/// Every entity → DTO mapping in the app, grouped by feature. <c>MappingTests</c> checks that each DTO field has a source.
/// </summary>
internal sealed class MappingProfile : Profile
{
    public MappingProfile()
    {
        MapAuth();
        MapPoints();
        MapAudit();
    }

    private void MapAuth()
    {
        CreateMap<MeSource, MeResponse>()
            .ForCtorParam(nameof(MeResponse.Id), o => o.MapFrom(s => s.Account.Id))
            .ForCtorParam(nameof(MeResponse.Email), o => o.MapFrom(s => s.Account.Email))
            .ForCtorParam(nameof(MeResponse.EmailVerified), o => o.MapFrom(s => s.Account.EmailConfirmed))
            .ForCtorParam(nameof(MeResponse.FullName), o => o.MapFrom(s => s.Profile.FullName))
            .ForCtorParam(nameof(MeResponse.PhotoUrl), o => o.MapFrom(s => s.Profile.PhotoUrl))
            .ForCtorParam(nameof(MeResponse.Phone), o => o.MapFrom(s => s.Profile.Phone))
            .ForCtorParam(nameof(MeResponse.PointsBalance), o => o.MapFrom(s => s.Profile.PointsBalance))
            .ForCtorParam(nameof(MeResponse.Roles), o => o.MapFrom(s => s.Roles.Order().ToList()));
    }

    private void MapPoints()
    {
        CreateMap<PointsLedgerEntry, PointsEntryResponse>();
    }

    private void MapAudit()
    {
        CreateMap<AuditLog, AuditLogResponse>()
            .ForCtorParam(nameof(AuditLogResponse.Data), o => o.MapFrom(s => ParseJson(s.Data)));
    }

    /// <summary>Stored JSON text → a JSON value in the response (not an escaped string).</summary>
    private static JsonElement? ParseJson(string? json)
    {
        if (json is null)
        {
            return null;
        }

        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}

/// <summary>The data <see cref="MeResponse"/> is built from: the login account, the profile and the roles.</summary>
internal sealed record MeSource(UserAccount Account, UserProfile Profile, IReadOnlyList<string> Roles);
