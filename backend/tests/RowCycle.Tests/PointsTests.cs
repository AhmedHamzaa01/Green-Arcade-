using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RowCycle.Application.Auth;
using RowCycle.Application.Common;
using RowCycle.Application.Dtos;
using RowCycle.Application.Points;
using RowCycle.Domain.Enums;
using RowCycle.Infrastructure.Persistence;
using RowCycle.Tests.Infrastructure;

namespace RowCycle.Tests;

[Collection(ApiCollection.Name)]
public sealed class PointsTests(ApiFactory factory)
{
    [Fact]
    public async Task Balance_always_equals_the_sum_of_the_ledger()
    {
        var userId = await TestUsers.CreateAsync(factory);
        var order = Guid.NewGuid();

        await WithPoints(p => p.EarnAsync(userId, 10, PointsSourceType.Submission, Guid.NewGuid()));
        await WithPoints(p => p.EarnAsync(userId, 10, PointsSourceType.Submission, Guid.NewGuid()));
        await AdjustAsync(userId, 50);
        await WithPoints(p => p.RedeemAsync(userId, 30, order));
        await WithPoints(p => p.ReverseAsync(PointsSourceType.Order, order, "Order cancelled"));
        var last = await AdjustAsync(userId, -5);

        Assert.Equal(65, last.Balance);
        var (balance, sum) = await BalanceAndSumAsync(userId);
        Assert.Equal(65, balance);
        Assert.Equal(balance, sum);
    }

    [Fact]
    public async Task Spending_more_than_the_balance_is_rejected_and_writes_nothing()
    {
        var userId = await TestUsers.CreateAsync(factory);
        await AdjustAsync(userId, 20);

        var error = await Assert.ThrowsAsync<AppException>(() => WithPoints(p => p.RedeemAsync(userId, 50, Guid.NewGuid())));

        Assert.Equal(ErrorKind.Conflict, error.Kind);
        Assert.Equal("Not enough points.", error.Title);
        var (balance, sum) = await BalanceAndSumAsync(userId);
        Assert.Equal(20, balance);
        Assert.Equal(20, sum);
    }

    [Fact]
    public async Task Simultaneous_redeems_cannot_overspend()
    {
        var userId = await TestUsers.CreateAsync(factory);
        await AdjustAsync(userId, 200);

        // Five purchases of 100 at the same moment, each in its own request scope. Only two can fit in 200.
        var attempts = Enumerable.Range(0, 5)
            .Select(_ => Task.Run(async () =>
            {
                try
                {
                    await WithPoints(p => p.RedeemAsync(userId, 100, Guid.NewGuid()));
                    return true;
                }
                catch (AppException e) when (e.Title == "Not enough points.")
                {
                    return false;
                }
            }))
            .ToList();
        var results = await Task.WhenAll(attempts);

        Assert.Equal(2, results.Count(ok => ok));
        var (balance, sum) = await BalanceAndSumAsync(userId);
        Assert.Equal(0, balance);
        Assert.Equal(0, sum);
    }

    [Fact]
    public async Task The_same_submission_cannot_earn_twice()
    {
        var userId = await TestUsers.CreateAsync(factory);
        var submission = Guid.NewGuid();
        await WithPoints(p => p.EarnAsync(userId, 10, PointsSourceType.Submission, submission));

        var error = await Assert.ThrowsAsync<AppException>(
            () => WithPoints(p => p.EarnAsync(userId, 10, PointsSourceType.Submission, submission)));

        Assert.Equal(ErrorKind.Conflict, error.Kind);
        Assert.Equal(10, (await BalanceAndSumAsync(userId)).Balance);
    }

    [Fact]
    public async Task Reverse_gives_points_back_exactly_once()
    {
        var userId = await TestUsers.CreateAsync(factory);
        var order = Guid.NewGuid();
        await AdjustAsync(userId, 150);
        await WithPoints(p => p.RedeemAsync(userId, 100, order));

        var reversed = await WithPoints(p => p.ReverseAsync(PointsSourceType.Order, order, "Order cancelled"));
        Assert.Equal(100, reversed.Amount);
        Assert.Equal(150, reversed.Balance);

        var again = await Assert.ThrowsAsync<AppException>(
            () => WithPoints(p => p.ReverseAsync(PointsSourceType.Order, order, "Order cancelled")));
        Assert.Equal(ErrorKind.Conflict, again.Kind);
        Assert.Equal(150, (await BalanceAndSumAsync(userId)).Balance);
    }

    [Fact]
    public async Task Admin_adjustment_needs_a_reason()
    {
        var userId = await TestUsers.CreateAsync(factory);

        var error = await Assert.ThrowsAsync<AppException>(() => WithPoints(p => p.AdjustAsync(userId, 10, " ", ApiFactory.AdminUserId(factory))));

        Assert.Equal(ErrorKind.Validation, error.Kind);
    }

    [Fact]
    public async Task Points_join_the_callers_transaction_and_roll_back_with_it()
    {
        var userId = await TestUsers.CreateAsync(factory);
        await AdjustAsync(userId, 100);

        // Like a checkout that redeems points and then fails on stock: everything must be undone.
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var points = scope.ServiceProvider.GetRequiredService<IPointsService>();

            await Assert.ThrowsAsync<InvalidOperationException>(() => unitOfWork.ExecuteInTransactionAsync(async ct =>
            {
                await points.RedeemAsync(userId, 60, Guid.NewGuid(), cancellationToken: ct);
                throw new InvalidOperationException("Out of stock");
            }));
        }

        var (balance, sum) = await BalanceAndSumAsync(userId);
        Assert.Equal(100, balance);
        Assert.Equal(100, sum);
    }

    [Fact]
    public async Task Ledger_rows_cannot_be_changed()
    {
        var userId = await TestUsers.CreateAsync(factory);
        var change = await AdjustAsync(userId, 10);

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var entry = await db.PointsLedger.SingleAsync(e => e.Id == change.EntryId);
        entry.Amount = 1000;

        await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task Me_points_returns_balance_and_history_newest_first()
    {
        var client = factory.CreateClient();
        var email = $"points-{Guid.NewGuid():N}@example.com";
        await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest("Points Member", email, "Secret123"));
        var login = await (await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(email, "Secret123")))
            .Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login!.AccessToken);
        var me = await client.GetFromJsonAsync<MeResponse>("/api/v1/me");

        await WithPoints(p => p.EarnAsync(me!.Id, 10, PointsSourceType.Submission, Guid.NewGuid(), "Can collection"));
        await AdjustAsync(me!.Id, 25);
        await AdjustAsync(me.Id, -5);

        var response = await client.GetAsync("/api/v1/me/points?page=1&pageSize=2");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = body.RootElement;
        Assert.Equal(30, root.GetProperty("balance").GetInt32());
        Assert.Equal(3, root.GetProperty("total").GetInt32());
        Assert.Equal(2, root.GetProperty("pageSize").GetInt32());
        var items = root.GetProperty("items");
        Assert.Equal(2, items.GetArrayLength());
        Assert.Equal(-5, items[0].GetProperty("amount").GetInt32());
        Assert.Equal("Adjust", items[0].GetProperty("type").GetString());
        Assert.Equal("Manual", items[0].GetProperty("sourceType").GetString());

        var tooBig = await client.GetAsync("/api/v1/me/points?pageSize=500");
        Assert.Equal(HttpStatusCode.BadRequest, tooBig.StatusCode);
    }

    private async Task<T> WithPoints<T>(Func<IPointsService, Task<T>> action)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        return await action(scope.ServiceProvider.GetRequiredService<IPointsService>());
    }

    private Task<PointsChange> AdjustAsync(Guid userId, int amount) =>
        WithPoints(p => p.AdjustAsync(userId, amount, "Test adjustment", ApiFactory.AdminUserId(factory)));

    private async Task<(int Balance, int Sum)> BalanceAndSumAsync(Guid userId)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var balance = await db.UserProfiles.Where(p => p.UserId == userId).Select(p => p.PointsBalance).SingleAsync();
        var sum = await db.PointsLedger.Where(e => e.UserId == userId).SumAsync(e => e.Amount);
        return (balance, sum);
    }
}
