namespace Industriall.MaintOps.Api.Features.WorkOrders.SubmitWorkOrder;

public sealed class SubmitWorkOrderValidator : AbstractValidator<SubmitWorkOrderCommand>
{
    public SubmitWorkOrderValidator()
    {
        RuleFor(x => x.EquipmentId)
            .NotEmpty().WithMessage("EquipmentId is required.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.")
            .MaximumLength(1000).WithMessage("Description cannot exceed 1000 characters.");
    }
}
