using FluentAssertions;
using Forestry.Flo.Services.InternalUsers.Entities.UserAccount;
using Forestry.Flo.Services.InternalUsers.Services;

namespace Forestry.Flo.Services.InternalUsers.Tests.Services;

public class UserAccountEqualityComparerTests
{
    /// <summary>
    /// Proxy class facilitates setting of protected Id
    /// </summary>
    private class UserAccountProxy : UserAccount
    {
        public UserAccountProxy(Guid id)
        {
            this.Id = id;
        }
    }

    [Fact]
    public void CompareSameUserAccounts_ShouldBeEqual()
    {
        var sharedGuid = Guid.NewGuid();

        var userAccount1 = new UserAccountProxy(sharedGuid);
        var userAccount2 = new UserAccountProxy(sharedGuid);

        //arrange
        var userAccountEqualityComparer = new UserAccountEqualityComparer();

        //act
        bool equals = userAccountEqualityComparer.Equals(userAccount1, userAccount2);

        //assert
        equals.Should().BeTrue();
    }

    [Fact]
    public void CompareDifferentUserAccounts_ShouldNotBeEqual()
    {
        var userAccount1 = new UserAccountProxy(Guid.NewGuid());
        var userAccount2 = new UserAccountProxy(Guid.NewGuid());

        //arrange
        var userAccountEqualityComparer = new UserAccountEqualityComparer();

        //act
        bool equals = userAccountEqualityComparer.Equals(userAccount1, userAccount2);

        //assert
        equals.Should().BeFalse();
    }
}