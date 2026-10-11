using AutoMapper;
using FluentValidation;
using RowCycle.Application.Audit;
using RowCycle.Application.Common;
using RowCycle.Application.Dtos;

namespace RowCycle.Application.Orders;

internal sealed class OrderAdminService(
    IOrderRepository orders,
    OrderWorkflow workflow,
    IAuditLogService audit,
    IUnitOfWork unitOfWork,
    IValidator<OrderQuery> queryValidator,
    IValidator<ChangeOrderStatusRequest> statusValidator,
    IValidator<PageRequest> pageValidator,
    IMapper mapper) : IOrderAdminService
{
    public async Task<PagedResult<OrderSummaryResponse>> SearchAsync(OrderQuery query, PageRequest page, CancellationToken cancellationToken = default)
    {
        await queryValidator.ValidateAndThrowAsync(query, cancellationToken);
        await pageValidator.ValidateAndThrowAsync(page, cancellationToken);
        return (await orders.SearchAsync(null, query, page, cancellationToken)).Map(mapper.Map<OrderSummaryResponse>);
    }

    public async Task<OrderResponse> GetAsync(Guid orderId, CancellationToken cancellationToken = default) =>
        mapper.Map<OrderResponse>(await orders.FindAsync(orderId, cancellationToken) ?? throw AppException.NotFound("Order not found."));

    public async Task<OrderResponse> ChangeStatusAsync(
        Guid orderId, ChangeOrderStatusRequest request, Guid staffId, CancellationToken cancellationToken = default)
    {
        await statusValidator.ValidateAndThrowAsync(request, cancellationToken);

        await unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var order = await orders.LockAsync(orderId, ct) ?? throw AppException.NotFound("Order not found.");
            var from = order.Status;
            await workflow.MoveAsync(order, request.Status, staffId, ct);
            audit.Record(staffId, AuditActions.OrderStatusChange, "order", orderId.ToString(),
                new { order.OrderNumber, from, to = request.Status });
        }, cancellationToken);

        return await GetAsync(orderId, cancellationToken);
    }
}

public sealed class OrderQueryValidator : AbstractValidator<OrderQuery>
{
    public OrderQueryValidator()
    {
        RuleFor(x => x.Status).IsInEnum().When(x => x.Status is not null);
        RuleFor(x => x.Search).MaximumLength(100);
    }
}

public sealed class ChangeOrderStatusRequestValidator : AbstractValidator<ChangeOrderStatusRequest>
{
    public ChangeOrderStatusRequestValidator() => RuleFor(x => x.Status).IsInEnum();
}
