using AutoFixture.Xunit2;
using Forestry.Flo.External.Web.Services.Validation;
using FluentValidation.TestHelper;
using Forestry.Flo.External.Web.Models.FellingLicenceApplication;
using Forestry.Flo.Services.Common.Models;

namespace Forestry.Flo.External.Web.Tests.Services.Validation;

public class OperationDetailsValidatorTests
{
    private readonly OperationDetailsValidator _sut;

    public OperationDetailsValidatorTests()
    {
        _sut = new OperationDetailsValidator();
    }

    [Theory, AutoData]
    public void ShouldValidateValidOperationDetailsModel_WithSuccess(OperationDetailsModel operationDetails)
    {
        //Arrange
        var now = DateTime.Now.AddDays(1);
        operationDetails.ProposedFellingStart = new DatePart { Day = now.Day, Month = now.Month, Year = now.Year };
        operationDetails.ProposedFellingEnd = new DatePart { Day = now.Day, Month = now.Month, Year = now.Year };
        operationDetails.DisplayDateReceived = false;

        //Act
        var result = _sut.TestValidate(operationDetails);

        //Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
    
    [Theory, AutoData]
    public void ShouldReturnError_WhenValidatedOperationDetailsModel_NotValidProposedFellingStartDate(OperationDetailsModel operationDetails)
    {
        //Arrange
        operationDetails.ProposedFellingStart = new DatePart
        {
            Day = 32,
            Month = 13,
            Year = 0
        };
         
        //Act
        var result = _sut.TestValidate(operationDetails);

        //Assert
        result.ShouldHaveValidationErrorFor(o => o.ProposedFellingStart!.Day);
        result.ShouldHaveValidationErrorFor(o => o.ProposedFellingStart!.Month);
        result.ShouldHaveValidationErrorFor(o => o.ProposedFellingStart!.Year);
    }
    
    [Theory, AutoData]
    public void ShouldReturnError_WhenValidatedOperationDetailsModel_NotValidProposedFellingEndDate(OperationDetailsModel operationDetails)
    {
        //Arrange
        operationDetails.ProposedFellingEnd = new DatePart
        {
            Day = 32,
            Month = 13,
            Year = 0
        };
         
        //Act
        var result = _sut.TestValidate(operationDetails);

        //Assert
        result.ShouldHaveValidationErrorFor(o => o.ProposedFellingEnd!.Day);
        result.ShouldHaveValidationErrorFor(o => o.ProposedFellingEnd!.Month);
        result.ShouldHaveValidationErrorFor(o => o.ProposedFellingEnd!.Year);
    }
    
    [Theory, AutoData]
    public void ShouldReturnError_WhenValidatedOperationDetailsModel_GivenFellingStartDateInThePast(OperationDetailsModel operationDetails)
    {
        //Arrange
        operationDetails.ProposedFellingStart = new DatePart(DateTime.Now.AddYears(-1), "felling-start");
         
        //Act
        var result = _sut.TestValidate(operationDetails);

        //Assert
        result.ShouldHaveValidationErrorFor(o => o.ProposedFellingStart);
    }
    
    [Theory, AutoData]
    public void ShouldReturnError_WhenValidatedOperationDetailsModel_GivenFellingEndDateExistsBeforeStartDate(OperationDetailsModel operationDetails)
    {
        //Arrange
        operationDetails.ProposedFellingStart = new DatePart(DateTime.Now, "felling-start");
        operationDetails.ProposedFellingEnd = new DatePart(DateTime.Now.AddYears(-1), "felling-end");
         
        //Act
        var result = _sut.TestValidate(operationDetails);

        //Assert
        result.ShouldHaveValidationErrorFor(o => o.ProposedFellingEnd);
    }

    [Theory, AutoData]
    public void ShouldValid_WhenReceivedDateIsLeftBlank(OperationDetailsModel operationDetails)
    {
        //Arrange
        operationDetails.DisplayDateReceived = true;
        operationDetails.DateReceived = new DatePart();

        //Act
        var result = _sut.TestValidate(operationDetails);

        //Assert
        result.ShouldNotHaveValidationErrorFor(o => o.DateReceived);
    }

    [Theory, AutoData]
    public void ShouldReturnError_WhenReceivedDateInputtedAndInvalid(OperationDetailsModel operationDetails)
    {
        //Arrange
        operationDetails.DisplayDateReceived = true;
        operationDetails.DateReceived = new DatePart(DateTime.UtcNow.AddDays(10), "date-received");

        //Act
        var result = _sut.TestValidate(operationDetails);

        //Assert
        result.ShouldHaveValidationErrorFor(o => o.DateReceived);
    }


    [Theory, AutoData]
    public void ShouldBeValid_WhenDateReceivedIsCurrentDate(OperationDetailsModel operationDetails)
    {
        //Arrange
        operationDetails.DisplayDateReceived = true;
        operationDetails.DateReceived = new DatePart(DateTime.UtcNow, "date-received");

        //Act
        var result = _sut.TestValidate(operationDetails);

        //Assert
        result.ShouldNotHaveValidationErrorFor(o => o.DateReceived);
    }
}