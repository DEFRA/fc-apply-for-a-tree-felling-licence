using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
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
        return new ApprovedInErrorService(_repo.Object, _clock.Object, new NullLogger<ApprovedInErrorService>());
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
        Assert.Equal(entity.CaseNote, result.Value.CaseNote);
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
            e.ReasonOther == model.ReasonOther &&
            e.CaseNote == model.CaseNote &&
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
        Assert.Equal(model.PreviousReference, existingEntity.PreviousReference);
        Assert.Equal(model.ReasonExpiryDate, existingEntity.ReasonExpiryDate);
        Assert.Equal(model.ReasonSupplementaryPoints, existingEntity.ReasonSupplementaryPoints);
        Assert.Equal(model.ReasonOther, existingEntity.ReasonOther);
        Assert.Equal(model.CaseNote, existingEntity.CaseNote);
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
        _repo.Verify(x => x.GetApprovedInErrorAsync(applicationId, It.IsAny<CancellationToken>()), Times.Once);
        _repo.Verify(x => x.AddOrUpdateApprovedInErrorAsync(It.IsAny<ApprovedInError>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory, AutoData]
    public async Task SaveApprovedInError_EnsuresApplicationIdConsistency(Guid applicationId, ApprovedInErrorModel model, Guid userId)
    {
        var sut = CreateSut();
        var differentId = Guid.NewGuid();
        model.ApplicationId = differentId; // Set to different ID
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
        _repo.Verify(x => x.AddOrUpdateApprovedInErrorAsync(
        It.Is<ApprovedInError>(e => e.FellingLicenceApplicationId == applicationId),
        It.IsAny<CancellationToken>()), Times.Once);
        _repo.Verify(x => x.AddStatusHistory(userId, applicationId, FellingLicenceStatus.ApprovedInError, It.IsAny<CancellationToken>()), Times.Once);
        mockUnitOfWork.Verify(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);
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
        mockReferenceRepo.Object,
        mockReferenceHelper.Object);
        
        var result = await sutWithRegeneration.SetToApprovedInErrorAsync(applicationId, model, userId, CancellationToken.None);
        
        Assert.True(result.IsSuccess);
        _repo.Verify(x => x.GetAsync(applicationId, It.IsAny<CancellationToken>()), Times.Exactly(2)); // Once for validation, once for regeneration
        _repo.Verify(x => x.Update(It.Is<FellingLicenceApplication>(a => a.ApplicationReference == "ABC/2024/12345")), Times.Once);
        _repo.Verify(x => x.AddStatusHistory(userId, applicationId, FellingLicenceStatus.ApprovedInError, It.IsAny<CancellationToken>()), Times.Once);
        mockReferenceRepo.Verify(x => x.GetNextApplicationReferenceIdValueAsync(application.CreatedTimestamp.Year, It.IsAny<CancellationToken>()), Times.Once);
        mockReferenceHelper.Verify(x => x.GenerateReferenceNumber(application, 12345L, null, null), Times.Once);
        mockReferenceHelper.Verify(x => x.UpdateReferenceNumber("NEW/2024/12345", "ABC"), Times.Once);
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
}
