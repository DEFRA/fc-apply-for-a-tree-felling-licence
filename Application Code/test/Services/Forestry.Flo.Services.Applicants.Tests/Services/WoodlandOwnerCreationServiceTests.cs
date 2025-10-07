using AutoFixture.Xunit2;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Applicants.Entities.WoodlandOwner;
using Forestry.Flo.Services.Applicants.Models;
using Forestry.Flo.Services.Applicants.Repositories;
using Forestry.Flo.Services.Applicants.Services;
using Forestry.Flo.Services.Common;
using Microsoft.Extensions.Logging.Abstractions;

namespace Forestry.Flo.Services.Applicants.Tests.Services;

public class WoodlandOwnerCreationServiceTests
{
    private readonly Mock<IWoodlandOwnerRepository> _mockRepository = new();

    [Theory, AutoData]
    public async Task WhenSaveToDatabaseFails(
        AddWoodlandOwnerDetailsRequest request)
    {
        // arrange
        var sut = CreateSut();
       
        _mockRepository
            .Setup(x => x.AddWoodlandOwnerAsync(It.IsAny<WoodlandOwner>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<WoodlandOwner, UserDbErrorReason>(UserDbErrorReason.NotUnique));

        // act
        var result = await sut.AddWoodlandOwnerDetails(request, CancellationToken.None);

        // assert
        Assert.True(result.IsFailure);

        _mockRepository.Verify(x => x.AddWoodlandOwnerAsync(It.Is<WoodlandOwner>(a =>
            a.OrganisationName == request.WoodlandOwner.OrganisationName && 
            a.ContactAddress == request.WoodlandOwner.ContactAddress && 
            a.ContactName == request.WoodlandOwner.ContactName && 
            a.ContactEmail == request.WoodlandOwner.ContactEmail && 
            a.ContactTelephone == request.WoodlandOwner.ContactTelephone &&
            a.IsOrganisation == request.WoodlandOwner.IsOrganisation &&
            a.OrganisationAddress == request.WoodlandOwner.OrganisationAddress),
            It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.VerifyNoOtherCalls();
    }
    
    [Fact]
    public async Task WhenRequestIsNull()
    {
        // arrange
        var sut = CreateSut();
       
        _mockRepository
            .Setup(x => x.AddWoodlandOwnerAsync(It.IsAny<WoodlandOwner>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<WoodlandOwner, UserDbErrorReason>(UserDbErrorReason.NotUnique));

        // act
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            sut.AddWoodlandOwnerDetails(null!, CancellationToken.None));
    }

    [Theory, AutoData]
    public async Task WhenSuccessful(
        AddWoodlandOwnerDetailsRequest request,
        WoodlandOwner savedEntity)
    {
        // arrange
        request.WoodlandOwner.Id = null;
        var sut = CreateSut();
        
        _mockRepository
            .Setup(x => x.AddWoodlandOwnerAsync(It.IsAny<WoodlandOwner>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<WoodlandOwner, UserDbErrorReason>(savedEntity));

        // act
        var result = await sut.AddWoodlandOwnerDetails(request, CancellationToken.None);

        // assert
        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value.WoodlandOwnerId);

        _mockRepository.Verify(x => x.AddWoodlandOwnerAsync(It.Is<WoodlandOwner>(a =>
                a.OrganisationName == request.WoodlandOwner.OrganisationName && 
                a.ContactAddress == request.WoodlandOwner.ContactAddress && 
                a.ContactName == request.WoodlandOwner.ContactName && 
                a.ContactEmail == request.WoodlandOwner.ContactEmail && 
                a.ContactTelephone == request.WoodlandOwner.ContactTelephone &&
                a.IsOrganisation == request.WoodlandOwner.IsOrganisation &&
                a.OrganisationAddress == request.WoodlandOwner.OrganisationAddress),
            It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task ReturnsTrue_WhenWoodlandOwnerUpdated(WoodlandOwnerModel model, WoodlandOwner owner)
    {
        var sut = CreateSut();

        _mockRepository.Setup(s => s.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(owner);

        _mockRepository.Setup(s => s.UnitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var result = await sut.AmendWoodlandOwnerDetailsAsync(model, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value);

        // assert values set correctly

        Assert.Equal(model.ContactAddress, owner.ContactAddress);
        Assert.Equal(model.ContactEmail, owner.ContactEmail);
        Assert.Equal(model.ContactName, owner.ContactName);
        Assert.Equal(model.ContactTelephone, owner.ContactTelephone);
        Assert.Equal(model.IsOrganisation, owner.IsOrganisation);
        Assert.Equal(model.OrganisationAddress, owner.OrganisationAddress);
        Assert.Equal(model.OrganisationName, owner.OrganisationName);

        _mockRepository.Verify(v => v.GetAsync(model.Id.Value, CancellationToken.None), Times.Once);
        _mockRepository.Verify(v => v.UnitOfWork.SaveChangesAsync(CancellationToken.None), Times.Once);
    }

    [Theory, AutoData]
    public async Task ReturnsFalse_WhenWoodlandOwnerUnchanged(WoodlandOwner owner)
    {
        var sut = CreateSut();

        _mockRepository.Setup(s => s.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(owner);

        _mockRepository.Setup(s => s.UnitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var model = new WoodlandOwnerModel
        {
            ContactAddress = owner.ContactAddress,
            ContactEmail = owner.ContactEmail,
            ContactName = owner.ContactName,
            ContactTelephone = owner.ContactTelephone,
            IsOrganisation = owner.IsOrganisation,
            Id = owner.Id,
            OrganisationAddress = owner.OrganisationAddress,
            OrganisationName = owner.OrganisationName
        };

        var result = await sut.AmendWoodlandOwnerDetailsAsync(model, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value);

        _mockRepository.Verify(v => v.GetAsync(model.Id.Value, CancellationToken.None), Times.Once);
        _mockRepository.Verify(v => v.UnitOfWork.SaveChangesAsync(CancellationToken.None), Times.Once);
    }

    [Theory, AutoData]
    public async Task ReturnsFailure_WhenWoodlandOwnerNotRetrieved(WoodlandOwnerModel model)
    {
        var sut = CreateSut();

        _mockRepository.Setup(s => s.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<WoodlandOwner,UserDbErrorReason>(UserDbErrorReason.General));

        var result = await sut.AmendWoodlandOwnerDetailsAsync(model, CancellationToken.None);

        Assert.True(result.IsFailure);

        _mockRepository.Verify(v => v.GetAsync(model.Id.Value, CancellationToken.None), Times.Once);
        _mockRepository.Verify(v => v.UnitOfWork.SaveChangesAsync(CancellationToken.None), Times.Never);
    }

    private WoodlandOwnerCreationService CreateSut()
    {
        _mockRepository.Reset();
        
        return new WoodlandOwnerCreationService(
            _mockRepository.Object,
            new NullLogger<WoodlandOwnerCreationService>());
    }
}