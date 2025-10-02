using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Applicants.Entities.Agent;
using Forestry.Flo.Services.Applicants.Entities.AgentAuthority;
using Forestry.Flo.Services.Applicants.Entities.UserAccount;
using Forestry.Flo.Services.Applicants.Entities.WoodlandOwner;
using Forestry.Flo.Services.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Forestry.Flo.Services.Applicants;

public class ApplicantsContext : DbContext, IUnitOfWork
{
    private readonly ILogger<ApplicantsContext>? _logger;
    private const string SchemaName = "Applicants";

    public DbSet<UserAccount> UserAccounts { get; set; }
    public DbSet<WoodlandOwner> WoodlandOwners { get; set; }
    public DbSet<AgentAuthority> AgentAuthorities { get; set; }
    public DbSet<Agency> Agencies { get; set; }

    public ApplicantsContext()
    {
    }

    public ApplicantsContext(DbContextOptions<ApplicantsContext> options) : base(options)
    {
    }

    public ApplicantsContext(DbContextOptions<ApplicantsContext> options, ILogger<ApplicantsContext> logger) : base(options)
    {
        _logger = logger;
    }
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);

        if (!optionsBuilder.IsConfigured)
        {
            var config = new ConfigurationBuilder()
            .AddUserSecrets<ApplicantsContext>()
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
        
        modelBuilder.Entity<UserAccount>().ToTable("UserAccount");
        modelBuilder.Entity<WoodlandOwner>().ToTable("WoodlandOwner");
        modelBuilder.Entity<Agency>().ToTable("Agency");
        modelBuilder.Entity<AgentAuthority>().ToTable("AgentAuthority");
        modelBuilder.Entity<AgentAuthorityForm>().ToTable("AgentAuthorityForm");
        modelBuilder.Entity<AafDocument>().ToTable("AafDocument");

        modelBuilder.Entity<UserAccount>().HasIndex(p => p.IdentityProviderId).IsUnique();
        modelBuilder.Entity<UserAccount>().HasIndex(p => p.Email).IsUnique();

        modelBuilder.Entity<UserAccount>().OwnsOne(p => p.ContactAddress);
        modelBuilder.Entity<WoodlandOwner>().OwnsOne(p => p.OrganisationAddress);
        modelBuilder.Entity<Agency>().OwnsOne(p => p.Address);

        modelBuilder.HasDefaultSchema(SchemaName);

        modelBuilder
            .Entity<UserAccount>()
            .Property(x => x.Id)
            .HasColumnType("uuid")
            .HasDefaultValueSql("uuid_generate_v4()")    
            .IsRequired();
        
        modelBuilder
            .Entity<UserAccount>()
            .Property(e => e.PreferredContactMethod)
            .HasConversion<string>();

        modelBuilder
            .Entity<UserAccount>()
            .Property(e => e.Status)
            .HasConversion<string>();
        
        modelBuilder
            .Entity<WoodlandOwner>()
            .Property(x => x.Id)
            .HasColumnType("uuid")
            .HasDefaultValueSql("uuid_generate_v4()")    
            .IsRequired();
        
        modelBuilder
            .Entity<Agency>()
            .Property(x => x.Id)
            .HasColumnType("uuid")
            .HasDefaultValueSql("uuid_generate_v4()")    
            .IsRequired();
        
        //Allow Agencies to be saved through EF only when IsFcAgency is set to false.
        modelBuilder
            .Entity<Agency>()
            .Property(x => x.IsFcAgency)
            .HasDefaultValue(false)
            .Metadata.SetBeforeSaveBehavior(PropertySaveBehavior.Throw);

        //Allow only one Agency record to have IsFcAgency set to true.
        modelBuilder
            .Entity<Agency>()
            .HasIndex(x => x.IsFcAgency)
            .IsUnique()
            .HasFilter("\"IsFcAgency\" = true");
        
        modelBuilder
            .Entity<AgentAuthority>()
            .Property(x => x.Id)
            .HasColumnType("uuid")
            .HasDefaultValueSql("uuid_generate_v4()")
            .IsRequired();

        modelBuilder
            .Entity<AgentAuthority>()
            .Property(x => x.Status)
            .HasConversion<string>();

        modelBuilder
            .Entity<AgentAuthorityForm>()
            .Property(x => x.Id)
            .HasColumnType("uuid")
            .HasDefaultValueSql("uuid_generate_v4()")
            .IsRequired();

        modelBuilder
            .Entity<AgentAuthorityForm>()
            .HasOne<AgentAuthority>()
            .WithMany(x => x.AgentAuthorityForms)
            .HasForeignKey(p => p.AgentAuthorityId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder
            .Entity<AafDocument>()
            .Property(x => x.Id)
            .HasColumnType("uuid")
            .HasDefaultValueSql("uuid_generate_v4()")
            .IsRequired();

        modelBuilder
            .Entity<AafDocument>()
            .HasOne<AgentAuthorityForm>()
            .WithMany(x => x.AafDocuments)
            .HasForeignKey(p => p.AgentAuthorityFormId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
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