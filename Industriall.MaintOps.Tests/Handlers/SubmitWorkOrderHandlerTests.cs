using FluentAssertions;
using Industriall.MaintOps.Api.Common.Exceptions;
using Industriall.MaintOps.Api.Domain.Entities;
using Industriall.MaintOps.Api.Domain.Enums;
using Industriall.MaintOps.Api.Features.WorkOrders.SubmitWorkOrder;
using Industriall.MaintOps.Api.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace Industriall.MaintOps.Tests.Handlers;

public sealed class SubmitWorkOrderHandlerTests
{
    private static ApplicationDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task Handle_WhenEquipmentExistsAndIsActive_ShouldCreateWorkOrder()
    {
        // Arrange
        await using var db = CreateInMemoryContext();

        var equipment = Equipment.Create("Pump A", CriticalityLevel.Medium).Value!;
        db.Equipment.Add(equipment);
        await db.SaveChangesAsync();

        var handler = new SubmitWorkOrderHandler(db);
        var command = new SubmitWorkOrderCommand(equipment.Id, "Inspect seals");

        // Act
        var response = await handler.Handle(command, CancellationToken.None);

        // Assert
        response.Should().NotBeNull();
        response.EquipmentId.Should().Be(equipment.Id);
        response.Status.Should().Be("Pending");

        db.WorkOrders.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_WhenEquipmentDoesNotExist_ShouldThrowNotFoundException()
    {
        // Arrange
        await using var db = CreateInMemoryContext();
        var handler = new SubmitWorkOrderHandler(db);
        var command = new SubmitWorkOrderCommand(Guid.NewGuid(), "Some description");

        // Act
        var act = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("*WorkOrder*");
    }

    [Fact]
    public async Task Handle_WhenEquipmentIsInactive_ShouldThrowNotFoundException()
    {
        // Arrange
        await using var db = CreateInMemoryContext();
        var equipment = Equipment.Create("Pump B", CriticalityLevel.Low).Value!;
        equipment.Deactivate();
        db.Equipment.Add(equipment);
        await db.SaveChangesAsync();

        var handler = new SubmitWorkOrderHandler(db);
        var command = new SubmitWorkOrderCommand(equipment.Id, "Check valve");

        // Act
        var act = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }
}
