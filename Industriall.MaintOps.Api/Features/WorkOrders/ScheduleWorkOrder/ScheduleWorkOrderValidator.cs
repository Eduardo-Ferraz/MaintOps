namespace Industriall.MaintOps.Api.Features.WorkOrders.ScheduleWorkOrder;

public sealed class ScheduleWorkOrderValidator : AbstractValidator<ScheduleWorkOrderCommand>
{
    public ScheduleWorkOrderValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("WorkOrder Id is required.");

        RuleFor(x => x.StartDate)
            .NotEmpty().WithMessage("StartDate is required.")
            .GreaterThanOrEqualTo(DateTime.UtcNow.Date)
            .WithMessage("StartDate cannot be in the past.");

        RuleFor(x => x.EndDate)
            .NotEmpty().WithMessage("EndDate is required.")
            .GreaterThan(x => x.StartDate)
            .WithMessage("EndDate must be after StartDate.");
    }
}
