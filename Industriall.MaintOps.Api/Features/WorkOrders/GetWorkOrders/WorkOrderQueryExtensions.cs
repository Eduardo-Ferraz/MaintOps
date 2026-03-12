using Industriall.MaintOps.Api.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Industriall.MaintOps.Api.Features.WorkOrders.GetWorkOrders;

/// <summary>
/// IQueryable extension methods that emulate the Specification Pattern for
/// dynamic EF Core filtering – avoids hand-rolled Specification classes while
/// keeping the handler readable and all filter logic in one place.
/// </summary>
internal static class WorkOrderQueryExtensions
{
    public static IQueryable<WorkOrder> WithEquipment(
        this IQueryable<WorkOrder> query, Guid? equipmentId)
        => equipmentId.HasValue
            ? query.Where(wo => wo.EquipmentId == equipmentId.Value)
            : query;

    public static IQueryable<WorkOrder> WithStatus(
        this IQueryable<WorkOrder> query, WorkOrderStatus? status)
        => status.HasValue
            ? query.Where(wo => wo.Status == status.Value)
            : query;

    public static IQueryable<WorkOrder> WithDescriptionContaining(
        this IQueryable<WorkOrder> query, string? keyword)
        => !string.IsNullOrWhiteSpace(keyword)
            ? query.Where(wo => wo.Description.ToLower().Contains(keyword.ToLower()))
            : query;

    public static async Task<PagedResult<WorkOrderSummary>> ToPagedResultAsync(
        this IQueryable<WorkOrder> query,
        int               page,
        int               pageSize,
        CancellationToken cancellationToken)
    {
        page     = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(wo => wo.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(wo => new WorkOrderSummary(
                wo.Id,
                wo.EquipmentId,
                wo.Description,
                wo.Status.ToString(),
                wo.Schedule != null ? wo.Schedule.StartDate : (DateTime?)null,
                wo.Schedule != null ? wo.Schedule.EndDate   : (DateTime?)null))
            .ToListAsync(cancellationToken);

        return new PagedResult<WorkOrderSummary>(
            items,
            totalCount,
            page,
            pageSize,
            (int)Math.Ceiling(totalCount / (double)pageSize));
    }
}
