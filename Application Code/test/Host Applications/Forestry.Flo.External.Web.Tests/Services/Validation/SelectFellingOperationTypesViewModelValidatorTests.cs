using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation.TestHelper;
using Forestry.Flo.External.Web.Models.FellingLicenceApplication;
using Forestry.Flo.External.Web.Services.Validation;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Tests.Common;

namespace Forestry.Flo.External.Web.Tests.Services.Validation
{
    public class SelectFellingOperationTypesViewModelValidatorTests
    {
        private readonly SelectFellingOperationTypesViewModelValidator _sut;

        public SelectFellingOperationTypesViewModelValidatorTests()
        {
            _sut = new SelectFellingOperationTypesViewModelValidator();
        }

        [Theory, AutoMoqData]
        public void ShouldValidateSelectFellingOperationTypesViewModel_WithSuccess(SelectFellingOperationTypesViewModel model)
        {
            model.OperationTypes = new List<FellingOperationType>() { FellingOperationType.Thinning };

            var result = _sut.TestValidate(model);

            result.ShouldNotHaveAnyValidationErrors();
        }

        [Theory, AutoMoqData]
        public void ShouldReturnError_WhenValidateSelectFellingOperationTypesViewModel_GivenNoOperationTypes(SelectFellingOperationTypesViewModel model)
        {
            model.OperationTypes = new List<FellingOperationType>();

            var result = _sut.TestValidate(model);

            result.ShouldHaveValidationErrorFor(x => x.OperationTypes);
        }
    }
}
