using FluentValidation.TestHelper;
using Forestry.Flo.External.Web.Models.FellingLicenceApplication;
using Forestry.Flo.External.Web.Services.Validation;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Tests.Common;

namespace Forestry.Flo.External.Web.Tests.Services.Validation
{
    public class SelectRestockingOptionsViewModelValidatorTests
    {
        private readonly SelectRestockingOptionsViewModelValidator _sut;

        public SelectRestockingOptionsViewModelValidatorTests()
        {
            _sut = new SelectRestockingOptionsViewModelValidator();
        }

        [Theory,AutoMoqData]
        public void ShouldValidateSelectRestockingOptionsViewModel_WithSuccess(SelectRestockingOptionsViewModel model)
        {
            model.RestockingOptions = new List<TypeOfProposal>() { TypeOfProposal.ReplantTheFelledArea };

            var result = _sut.TestValidate(model);

            result.ShouldNotHaveAnyValidationErrors();
        }

        [Theory, AutoMoqData]
        public void ShouldReturnError_WhenValidateSelectRestockingOptionsViewModel_GivenNoRestockingOptions(SelectRestockingOptionsViewModel model)
        {
            model.RestockingOptions = new List<TypeOfProposal>();

            var result = _sut.TestValidate(model);

            result.ShouldHaveValidationErrorFor(x => x.RestockingOptions);
        }
    }
}
