using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.External.Web.Infrastructure;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
sealed public class NoGuidEmpty : ValidationAttribute
{
    public override bool IsValid(object? value) =>
        !((Guid)(value ?? Guid.Empty) == Guid.Empty);
}