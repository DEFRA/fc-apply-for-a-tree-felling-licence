using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.PropertyProfiles.Entities;
using Forestry.Flo.Services.PropertyProfiles.Repositories;
using Forestry.Flo.Tests.Common;
using Xunit;

namespace Forestry.Flo.Services.PropertyProfiles.Tests.Repositories;

public class PropertyProfileRepositoryTests
{
    private readonly PropertyProfilesContext _propertyProfilesContext;
    private readonly PropertyProfileRepository _sut;
    
    public PropertyProfileRepositoryTests()
    {
        _propertyProfilesContext = TestPropertyProfilesDatabaseFactory.CreateDefaultTestContext();
        _sut = new PropertyProfileRepository(_propertyProfilesContext);
    }

    [Theory, AutoMoqData]
    public async Task ShouldGetPropertyProfile_GivenId(PropertyProfile propertyProfile)
    {
        //arrange
        _propertyProfilesContext.PropertyProfiles.Add(propertyProfile);
        await _propertyProfilesContext.SaveChangesAsync();
        
        //act
        var result = await _sut.GetByIdAsync(propertyProfile.Id, CancellationToken.None);

        //assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be(propertyProfile.Name);
    }
    
    [Theory, AutoMoqData]
    public async Task ShouldGetPropertyProfileWithCompartments_GivenPropertyId(PropertyProfile propertyProfile)
    {
        //arrange
        _propertyProfilesContext.PropertyProfiles.Add(propertyProfile);
        propertyProfile.Compartments.ForEach(c => c.PropertyProfileId = propertyProfile.Id);
        _propertyProfilesContext.Compartments.AddRange(propertyProfile.Compartments);
        await _propertyProfilesContext.SaveChangesAsync();
        
        //act
        var result = await _sut.GetByIdAsync(propertyProfile.Id, CancellationToken.None);

        //assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Compartments.Should().HaveCount(propertyProfile.Compartments.Count);
        result.Value.Compartments.First().Should().BeEquivalentTo(propertyProfile.Compartments.First());
    }
    
    [Theory, AutoMoqData]
    public async Task ShouldReturnNoFound_GivenNotExistingPropertyProfileId(PropertyProfile propertyProfile)
    {
        //arrange
        _propertyProfilesContext.PropertyProfiles.Add(propertyProfile);
        
        //act
        var result =  await _sut.GetByIdAsync(propertyProfile.Id, CancellationToken.None);

        //assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(UserDbErrorReason.NotFound);
    }
    
    [Theory, AutoMoqData]
    public async Task ShouldGetPropertyProfile_GivenIdAndWoodlandOwner(PropertyProfile propertyProfile)
    {
        //arrange
        _propertyProfilesContext.PropertyProfiles.Add(propertyProfile);
        await _propertyProfilesContext.SaveChangesAsync();
        
        //act
        var result = await _sut.GetAsync(propertyProfile.Id, propertyProfile.WoodlandOwnerId, CancellationToken.None);

        //assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be(propertyProfile.Name);
    }
    
    [Theory, AutoMoqData]
    public async Task ShouldGetPropertyProfileWithCompartments_GivenPropertyIdAndWoodlandOwner(PropertyProfile propertyProfile)
    {
        //arrange
        _propertyProfilesContext.PropertyProfiles.Add(propertyProfile);
        propertyProfile.Compartments.ForEach(c => c.PropertyProfileId = propertyProfile.Id);
        _propertyProfilesContext.Compartments.AddRange(propertyProfile.Compartments);
        await _propertyProfilesContext.SaveChangesAsync();
        
        //act
        var result = await _sut.GetAsync(propertyProfile.Id, propertyProfile.WoodlandOwnerId, CancellationToken.None);

        //assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Compartments.Should().HaveCount(propertyProfile.Compartments.Count);
        result.Value.Compartments.First().Should().BeEquivalentTo(propertyProfile.Compartments.First());
    }
    
    [Theory, AutoMoqData]
    public async Task ShouldListPropertyProfiles_GivenWoodlandOwnerId(List<PropertyProfile> propertyProfiles, Guid woodlandOwnerId)
    {
        //arrange
        propertyProfiles.ForEach(p => p.WoodlandOwnerId = woodlandOwnerId);
        _propertyProfilesContext.PropertyProfiles.AddRange(propertyProfiles);
        await _propertyProfilesContext.SaveChangesAsync();
        
        //act
        var result = (await _sut.ListAsync(new ListPropertyProfilesQuery(woodlandOwnerId) , CancellationToken.None)).ToList();

        //assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(propertyProfiles);
    }

    [Theory, AutoMoqData]
    public async Task ShouldListPropertyProfiles_GivenWoodlandOwnerIdAndPropertyProfileIds(List<PropertyProfile> propertyProfiles, Guid woodlandOwnerId)
    {
        //arrange
        propertyProfiles.ForEach(p => p.WoodlandOwnerId = woodlandOwnerId);
        _propertyProfilesContext.PropertyProfiles.AddRange(propertyProfiles);
        await _propertyProfilesContext.SaveChangesAsync();
        
        //act
        var result = (await _sut.ListAsync(new ListPropertyProfilesQuery(woodlandOwnerId, new []{propertyProfiles.Last().Id}) , CancellationToken.None)).ToList();

        //assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result.First().Should().BeEquivalentTo(propertyProfiles.Last());
    }
    
    [Theory, AutoMoqData]
    public async Task ShouldReturnNoFound_GivenNotExistingPropertyProfileIdAndWoodlandOwner(PropertyProfile propertyProfile)
    {
        //arrange
        //act
        var result = await _sut.GetAsync(propertyProfile.Id, propertyProfile.WoodlandOwnerId, CancellationToken.None);

        //assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(UserDbErrorReason.NotFound);
    }
    
    [Theory, AutoMoqData]
    public async Task ShouldReturnNoFound_GivenPropertyProfileIdAndNotExistingWoodlandOwner(PropertyProfile propertyProfile, Guid wrongOwnerId)
    {
        //arrange
        _propertyProfilesContext.PropertyProfiles.Add(propertyProfile);
        
        //act
        var result = await _sut.GetAsync(propertyProfile.Id, wrongOwnerId, CancellationToken.None);

        //assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(UserDbErrorReason.NotFound);
    }


    [Theory, AutoMoqData]
    public async Task ShouldUpdatePropertyProfile_GivenExistingPropertyProfile(TestPropertyProfile propertyProfile)
    {
        //arrange
        _propertyProfilesContext.PropertyProfiles.Add(propertyProfile);
        await _propertyProfilesContext.SaveChangesAsync();
        
        var updated = new TestPropertyProfile(propertyProfile);
        updated.SetName("Test");
        
        //act
        var updateResult =  await _sut.UpdateAsync(updated);
        var saveResult = await _sut.UnitOfWork.SaveEntitiesAsync(CancellationToken.None);
         
        //assert
        updateResult.IsSuccess.Should().BeTrue();
        saveResult.IsSuccess.Should().BeTrue();
        var result = await _propertyProfilesContext.PropertyProfiles.FindAsync(propertyProfile.Id);
        result.Should().NotBeNull();
        result!.Name.Should().Be(updated.Name);
    }
    
    [Theory, AutoMoqData]
    public async Task ShouldReturnNotFound_WhenUpdatePropertyProfile_GivenNotExistingPropertyProfile(TestPropertyProfile propertyProfile)
    {
        //arrange
        var updated = new TestPropertyProfile(propertyProfile);
        updated.SetName("Test");
        
        //act
        var result =  await _sut.UpdateAsync(updated);
         
        //assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserDbErrorReason.NotFound);
    }

    [Theory, AutoMoqData]
    public async Task WhenUserCanAccessPropertyProfileAsIsTheWoodlandOwner(PropertyProfile propertyProfile)
    {
        //arrange
        _propertyProfilesContext.PropertyProfiles.Add(propertyProfile);
        await _propertyProfilesContext.SaveChangesAsync();

        var userAccessModel = new UserAccessModel
        {
            AgencyId = null,
            IsFcUser = false,
            UserAccountId = Guid.NewGuid(),
            WoodlandOwnerIds = new List<Guid> { propertyProfile.WoodlandOwnerId}
        };

        //act
        var result = await _sut.CheckUserCanAccessPropertyProfileAsync(propertyProfile.Id, userAccessModel , new CancellationToken());

        //assert
        result.IsSuccess.Should().BeTrue();
        Assert.True(result.Value);
    }

    [Theory, AutoMoqData]
    public async Task WhenUserCanAccessPropertyProfileAsIsAnAgentHavingAccessToTheWoodland(PropertyProfile propertyProfile)
    {
        //arrange
        _propertyProfilesContext.PropertyProfiles.Add(propertyProfile);
        await _propertyProfilesContext.SaveChangesAsync();

        var userAccessModel = new UserAccessModel
        {
            AgencyId = Guid.NewGuid(),
            IsFcUser = false,
            UserAccountId = Guid.NewGuid(),
            WoodlandOwnerIds = new List<Guid> { propertyProfile.WoodlandOwnerId, Guid.NewGuid(), Guid.NewGuid() }
        };

        //act
        var result = await _sut.CheckUserCanAccessPropertyProfileAsync(propertyProfile.Id, userAccessModel, new CancellationToken());

        //assert
        result.IsSuccess.Should().BeTrue();
        Assert.True(result.Value);
    }

    [Theory, AutoMoqData]
    public async Task WhenUserCanAccessPropertyProfileAsIsFcAgent(PropertyProfile propertyProfile)
    {
        //arrange
        _propertyProfilesContext.PropertyProfiles.Add(propertyProfile);
        await _propertyProfilesContext.SaveChangesAsync();

        var userAccessModel = new UserAccessModel
        {
            AgencyId = null,
            IsFcUser = true,
            UserAccountId = Guid.NewGuid(),
            WoodlandOwnerIds = new List<Guid>()
        };

        //act
        var result = await _sut.CheckUserCanAccessPropertyProfileAsync(propertyProfile.Id, userAccessModel, new CancellationToken());

        //assert
        result.IsSuccess.Should().BeTrue();
        Assert.True(result.Value);
    }

    [Theory, AutoMoqData]
    public async Task WhenUserCannotAccessPropertyProfile(PropertyProfile propertyProfile)
    {
        //arrange
        _propertyProfilesContext.PropertyProfiles.Add(propertyProfile);
        await _propertyProfilesContext.SaveChangesAsync();

        var userAccessModel = new UserAccessModel
        {
            AgencyId = null,
            IsFcUser = false,
            UserAccountId = Guid.NewGuid(),
            WoodlandOwnerIds = new List<Guid>{Guid.NewGuid()}
        };

        //act
        var result = await _sut.CheckUserCanAccessPropertyProfileAsync(propertyProfile.Id, userAccessModel, new CancellationToken());

        //assert
        result.IsSuccess.Should().BeTrue();
        Assert.False(result.Value);
    }

    [Theory, AutoMoqData]
    public async Task WhenUserCanAccessPropertyProfilesAsIsTheWoodlandOwner(PropertyProfile propertyProfile)
    {
        //arrange
        _propertyProfilesContext.PropertyProfiles.Add(propertyProfile);
        await _propertyProfilesContext.SaveChangesAsync();

        var query = new ListPropertyProfilesQuery(propertyProfile.WoodlandOwnerId);

        var userAccessModel = new UserAccessModel
        {
            AgencyId = null,
            IsFcUser = false,
            UserAccountId = Guid.NewGuid(),
            WoodlandOwnerIds = new List<Guid> { propertyProfile.WoodlandOwnerId }
        };

        //act
        var result = await _sut.CheckUserCanAccessPropertyProfilesAsync(query, userAccessModel, new CancellationToken());

        //assert
        result.IsSuccess.Should().BeTrue();
        Assert.True(result.Value);
    }

    [Theory, AutoMoqData]
    public async Task WhenUserCanAccessPropertyProfilesAsIsFcAgent(PropertyProfile propertyProfile)
    {
        //arrange
        _propertyProfilesContext.PropertyProfiles.Add(propertyProfile);
        await _propertyProfilesContext.SaveChangesAsync();

        var query = new ListPropertyProfilesQuery(propertyProfile.WoodlandOwnerId);

        var userAccessModel = new UserAccessModel
        {
            AgencyId = null,
            IsFcUser = true,
            UserAccountId = Guid.NewGuid(),
            WoodlandOwnerIds = new List<Guid>()
        };

        //act
        var result = await _sut.CheckUserCanAccessPropertyProfilesAsync(query, userAccessModel, new CancellationToken());

        //assert
        result.IsSuccess.Should().BeTrue();
        Assert.True(result.Value);
    }

    [Theory, AutoMoqData]
    public async Task WhenUserCanAccessPropertyProfilesAsIsAnAgentHavingAccessToTheWoodland(PropertyProfile propertyProfile)
    {
        //arrange
        _propertyProfilesContext.PropertyProfiles.Add(propertyProfile);
        await _propertyProfilesContext.SaveChangesAsync();

        var query = new ListPropertyProfilesQuery(propertyProfile.WoodlandOwnerId);

        var userAccessModel = new UserAccessModel
        {
            AgencyId = Guid.NewGuid(),
            IsFcUser = true,
            UserAccountId = Guid.NewGuid(),
            WoodlandOwnerIds = new List<Guid> { propertyProfile.WoodlandOwnerId, Guid.NewGuid(), Guid.NewGuid() }
        };

        //act
        var result = await _sut.CheckUserCanAccessPropertyProfilesAsync(query, userAccessModel, new CancellationToken());

        //assert
        result.IsSuccess.Should().BeTrue();
        Assert.True(result.Value);
    }

    [Theory, AutoMoqData]
    public async Task WhenUserCannotAccessPropertyProfiles(PropertyProfile propertyProfile)
    {
        //arrange
        _propertyProfilesContext.PropertyProfiles.Add(propertyProfile);
        await _propertyProfilesContext.SaveChangesAsync();

        var query = new ListPropertyProfilesQuery(propertyProfile.WoodlandOwnerId);

        var userAccessModel = new UserAccessModel
        {
            AgencyId = null,
            IsFcUser = false,
            UserAccountId = Guid.NewGuid(),
            WoodlandOwnerIds = new List<Guid> { Guid.NewGuid() }
        };

        //act
        var result = await _sut.CheckUserCanAccessPropertyProfilesAsync(query, userAccessModel, new CancellationToken());

        //assert
        result.IsSuccess.Should().BeTrue();
        Assert.False(result.Value);
    }

    [Theory, AutoMoqData]
    public async Task WhenUserCannotAccessOneOrMoreSpecificPropertyProfilesRequestedInQueryThenShouldFail(PropertyProfile propertyProfile)
    {
        //arrange
        _propertyProfilesContext.PropertyProfiles.Add(propertyProfile);
        await _propertyProfilesContext.SaveChangesAsync();

        var query = new ListPropertyProfilesQuery(
            propertyProfile.WoodlandOwnerId,  //good
            new List<Guid>{Guid.NewGuid()} //bad
            );

        var userAccessModel = new UserAccessModel
        {
            AgencyId = null,
            IsFcUser = false,
            UserAccountId = Guid.NewGuid(),
            WoodlandOwnerIds = new List<Guid> {propertyProfile.Id, Guid.NewGuid() }
        };

        //act
        var result = await _sut.CheckUserCanAccessPropertyProfilesAsync(query, userAccessModel, new CancellationToken());

        //assert
        result.IsFailure.Should().BeTrue();
    }

    [Theory, AutoMoqData]
    public async Task WhenUserHasNoPropertyProfilesToRetrieveThenShouldBeSuccess(PropertyProfile propertyProfile)
    {
        //arrange
        _propertyProfilesContext.PropertyProfiles.RemoveRange(_propertyProfilesContext.PropertyProfiles.ToList());
        await _propertyProfilesContext.SaveChangesAsync();

        var query = new ListPropertyProfilesQuery(
            propertyProfile.WoodlandOwnerId);

        var userAccessModel = new UserAccessModel
        {
            AgencyId = null,
            IsFcUser = false,
            UserAccountId = Guid.NewGuid(),
            WoodlandOwnerIds = new List<Guid> { propertyProfile.WoodlandOwnerId }
        };

        //act
        var result = await _sut.CheckUserCanAccessPropertyProfilesAsync(query, userAccessModel, new CancellationToken());

        //assert
        result.IsSuccess.Should().BeTrue();
    }

    [Theory, AutoMoqData]
    public async Task ShouldAddPropertyProfile_GivenValidPropertyProfile(PropertyProfile propertyProfile)
    {
        //arrange
       
        //act
        _sut.Add(propertyProfile);
         var saveResult = await _sut.UnitOfWork.SaveEntitiesAsync(CancellationToken.None);
         
        //assert
        saveResult.IsSuccess.Should().BeTrue();
        var result = await _propertyProfilesContext.PropertyProfiles.FindAsync(propertyProfile.Id);
        result.Should().NotBeNull();
        result!.Name.Should().Be(propertyProfile.Name);
    }
}