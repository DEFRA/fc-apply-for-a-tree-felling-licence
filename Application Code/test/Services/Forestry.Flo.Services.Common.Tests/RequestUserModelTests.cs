using System;
using System.Collections.Generic;
using System.Security.Claims;
using Forestry.Flo.Services.Common.User;
using Xunit;

namespace Forestry.Flo.Services.Common.Tests
{
    public class RequestUserModelTests
    {
        [Fact]
        public void InternalAccountTypeResultsToInternalUserActor()
        {
            foreach (AccountTypeInternal accountType in Enum.GetValues(typeof(AccountTypeInternal)))
            {
                //arrange
                var claims = new List<Claim> { new (FloClaimTypes.AccountType, accountType.ToString()) };
                var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));
                
                //act
                var result = new RequestUserModel(principal).ActorType;
                
                //assert
                Assert.True(result == ActorType.InternalUser);
            }
        }

        [Fact]
        public void ExternalAccountTypeResultsToExternalApplicantActor()
        {
            foreach (AccountTypeExternal accountType in Enum.GetValues(typeof(AccountTypeExternal)))
            {
                //arrange
                var claims = new List<Claim> { new (FloClaimTypes.AccountType, accountType.ToString()) };
                var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));

                //act
                var result = new RequestUserModel(principal).ActorType;
                
                //assert
                Assert.True(result == ActorType.ExternalApplicant);
            }
        }
        
        [Fact]
        public void NeitherAnInternalUserOrExternalApplicantAccountTypeResultsToSystemActor()
        {
            //arrange
            var principal = new ClaimsPrincipal(new ClaimsIdentity());
            
            //act
            var result = new RequestUserModel(principal).ActorType;
            
            //assert
            Assert.True(result == ActorType.System);
        }
    }
}
