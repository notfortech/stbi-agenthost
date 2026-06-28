using FluentValidation;
using StudioTechBI.AgentHostApplication.Abstractions;
using StudioTechBI.AgentHostApplication.Models.Requests;

namespace StudioTechBI.AgentHostApplication.Validation;

public sealed class BlueprintRequestValidatorAdapter : IBlueprintRequestValidator
{
    private readonly IValidator<BlueprintRequest> _validator;

    public BlueprintRequestValidatorAdapter(IValidator<BlueprintRequest> validator)
    {
        _validator = validator;
    }

    public ValidationReport Validate(BlueprintRequest request)
    {
        var result = _validator.Validate(request);
        var errors = result.Errors.Select(e => e.ErrorMessage).ToList();
        return new ValidationReport(result.IsValid, errors, []);
    }
}
