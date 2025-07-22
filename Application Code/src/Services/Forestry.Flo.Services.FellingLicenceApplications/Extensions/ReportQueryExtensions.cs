using System.Linq.Expressions;
using Forestry.Flo.Services.FellingLicenceApplications.Entities;
using LinqKit;

namespace Forestry.Flo.Services.FellingLicenceApplications.Extensions;

public static class ReportQueryExtensions
{
    public static Expression<Func<FellingLicenceApplication, bool>> ByApplicationReference(string reference)
    {
        var predicate = PredicateBuilder.New<FellingLicenceApplication>();

        predicate = predicate.And(p => p.ApplicationReference == reference);
        return predicate;
    }

    public static Expression<Func<FellingLicenceApplication, bool>> HasAssignedUsers(List<Guid> userIds)
    {
        return fla =>
            fla.AssigneeHistories.AsQueryable().Any(HasAssignedUsersQuery(userIds));
    }

    private static Expression<Func<AssigneeHistory, bool>> HasAssignedUsersQuery(List<Guid> userIds)
    {
        var predicate = PredicateBuilder.New<AssigneeHistory>();

        foreach (var userId in userIds)
        {
            var tempUserId = userId; //have to do this - https://github.com/scottksmith95/LINQKit#predicatebuilder
            predicate = predicate.Or(p => p.AssignedUserId == tempUserId && p.Role == AssignedUserRole.AdminOfficer);
        }

        return predicate;
    }

    public static Expression<Func<SubmittedFlaPropertyCompartment, bool>> CompartmentsHaveConfirmedFellingSpecies(params string[] species)
    {
        return compartment =>
            compartment.ConfirmedFellingDetails.SelectMany(fellingDetail => fellingDetail.ConfirmedFellingSpecies).AsQueryable().Any(HasConfirmedFellingSpecies(species));
    }

    private static Expression<Func<ConfirmedFellingSpecies, bool>> HasConfirmedFellingSpecies(params string[] species)
    {
        var predicate = PredicateBuilder.New<ConfirmedFellingSpecies>();
        
        foreach (var sp in species)
        {
            var tempSp = sp; //have to do this - https://github.com/scottksmith95/LINQKit#predicatebuilder
            predicate = predicate.Or(p => p.Species == tempSp);
        }

        return predicate;
    }
}
