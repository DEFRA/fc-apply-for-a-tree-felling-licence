using FluentValidation.TestHelper;
using Forestry.Flo.External.Web.Models.FellingLicenceApplication;
using Forestry.Flo.External.Web.Services.Validation;
using Forestry.Flo.Tests.Common;

namespace Forestry.Flo.External.Web.Tests.Services.Validation
{
    public class DecisionToRestockViewModelValidatorTests
    {
        private readonly DecisionToRestockViewModelValidator _sut;

        public DecisionToRestockViewModelValidatorTests()
        {
            _sut = new DecisionToRestockViewModelValidator();
        }

        [Theory, AutoMoqData]
        public void ShouldValidateDecisionToRestockViewModel_WithSuccessIfReasonGiven(DecisionToRestockViewModel model)
        {
            model.IsRestockSelected = false;
            model.Reason = "Some reason";

            var result = _sut.TestValidate(model);

            result.ShouldNotHaveAnyValidationErrors();
        }

        [Theory, AutoMoqData]
        public void ShouldValidateDecisionToRestockViewModel_WithSuccessIfRestockSelected(DecisionToRestockViewModel model)
        {
            model.IsRestockSelected = true;
            model.Reason = string.Empty;

            var result = _sut.TestValidate(model);

            result.ShouldNotHaveAnyValidationErrors();
        }

        [Theory, AutoMoqData]
        public void ShouldReturnError_WhenValidateDecisionToRestockViewModel_GivenNoReasonToNotRestock(DecisionToRestockViewModel model)
        {
            model.IsRestockSelected = false;
            model.Reason = string.Empty;

            var result = _sut.TestValidate(model);

            result.ShouldHaveValidationErrorFor(x => x.Reason);
        }
    }
}
