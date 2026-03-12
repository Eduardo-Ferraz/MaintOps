using Industriall.MaintOps.Api.Common.Exceptions;
using Industriall.MaintOps.Api.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Industriall.MaintOps.Api.Features.WorkOrders.CompleteWorkOrder;

internal sealed class CompleteWorkOrderHandler
    : IRequestHandler<CompleteWorkOrderCommand, CompleteWorkOrderResponse>
{
    private readonly ApplicationDbContext _db;

    public CompleteWorkOrderHandler(ApplicationDbContext db) => _db = db;

    public async Task<CompleteWorkOrderResponse> Handle(
        CompleteWorkOrderCommand command,
        CancellationToken        cancellationToken)
    {
        // 1. Load the aggregate.
        var workOrder = await _db.WorkOrders
            .FirstOrDefaultAsync(wo => wo.Id == command.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(WorkOrder), command.Id);

        // 2. Domain Rule 1: Pending WorkOrders cannot be completed (enforced inside WorkOrder.Complete()).
        var result = workOrder.Complete();
        if (result.IsFailure)
            throw new DomainException(result.Error);

        // 3. Persist.
        await _db.SaveChangesAsync(cancellationToken);

        return new CompleteWorkOrderResponse(workOrder.Id, workOrder.Status.ToString());
    }
}
