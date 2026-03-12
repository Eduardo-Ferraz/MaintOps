namespace Industriall.MaintOps.Api.Features.WorkOrders.CompleteWorkOrder;

public sealed class CompleteWorkOrderValidator : AbstractValidator<CompleteWorkOrderCommand>
{
    public CompleteWorkOrderValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("WorkOrder Id is required.");
    }
}
