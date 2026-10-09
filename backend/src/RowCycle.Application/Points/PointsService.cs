using AutoMapper;
using FluentValidation;
using RowCycle.Application.Common;
using RowCycle.Application.Dtos;
using RowCycle.Application.Users;
using RowCycle.Domain.Entities;
using RowCycle.Domain.Enums;

namespace RowCycle.Application.Points;

internal sealed class PointsService(
    IPointsLedgerRepository ledger,
    IUserProfileRepository profiles,
    IUnitOfWork unitOfWork,
    IValidator<PageRequest> pageValidator,
    IMapper mapper,
    TimeProvider timeProvider) : IPointsService
{
    public Task<PointsChange> EarnAsync(
        Guid userId, int amount, PointsSourceType sourceType, Guid sourceId,
        string? reason = null, Guid? createdBy = null, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(amount);
        if (sourceType == PointsSourceType.Manual)
        {
            throw new ArgumentException("Manual changes use AdjustAsync.", nameof(sourceType));
        }

        return ApplyAsync(userId, amount, PointsEntryType.Earn, sourceType, sourceId, reason, createdBy, cancellationToken);
    }

    public Task<PointsChange> RedeemAsync(
        Guid userId, int amount, Guid orderId, string? reason = null, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(amount);
        return ApplyAsync(userId, -amount, PointsEntryType.Redeem, PointsSourceType.Order, orderId, reason, null, cancellationToken);
    }

    public async Task<PointsChange> ReverseAsync(
        PointsSourceType sourceType, Guid sourceId, string reason, Guid? createdBy = null, CancellationToken cancellationToken = default)
    {
        PointsChange? change = null;
        await unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var entries = await ledger.GetBySourceAsync(sourceType, sourceId, ct);
            if (entries.Count == 0)
            {
                throw AppException.NotFound("Nothing to reverse.", $"No points were recorded for this {sourceType.ToString().ToLowerInvariant()}.");
            }

            // Lock first, then re-read, so two reversals of the same source can't both pass the check.
            var userId = entries[0].UserId;
            var profile = await LockAsync(userId, ct);
            entries = await ledger.GetBySourceAsync(sourceType, sourceId, ct);

            // Undo exactly what this source did: the opposite of its net effect.
            var net = entries.Sum(e => e.Amount);
            if (net == 0 || entries.Any(e => e.Type == PointsEntryType.Reverse))
            {
                throw AppException.Conflict("Already reversed.", "These points were already given back or taken back.");
            }

            change = Write(profile, -net, PointsEntryType.Reverse, sourceType, sourceId, reason, createdBy);
        }, cancellationToken);

        return change!;
    }

    public Task<PointsChange> AdjustAsync(
        Guid userId, int amount, string reason, Guid adminId, CancellationToken cancellationToken = default)
    {
        if (amount == 0)
        {
            throw AppException.Validation("Amount can't be 0.");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw AppException.Validation("A reason is required.", "Explain why the points are being changed.");
        }

        return ApplyAsync(userId, amount, PointsEntryType.Adjust, PointsSourceType.Manual, null, reason.Trim(), adminId, cancellationToken);
    }

    public async Task<PointsHistoryResponse> GetHistoryAsync(Guid userId, PageRequest page, CancellationToken cancellationToken = default)
    {
        await pageValidator.ValidateAndThrowAsync(page, cancellationToken);

        var profile = await profiles.FindAsync(userId, cancellationToken) ?? throw AppException.NotFound("User not found.");
        var history = await ledger.GetHistoryAsync(userId, page, cancellationToken);

        return new PointsHistoryResponse(
            profile.PointsBalance,
            mapper.Map<List<PointsEntryResponse>>(history.Items),
            history.Page,
            history.PageSize,
            history.Total);
    }

    /// <summary>Locks the balance, checks the rules, writes the row and the new balance, all in one transaction.</summary>
    private async Task<PointsChange> ApplyAsync(
        Guid userId, int amount, PointsEntryType type, PointsSourceType sourceType, Guid? sourceId,
        string? reason, Guid? createdBy, CancellationToken cancellationToken)
    {
        PointsChange? change = null;
        await unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var profile = await LockAsync(userId, ct);

            // One Earn per submission/order and one Redeem per order, even if the caller retries.
            if (sourceId is { } id && await ledger.ExistsAsync(sourceType, id, type, ct))
            {
                throw AppException.Conflict("Points already recorded.", $"This {sourceType.ToString().ToLowerInvariant()} already has a {type} entry.");
            }

            change = Write(profile, amount, type, sourceType, sourceId, reason, createdBy);
        }, cancellationToken);

        return change!;
    }

    private async Task<UserProfile> LockAsync(Guid userId, CancellationToken cancellationToken) =>
        await ledger.LockBalanceAsync(userId, cancellationToken) ?? throw AppException.NotFound("User not found.");

    private PointsChange Write(
        UserProfile profile, int amount, PointsEntryType type, PointsSourceType sourceType, Guid? sourceId,
        string? reason, Guid? createdBy)
    {
        var balance = profile.PointsBalance + amount;
        if (balance < 0)
        {
            throw AppException.Conflict("Not enough points.", $"Balance is {profile.PointsBalance}; this needs {-amount}.");
        }

        var entry = new PointsLedgerEntry
        {
            Id = Guid.CreateVersion7(),
            UserId = profile.UserId,
            Amount = amount,
            Type = type,
            SourceType = sourceType,
            SourceId = sourceId,
            Reason = reason,
            CreatedBy = createdBy,
            CreatedAt = timeProvider.GetUtcNow(),
        };
        ledger.Add(entry);
        profile.PointsBalance = balance;

        return new PointsChange(entry.Id, amount, balance);
    }
}
