using AutoFixture.Xunit2;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Applicants.Entities.Agent;
using Forestry.Flo.Services.Applicants.Models;
using Forestry.Flo.Services.Applicants.Repositories;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Tests.Common;
using Microsoft.Extensions.Logging.Abstractions;

namespace Forestry.Flo.Services.Applicants.Tests.Services;

public class AgencyCreationServiceTests
{
    private readonly Mock<IAgencyRepository> _mockRepository = new();
    private readonly Mock<IUnitOfWork> _mockUnitOfWork = new();

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
            a.OrganisationName == request.AgencyModel.OrganisationName && 
            a.Address == request.AgencyModel.Address && 
            a.ContactName == request.AgencyModel.ContactName && 
            a.ContactEmail == request.AgencyModel.ContactEmail),
            It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.VerifyNoOtherCalls();
    }
    
    [Fact]
    public async Task WhenRequestIsNull()
    {
        // arrange
        var sut = CreateSut();

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
        request.AgencyModel.AgencyId = null;
        var sut = CreateSut();
        
        _mockRepository
            .Setup(x => x.AddAgencyAsync(It.IsAny<Agency>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<Agency, UserDbErrorReason>(savedEntity));

        // act
        var result = await sut.AddAgencyAsync(request, CancellationToken.None);

        // assert
        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value.AgencyId);

        _mockRepository.Verify(x => x.AddAgencyAsync(It.Is<Agency>(a =>
                a.OrganisationName == request.AgencyModel.OrganisationName &&
                a.Address == request.AgencyModel.Address &&
                a.ContactName == request.AgencyModel.ContactName &&
                a.ContactEmail == request.AgencyModel.ContactEmail),
            It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Update_WhenRequestIsNull()
    {
        // arrange
        var sut = CreateSut();

        // act
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            sut.UpdateAgencyAsync(null!, CancellationToken.None));

        _mockRepository.VerifyNoOtherCalls();
        _mockUnitOfWork.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task Update_WhenAgencyNotFound(UpdateAgencyDetailsRequest request)
    {
        // arrange
        var sut = CreateSut();

        _mockRepository.Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<Agency, UserDbErrorReason>(UserDbErrorReason.NotFound));

        var result = await sut.UpdateAgencyAsync(request, CancellationToken.None);

        Assert.False(result.IsSuccess);

        _mockRepository.Verify(x => x.GetAsync(request.AgencyId, It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task Update_WhenSaveFails(
        UpdateAgencyDetailsRequest request,
        Agency agency)
    {
        // arrange
        var sut = CreateSut();

        _mockRepository.Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<Agency, UserDbErrorReason>(agency));

        _mockUnitOfWork.Setup(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Failure(UserDbErrorReason.General));

        var result = await sut.UpdateAgencyAsync(request, CancellationToken.None);

        Assert.False(result.IsSuccess);

        _mockRepository.Verify(x => x.GetAsync(request.AgencyId, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.VerifyGet(x => x.UnitOfWork, Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.VerifyNoOtherCalls();
        _mockRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task Update_WhenSuccessful(
        UpdateAgencyDetailsRequest request,
        Agency agency)
    {
        // arrange
        var sut = CreateSut();

        _mockRepository.Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<Agency, UserDbErrorReason>(agency));

        _mockUnitOfWork.Setup(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());

        var result = await sut.UpdateAgencyAsync(request, CancellationToken.None);

        Assert.True(result.IsSuccess);

        _mockRepository.Verify(x => x.GetAsync(request.AgencyId, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.VerifyGet(x => x.UnitOfWork, Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.VerifyNoOtherCalls();
        _mockRepository.VerifyNoOtherCalls();

        Assert.Equal(request.AgencyModel.IsOrganisation, agency.IsOrganisation);
        Assert.Equal(request.AgencyModel.OrganisationName, agency.OrganisationName);
        Assert.Equal(request.AgencyModel.ContactName, agency.ContactName);
        Assert.Equal(request.AgencyModel.ContactEmail, agency.ContactEmail);
        Assert.Equal(request.AgencyModel.Address.Line1, agency.Address.Line1);
        Assert.Equal(request.AgencyModel.Address.Line2, agency.Address.Line2);
        Assert.Equal(request.AgencyModel.Address.Line3, agency.Address.Line3);
        Assert.Equal(request.AgencyModel.Address.Line4, agency.Address.Line4);
        Assert.Equal(request.AgencyModel.Address.PostalCode, agency.Address.PostalCode);
    }

    [Theory, AutoData]
    public async Task Get_WhenAgencyNotFound(Guid agencyId)
    {
        var sut = CreateSut();

        _mockRepository.Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<Agency, UserDbErrorReason>(UserDbErrorReason.NotFound));

        var result = await sut.GetAgencyDetailsAsync(agencyId, CancellationToken.None);

        Assert.False(result.IsSuccess);

        _mockRepository.Verify(x => x.GetAsync(agencyId, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task Get_WhenAgencyFound(
        Guid agencyId,
        Agency agency)
    {
        var sut = CreateSut();

        _mockRepository.Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<Agency, UserDbErrorReason>(agency));

        var result = await sut.GetAgencyDetailsAsync(agencyId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(agency.Id, result.Value.AgencyId);
        Assert.Equal(agency.OrganisationName, result.Value.OrganisationName);
        Assert.Equal(agency.ContactName, result.Value.ContactName);
        Assert.Equal(agency.ContactEmail, result.Value.ContactEmail);
        Assert.Equal(agency.Address.Line1, result.Value.Address.Line1);
        Assert.Equal(agency.Address.Line2, result.Value.Address.Line2);
        Assert.Equal(agency.Address.Line3, result.Value.Address.Line3);
        Assert.Equal(agency.Address.Line4, result.Value.Address.Line4);
        Assert.Equal(agency.Address.PostalCode, result.Value.Address.PostalCode);

        _mockRepository.Verify(x => x.GetAsync(agencyId, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.VerifyNoOtherCalls();
    }

    private AgencyCreationService CreateSut()
    {
        _mockRepository.Reset();
        _mockUnitOfWork.Reset();

        _mockRepository.SetupGet(x => x.UnitOfWork).Returns(_mockUnitOfWork.Object);
        
        return new AgencyCreationService(
            _mockRepository.Object,
            new NullLogger<AgencyCreationService>());
    }
}