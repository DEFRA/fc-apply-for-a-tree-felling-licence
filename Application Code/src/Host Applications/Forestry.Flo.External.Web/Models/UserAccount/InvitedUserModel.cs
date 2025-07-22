namespace Forestry.Flo.External.Web.Models.UserAccount;

public record InvitedUserModel(string UserEmail, string? OrganisationName, string InviteToken);
