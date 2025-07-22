﻿using Forestry.Flo.Services.InternalUsers.Entities.UserAccount;

namespace Forestry.Flo.Services.InternalUsers.Services
{
    public class RolesService
    {
        public static string RolesStringFromList(IList<Roles> roles)
        {
            string rolesString = null;

            foreach (var role in roles)
            {
                rolesString += role + ",";
            }

            rolesString = rolesString.TrimEnd(',');

            return rolesString;
        }

        public static IList<Roles> RolesListFromString(string rolesString)
        {
            string[] roleStrings = rolesString.Split(',');

            var rolesList = new List<Roles>();

            foreach (var roleString in roleStrings)
            {
                var role = Enum.Parse<Roles>(roleString);

                rolesList.Add(role);
            }

            return rolesList;
        }

        public static bool RolesStringHasAnyOfRoles(string rolesString, IList<Roles> permissableRoles)
        {
            bool hasAnyOfRoles = false;

            string[] roleStrings = rolesString.Split(',');

            var rolesList = new List<Roles>();

            foreach (var roleString in roleStrings)
            {
                var role = Enum.Parse<Roles>(roleString);

                rolesList.Add(role);
            }

            foreach (var role in permissableRoles)
            {
                foreach (var userAccountRole in rolesList)
                {
                    if (userAccountRole == role)
                    {
                        hasAnyOfRoles = true;

                        // Stop once a role has been matched

                        return hasAnyOfRoles;
                    }
                }
            }

            return hasAnyOfRoles;
        }
    }
}
