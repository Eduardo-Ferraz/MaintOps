using FluentAssertions;
using Industriall.MaintOps.Api.Common.Exceptions;
using Industriall.MaintOps.Api.Domain.Entities;
using Industriall.MaintOps.Api.Domain.Enums;
using Industriall.MaintOps.Api.Domain.ValueObjects;
using Industriall.MaintOps.Api.Features.WorkOrders.CompleteWorkOrder;
using Industriall.MaintOps.Api.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Industriall.MaintOps.Tests.Handlers;

public sealed class CompleteWorkOrderHandlerTests
{
    private static ApplicationDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task Handle_WhenWorkOrderIsPending_ShouldThrowDomainException()
    {
        // Arrange
        await using var db = CreateInMemoryContext();
        var workOrder = WorkOrder.Create(Guid.NewGuid(), "Pending task").Value!;
        db.WorkOrders.Add(workOrder);
        await db.SaveChangesAsync();

        var handler = new CompleteWorkOrderHandler(db);
        var command = new CompleteWorkOrderCommand(workOrder.Id);

        // Act
        var act = async () => await handler.Handle(command, CancellationToken.None);

        // Assert – Domain Rule 1
        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*Pending*");
    }

    [Fact]
    public async Task Handle_WhenWorkOrderIsScheduled_ShouldComplete()
    {
        // Arrange
        await using var db = CreateInMemoryContext();
        var workOrder = WorkOrder.Create(Guid.NewGuid(), "Scheduled task").Value!;
        var schedule  = MaintenanceSchedule.Create(
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(2)).Value!;
        workOrder.AssignSchedule(schedule);
        db.WorkOrders.Add(workOrder);
        await db.SaveChangesAsync();

        var handler = new CompleteWorkOrderHandler(db);
        var command = new CompleteWorkOrderCommand(workOrder.Id);

        // Act
        var response = await handler.Handle(command, CancellationToken.None);

        // Assert
        response.Status.Should().Be("Completed");
    }

    [Fact]
    public async Task Handle_WhenWorkOrderDoesNotExist_ShouldThrowNotFoundException()
    {
        await using var db = CreateInMemoryContext();
        var handler = new CompleteWorkOrderHandler(db);

        var act = async () => await handler.Handle(
            new CompleteWorkOrderCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
