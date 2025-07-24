using FluentValidation.TestHelper;
using Forestry.Flo.Internal.Web.Models.ExternalConsulteeInvite;
using Forestry.Flo.Internal.Web.Services.ExternalConsulteeReview.Validation;
using Forestry.Flo.Tests.Common;

namespace Forestry.Flo.Internal.Web.Tests.Services.ExternalConsulteeReview.Validators;

public class ExternalConsulteeInviteConfirmationModelValidatorTests
{
    private readonly ExternalConsulteeInviteConfirmationModelValidator _sut;
    
    public ExternalConsulteeInviteConfirmationModelValidatorTests()
    {
        _sut = new ExternalConsulteeInviteConfirmationModelValidator();
    }

    [Theory, AutoMoqData]
    public void ShouldValidateExternalConsulteeInviteConfirmationModel_WithSuccess_GivenValidModel(ExternalConsulteeInviteConfirmationModel inviteConfirmationModel)
    {
        //arrange
        inviteConfirmationModel.ConfirmedEmail = inviteConfirmationModel.Email;
        
        //act
        var result = _sut.TestValidate(inviteConfirmationModel);

        //assert
        result. ShouldNotHaveAnyValidationErrors();
    }
    
    [Theory, AutoMoqData]
    public void ShouldReturnError_WhenValidateExternalConsulteeInviteConfirmationModel_GivenNotValidModel(ExternalConsulteeInviteConfirmationModel inviteConfirmationModel, string email)
    {
        //arrange
        inviteConfirmationModel.ConfirmedEmail = email;
        
        //act
        var result = _sut.TestValidate(inviteConfirmationModel);

        //assert
        result.ShouldHaveValidationErrorFor(m => m.ConfirmedEmail);
    }
}