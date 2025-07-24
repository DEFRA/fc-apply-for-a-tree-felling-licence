using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Applicants;
using Forestry.Flo.Services.Applicants.Entities.Agent;
using Forestry.Flo.Services.Applicants.Entities.UserAccount;
using Forestry.Flo.Services.Applicants.Entities.WoodlandOwner;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.PropertyProfiles.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Forestry.Flo.Services.PropertyProfiles;

public class PropertyProfilesContext : DbContext, IUnitOfWork
{
    private readonly ILogger<PropertyProfilesContext>? _logger;
    private const string SchemaName = "PropertyProfiles";

    public DbSet<PropertyProfile> PropertyProfiles { get; set; } = null!;

    public DbSet<Compartment>  Compartments { get; set; } = null!;

    public PropertyProfilesContext()
    {
        
    }

    public PropertyProfilesContext(DbContextOptions<PropertyProfilesContext> options): base(options)
    {
        
    }
    
    public PropertyProfilesContext(DbContextOptions<PropertyProfilesContext> options, ILogger<PropertyProfilesContext>? logger): base(options)
    {
        _logger = logger;
    }
    
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);

        if (optionsBuilder.IsConfigured) return;
        
        var environmentName = 
            Environment.GetEnvironmentVariable(
                "Hosting:Environment");
        var builder = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory()))
            .AddJsonFile("appsettings.json")
            .AddJsonFile($"appsettings.{environmentName}.json", true)
            .AddUserSecrets<PropertyProfilesContext>()
            .AddEnvironmentVariables();

        var config = builder
            
            .Build();
        var connString = config.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connString))
        {
            throw new InvalidOperationException(
                "Could not find a connection string named 'DefaultConnection'.");
        }
            
        optionsBuilder.UseNpgsql(connString,
            x => x.MigrationsHistoryTable("__EFMigrationsHistory", SchemaName));
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasPostgresExtension("uuid-ossp");

        modelBuilder.Entity<PropertyProfile>().ToTable("PropertyProfile");
        modelBuilder.Entity<Compartment>().ToTable("Compartment");
        modelBuilder.Entity<WoodlandOwner>().ToTable("WoodlandOwner","Applicants", t => t.ExcludeFromMigrations());
        modelBuilder.Entity<UserAccount>().ToTable("UserAccount","Applicants", t => t.ExcludeFromMigrations());
        modelBuilder.Entity<Agency>().ToTable("Agency", "Applicants", t => t.ExcludeFromMigrations());

        modelBuilder
            .Entity<PropertyProfile>()
            .Property(x => x.Id)
            .HasColumnType("uuid")
            .HasDefaultValueSql("uuid_generate_v4()")    
            .IsRequired();

        modelBuilder.Entity<PropertyProfile>(b =>
        {
            b.HasIndex(p => p.WoodlandOwnerId);
            b.HasIndex(p => new { p.Name, p.WoodlandOwnerId })
                .IsUnique();
        });
        
        modelBuilder
            .Entity<Compartment>()
            .Property(x => x.Id)
            .HasColumnType("uuid")
            .HasDefaultValueSql("uuid_generate_v4()")    
            .IsRequired();     
        
        modelBuilder.Entity<Compartment>(b =>
            {
                b.HasIndex(c => new { c.CompartmentNumber, c.PropertyProfileId })
                    .IsUnique()
                    .HasFilter("\"SubCompartmentName\" IS NULL");
                b.HasIndex(c => new { c.CompartmentNumber, c.SubCompartmentName, c.PropertyProfileId })
                    .IsUnique();
            }
        );
        modelBuilder.HasDefaultSchema(SchemaName);
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
            _logger?.LogError("Error occured during the entity update, the entity is not unique, exception: {Exception}", ex.ToString());
            return UnitResult.Failure(UserDbErrorReason.NotUnique);
        }
    }
}