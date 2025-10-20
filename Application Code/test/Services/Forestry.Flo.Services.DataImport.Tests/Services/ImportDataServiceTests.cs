using AutoFixture;
using CSharpFunctionalExtensions;
using FluentValidation.Results;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.User;
using Forestry.Flo.Services.DataImport.Models;
using Forestry.Flo.Services.DataImport.Services;
using Forestry.Flo.Services.FellingLicenceApplications.DataImports;
using Forestry.Flo.Services.FellingLicenceApplications.DataImports.Models;
using Forestry.Flo.Tests.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.PropertyProfiles.DataImports;
using Xunit;

namespace Forestry.Flo.Services.DataImport.Tests.Services;

public class ImportDataServiceTests
{
    private readonly Mock<IImportApplications> _mockImportApplications = new();
    private readonly Mock<IGetPropertiesForWoodlandOwner> _mockGetProperties = new();
    private readonly Mock<IValidateImportFileSets> _mockValidator = new();
    private readonly Mock<IReadImportFileCollections> _mockFileReader = new();
    private readonly Mock<IAuditService<ImportDataService>> _mockAudit = new();
    private string _correlationId;
    private static readonly Fixture FixtureInstance = new();

    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [Theory, AutoMoqData]
    public async Task ParseShouldReturnFailureWhenUnableToReadFileset(
        DataImportRequest request,
        List<string> errors)
    {
        var sut = CreateSut();

        _mockFileReader
            .Setup(x => x.ReadInputFormFileCollectionAsync(It.IsAny<FormFileCollection>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<ImportFileSetContents, List<string>>(errors));

        var result = await sut.ParseDataImportRequestAsync(request, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(errors, result.Error);

        _mockFileReader.Verify(x => x.ReadInputFormFileCollectionAsync(request.ImportFileSet, It.IsAny<CancellationToken>()), Times.Once);
        _mockFileReader.VerifyNoOtherCalls();

        _mockValidator.VerifyNoOtherCalls();

        _mockGetProperties.VerifyNoOtherCalls();

        _mockImportApplications.VerifyNoOtherCalls();

        _mockAudit.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
            a.UserId == request.UserAccessModel.UserAccountId
            && a.ActorType == ActorType.ExternalApplicant
            && a.CorrelationId == _correlationId
            && a.SourceEntityId == request.WoodlandOwnerId
            && a.SourceEntityType == SourceEntityType.WoodlandOwner
            && a.EventName == AuditEvents.ImportDataFromCsvFailure
            && JsonSerializer.Serialize(a.AuditData, _serializerOptions) ==
            JsonSerializer.Serialize(new
            {
                FellingLicenceApplicationsInImportFile = (int?)null,
                ValidationErrors = errors,
                Error = "Failed to read provided import file collection"
            }, _serializerOptions)),
            It.IsAny<CancellationToken>()), Times.Once);
        _mockAudit.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task ParseShouldReturnFailureWhenUnableToRetrieveProperties(
        DataImportRequest request,
        ImportFileSetContents fileSet,
        string error)
    {
        var sut = CreateSut();

        _mockFileReader
            .Setup(x => x.ReadInputFormFileCollectionAsync(It.IsAny<FormFileCollection>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<ImportFileSetContents, List<string>>(fileSet));
        _mockGetProperties.Setup(x => x.GetPropertiesForDataImport(It.IsAny<UserAccessModel>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<IEnumerable<PropertyIds>>(error));

        var result = await sut.ParseDataImportRequestAsync(request, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Single(result.Error);
        Assert.Equal("Failed to retrieve properties for woodland owner", result.Error.Single());

        _mockFileReader.Verify(x => x.ReadInputFormFileCollectionAsync(request.ImportFileSet, It.IsAny<CancellationToken>()), Times.Once);
        _mockFileReader.VerifyNoOtherCalls();

        _mockGetProperties.Verify(x => x.GetPropertiesForDataImport(request.UserAccessModel, request.WoodlandOwnerId, It.IsAny<CancellationToken>()), Times.Once);
        _mockGetProperties.VerifyNoOtherCalls();

        _mockValidator.VerifyNoOtherCalls();

        _mockImportApplications.VerifyNoOtherCalls();

        _mockAudit.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.UserId == request.UserAccessModel.UserAccountId
                && a.ActorType == ActorType.ExternalApplicant
                && a.CorrelationId == _correlationId
                && a.SourceEntityId == request.WoodlandOwnerId
                && a.SourceEntityType == SourceEntityType.WoodlandOwner
                && a.EventName == AuditEvents.ImportDataFromCsvFailure
                && JsonSerializer.Serialize(a.AuditData, _serializerOptions) ==
                JsonSerializer.Serialize(new
                {
                    FellingLicenceApplicationsInImportFile = (int?)null,
                    ValidationErrors = new List<string> { error },
                    Error = "Failed to retrieve properties for woodland owner"
                }, _serializerOptions)),
            It.IsAny<CancellationToken>()), Times.Once);
        _mockAudit.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task ParseShouldReturnFailureWhenFilesetFailsValidation(
        DataImportRequest request,
        ImportFileSetContents fileSet,
        IEnumerable<PropertyIds> propertyIds,
        List<ValidationFailure> errors)
    {
        var sut = CreateSut();

        _mockFileReader
            .Setup(x => x.ReadInputFormFileCollectionAsync(It.IsAny<FormFileCollection>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<ImportFileSetContents, List<string>>(fileSet));
        _mockValidator
            .Setup(x => x.ValidateImportFileSetAsync(It.IsAny<Guid>(), It.IsAny<IEnumerable<PropertyIds>>(), It.IsAny<ImportFileSetContents>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Failure(errors));
        _mockGetProperties.Setup(x => x.GetPropertiesForDataImport(It.IsAny<UserAccessModel>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(propertyIds));

        var result = await sut.ParseDataImportRequestAsync(request, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(errors.Count, result.Error.Count);
        Assert.True(errors.All(x => result.Error.Contains(x.ErrorMessage)));

        _mockFileReader.Verify(x => x.ReadInputFormFileCollectionAsync(request.ImportFileSet, It.IsAny<CancellationToken>()), Times.Once);
        _mockFileReader.VerifyNoOtherCalls();

        _mockValidator.Verify(x => x.ValidateImportFileSetAsync(request.WoodlandOwnerId, propertyIds, fileSet, It.IsAny<CancellationToken>()), Times.Once);
        _mockValidator.VerifyNoOtherCalls();

        _mockImportApplications.VerifyNoOtherCalls();

        _mockAudit.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.UserId == request.UserAccessModel.UserAccountId
                && a.ActorType == ActorType.ExternalApplicant
                && a.CorrelationId == _correlationId
                && a.SourceEntityId == request.WoodlandOwnerId
                && a.SourceEntityType == SourceEntityType.WoodlandOwner
                && a.EventName == AuditEvents.ImportDataFromCsvFailure
                && JsonSerializer.Serialize(a.AuditData, _serializerOptions) ==
                JsonSerializer.Serialize(new
                {
                    FellingLicenceApplicationsInImportFile = fileSet.ApplicationSourceRecords.Count,
                    ValidationErrors = errors.Select(y => y.ErrorMessage).ToList(),
                    Error = "Validation failed for provided import file collection"
                }, _serializerOptions)),
            It.IsAny<CancellationToken>()), Times.Once);
        _mockAudit.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task ParseShouldReturnSuccessWhenFilesetPassesValidation(
        DataImportRequest request,
        ImportFileSetContents fileSet,
        IEnumerable<PropertyIds> propertyIds)
    {
        var sut = CreateSut();

        _mockFileReader
            .Setup(x => x.ReadInputFormFileCollectionAsync(It.IsAny<FormFileCollection>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<ImportFileSetContents, List<string>>(fileSet));
        _mockValidator
            .Setup(x => x.ValidateImportFileSetAsync(It.IsAny<Guid>(), It.IsAny<IEnumerable<PropertyIds>>(), It.IsAny<ImportFileSetContents>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Success<List<ValidationFailure>>());
        _mockGetProperties.Setup(x => x.GetPropertiesForDataImport(It.IsAny<UserAccessModel>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(propertyIds));

        var result = await sut.ParseDataImportRequestAsync(request, CancellationToken.None);

        Assert.True(result.IsSuccess);

        _mockFileReader.Verify(x => x.ReadInputFormFileCollectionAsync(request.ImportFileSet, It.IsAny<CancellationToken>()), Times.Once);
        _mockFileReader.VerifyNoOtherCalls();

        _mockValidator.Verify(x => x.ValidateImportFileSetAsync(request.WoodlandOwnerId, propertyIds, fileSet, It.IsAny<CancellationToken>()), Times.Once);
        _mockValidator.VerifyNoOtherCalls();

        _mockImportApplications.VerifyNoOtherCalls();

        _mockAudit.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task ImportShouldReturnFailureWhenGetPropertiesFails(
        UserAccessModel user,
        Guid woodlandOwnerId,
        ImportFileSetContents fileSet,
        string error)
    {
        var sut = CreateSut();

        _mockGetProperties.Setup(x => x.GetPropertiesForDataImport(It.IsAny<UserAccessModel>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<IEnumerable<PropertyIds>>(error));

        var result = await sut.ImportDataAsync(user, woodlandOwnerId, fileSet, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Single(result.Error);
        Assert.Equal("Failed to retrieve properties for woodland owner", result.Error.Single());

        _mockFileReader.VerifyNoOtherCalls();

        _mockValidator.VerifyNoOtherCalls();

        _mockImportApplications.VerifyNoOtherCalls();

        _mockGetProperties.Verify(x => x.GetPropertiesForDataImport(user, woodlandOwnerId, It.IsAny<CancellationToken>()), Times.Once);
        _mockGetProperties.VerifyNoOtherCalls();

        _mockAudit.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.UserId == user.UserAccountId
                && a.ActorType == ActorType.ExternalApplicant
                && a.CorrelationId == _correlationId
                && a.SourceEntityId == woodlandOwnerId
                && a.SourceEntityType == SourceEntityType.WoodlandOwner
                && a.EventName == AuditEvents.ImportDataFromCsvFailure
                && JsonSerializer.Serialize(a.AuditData, _serializerOptions) ==
                JsonSerializer.Serialize(new
                {
                    FellingLicenceApplicationsInImportFile = (int?)null,
                    ValidationErrors = new List<string> { error },
                    Error = "Failed to retrieve properties for woodland owner"
                }, _serializerOptions)),
            It.IsAny<CancellationToken>()), Times.Once);
        _mockAudit.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task ImportShouldReturnFailureWhenValidationFails(
        UserAccessModel user,
        Guid woodlandOwnerId,
        ImportFileSetContents fileSet,
        IEnumerable<PropertyIds> properties,
        List<ValidationFailure> errors)
    {
        var sut = CreateSut();

        _mockGetProperties.Setup(x => x.GetPropertiesForDataImport(It.IsAny<UserAccessModel>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(properties));
        _mockValidator
            .Setup(x => x.ValidateImportFileSetAsync(It.IsAny<Guid>(), It.IsAny<IEnumerable<PropertyIds>>(), It.IsAny<ImportFileSetContents>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Failure(errors));

        var result = await sut.ImportDataAsync(user, woodlandOwnerId, fileSet, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(errors.Count, result.Error.Count);
        Assert.True(errors.All(x => result.Error.Contains(x.ErrorMessage)));

        _mockFileReader.VerifyNoOtherCalls();

        _mockValidator.Verify(x => x.ValidateImportFileSetAsync(woodlandOwnerId, properties, fileSet, It.IsAny<CancellationToken>()), Times.Once);
        _mockValidator.VerifyNoOtherCalls();

        _mockImportApplications.VerifyNoOtherCalls();

        _mockAudit.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.UserId == user.UserAccountId
                && a.ActorType == ActorType.ExternalApplicant
                && a.CorrelationId == _correlationId
                && a.SourceEntityId == woodlandOwnerId
                && a.SourceEntityType == SourceEntityType.WoodlandOwner
                && a.EventName == AuditEvents.ImportDataFromCsvFailure
                && JsonSerializer.Serialize(a.AuditData, _serializerOptions) ==
                JsonSerializer.Serialize(new
                {
                    fellingLicenceApplicationsInImportFile = fileSet.ApplicationSourceRecords.Count,
                    validationErrors = errors.Select(y => y.ErrorMessage).ToList(),
                    error = "Validation failed for provided import file collection"
                }, _serializerOptions)),
            It.IsAny<CancellationToken>()), Times.Once);
        _mockAudit.VerifyNoOtherCalls();
    }

    [Theory, AutoMoqData]
    public async Task ImportShouldReturnFailureWhenApplicationImportFails(
        UserAccessModel user,
        Guid woodlandOwnerId,
        ImportFileSetContents fileSet,
        IEnumerable<PropertyIds> properties,
        string error)
    {
        var sut = CreateSut();

        _mockValidator
            .Setup(x => x.ValidateImportFileSetAsync(It.IsAny<Guid>(), It.IsAny<IEnumerable<PropertyIds>>(), It.IsAny<ImportFileSetContents>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Success<List<ValidationFailure>>());
        _mockImportApplications
            .Setup(x => x.RunDataImportAsync(It.IsAny<ImportApplicationsRequest>(), It.IsAny<RequestContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<Dictionary<Guid, string>>(error));
        _mockGetProperties.Setup(x => x.GetPropertiesForDataImport(It.IsAny<UserAccessModel>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(properties));

        var result = await sut.ImportDataAsync(user, woodlandOwnerId, fileSet, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Single(result.Error);
        Assert.Equal(error, result.Error.Single());

        _mockFileReader.VerifyNoOtherCalls();

        _mockValidator.Verify(x => x.ValidateImportFileSetAsync(woodlandOwnerId, properties, fileSet, It.IsAny<CancellationToken>()), Times.Once);
        _mockValidator.VerifyNoOtherCalls();

        _mockImportApplications.Verify(x => x.RunDataImportAsync(
            It.Is<ImportApplicationsRequest>(i => 
                i.UserId == user.UserAccountId
                && i.WoodlandOwnerId == woodlandOwnerId
                && i.PropertyIds == properties
                && i.ApplicationRecords == fileSet.ApplicationSourceRecords
                && i.FellingRecords == fileSet.ProposedFellingSourceRecords
                && i.RestockingRecords == fileSet.ProposedRestockingSourceRecords),
            It.IsAny<RequestContext>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockImportApplications.VerifyNoOtherCalls();

        _mockAudit.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.UserId == user.UserAccountId
                && a.ActorType == ActorType.ExternalApplicant
                && a.CorrelationId == _correlationId
                && a.SourceEntityId == woodlandOwnerId
                && a.SourceEntityType == SourceEntityType.WoodlandOwner
                && a.EventName == AuditEvents.ImportDataFromCsvFailure
                && JsonSerializer.Serialize(a.AuditData, _serializerOptions) ==
                JsonSerializer.Serialize(new
                {
                    fellingLicenceApplicationsInImportFile = fileSet.ApplicationSourceRecords.Count,
                    validationErrors = (List<string>?)null,
                    error = "Failed to import felling licence applications provided: " + error
                }, _serializerOptions)),
            It.IsAny<CancellationToken>()), Times.Once);
        _mockAudit.VerifyNoOtherCalls();
    }
    
    [Theory, AutoMoqData]
    public async Task ShouldReturnSuccessWhenImportsSucceed(
        UserAccessModel user,
        Guid woodlandOwnerId,
        ImportFileSetContents fileSet,
        IEnumerable<PropertyIds> propertyIds,
        Dictionary<Guid, string> applicationsImported)
    {
        var sut = CreateSut();

        _mockValidator
            .Setup(x => x.ValidateImportFileSetAsync(It.IsAny<Guid>(), It.IsAny<IEnumerable<PropertyIds>>(), It.IsAny<ImportFileSetContents>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Success<List<ValidationFailure>>());
        _mockImportApplications
            .Setup(x => x.RunDataImportAsync(It.IsAny<ImportApplicationsRequest>(), It.IsAny<RequestContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(applicationsImported));
        _mockGetProperties.Setup(x => x.GetPropertiesForDataImport(It.IsAny<UserAccessModel>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(propertyIds));

        var result = await sut.ImportDataAsync(user, woodlandOwnerId, fileSet, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(result.Value, applicationsImported);

        _mockFileReader.VerifyNoOtherCalls();

        _mockValidator.Verify(x => x.ValidateImportFileSetAsync(woodlandOwnerId, propertyIds, fileSet, It.IsAny<CancellationToken>()), Times.Once);
        _mockValidator.VerifyNoOtherCalls();

        _mockImportApplications.Verify(x => x.RunDataImportAsync(
            It.Is<ImportApplicationsRequest>(i =>
                i.PropertyIds == propertyIds
                && i.ApplicationRecords == fileSet.ApplicationSourceRecords
                && i.FellingRecords == fileSet.ProposedFellingSourceRecords
                && i.RestockingRecords == fileSet.ProposedRestockingSourceRecords),
            It.IsAny<RequestContext>(),
            It.IsAny<CancellationToken>()), Times.Once);
        _mockImportApplications.VerifyNoOtherCalls();

        _mockAudit.Verify(x => x.PublishAuditEventAsync(It.Is<AuditEvent>(a =>
                a.UserId == user.UserAccountId
                && a.ActorType == ActorType.ExternalApplicant
                && a.CorrelationId == _correlationId
                && a.SourceEntityId == woodlandOwnerId
                && a.SourceEntityType == SourceEntityType.WoodlandOwner
                && a.EventName == AuditEvents.ImportDataFromCsv
                && JsonSerializer.Serialize(a.AuditData, _serializerOptions) ==
                JsonSerializer.Serialize(new
                {
                    FellingLicenceApplicationsInImportFile = fileSet.ApplicationSourceRecords.Count,
                    FellingLicenceApplicationsImported = applicationsImported
                }, _serializerOptions)),
            It.IsAny<CancellationToken>()), Times.Once);
        _mockAudit.VerifyNoOtherCalls();
    }

    private ImportDataService CreateSut()
    {
        _mockImportApplications.Reset();
        _mockGetProperties.Reset();
        _mockValidator.Reset();
        _mockFileReader.Reset();
        _mockAudit.Reset();
        
        _correlationId = Guid.NewGuid().ToString();
        var user = UserFactory.CreateExternalApplicantIdentityProviderClaimsPrincipal(localAccountId: Guid.NewGuid());
        var requestContext = new RequestContext(
            _correlationId,
            new RequestUserModel(user));

        return new ImportDataService(
            _mockFileReader.Object,
            _mockValidator.Object,
            _mockGetProperties.Object,
            _mockImportApplications.Object,
            _mockAudit.Object,
            requestContext,
            new NullLogger<ImportDataService>());
    }
}