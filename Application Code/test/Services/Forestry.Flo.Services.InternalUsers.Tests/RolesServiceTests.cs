using Forestry.Flo.Services.InternalUsers.Entities.UserAccount;
using Forestry.Flo.Services.InternalUsers.Services;

namespace Forestry.Flo.Services.InternalUsers.Tests
{
    public class RolesServiceTests
    {
        [Fact]
        public void CanWriteRolesListToString()
        {
            var rolesList = new List<Roles> { Roles.FcUser, Roles.FcAdministrator };

            string rolesString = RolesService.RolesStringFromList(rolesList);

            Assert.Equal("FcUser,FcAdministrator", rolesString);
        }

        [Fact]
        public void CanParseRolesString()
        {
            string rolesString = "FcUser,FcAdministrator";

            var rolesList = RolesService.RolesListFromString(rolesString);

            Assert.Equal(new List<Roles> { Roles.FcUser, Roles.FcAdministrator }, rolesList);
        }

        [Theory]
        [MemberData(nameof(TestRolesStringHasAnyOfRolesData))]
        public void TestRolesStringHasAnyOfRoles(string rolesString, IList<Roles> permissableRoles, bool expectedResult)
        {
            var rolesStringHasAnyOfRoles = RolesService.RolesStringHasAnyOfRoles(rolesString, permissableRoles);

            Assert.Equal(expectedResult, rolesStringHasAnyOfRoles);
        }

        public static IEnumerable<object[]> TestRolesStringHasAnyOfRolesData()
        {
            yield return new object[] { "FcUser,FcAdministrator", new List<Roles> { Roles.FcUser }, true};
            yield return new object[] { "FcAdministrator", new List<Roles> { Roles.FcAdministrator }, true };
            yield return new object[] { "FcUser", new List<Roles> { Roles.FcAdministrator }, false };
            yield return new object[] { "FcAdministrator", new List<Roles> { Roles.FcUser }, false };
        }
    }
}