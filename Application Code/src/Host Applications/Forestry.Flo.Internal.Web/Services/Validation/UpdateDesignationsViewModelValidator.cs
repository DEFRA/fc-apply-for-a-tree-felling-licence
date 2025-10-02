using FluentValidation;
using Forestry.Flo.Internal.Web.Models.WoodlandOfficerReview;
using Forestry.Flo.Services.FellingLicenceApplications.Models.WoodlandOfficerReview;

namespace Forestry.Flo.Internal.Web.Services.Validation;

public class UpdateDesignationsViewModelValidator : AbstractValidator<UpdateDesignationsViewModel>
{
    public UpdateDesignationsViewModelValidator()
    {
        RuleFor(m => m.CompartmentDesignations)
            .Must(m =>
                ((m.Sssi || m.Sacs || m.Spa || m.Ramser || m.Sbi || m.Other) && !m.None)
                || m is { Sssi: false, Sacs: false, Spa: false, Ramser: false, Sbi: false, Other: false, None: true })
            .WithMessage("Select at least one designation or 'None'");
       
        When(m => m.CompartmentDesignations.Other, () =>
        {
            RuleFor(m => m.CompartmentDesignations.OtherDesignationDetails)
                .NotEmpty()
                .WithMessage("Enter the other designation details when 'Other designation' is selected");
        });
    }
}