using AutoFixture.Xunit2;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Applicants.Entities.Agent;
using Forestry.Flo.Services.Applicants.Models;
using Forestry.Flo.Services.Applicants.Repositories;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Microsoft.Extensions.Logging.Abstractions;

namespace Forestry.Flo.Services.Applicants.Tests.Services;

public class AgencyCreationServiceTests
{
    private readonly Mock<IAgencyRepository> _mockRepository = new();

    [Theory, AutoData]
    public async Task WhenSaveToDatabaseFails(
        AddAgencyDetailsRequest request)
    {
        // arrange
        var sut = CreateSut();
       
        _mockRepository
            .Setup(x => x.AddAgencyAsync(It.IsAny<Agency>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<Agency, UserDbErrorReason>(UserDbErrorReason.NotUnique));

        // act
        var result = await sut.AddAgencyAsync(request, CancellationToken.None);

        // assert
        Assert.True(result.IsFailure);

        _mockRepository.Verify(x => x.AddAgencyAsync(It.Is<Agency>(a =>
            a.OrganisationName == request.agencyModel.OrganisationName && 
            a.Address == request.agencyModel.Address && 
            a.ContactName == request.agencyModel.ContactName && 
            a.ContactEmail == request.agencyModel.ContactEmail),
            It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.VerifyNoOtherCalls();
    }
    
    [Fact]
    public async Task WhenRequestIsNull()
    {
        // arrange
        var sut = CreateSut();
       
        _mockRepository
            .Setup(x => x.AddAgencyAsync(It.IsAny<Agency>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<Agency, UserDbErrorReason>(UserDbErrorReason.NotUnique));

        // act
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            sut.AddAgencyAsync(null!, CancellationToken.None));
    }

    [Theory, AutoData]
    public async Task WhenSuccessful(
        AddAgencyDetailsRequest request,
        Agency savedEntity)
    {
        // arrange
        request.agencyModel.AgencyId = null;
        var sut = CreateSut();
        
        _mockRepository
            .Setup(x => x.AddAgencyAsync(It.IsAny<Agency>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<Agency, UserDbErrorReason>(savedEntity));

        // act
        var result = await sut.AddAgencyAsync(request, CancellationToken.None);

        // assert
        Assert.True(result.IsSuccess);
        result.Value.AgencyId.Should().NotBeEmpty();

        _mockRepository.Verify(x => x.AddAgencyAsync(It.Is<Agency>(a =>
                a.OrganisationName == request.agencyModel.OrganisationName &&
                a.Address == request.agencyModel.Address &&
                a.ContactName == request.agencyModel.ContactName &&
                a.ContactEmail == request.agencyModel.ContactEmail),
            It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.VerifyNoOtherCalls();
    }

    private AgencyCreationService CreateSut()
    {
        _mockRepository.Reset();
        
        return new AgencyCreationService(
            _mockRepository.Object,
            new NullLogger<AgencyCreationService>());
    }
}