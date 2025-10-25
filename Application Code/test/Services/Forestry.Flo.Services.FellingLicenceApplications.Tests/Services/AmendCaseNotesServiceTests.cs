using System;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.FellingLicenceApplications.Models;
using Forestry.Flo.Services.FellingLicenceApplications.Repositories;
using Forestry.Flo.Services.FellingLicenceApplications.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NodaTime;
using Xunit;

namespace Forestry.Flo.Services.FellingLicenceApplications.Tests.Services;

public class AmendCaseNotesServiceTests
{
    private readonly Mock<IFellingLicenceApplicationInternalRepository> _fellingLicenceApplicationRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IClock> _clockMock = new();
    private readonly DateTime _now = DateTime.UtcNow;

    [Theory, AutoData]
    public async Task WhenRepositoryThrows(AddCaseNoteRecord addCaseNote, Guid userId)
    {
        var sut = CreateSut();

        _fellingLicenceApplicationRepository
            .Setup(x => x.AddCaseNoteAsync(It.IsAny<Entities.CaseNote>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Test Exception"));

        var result = await sut.AddCaseNoteAsync(addCaseNote, userId, default);
        Assert.True(result.IsFailure);

        _fellingLicenceApplicationRepository.Verify(x => x.AddCaseNoteAsync(It.Is<Entities.CaseNote>(c =>
            c.FellingLicenceApplicationId == addCaseNote.FellingLicenceApplicationId
            && c.Type == addCaseNote.Type
            && c.Text == addCaseNote.Text
            && c.VisibleToApplicant == addCaseNote.VisibleToApplicant
            && c.VisibleToConsultee == addCaseNote.VisibleToConsultee
            && c.CreatedTimestamp == _now
            && c.CreatedByUserId == userId), It.IsAny<CancellationToken>()), Times.Once);
        _fellingLicenceApplicationRepository.VerifyNoOtherCalls();
        _unitOfWork.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task WhenSaveReturnsError(AddCaseNoteRecord addCaseNote, Guid userId)
    {
        var sut = CreateSut();

        _unitOfWork
            .Setup(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Failure(UserDbErrorReason.General));

        var result = await sut.AddCaseNoteAsync(addCaseNote, userId, CancellationToken.None);
        Assert.True(result.IsFailure);

        _fellingLicenceApplicationRepository.Verify(x => x.AddCaseNoteAsync(It.Is<Entities.CaseNote>(c =>
            c.FellingLicenceApplicationId == addCaseNote.FellingLicenceApplicationId
            && c.Type == addCaseNote.Type
            && c.Text == addCaseNote.Text
            && c.VisibleToApplicant == addCaseNote.VisibleToApplicant
            && c.VisibleToConsultee == addCaseNote.VisibleToConsultee
            && c.CreatedTimestamp == _now
            && c.CreatedByUserId == userId), It.IsAny<CancellationToken>()), Times.Once);

        _unitOfWork.Verify(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);

        _fellingLicenceApplicationRepository.VerifyNoOtherCalls();
        _unitOfWork.VerifyNoOtherCalls();
    }

    [Theory, AutoData]
    public async Task WhenSaveSuccess(AddCaseNoteRecord addCaseNote, Guid userId)
    {
        var sut = CreateSut();

        _unitOfWork
            .Setup(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(UnitResult.Success<UserDbErrorReason>());

        var result = await sut.AddCaseNoteAsync(addCaseNote, userId, CancellationToken.None);
        Assert.True(result.IsSuccess);

        _fellingLicenceApplicationRepository.Verify(x => x.AddCaseNoteAsync(It.Is<Entities.CaseNote>(c =>
            c.FellingLicenceApplicationId == addCaseNote.FellingLicenceApplicationId
            && c.Type == addCaseNote.Type
            && c.Text == addCaseNote.Text
            && c.VisibleToApplicant == addCaseNote.VisibleToApplicant
            && c.VisibleToConsultee == addCaseNote.VisibleToConsultee
            && c.CreatedTimestamp == _now
            && c.CreatedByUserId == userId), It.IsAny<CancellationToken>()), Times.Once);

        _unitOfWork.Verify(x => x.SaveEntitiesAsync(It.IsAny<CancellationToken>()), Times.Once);

        _fellingLicenceApplicationRepository.VerifyNoOtherCalls();
        _unitOfWork.VerifyNoOtherCalls();
    }

    private AmendCaseNotes CreateSut()
    {
        _fellingLicenceApplicationRepository.Reset();
        _unitOfWork.Reset();

        _fellingLicenceApplicationRepository.SetupGet(x => x.UnitOfWork).Returns(_unitOfWork.Object);
        _clockMock.Setup(x => x.GetCurrentInstant()).Returns(Instant.FromDateTimeUtc(_now));

        return new AmendCaseNotes(
            _fellingLicenceApplicationRepository.Object,
            _clockMock.Object,
            new NullLogger<AmendCaseNotes>());
    }
}