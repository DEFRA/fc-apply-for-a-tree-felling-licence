using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Models;
using Forestry.Flo.Services.PropertyProfiles.Entities;
using Forestry.Flo.Services.PropertyProfiles.Repositories;
using Forestry.Flo.Tests.Common;
using Xunit;

namespace Forestry.Flo.Services.PropertyProfiles.Tests.Repositories;

public class CompartmentRepositoryTests
{
    private readonly PropertyProfilesContext _propertyProfileContext;
    private readonly CompartmentRepository _sut;
    
    public CompartmentRepositoryTests()
    {
        _propertyProfileContext = TestPropertyProfilesDatabaseFactory.CreateDefaultTestContext();
        _sut = new CompartmentRepository(_propertyProfileContext);
    }

    [Theory, AutoMoqData]
    public async Task ShouldGetCompartment_GivenId(Compartment compartment)
    {
        //arrange
        _propertyProfileContext.Compartments.Add(compartment);
        await _propertyProfileContext.SaveChangesAsync();
        
        //act
        var result = await _sut.GetByIdAsync(compartment.Id, CancellationToken.None);

        //assert
        Assert.True(result.IsSuccess);
        Assert.Equal(compartment.CompartmentNumber, result.Value.CompartmentNumber);
    }
    
    [Theory, AutoMoqData]
    public async Task ShouldGetCompartmentWithPropertyProfile_GivenPropertyId(Compartment compartment)
    {
        //arrange
        _propertyProfileContext.Compartments.Add(compartment);
        _propertyProfileContext.PropertyProfiles.Add(compartment.PropertyProfile);
        await _propertyProfileContext.SaveChangesAsync();
        
        //act
        var result = await _sut.GetByIdAsync(compartment.Id, CancellationToken.None);

        //assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value.PropertyProfile);
        Assert.Equivalent(compartment.PropertyProfile, result.Value.PropertyProfile);
    }
    
    [Theory, AutoMoqData]
    public async Task ShouldReturnNoFound_GivenNotExistingCompartmentId(Compartment compartment)
    {
        //arrange
        _propertyProfileContext.Compartments.Add(compartment);
        
        //act
        var result =  await _sut.GetByIdAsync(compartment.Id, CancellationToken.None);

        //assert
        Assert.False(result.IsSuccess);
        Assert.Equal(UserDbErrorReason.NotFound, result.Error);
    }
    
    [Theory, AutoMoqData]
    public async Task ShouldGetCompartment_GivenIdAndWoodlandOwner(Compartment compartment)
    {
        //arrange
        _propertyProfileContext.Compartments.Add(compartment);
        _propertyProfileContext.PropertyProfiles.Add(compartment.PropertyProfile);
        await _propertyProfileContext.SaveChangesAsync();
        
        //act
        var result = await _sut.GetAsync(compartment.Id, compartment.PropertyProfile.WoodlandOwnerId, CancellationToken.None);

        //assert
        Assert.True(result.IsSuccess);
        Assert.Equal(compartment.CompartmentNumber, result.Value.CompartmentNumber);
    }
    
    [Theory, AutoMoqData]
    public async Task ShouldGetCompartmentWithPropertyProfile_GivenPropertyIdAndWoodlandOwner(Compartment compartment)
    {
        //arrange
        _propertyProfileContext.Compartments.Add(compartment);
        _propertyProfileContext.PropertyProfiles.Add(compartment.PropertyProfile);
        await _propertyProfileContext.SaveChangesAsync();
        
        //act
        var result = await _sut.GetAsync(compartment.Id, compartment.PropertyProfile.WoodlandOwnerId, CancellationToken.None);

        //assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value.PropertyProfile);
        Assert.Equivalent(compartment.PropertyProfile, result.Value.PropertyProfile);
    }
    
    [Theory, AutoMoqData]
    public async Task ShouldReturnNoFound_GivenNotExistingCompartmentIdAndWoodlandOwner(Compartment compartment)
    {
        //arrange
        //act
        var result = await _sut.GetAsync(compartment.Id, compartment.PropertyProfile.WoodlandOwnerId, CancellationToken.None);

        //assert
        Assert.False(result.IsSuccess);
        Assert.Equal(UserDbErrorReason.NotFound, result.Error);
    }
    
    [Theory, AutoMoqData]
    public async Task ShouldReturnNoFound_GivenCompartmentIdAndNotExistingWoodlandOwner(Compartment compartment, Guid wrongOwnerId)
    {
        //arrange
        _propertyProfileContext.Compartments.Add(compartment);
        
        //act
        var result = await _sut.GetAsync(compartment.Id, wrongOwnerId, CancellationToken.None);

        //assert
        Assert.False(result.IsSuccess);
        Assert.Equal(UserDbErrorReason.NotFound, result.Error);
    }

    [Theory, AutoMoqData]
    public async Task ShouldUpdateCompartmentGivenExistingCompartment(TestCompartment compartment)
    {
        //arrange
        _propertyProfileContext.Compartments.Add(compartment);
        await _propertyProfileContext.SaveChangesAsync();
        
        var updated = new TestCompartment(compartment);
        updated.SetCompartmentNumber("Test");
        
        //act
        await _sut.UpdateAsync(updated);
        var saveResult = await _sut.UnitOfWork.SaveEntitiesAsync(CancellationToken.None);
         
        //assert
        Assert.True(saveResult.IsSuccess);
        var result = await _propertyProfileContext.Compartments.FindAsync(compartment.Id);
        Assert.NotNull(result);
        Assert.Equal(updated.CompartmentNumber, result!.CompartmentNumber);
    }
    
    [Theory, AutoMoqData]
    public async Task ShouldReturnBotFound_WhenUpdateCompartment_GivenNotExistingCompartment(TestCompartment compartment)
    {
        //arrange
        var updated = new TestCompartment(compartment);
        updated.SetCompartmentNumber("Test");
        
        //act
        var updateResult = await _sut.UpdateAsync(updated);
         
        //assert
        Assert.False(updateResult.IsSuccess);
        Assert.Equal(UserDbErrorReason.NotFound, updateResult.Error);
    }
    
    [Theory, AutoMoqData]
    public async Task ShouldAddCompartment_GivenValidCompartment(Compartment compartment)
    {
        //arrange
       
        //act
        _sut.Add(compartment);
         var saveResult = await _sut.UnitOfWork.SaveEntitiesAsync(CancellationToken.None);
         
        //assert
        Assert.True(saveResult.IsSuccess);
        var result = await _propertyProfileContext.Compartments.FindAsync(compartment.Id);
        Assert.NotNull(result);
        Assert.Equal(compartment.CompartmentNumber, result!.CompartmentNumber);
    }

    [Theory, AutoMoqData]
    public async Task CanRetrieveListOfCompartmentsForIds(Compartment compartment1, Compartment compartment2, Compartment compartment3)
    {
        _sut.Add(compartment1);
        _sut.Add(compartment2);
        _sut.Add(compartment3);
        await _sut.UnitOfWork.SaveEntitiesAsync(CancellationToken.None);

        var ids = new List<Guid> { compartment1.Id, compartment2.Id, Guid.NewGuid() };

        var result = await _sut.ListAsync(ids, CancellationToken.None);

        Assert.Equal(2, result.Count());

        Assert.Contains(compartment1, result);
        Assert.Contains(compartment2, result);
    }

    [Theory, AutoMoqData]
    public async Task WhenUserCanAccessCompartmentAsIsTheWoodlandOwner(Compartment compartment)
    {
        //arrange
        _propertyProfileContext.Compartments.Add(compartment);
        _propertyProfileContext.PropertyProfiles.Add(compartment.PropertyProfile);
        await _propertyProfileContext.SaveChangesAsync();

        var userAccessModel = new UserAccessModel
        {
            AgencyId = null,
            IsFcUser = false,
            UserAccountId = Guid.NewGuid(),
            WoodlandOwnerIds = new List<Guid> { compartment.PropertyProfile.WoodlandOwnerId}
        };

        //act
        var result = await _sut.CheckUserCanAccessCompartmentAsync(compartment.Id, userAccessModel, new CancellationToken());

        //assert
        Assert.True(result.IsSuccess);
        Assert.True(result.Value);
    }

    [Theory, AutoMoqData]
    public async Task WhenUserCanAccessCompartmentAsIsFcAgent(Compartment compartment)
    {
        //arrange
        _propertyProfileContext.Compartments.Add(compartment);
        _propertyProfileContext.PropertyProfiles.Add(compartment.PropertyProfile);
        await _propertyProfileContext.SaveChangesAsync();

        var userAccessModel = new UserAccessModel
        {
            AgencyId = null,
            IsFcUser = true,
            UserAccountId = Guid.NewGuid(),
            WoodlandOwnerIds = new List<Guid>( )
        };

        //act
        var result = await _sut.CheckUserCanAccessCompartmentAsync(compartment.Id, userAccessModel, new CancellationToken());

        //assert
        Assert.True(result.IsSuccess);
        Assert.True(result.Value);
    }

    [Theory, AutoMoqData]
    public async Task WhenUserCannotAccessCompartment(Compartment compartment)
    {
        //arrange
        _propertyProfileContext.Compartments.Add(compartment);
        _propertyProfileContext.PropertyProfiles.Add(compartment.PropertyProfile);
        await _propertyProfileContext.SaveChangesAsync();

        var userAccessModel = new UserAccessModel
        {
            AgencyId = null,
            IsFcUser = false,
            UserAccountId = Guid.NewGuid(),
            WoodlandOwnerIds = new List<Guid>()
        };

        //act
        var result = await _sut.CheckUserCanAccessCompartmentAsync(compartment.Id, userAccessModel, new CancellationToken());

        //assert
        Assert.True(result.IsSuccess);
        Assert.False(result.Value);
    }

    [Theory, AutoMoqData]
    public async Task WhenUserCannotAccessCompartmentAsCompartmentToCheckDoesNotExist(Compartment compartment)
    {
        //arrange
        _propertyProfileContext.Compartments.Add(compartment);
        _propertyProfileContext.PropertyProfiles.Add(compartment.PropertyProfile);
        await _propertyProfileContext.SaveChangesAsync();

        var userAccessModel = new UserAccessModel
        {
            AgencyId = null,
            IsFcUser = false,
            UserAccountId = Guid.NewGuid(),
            WoodlandOwnerIds = new List<Guid>{compartment.PropertyProfile.WoodlandOwnerId}
        };

        //act
        var result = await _sut.CheckUserCanAccessCompartmentAsync(Guid.NewGuid(), userAccessModel, new CancellationToken());

        //assert
        Assert.True(result.IsFailure);
    }

    [Theory, AutoMoqData]
    public async Task WhenUserCannotAccessCompartmentAsCompartmentToCheckDoesNotExistInWoodland(
        Guid woodlandOwnerId,
        Compartment compartment, 
        Compartment compartmentOther)
    {
        //arrange
        _propertyProfileContext.Compartments.Add(compartment);
        compartment.PropertyProfile.WoodlandOwnerId = woodlandOwnerId;
        _propertyProfileContext.PropertyProfiles.Add(compartment.PropertyProfile);

        _propertyProfileContext.Compartments.Add(compartmentOther);
        _propertyProfileContext.PropertyProfiles.Add(compartmentOther.PropertyProfile);

        await _propertyProfileContext.SaveChangesAsync();

        var userAccessModel = new UserAccessModel
        {
            AgencyId = null,
            IsFcUser = false,
            UserAccountId = Guid.NewGuid(),
            WoodlandOwnerIds = new List<Guid> { woodlandOwnerId }
        };

        //act
        var result = await _sut.CheckUserCanAccessCompartmentAsync(compartmentOther.PropertyProfile.WoodlandOwnerId, userAccessModel, new CancellationToken());

        //assert
        Assert.True(result.IsFailure);
    }
    
    [Theory, AutoMoqData]
    public async Task WhenUserCanAccessCompartmentAsIsAnAgentHavingAccessToTheWoodland(Compartment compartment)
    {
        //arrange
        _propertyProfileContext.Compartments.Add(compartment);
        _propertyProfileContext.PropertyProfiles.Add(compartment.PropertyProfile);
        await _propertyProfileContext.SaveChangesAsync();

        var userAccessModel = new UserAccessModel
        {
            AgencyId = Guid.NewGuid(),
            IsFcUser = false,
            UserAccountId = Guid.NewGuid(),
            WoodlandOwnerIds = new List<Guid> { compartment.PropertyProfile.WoodlandOwnerId, Guid.NewGuid(), Guid.NewGuid() }
        };

        //act
        var result = await _sut.CheckUserCanAccessCompartmentAsync(compartment.Id, userAccessModel, new CancellationToken());

        //assert
        Assert.True(result.IsSuccess);
        Assert.True(result.Value);
    }
}

public class TestCompartment : Compartment
{
    public TestCompartment(Compartment compartment) : base(compartment.CompartmentNumber,
        compartment.SubCompartmentName, compartment.TotalHectares, compartment.Designation,
        compartment.GISData, compartment.PropertyProfileId)
    {
        Id = compartment.Id;
    }
    
    public void SetCompartmentNumber(string newNumber)
    {
        CompartmentNumber = newNumber;
    }
}
