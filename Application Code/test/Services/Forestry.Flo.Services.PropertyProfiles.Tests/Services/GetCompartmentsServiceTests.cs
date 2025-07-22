using System;
using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.PropertyProfiles.Entities;
using Forestry.Flo.Services.PropertyProfiles.Repositories;
using Forestry.Flo.Services.PropertyProfiles.Services;
using Forestry.Flo.Tests.Common;
using Moq;
using Xunit;

namespace Forestry.Flo.Services.PropertyProfiles.Tests.Services
{
    public class GetCompartmentsServiceTests
    {
        private readonly Mock<ICompartmentRepository> _mockGetCompartmentRepository = new();

        [Theory, AutoMoqData]
        public async Task GetCompartmentByIdWhenUserHasAccess(Compartment compartment)
        {
            //arrange
            var sut = CreateSut();

            _mockGetCompartmentRepository
                .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Success<Compartment, UserDbErrorReason>(compartment));

            //act
            var result = await sut.GetCompartmentByIdAsync(compartment.Id, new UserAccessModel(), new CancellationToken());

            //assert
            Assert.True(result.IsSuccess);
            Assert.Equal(compartment.Id, result.Value.Id);
            _mockGetCompartmentRepository.Verify(x=>x.GetByIdAsync(compartment.Id,It.IsAny<CancellationToken>()), Times.Once);
        }

        [Theory, AutoMoqData]
        public async Task CannotGetCompartmentByIdWhenUserDoesNotHaveAccess(Compartment compartment)
        {
            //arrange
            var sut = CreateSut(canAccess:false);

            //act
            var result = await sut.GetCompartmentByIdAsync(compartment.Id, new UserAccessModel(), new CancellationToken());

            //assert
            Assert.True(result.IsFailure);
            _mockGetCompartmentRepository.Verify(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task CannotGetCompartmentWhenIdIsNotFound()
        {
            //arrange
            var sut = CreateSut();

            _mockGetCompartmentRepository
                .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Failure<Compartment, UserDbErrorReason>(UserDbErrorReason.NotFound));

            //act
            var result = await sut.GetCompartmentByIdAsync(Guid.NewGuid(), new UserAccessModel(), new CancellationToken());

            //assert
            Assert.True(result.IsFailure);
            _mockGetCompartmentRepository.Verify(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())
                , Times.Once);
        }

        private GetCompartmentsService CreateSut(bool canAccess = true)
        {
            _mockGetCompartmentRepository.Reset();

            _mockGetCompartmentRepository
                .Setup(x => x.CheckUserCanAccessCompartmentAsync(It.IsAny<Guid>(), It.IsAny<UserAccessModel>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result.Success(canAccess));
            
            return new GetCompartmentsService(_mockGetCompartmentRepository.Object);
        }
    }
}
