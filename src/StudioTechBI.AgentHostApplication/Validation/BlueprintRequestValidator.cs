using FluentValidation;
using StudioTechBI.AgentHostApplication.Models.Requests;

namespace StudioTechBI.AgentHostApplication.Validation;

public sealed class BlueprintRequestValidator : AbstractValidator<BlueprintRequest>
{
    public BlueprintRequestValidator()
    {
        RuleFor(x => x.Industry).NotEmpty().MaximumLength(200);
        RuleFor(x => x.BusinessCapability).NotEmpty().MaximumLength(200);
        RuleFor(x => x.BusinessGoal).NotEmpty().MaximumLength(500);
        RuleFor(x => x.BusinessRequirements).NotEmpty().MaximumLength(5000);
        RuleFor(x => x.Temperature).InclusiveBetween(0.0, 2.0).When(x => x.Temperature.HasValue);
        RuleFor(x => x.MaxTokens).InclusiveBetween(256, 8192).When(x => x.MaxTokens.HasValue);
    }
}
