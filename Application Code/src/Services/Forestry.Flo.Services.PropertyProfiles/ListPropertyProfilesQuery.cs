namespace Forestry.Flo.Services.PropertyProfiles;

public record ListPropertyProfilesQuery(Guid WoodlandOwnerId, IReadOnlyList<Guid>? Ids = null)    {
    public IReadOnlyList<Guid> Ids { get; } = Ids ?? Array.Empty<Guid>();
}
