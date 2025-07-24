using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Auditing;
using Forestry.Flo.Services.Common.Extensions;
using Forestry.Flo.Services.ConditionsBuilder.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Forestry.Flo.Services.ConditionsBuilder;

public class ConditionsBuilderContext : DbContext, IUnitOfWork
{
    private readonly ILogger<ConditionsBuilderContext> _logger;
    private const string SchemaName = "Conditions";

    public ConditionsBuilderContext()
    {
    }

    public ConditionsBuilderContext(DbContextOptions<ConditionsBuilderContext> options)
        : base(options)
    {
    }

    public ConditionsBuilderContext(DbContextOptions<ConditionsBuilderContext> options, ILogger<ConditionsBuilderContext> logger)
        : base(options)
    {
        _logger = logger;
    }

    public DbSet<FellingLicenceCondition> FellingLicenceConditions { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);

        if (!optionsBuilder.IsConfigured)
        {
            var config = new ConfigurationBuilder()
            .AddUserSecrets<ConditionsBuilderContext>()
            .Build();

            var connectionString = config.GetConnectionString("DefaultConnection");

            optionsBuilder.UseNpgsql(
                connectionString,
                x => x.MigrationsHistoryTable("__EFMigrationsHistory", SchemaName));
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasPostgresExtension("uuid-ossp");

        modelBuilder.Entity<FellingLicenceCondition>().ToTable("FellingLicenceCondition");

        modelBuilder.HasDefaultSchema(SchemaName);

        modelBuilder
            .Entity<FellingLicenceCondition>()
            .Property(x => x.Id)
            .HasColumnType("uuid")
            .HasDefaultValueSql("uuid_generate_v4()")
            .IsRequired();

        modelBuilder
            .Entity<FellingLicenceCondition>()
            .Property(x => x.AppliesToSubmittedCompartmentIds)
            .HasJsonConversion();

        modelBuilder
            .Entity<FellingLicenceCondition>()
            .Property(x => x.Parameters)
            .HasJsonConversion();

        modelBuilder
            .Entity<FellingLicenceCondition>()
            .Property(x => x.ConditionsText)
            .HasJsonConversion();
    }

    public async Task<UnitResult<UserDbErrorReason>> SaveEntitiesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await base.SaveChangesAsync(cancellationToken);
            return UnitResult.Success<UserDbErrorReason>();
        }
        catch (DbUpdateException ex) when (ex.InnerException is Npgsql.PostgresException { SqlState: "23505" })
        {
            _logger?.LogError("Error occurred during the entity update, the entity is not unique, exception: {Exception}", ex.ToString());
            return UnitResult.Failure(UserDbErrorReason.NotUnique);
        }
    }
}