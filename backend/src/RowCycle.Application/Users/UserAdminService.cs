using FluentValidation;
using RowCycle.Application.Audit;
using RowCycle.Application.Auth;
using RowCycle.Application.Common;
using RowCycle.Application.Dtos;
using RowCycle.Application.Points;
using RowCycle.Domain.Constants;

namespace RowCycle.Application.Users;

/// <summary>Users for admins (F8): search, roles, block, manual points. Every change is audit-logged (FR-19).</summary>
public interface IUserAdminService
{
    Task<PagedResult<AdminUserListItem>> SearchAsync(UserQuery query, PageRequest page, CancellationToken cancellationToken = default);

    Task<AdminUserDetail> GetAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<PointsHistoryResponse> GetPointsAsync(Guid userId, PageRequest page, CancellationToken cancellationToken = default);

    /// <summary>Takes effect at once: the user's next request is re-authorized with the new roles.</summary>
    Task<AdminUserDetail> SetRolesAsync(Guid userId, SetRolesRequest request, Guid adminId, CancellationToken cancellationToken = default);

    /// <summary>Blocking ends every session at once (tokens refused, refresh tokens revoked); unblocking lets them log in again.</summary>
    Task<AdminUserDetail> SetBlockedAsync(Guid userId, SetBlockedRequest request, Guid adminId, CancellationToken cancellationToken = default);

    /// <summary>Writes an Adjust ledger row with the admin's id (FR-07). The balance can't go below 0 (409).</summary>
    Task<PointsChange> AdjustPointsAsync(Guid userId, AdjustPointsRequest request, Guid adminId, CancellationToken cancellationToken = default);
}

internal sealed class UserAdminService(
    IUserDirectory users,
    IRefreshTokenStore refreshTokens,
    IPointsService points,
    IAuditLogService audit,
    IUnitOfWork unitOfWork,
    IValidator<UserQuery> queryValidator,
    IValidator<SetRolesRequest> rolesValidator,
    IValidator<AdjustPointsRequest> adjustValidator,
    IValidator<PageRequest> pageValidator) : IUserAdminService
{
    public async Task<PagedResult<AdminUserListItem>> SearchAsync(
        UserQuery query, PageRequest page, CancellationToken cancellationToken = default)
    {
        await queryValidator.ValidateAndThrowAsync(query, cancellationToken);
        await pageValidator.ValidateAndThrowAsync(page, cancellationToken);
        return await users.SearchAsync(query, page, cancellationToken);
    }

    public async Task<AdminUserDetail> GetAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await users.GetAsync(userId, cancellationToken) ?? throw AppException.NotFound("User not found.");

    public async Task<PointsHistoryResponse> GetPointsAsync(Guid userId, PageRequest page, CancellationToken cancellationToken = default)
    {
        await GetAsync(userId, cancellationToken);
        return await points.GetHistoryAsync(userId, page, cancellationToken);
    }

    public async Task<AdminUserDetail> SetRolesAsync(
        Guid userId, SetRolesRequest request, Guid adminId, CancellationToken cancellationToken = default)
    {
        await rolesValidator.ValidateAndThrowAsync(request, cancellationToken);
        await GetAsync(userId, cancellationToken);

        var roles = request.Roles.Distinct().Order().ToList();
        await unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var before = (await users.GetRolesAsync(userId, ct)).Order().ToList();
            if (before.SequenceEqual(roles))
            {
                return;   // nothing changed: no audit row, sessions untouched
            }

            await users.SetRolesAsync(userId, roles, ct);
            audit.Record(adminId, AuditActions.UserRoles, "user", userId.ToString(), new { before, after = roles });
        }, cancellationToken);

        return await GetAsync(userId, cancellationToken);
    }

    public async Task<AdminUserDetail> SetBlockedAsync(
        Guid userId, SetBlockedRequest request, Guid adminId, CancellationToken cancellationToken = default)
    {
        var user = await GetAsync(userId, cancellationToken);
        if (user.IsActive == !request.Blocked)
        {
            return user;   // already in that state
        }

        await unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            await users.SetActiveAsync(userId, !request.Blocked, ct);
            if (request.Blocked)
            {
                // No new access tokens either: every refresh token is revoked.
                await refreshTokens.RevokeAllAsync(userId, ct);
            }

            audit.Record(adminId, request.Blocked ? AuditActions.UserBlock : AuditActions.UserUnblock, "user", userId.ToString(),
                new { user.Email });
        }, cancellationToken);

        return await GetAsync(userId, cancellationToken);
    }

    public async Task<PointsChange> AdjustPointsAsync(
        Guid userId, AdjustPointsRequest request, Guid adminId, CancellationToken cancellationToken = default)
    {
        await adjustValidator.ValidateAndThrowAsync(request, cancellationToken);
        await GetAsync(userId, cancellationToken);

        PointsChange? change = null;
        await unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            // The ledger row stores the admin as created_by (FR-07); the audit row records the admin action (FR-19).
            change = await points.AdjustAsync(userId, request.Amount, request.Reason.Trim(), adminId, ct);
            audit.Record(adminId, AuditActions.PointsAdjust, "user", userId.ToString(),
                new { request.Amount, reason = request.Reason.Trim(), entryId = change.EntryId, balance = change.Balance });
        }, cancellationToken);

        return change!;
    }
}

public sealed class UserQueryValidator : AbstractValidator<UserQuery>
{
    public UserQueryValidator()
    {
        RuleFor(x => x.Search).MaximumLength(100);
        RuleFor(x => x.Role).Must(role => UserRoles.All.Contains(role!)).When(x => x.Role is not null)
            .WithMessage("Unknown role.");
    }
}

public sealed class SetRolesRequestValidator : AbstractValidator<SetRolesRequest>
{
    public SetRolesRequestValidator()
    {
        RuleFor(x => x.Roles).NotEmpty().WithMessage("Give the user at least one role.");
        RuleForEach(x => x.Roles).Must(role => UserRoles.All.Contains(role))
            .WithMessage(role => $"Unknown role. Use {string.Join(", ", UserRoles.All)}.");
    }
}

public sealed class AdjustPointsRequestValidator : AbstractValidator<AdjustPointsRequest>
{
    public const int MaxAmount = 100_000;

    public AdjustPointsRequestValidator()
    {
        RuleFor(x => x.Amount).NotEqual(0).InclusiveBetween(-MaxAmount, MaxAmount);
        RuleFor(x => x.Reason).NotEmpty().WithMessage("A reason is required.").MaximumLength(500);
    }
}

/// <summary>Every role an admin can give.</summary>
internal static class UserRoles
{
    public static readonly IReadOnlyList<string> All = [Roles.Member, Roles.Moderator, Roles.StoreManager, Roles.Admin];
}
