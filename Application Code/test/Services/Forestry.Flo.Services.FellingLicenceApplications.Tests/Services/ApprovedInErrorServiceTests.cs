using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Forestry.Flo.Services.Gis.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NodaTime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.Xunit2;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Tests.Common;
using Xunit;

namespace Forestry.Flo.Services.FellingLicenceApplications.Tests.Services;

public class ApprovedInErrorServiceTests
{
    private readonly Mock<IFellingLicenceApplicationInternalRepository> _repo = new();
    private readonly Mock<IClock> _clock = new();
    private readonly Mock<IPublicRegister> _publicRegister = new();
    private readonly Mock<IUpdateFellingLicenceApplication> _updateFellingLicenceApplicationService = new();
    private readonly DateTime _now = DateTime.UtcNow;
    private readonly Fixture _fixture;

    public ApprovedInErrorServiceTests()
    {
        _fixture = new Fixture();
        // Configure AutoFixture to handle circular references
        _fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
            .ForEach(b => _fixture.Behaviors.Remove(b));
        _fixture.Behaviors.Add(new OmitOnRecursionBehavior());
    }

    private ApprovedInErrorService CreateSut()
    {
        _repo.Reset();
        _clock.Setup(x => x.GetCurrentInstant()).Returns(Instant.FromDateTimeUtc(_now));
        return new ApprovedInErrorService(
            _repo.Object, 
            _clock.Object, 
            new NullLogger<ApprovedInErrorService>(),
            _publicRegister.Object,
            _updateFellingLicenceApplicationService.Object);
    }

    [Theory, AutoData]
    public async Task GetApprovedInError_WhenNoRecordExists_ReturnsNone(Guid applicationId)
    {
        var sut = CreateSut();
        _repo.Setup(x => x.GetApprovedInErrorAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(Maybe<ApprovedInError>.None);
        var result = await sut.GetApprovedInErrorAsync(applicationId, CancellationToken.None);
        Assert.True(result.HasNoValue);
        _repo.Verify(x => x.GetApprovedInErrorAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _repo.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task GetApprovedInError_WhenRecordExists_ReturnsModel(Guid applicationId, ApprovedInError entity)
    {
        var sut = CreateSut();
        _repo.Setup(x => x.GetApprovedInErrorAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(Maybe.From(entity));
        var result = await sut.GetApprovedInErrorAsync(applicationId, CancellationToken.None);
        Assert.True(result.HasValue);
        Assert.Equal(entity.Id, result.Value.Id);
        Assert.Equal(entity.FellingLicenceApplicationId, result.Value.ApplicationId);
        Assert.Equal(entity.PreviousReference, result.Value.PreviousReference);
        Assert.Equal(entity.ReasonExpiryDate, result.Value.ReasonExpiryDate);
        Assert.Equal(entity.ReasonSupplementaryPoints, result.Value.ReasonSupplementaryPoints);
        Assert.Equal(entity.ReasonOther, result.Value.ReasonOther);
        Assert.Equal(entity.ReasonExpiryDateText, result.Value.ReasonExpiryDateText);
        _repo.Verify(x => x.GetApprovedInErrorAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _repo.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task SaveApprovedInError_WhenModelIsNull_ThrowsException(Guid applicationId, Guid userId)
    {
        var sut = CreateSut();
        await Assert.ThrowsAsync<ArgumentNullException>(() => sut.SetToApprovedInErrorAsync(applicationId, null!, userId, CancellationToken.None));
        _repo.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task SaveApprovedInError_CreatesNewEntity_WhenNoneExists(Guid applicationId, ApprovedInErrorModel model, Guid userId)
    {
        var sut = CreateSut();
        model.ApplicationId = applicationId;
        model.ReasonOther = true; // Skip reference regeneration

        // Mock the application with Approved status and NO documents
        var application = new FellingLicenceApplication
        {
            Id = applicationId,
            StatusHistories = new List<StatusHistory>
            {
                new StatusHistory { Status = FellingLicenceStatus.Approved, Created = DateTime.UtcNow }
            },
            Documents = new List<Document>() // Empty document list
        };

        var mockUnitOfWork = new Mock<IUnitOfWork>();
        _repo.SetupGet(x => x.UnitOfWork).Returns(mockUnitOfWork.Object);
        mockUnitOfWork.Setup(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()))
        .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());
        
        _repo.Setup(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()))
        .ReturnsAsync(Maybe<FellingLicenceApplication>.From(application));
        
        _repo.Setup(x => x.GetApprovedInErrorAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(Maybe<ApprovedInError>.None);
        
        _repo.Setup(x => x.AddOrUpdateApprovedInErrorAsync(It.IsAny<ApprovedInError>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());

        _repo.Setup(x => x.AddStatusHistory(userId, applicationId, FellingLicenceStatus.ApprovedInError, It.IsAny<CancellationToken>()))
        .Returns(Task.CompletedTask);

        var result = await sut.SetToApprovedInErrorAsync(applicationId, model, userId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        _repo.Verify(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _repo.Verify(x => x.GetApprovedInErrorAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _repo.Verify(x => x.AddOrUpdateApprovedInErrorAsync(It.Is<ApprovedInError>(e =>
            e.FellingLicenceApplicationId == applicationId &&
            e.PreviousReference == model.PreviousReference &&
            e.ReasonExpiryDate == model.ReasonExpiryDate &&
            e.ReasonSupplementaryPoints == model.ReasonSupplementaryPoints &&
            e.ReasonOther == true ? string.IsNullOrEmpty(e.PreviousReference) : e.ReasonOther == model.ReasonOther &&
            e.ReasonExpiryDateText == model.ReasonExpiryDateText &&
            e.LastUpdatedById == userId &&
            e.LastUpdatedDate == _now), It.IsAny<CancellationToken>()), Times.Once);
        _repo.Verify(x => x.AddStatusHistory(userId, applicationId, FellingLicenceStatus.ApprovedInError, It.IsAny<CancellationToken>()), Times.Once);
        // UpdateDocumentVisibleToApplicantAsync should NOT be called when there are no documents
        _repo.Verify(x => x.UpdateDocumentVisibleToApplicantAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
        mockUnitOfWork.Verify(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task SaveApprovedInError_UpdatesExistingEntity_WhenExists(Guid applicationId, ApprovedInErrorModel model, ApprovedInError existingEntity, Guid userId)
    {
        var sut = CreateSut();
        model.ApplicationId = applicationId;
        model.ReasonOther = true; // Skip reference regeneration

        // Mock the application with Approved status
        var application = new FellingLicenceApplication
        {
            Id = applicationId,
            StatusHistories = new List<StatusHistory>
            {
                new StatusHistory { Status = FellingLicenceStatus.Approved, Created = DateTime.UtcNow }
            },
            Documents = new List<Document>()
        };

        var mockUnitOfWork = new Mock<IUnitOfWork>();
        _repo.SetupGet(x => x.UnitOfWork).Returns(mockUnitOfWork.Object);
        mockUnitOfWork.Setup(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()))
        .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());
        
        _repo.Setup(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()))
        .ReturnsAsync(Maybe<FellingLicenceApplication>.From(application));
        
        _repo.Setup(x => x.GetApprovedInErrorAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(Maybe<ApprovedInError>.From(existingEntity));
        
        _repo.Setup(x => x.AddOrUpdateApprovedInErrorAsync(It.IsAny<ApprovedInError>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());

        _repo.Setup(x => x.AddStatusHistory(userId, applicationId, FellingLicenceStatus.ApprovedInError, It.IsAny<CancellationToken>()))
        .Returns(Task.CompletedTask);

        var result = await sut.SetToApprovedInErrorAsync(applicationId, model, userId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        _repo.Verify(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _repo.Verify(x => x.GetApprovedInErrorAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _repo.Verify(x => x.AddOrUpdateApprovedInErrorAsync(existingEntity, It.IsAny<CancellationToken>()), Times.Once);

        Assert.Equal(applicationId, existingEntity.FellingLicenceApplicationId);

        // PreviousReference is cleared when ReasonOther is true
        Assert.True(model.ReasonOther == true
            ? string.IsNullOrEmpty(existingEntity.PreviousReference)
            : existingEntity.PreviousReference == model.PreviousReference);

        Assert.Equal(model.ReasonExpiryDate, existingEntity.ReasonExpiryDate);
        Assert.Equal(model.ReasonSupplementaryPoints, existingEntity.ReasonSupplementaryPoints);
        Assert.Equal(model.ReasonOther, existingEntity.ReasonOther);
        Assert.Equal(model.ReasonExpiryDateText, existingEntity.ReasonExpiryDateText);
        Assert.Equal(userId, existingEntity.LastUpdatedById);
        Assert.Equal(_now, existingEntity.LastUpdatedDate);

        _repo.Verify(x => x.AddStatusHistory(userId, applicationId, FellingLicenceStatus.ApprovedInError, It.IsAny<CancellationToken>()), Times.Once);
        mockUnitOfWork.Verify(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _repo.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task SaveApprovedInError_WhenSaveFails_ReturnsFailure(Guid applicationId, ApprovedInErrorModel model, Guid userId)
    {
        var sut = CreateSut();
        model.ApplicationId = applicationId;
        model.ReasonOther = true;

        // Mock the application with Approved status
        var application = new FellingLicenceApplication
        {
            Id = applicationId,
            StatusHistories = new List<StatusHistory>
            {
                new StatusHistory { Status = FellingLicenceStatus.Approved, Created = DateTime.UtcNow }
            },
            Documents = new List<Document>()
        };

        _repo.Setup(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()))
        .ReturnsAsync(Maybe<FellingLicenceApplication>.From(application));
        
        _repo.Setup(x => x.GetApprovedInErrorAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(Maybe<ApprovedInError>.None);
        
        _repo.Setup(x => x.AddOrUpdateApprovedInErrorAsync(It.IsAny<ApprovedInError>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(UnitResult.Failure(UserDbErrorReason.General));

        var result = await sut.SetToApprovedInErrorAsync(applicationId, model, userId, CancellationToken.None);

        Assert.True(result.IsFailure);
        _repo.Verify(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _repo.Verify(x => x.GetApprovedInErrorAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _repo.Verify(x => x.AddOrUpdateApprovedInErrorAsync(It.IsAny<ApprovedInError>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoData]
    public async Task SaveApprovedInError_WhenSaveThrows_ReturnsFailure(Guid applicationId, ApprovedInErrorModel model, Guid userId)
    {
        var sut = CreateSut();
        model.ApplicationId = applicationId;
        model.ReasonOther = true;

        // Mock the application with Approved status
        var application = new FellingLicenceApplication
        {
            Id = applicationId,
            StatusHistories = new List<StatusHistory>
            {
                new StatusHistory { Status = FellingLicenceStatus.Approved, Created = DateTime.UtcNow }
            },
            Documents = new List<Document>()
        };

        _repo.Setup(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()))
        .ReturnsAsync(Maybe<FellingLicenceApplication>.From(application));
        
        _repo.Setup(x => x.GetApprovedInErrorAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(Maybe<ApprovedInError>.None);
        
        _repo.Setup(x => x.AddOrUpdateApprovedInErrorAsync(It.IsAny<ApprovedInError>(), It.IsAny<CancellationToken>()))
        .ThrowsAsync(new Exception("Test exception"));

        var result = await sut.SetToApprovedInErrorAsync(applicationId, model, userId, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("Test exception", result.Error);
        _repo.Verify(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _repo.Verify(x => x.GetApprovedInErrorAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Once);
        _repo.Verify(x => x.AddOrUpdateApprovedInErrorAsync(It.IsAny<ApprovedInError>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task SaveApprovedInError_RegeneratesReference_WhenReasonOtherIsFalse(Guid applicationId, ApprovedInErrorModel model, FellingLicenceApplication application, Guid userId)
    {
        model.ApplicationId = applicationId;
        model.ReasonOther = false;
        model.PreviousReference = "ABC/2024/12345";
        // Service extracts prefix from the application's current reference
        application.ApplicationReference = model.PreviousReference;
        application.StatusHistories = new List<StatusHistory>
        {
            new StatusHistory { Status = FellingLicenceStatus.Approved, Created = DateTime.UtcNow }
        };
        application.Documents = new List<Document>();
        
        var mockReferenceRepo = new Mock<IFellingLicenceApplicationReferenceRepository>();
        var mockReferenceHelper = new Mock<IApplicationReferenceHelper>();
        var mockUnitOfWork = new Mock<IUnitOfWork>();
        
        _repo.Setup(x => x.GetApprovedInErrorAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(Maybe<ApprovedInError>.None);
        
        _repo.Setup(x => x.AddOrUpdateApprovedInErrorAsync(It.IsAny<ApprovedInError>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());

        _repo.Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(Maybe<FellingLicenceApplication>.From(application));
        
        _repo.SetupGet(x => x.UnitOfWork).Returns(mockUnitOfWork.Object);
        
        mockReferenceRepo.Setup(x => x.GetNextApplicationReferenceIdValueAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(12345L);
        
        mockReferenceHelper.Setup(x => x.GenerateReferenceNumber(It.IsAny<FellingLicenceApplication>(), It.IsAny<long>(), It.IsAny<string>(), It.IsAny<int?>()))
        .Returns("NEW/2024/12345");
        
        mockReferenceHelper.Setup(x => x.UpdateReferenceNumber(It.IsAny<string>(), It.IsAny<string>()))
        .Returns("ABC/2024/12345");
        
        mockUnitOfWork.Setup(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()))
        .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());

        _repo.Setup(x => x.AddStatusHistory(userId, applicationId, FellingLicenceStatus.ApprovedInError, It.IsAny<CancellationToken>()))
        .Returns(Task.CompletedTask);

        var sutWithRegeneration = new ApprovedInErrorService(
        _repo.Object,
        _clock.Object,
        new NullLogger<ApprovedInErrorService>(),
        _publicRegister.Object,
        _updateFellingLicenceApplicationService.Object,
        mockReferenceRepo.Object,
        mockReferenceHelper.Object);
        
        var result = await sutWithRegeneration.SetToApprovedInErrorAsync(applicationId, model, userId, CancellationToken.None);
        
        Assert.True(result.IsSuccess);
        _repo.Verify(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()), Times.Exactly(2)); // Once for validation, once for regeneration
        _repo.Verify(x => x.Update(It.Is<FellingLicenceApplication>(a => a.ApplicationReference == "ABC/2024/12345")), Times.Once);
        _repo.Verify(x => x.AddStatusHistory(userId, applicationId, FellingLicenceStatus.ApprovedInError, It.IsAny<CancellationToken>()), Times.Once);
        mockReferenceRepo.Verify(x => x.GetNextApplicationReferenceIdValueAsync(application.CreatedTimestamp.Year, It.IsAny<CancellationToken>()), Times.Once);
        mockReferenceHelper.Verify(x => x.GenerateReferenceNumber(application, 12345L, "12345", null), Times.Once);
        mockReferenceHelper.Verify(x => x.UpdateReferenceNumber("NEW/2024/12345", "ABC"), Times.Once);
        mockUnitOfWork.Verify(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task SaveApprovedInError_Regeneration_ExtractsPostfix_WhenPresent(Guid applicationId, ApprovedInErrorModel model, FellingLicenceApplication application, Guid userId)
    {
        // Application reference with postfix after the last slash
        var currentRefWithPostfix = "ABC/2024/12345/XYZ";
        model.ApplicationId = applicationId;
        model.ReasonOther = false;
        application.ApplicationReference = currentRefWithPostfix;
        application.StatusHistories = new List<StatusHistory>
        {
            new StatusHistory { Status = FellingLicenceStatus.Approved, Created = DateTime.UtcNow }
        };
        application.Documents = new List<Document>();

        var mockReferenceRepo = new Mock<IFellingLicenceApplicationReferenceRepository>();
        var mockReferenceHelper = new Mock<IApplicationReferenceHelper>();
        var mockUnitOfWork = new Mock<IUnitOfWork>();

        _repo.Setup(x => x.GetApprovedInErrorAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<ApprovedInError>.None);
        _repo.Setup(x => x.AddOrUpdateApprovedInErrorAsync(It.IsAny<ApprovedInError>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());
        _repo.Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.From(application));
        _repo.SetupGet(x => x.UnitOfWork).Returns(mockUnitOfWork.Object);

        mockReferenceRepo.Setup(x => x.GetNextApplicationReferenceIdValueAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(55555L);

        // Assert that postfix "XYZ" is passed to GenerateReferenceNumber
        mockReferenceHelper.Setup(x => x.GenerateReferenceNumber(It.IsAny<FellingLicenceApplication>(), It.IsAny<long>(), It.IsAny<string>(), It.IsAny<int?>()))
            .Returns("NEW/2024/55555/XYZ");
        mockReferenceHelper.Setup(x => x.UpdateReferenceNumber(It.IsAny<string>(), It.IsAny<string>()))
            .Returns("ABC/2024/55555/XYZ");
        mockUnitOfWork.Setup(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());
        _repo.Setup(x => x.AddStatusHistory(userId, applicationId, FellingLicenceStatus.ApprovedInError, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sutWithRegeneration = new ApprovedInErrorService(
            _repo.Object,
            _clock.Object,
            new NullLogger<ApprovedInErrorService>(),
            _publicRegister.Object,
            _updateFellingLicenceApplicationService.Object,
            mockReferenceRepo.Object,
            mockReferenceHelper.Object);

        var result = await sutWithRegeneration.SetToApprovedInErrorAsync(applicationId, model, userId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        mockReferenceHelper.Verify(x => x.GenerateReferenceNumber(application, 55555L, "XYZ", null), Times.Once);
        mockReferenceHelper.Verify(x => x.UpdateReferenceNumber("NEW/2024/55555/XYZ", "ABC"), Times.Once);
        _repo.Verify(x => x.Update(It.Is<FellingLicenceApplication>(a => a.ApplicationReference == "ABC/2024/55555/XYZ")), Times.Once);
        mockUnitOfWork.Verify(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task SaveApprovedInError_Regeneration_SetsNoPostfix_WhenLastSlashIsEnd(Guid applicationId, ApprovedInErrorModel model, FellingLicenceApplication application, Guid userId)
    {
        // Application reference ending with slash should not produce postfix
        var currentRefNoPostfix = "ABC/2024/";
        model.ApplicationId = applicationId;
        model.ReasonOther = false;
        application.ApplicationReference = currentRefNoPostfix;
        application.StatusHistories = new List<StatusHistory>
        {
            new StatusHistory { Status = FellingLicenceStatus.Approved, Created = DateTime.UtcNow }
        };
        application.Documents = new List<Document>();

        var mockReferenceRepo = new Mock<IFellingLicenceApplicationReferenceRepository>();
        var mockReferenceHelper = new Mock<IApplicationReferenceHelper>();
        var mockUnitOfWork = new Mock<IUnitOfWork>();

        _repo.Setup(x => x.GetApprovedInErrorAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<ApprovedInError>.None);
        _repo.Setup(x => x.AddOrUpdateApprovedInErrorAsync(It.IsAny<ApprovedInError>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());
        _repo.Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.From(application));
        _repo.SetupGet(x => x.UnitOfWork).Returns(mockUnitOfWork.Object);

        mockReferenceRepo.Setup(x => x.GetNextApplicationReferenceIdValueAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(77777L);

        mockReferenceHelper.Setup(x => x.GenerateReferenceNumber(It.IsAny<FellingLicenceApplication>(), It.IsAny<long>(), It.IsAny<string>(), It.IsAny<int?>()))
            .Returns("NEW/2024/77777");
        mockReferenceHelper.Setup(x => x.UpdateReferenceNumber(It.IsAny<string>(), It.IsAny<string>()))
            .Returns("ABC/2024/77777");
        mockUnitOfWork.Setup(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());
        _repo.Setup(x => x.AddStatusHistory(userId, applicationId, FellingLicenceStatus.ApprovedInError, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sutWithRegeneration = new ApprovedInErrorService(
            _repo.Object,
            _clock.Object,
            new NullLogger<ApprovedInErrorService>(),
            _publicRegister.Object,
            _updateFellingLicenceApplicationService.Object,
            mockReferenceRepo.Object,
            mockReferenceHelper.Object);

        var result = await sutWithRegeneration.SetToApprovedInErrorAsync(applicationId, model, userId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        // Postfix must be null since last slash is at the end
        mockReferenceHelper.Verify(x => x.GenerateReferenceNumber(application, 77777L, null, null), Times.Once);
        mockReferenceHelper.Verify(x => x.UpdateReferenceNumber("NEW/2024/77777", "ABC"), Times.Once);
        _repo.Verify(x => x.Update(It.Is<FellingLicenceApplication>(a => a.ApplicationReference == "ABC/2024/77777")), Times.Once);
        mockUnitOfWork.Verify(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoMoqData]
    public async Task SaveApprovedInError_SkipsReferenceRegeneration_WhenDependenciesNotConfigured(Guid applicationId, ApprovedInErrorModel model, Guid userId)
    {
        var sut = CreateSut(); // Creates with null reference dependencies
        model.ApplicationId = applicationId;
        model.ReasonOther = false;
        model.PreviousReference = "ABC/2024/12345";

        // Mock the application with Approved status
        var application = new FellingLicenceApplication
        {
            Id = applicationId,
            StatusHistories = new List<StatusHistory>
            {
                new StatusHistory { Status = FellingLicenceStatus.Approved, Created = DateTime.UtcNow }
            },
            Documents = new List<Document>()
        };

        var mockUnitOfWork = new Mock<IUnitOfWork>();
        _repo.SetupGet(x => x.UnitOfWork).Returns(mockUnitOfWork.Object);
        mockUnitOfWork.Setup(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()))
        .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());
        
        _repo.Setup(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()))
        .ReturnsAsync(Maybe<FellingLicenceApplication>.From(application));
        
        _repo.Setup(x => x.GetApprovedInErrorAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(Maybe<ApprovedInError>.None);
        
        _repo.Setup(x => x.AddOrUpdateApprovedInErrorAsync(It.IsAny<ApprovedInError>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());

        _repo.Setup(x => x.AddStatusHistory(userId, applicationId, FellingLicenceStatus.ApprovedInError, It.IsAny<CancellationToken>()))
        .Returns(Task.CompletedTask);

        var result = await sut.SetToApprovedInErrorAsync(applicationId, model, userId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        // Since dependencies are not configured, GetAsync is called once for validation but not again for regeneration
        _repo.Verify(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _repo.Verify(x => x.AddStatusHistory(userId, applicationId, FellingLicenceStatus.ApprovedInError, It.IsAny<CancellationToken>()), Times.Once);
        mockUnitOfWork.Verify(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoData]
    public async Task SaveApprovedInError_ReturnsFailure_WhenApplicationNotFound(Guid applicationId, ApprovedInErrorModel model, Guid userId)
    {
        var sut = CreateSut();
        model.ApplicationId = applicationId;

        _repo.Setup(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()))
        .ReturnsAsync(Maybe<FellingLicenceApplication>.None);

        var result = await sut.SetToApprovedInErrorAsync(applicationId, model, userId, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("Felling licence application not found", result.Error);
        _repo.Verify(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _repo.Verify(x => x.GetApprovedInErrorAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory, AutoData]
    public async Task SaveApprovedInError_ReturnsFailure_WhenApplicationNotInApprovedState(Guid applicationId, ApprovedInErrorModel model, Guid userId)
    {
        var sut = CreateSut();
        model.ApplicationId = applicationId;

        // Mock the application with non-Approved status
        var application = new FellingLicenceApplication
        {
            Id = applicationId,
            StatusHistories = new List<StatusHistory>
            {
                new StatusHistory { Status = FellingLicenceStatus.Submitted, Created = DateTime.UtcNow }
            },
            Documents = new List<Document>()
        };

        _repo.Setup(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()))
        .ReturnsAsync(Maybe<FellingLicenceApplication>.From(application));

        var result = await sut.SetToApprovedInErrorAsync(applicationId, model, userId, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("cannot be marked as approved in error as it is not in the approved state", result.Error);
        _repo.Verify(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _repo.Verify(x => x.GetApprovedInErrorAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory, AutoData]
    public async Task SaveApprovedInError_HidesApplicationDocument_WhenPresent(Guid applicationId, ApprovedInErrorModel model, Guid userId)
    {
        var sut = CreateSut();
        model.ApplicationId = applicationId;
        model.ReasonOther = true;

        var document = _fixture.Build<Document>()
            .With(d => d.Purpose, DocumentPurpose.ApplicationDocument)
            .With(d => d.CreatedTimestamp, DateTime.UtcNow)
            .With(d => d.FellingLicenceApplicationId, applicationId)
            .Create();

        var application = new FellingLicenceApplication
        {
            Id = applicationId,
            StatusHistories = new List<StatusHistory>
            {
                new StatusHistory { Status = FellingLicenceStatus.Approved, Created = DateTime.UtcNow }
            },
            Documents = new List<Document> { document }
        };

        var mockUnitOfWork = new Mock<IUnitOfWork>();
        _repo.SetupGet(x => x.UnitOfWork).Returns(mockUnitOfWork.Object);
        mockUnitOfWork.Setup(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()))
        .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());
        
        _repo.Setup(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()))
        .ReturnsAsync(Maybe<FellingLicenceApplication>.From(application));
        
        _repo.Setup(x => x.GetApprovedInErrorAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(Maybe<ApprovedInError>.None);
        
        _repo.Setup(x => x.AddOrUpdateApprovedInErrorAsync(It.IsAny<ApprovedInError>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());

        _repo.Setup(x => x.UpdateDocumentVisibleToApplicantAsync(
            applicationId, 
            document.Id, 
            false, 
            It.IsAny<CancellationToken>()))
        .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());

        _repo.Setup(x => x.AddStatusHistory(userId, applicationId, FellingLicenceStatus.ApprovedInError, It.IsAny<CancellationToken>()))
        .Returns(Task.CompletedTask);

        var result = await sut.SetToApprovedInErrorAsync(applicationId, model, userId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        _repo.Verify(x => x.UpdateDocumentVisibleToApplicantAsync(
            applicationId, 
            document.Id, 
            false, 
            It.IsAny<CancellationToken>()), Times.Once);
        _repo.Verify(x => x.AddStatusHistory(userId, applicationId, FellingLicenceStatus.ApprovedInError, It.IsAny<CancellationToken>()), Times.Once);
        mockUnitOfWork.Verify(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoData]
    public async Task SaveApprovedInError_ReturnsFailure_WhenHideDocumentFails(Guid applicationId, ApprovedInErrorModel model, Guid userId)
    {
        var sut = CreateSut();
        model.ApplicationId = applicationId;
        model.ReasonOther = true;

        var document = _fixture.Build<Document>()
            .With(d => d.Purpose, DocumentPurpose.ApplicationDocument)
            .With(d => d.CreatedTimestamp, DateTime.UtcNow)
            .With(d => d.FellingLicenceApplicationId, applicationId)
            .Create();

        var application = new FellingLicenceApplication
        {
            Id = applicationId,
            StatusHistories = new List<StatusHistory>
            {
                new StatusHistory { Status = FellingLicenceStatus.Approved, Created = DateTime.UtcNow }
            },
            Documents = new List<Document> { document }
        };

        _repo.Setup(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()))
        .ReturnsAsync(Maybe<FellingLicenceApplication>.From(application));

        _repo.Setup(x => x.UpdateDocumentVisibleToApplicantAsync(
            applicationId, 
            document.Id, 
            false, 
            It.IsAny<CancellationToken>()))
        .ReturnsAsync(UnitResult.Failure(UserDbErrorReason.NotFound));

        var result = await sut.SetToApprovedInErrorAsync(applicationId, model, userId, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("Could not update document visibility", result.Error);
        _repo.Verify(x => x.GetApprovedInErrorAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #region RemoveApplicationFromDecisionRegister Tests

    [Theory, AutoData]
    public async Task SaveApprovedInError_DoesNotCallPublicRegister_WhenNoPublicRegisterEntry(Guid applicationId, ApprovedInErrorModel model, Guid userId)
    {
        var sut = CreateSut();
        model.ApplicationId = applicationId;
        model.ReasonOther = true;

        // Mock application with NO PublicRegister entry
        var application = new FellingLicenceApplication
        {
            Id = applicationId,
            ApplicationReference = "ABC/2024/12345",
            StatusHistories = new List<StatusHistory>
            {
                new StatusHistory { Status = FellingLicenceStatus.Approved, Created = DateTime.UtcNow }
            },
            Documents = new List<Document>(),
            PublicRegister = null // No public register entry
        };

        var mockUnitOfWork = new Mock<IUnitOfWork>();
        _repo.SetupGet(x => x.UnitOfWork).Returns(mockUnitOfWork.Object);
        mockUnitOfWork.Setup(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());

        _repo.Setup(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.From(application));

        _repo.Setup(x => x.GetApprovedInErrorAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<ApprovedInError>.None);

        _repo.Setup(x => x.AddOrUpdateApprovedInErrorAsync(It.IsAny<ApprovedInError>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());

        _repo.Setup(x => x.AddStatusHistory(userId, applicationId, FellingLicenceStatus.ApprovedInError, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await sut.SetToApprovedInErrorAsync(applicationId, model, userId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        // Verify that public register removal was NOT attempted
        _publicRegister.Verify(x => x.RemoveCaseFromDecisionRegisterAsync(
            It.IsAny<int>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
        _updateFellingLicenceApplicationService.Verify(x => x.SetRemovalDateOnDecisionPublicRegisterEntryAsync(
            It.IsAny<Guid>(),
            It.IsAny<DateTime>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory, AutoData]
    public async Task SaveApprovedInError_DoesNotCallPublicRegister_WhenNoEsriId(Guid applicationId, ApprovedInErrorModel model, Guid userId)
    {
        var sut = CreateSut();
        model.ApplicationId = applicationId;
        model.ReasonOther = true;

        // Mock application with PublicRegister but NO EsriId
        var application = new FellingLicenceApplication
        {
            Id = applicationId,
            ApplicationReference = "ABC/2024/12345",
            StatusHistories = new List<StatusHistory>
            {
                new StatusHistory { Status = FellingLicenceStatus.Approved, Created = DateTime.UtcNow }
            },
            Documents = new List<Document>(),
            PublicRegister = new PublicRegister
            {
                FellingLicenceApplicationId = applicationId,
                EsriId = null // No ESRI ID
            }
        };

        var mockUnitOfWork = new Mock<IUnitOfWork>();
        _repo.SetupGet(x => x.UnitOfWork).Returns(mockUnitOfWork.Object);
        mockUnitOfWork.Setup(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());

        _repo.Setup(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.From(application));

        _repo.Setup(x => x.GetApprovedInErrorAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<ApprovedInError>.None);

        _repo.Setup(x => x.AddOrUpdateApprovedInErrorAsync(It.IsAny<ApprovedInError>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());

        _repo.Setup(x => x.AddStatusHistory(userId, applicationId, FellingLicenceStatus.ApprovedInError, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await sut.SetToApprovedInErrorAsync(applicationId, model, userId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        // Verify that public register removal was NOT attempted
        _publicRegister.Verify(x => x.RemoveCaseFromDecisionRegisterAsync(
            It.IsAny<int>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
        _updateFellingLicenceApplicationService.Verify(x => x.SetRemovalDateOnDecisionPublicRegisterEntryAsync(
            It.IsAny<Guid>(),
            It.IsAny<DateTime>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory, AutoData]
    public async Task SaveApprovedInError_DoesNotCallPublicRegister_WhenNotPublishedToDecisionRegister(Guid applicationId, ApprovedInErrorModel model, Guid userId, int esriId)
    {
        var sut = CreateSut();
        model.ApplicationId = applicationId;
        model.ReasonOther = true;

        // Mock application with PublicRegister but NO DecisionPublicRegisterPublicationTimestamp
        var application = new FellingLicenceApplication
        {
            Id = applicationId,
            ApplicationReference = "ABC/2024/12345",
            StatusHistories = new List<StatusHistory>
            {
                new StatusHistory { Status = FellingLicenceStatus.Approved, Created = DateTime.UtcNow }
            },
            Documents = new List<Document>(),
            PublicRegister = new PublicRegister
            {
                FellingLicenceApplicationId = applicationId,
                EsriId = esriId,
                DecisionPublicRegisterPublicationTimestamp = null // Not published to decision register
            }
        };

        var mockUnitOfWork = new Mock<IUnitOfWork>();
        _repo.SetupGet(x => x.UnitOfWork).Returns(mockUnitOfWork.Object);
        mockUnitOfWork.Setup(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());

        _repo.Setup(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.From(application));

        _repo.Setup(x => x.GetApprovedInErrorAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<ApprovedInError>.None);

        _repo.Setup(x => x.AddOrUpdateApprovedInErrorAsync(It.IsAny<ApprovedInError>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());

        _repo.Setup(x => x.AddStatusHistory(userId, applicationId, FellingLicenceStatus.ApprovedInError, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await sut.SetToApprovedInErrorAsync(applicationId, model, userId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        // Verify that public register removal was NOT attempted
        _publicRegister.Verify(x => x.RemoveCaseFromDecisionRegisterAsync(
            It.IsAny<int>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
        _updateFellingLicenceApplicationService.Verify(x => x.SetRemovalDateOnDecisionPublicRegisterEntryAsync(
            It.IsAny<Guid>(),
            It.IsAny<DateTime>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory, AutoData]
    public async Task SaveApprovedInError_DoesNotCallPublicRegister_WhenAlreadyRemovedFromDecisionRegister(Guid applicationId, ApprovedInErrorModel model, Guid userId, int esriId)
    {
        var sut = CreateSut();
        model.ApplicationId = applicationId;
        model.ReasonOther = true;

        var publishedTimestamp = DateTime.UtcNow.AddDays(-10);
        var removedTimestamp = DateTime.UtcNow.AddDays(-5);

        // Mock application with PublicRegister that's already been removed
        var application = new FellingLicenceApplication
        {
            Id = applicationId,
            ApplicationReference = "ABC/2024/12345",
            StatusHistories = new List<StatusHistory>
            {
                new StatusHistory { Status = FellingLicenceStatus.Approved, Created = DateTime.UtcNow }
            },
            Documents = new List<Document>(),
            PublicRegister = new PublicRegister
            {
                FellingLicenceApplicationId = applicationId,
                EsriId = esriId,
                DecisionPublicRegisterPublicationTimestamp = publishedTimestamp,
                DecisionPublicRegisterRemovedTimestamp = removedTimestamp // Already removed
            }
        };

        var mockUnitOfWork = new Mock<IUnitOfWork>();
        _repo.SetupGet(x => x.UnitOfWork).Returns(mockUnitOfWork.Object);
        mockUnitOfWork.Setup(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());

        _repo.Setup(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.From(application));

        _repo.Setup(x => x.GetApprovedInErrorAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<ApprovedInError>.None);

        _repo.Setup(x => x.AddOrUpdateApprovedInErrorAsync(It.IsAny<ApprovedInError>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());

        _repo.Setup(x => x.AddStatusHistory(userId, applicationId, FellingLicenceStatus.ApprovedInError, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await sut.SetToApprovedInErrorAsync(applicationId, model, userId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        // Verify that public register removal was NOT attempted since it was already removed
        _publicRegister.Verify(x => x.RemoveCaseFromDecisionRegisterAsync(
            It.IsAny<int>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
        _updateFellingLicenceApplicationService.Verify(x => x.SetRemovalDateOnDecisionPublicRegisterEntryAsync(
            It.IsAny<Guid>(),
            It.IsAny<DateTime>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory, AutoData]
    public async Task SaveApprovedInError_RemovesFromDecisionRegister_WhenPublicRegisterEntryExists(Guid applicationId, ApprovedInErrorModel model, Guid userId, int esriId)
    {
        var sut = CreateSut();
        model.ApplicationId = applicationId;
        model.ReasonOther = true;

        var applicationReference = "ABC/2024/12345";
        var publishedTimestamp = DateTime.UtcNow.AddDays(-10);

        // Mock application with PublicRegister and EsriId, published to decision register
        var application = new FellingLicenceApplication
        {
            Id = applicationId,
            ApplicationReference = applicationReference,
            StatusHistories = new List<StatusHistory>
            {
                new StatusHistory { Status = FellingLicenceStatus.Approved, Created = DateTime.UtcNow }
            },
            Documents = new List<Document>(),
            PublicRegister = new PublicRegister
            {
                FellingLicenceApplicationId = applicationId,
                EsriId = esriId,
                DecisionPublicRegisterPublicationTimestamp = publishedTimestamp, // Published to decision register
                DecisionPublicRegisterRemovedTimestamp = null // Not yet removed
            }
        };

        var mockUnitOfWork = new Mock<IUnitOfWork>();
        _repo.SetupGet(x => x.UnitOfWork).Returns(mockUnitOfWork.Object);
        mockUnitOfWork.Setup(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());

        _repo.Setup(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.From(application));

        _repo.Setup(x => x.GetApprovedInErrorAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<ApprovedInError>.None);

        _repo.Setup(x => x.AddOrUpdateApprovedInErrorAsync(It.IsAny<ApprovedInError>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());

        _repo.Setup(x => x.AddStatusHistory(userId, applicationId, FellingLicenceStatus.ApprovedInError, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Setup public register removal to succeed
        _publicRegister.Setup(x => x.RemoveCaseFromDecisionRegisterAsync(
            esriId,
            applicationReference,
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        _updateFellingLicenceApplicationService.Setup(x => x.SetRemovalDateOnDecisionPublicRegisterEntryAsync(
            applicationId,
            It.IsAny<DateTime>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var result = await sut.SetToApprovedInErrorAsync(applicationId, model, userId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        // Verify that public register removal WAS attempted
        _publicRegister.Verify(x => x.RemoveCaseFromDecisionRegisterAsync(
            esriId,
            applicationReference,
            It.IsAny<CancellationToken>()), Times.Once);
        // Verify that removal date was set
        _updateFellingLicenceApplicationService.Verify(x => x.SetRemovalDateOnDecisionPublicRegisterEntryAsync(
            applicationId,
            _now,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoData]
    public async Task SaveApprovedInError_ContinuesSuccessfully_WhenPublicRegisterRemovalFails(Guid applicationId, ApprovedInErrorModel model, Guid userId, int esriId)
    {
        var sut = CreateSut();
        model.ApplicationId = applicationId;
        model.ReasonOther = true;

        var applicationReference = "ABC/2024/12345";
        var publishedTimestamp = DateTime.UtcNow.AddDays(-10);

        // Mock application with PublicRegister and EsriId
        var application = new FellingLicenceApplication
        {
            Id = applicationId,
            ApplicationReference = applicationReference,
            StatusHistories = new List<StatusHistory>
            {
                new StatusHistory { Status = FellingLicenceStatus.Approved, Created = DateTime.UtcNow }
            },
            Documents = new List<Document>(),
            PublicRegister = new PublicRegister
            {
                FellingLicenceApplicationId = applicationId,
                EsriId = esriId,
                DecisionPublicRegisterPublicationTimestamp = publishedTimestamp,
                DecisionPublicRegisterRemovedTimestamp = null
            }
        };

        var mockUnitOfWork = new Mock<IUnitOfWork>();
        _repo.SetupGet(x => x.UnitOfWork).Returns(mockUnitOfWork.Object);
        mockUnitOfWork.Setup(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());

        _repo.Setup(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.From(application));

        _repo.Setup(x => x.GetApprovedInErrorAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<ApprovedInError>.None);

        _repo.Setup(x => x.AddOrUpdateApprovedInErrorAsync(It.IsAny<ApprovedInError>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());

        _repo.Setup(x => x.AddStatusHistory(userId, applicationId, FellingLicenceStatus.ApprovedInError, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Setup public register removal to fail
        _publicRegister.Setup(x => x.RemoveCaseFromDecisionRegisterAsync(
            esriId,
            applicationReference,
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("GIS service error"));

        var result = await sut.SetToApprovedInErrorAsync(applicationId, model, userId, CancellationToken.None);

        // The operation should still succeed even if public register removal fails
        Assert.True(result.IsSuccess);
        // Verify that public register removal WAS attempted
        _publicRegister.Verify(x => x.RemoveCaseFromDecisionRegisterAsync(
            esriId,
            applicationReference,
            It.IsAny<CancellationToken>()), Times.Once);
        // Verify that removal date was NOT set since GIS removal failed
        _updateFellingLicenceApplicationService.Verify(x => x.SetRemovalDateOnDecisionPublicRegisterEntryAsync(
            It.IsAny<Guid>(),
            It.IsAny<DateTime>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory, AutoData]
    public async Task SaveApprovedInError_LogsError_WhenSetRemovalDateFails(Guid applicationId, ApprovedInErrorModel model, Guid userId, int esriId)
    {
        var sut = CreateSut();
        model.ApplicationId = applicationId;
        model.ReasonOther = true;

        var applicationReference = "ABC/2024/12345";
        var publishedTimestamp = DateTime.UtcNow.AddDays(-10);

        // Mock application with PublicRegister and EsriId
        var application = new FellingLicenceApplication
        {
            Id = applicationId,
            ApplicationReference = applicationReference,
            StatusHistories = new List<StatusHistory>
            {
                new StatusHistory { Status = FellingLicenceStatus.Approved, Created = DateTime.UtcNow }
            },
            Documents = new List<Document>(),
            PublicRegister = new PublicRegister
            {
                FellingLicenceApplicationId = applicationId,
                EsriId = esriId,
                DecisionPublicRegisterPublicationTimestamp = publishedTimestamp,
                DecisionPublicRegisterRemovedTimestamp = null
            }
        };

        var mockUnitOfWork = new Mock<IUnitOfWork>();
        _repo.SetupGet(x => x.UnitOfWork).Returns(mockUnitOfWork.Object);
        mockUnitOfWork.Setup(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());

        _repo.Setup(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.From(application));

        _repo.Setup(x => x.GetApprovedInErrorAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<ApprovedInError>.None);

        _repo.Setup(x => x.AddOrUpdateApprovedInErrorAsync(It.IsAny<ApprovedInError>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());

        _repo.Setup(x => x.AddStatusHistory(userId, applicationId, FellingLicenceStatus.ApprovedInError, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Setup public register removal to succeed
        _publicRegister.Setup(x => x.RemoveCaseFromDecisionRegisterAsync(
            esriId,
            applicationReference,
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        // Setup removal date update to fail
        _updateFellingLicenceApplicationService.Setup(x => x.SetRemovalDateOnDecisionPublicRegisterEntryAsync(
            applicationId,
            It.IsAny<DateTime>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("Database update error"));

        var result = await sut.SetToApprovedInErrorAsync(applicationId, model, userId, CancellationToken.None);

        // The operation should still succeed even if setting removal date fails (it's logged but non-blocking)
        Assert.True(result.IsSuccess);
        _publicRegister.Verify(x => x.RemoveCaseFromDecisionRegisterAsync(
            esriId,
            applicationReference,
            It.IsAny<CancellationToken>()), Times.Once);
        // Verify that removal date was set
        _updateFellingLicenceApplicationService.Verify(x => x.SetRemovalDateOnDecisionPublicRegisterEntryAsync(
            applicationId,
            _now,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoData]
    public async Task SaveApprovedInError_RemovesFromDecisionRegisterAfterPersistingChanges(Guid applicationId, ApprovedInErrorModel model, Guid userId, int esriId)
    {
        var sut = CreateSut();
        model.ApplicationId = applicationId;
        model.ReasonOther = true;

        var applicationReference = "ABC/2024/12345";
        var publishedTimestamp = DateTime.UtcNow.AddDays(-10);
        var callSequence = new List<string>();

        // Mock application with PublicRegister and EsriId
        var application = new FellingLicenceApplication
        {
            Id = applicationId,
            ApplicationReference = applicationReference,
            StatusHistories = new List<StatusHistory>
            {
                new StatusHistory { Status = FellingLicenceStatus.Approved, Created = DateTime.UtcNow }
            },
            Documents = new List<Document>(),
            PublicRegister = new PublicRegister
            {
                FellingLicenceApplicationId = applicationId,
                EsriId = esriId,
                DecisionPublicRegisterPublicationTimestamp = publishedTimestamp,
                DecisionPublicRegisterRemovedTimestamp = null
            }
        };

        var mockUnitOfWork = new Mock<IUnitOfWork>();
        _repo.SetupGet(x => x.UnitOfWork).Returns(mockUnitOfWork.Object);
        mockUnitOfWork.Setup(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Success<UserDbErrorReason>())
            .Callback(() => callSequence.Add("SaveEntities"));

        _repo.Setup(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<FellingLicenceApplication>.From(application));

        _repo.Setup(x => x.GetApprovedInErrorAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Maybe<ApprovedInError>.None);

        _repo.Setup(x => x.AddOrUpdateApprovedInErrorAsync(It.IsAny<ApprovedInError>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());

        _repo.Setup(x => x.AddStatusHistory(userId, applicationId, FellingLicenceStatus.ApprovedInError, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _publicRegister.Setup(x => x.RemoveCaseFromDecisionRegisterAsync(
            esriId,
            applicationReference,
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success())
            .Callback(() => callSequence.Add("RemoveFromDecisionRegister"));

        _updateFellingLicenceApplicationService.Setup(x => x.SetRemovalDateOnDecisionPublicRegisterEntryAsync(
            applicationId,
            It.IsAny<DateTime>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        var result = await sut.SetToApprovedInErrorAsync(applicationId, model, userId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        // Verify that SaveEntities is called before RemoveFromDecisionRegister
        Assert.Equal(2, callSequence.Count);
        Assert.Equal("SaveEntities", callSequence[0]);
        Assert.Equal("RemoveFromDecisionRegister", callSequence[1]);
    }

    #endregion
}
