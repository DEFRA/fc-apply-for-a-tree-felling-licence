using CSharpFunctionalExtensions;
using Forestry.Flo.Services.AdminHubs.Entities;
using Forestry.Flo.Services.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Forestry.Flo.Services.AdminHubs;

public class AdminHubContext : DbContext, IUnitOfWork
{
    private readonly ILogger<AdminHubContext>? _logger;
    private const string SchemaName = "AdminHubs";

    public DbSet<AdminHub> AdminHubs { get; set; }
    public DbSet<AdminHubOfficer> AdminHubOfficers { get; set; }
    public DbSet<Area> Areas { get; set; }

    public AdminHubContext()
    {
    }

    public AdminHubContext(DbContextOptions<AdminHubContext> options) : base(options)
    {
    }

    public AdminHubContext(DbContextOptions<AdminHubContext> options, ILogger<AdminHubContext> logger) : base(options)
    {
        _logger = logger;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);

        if (!optionsBuilder.IsConfigured)
        {
            var config = new ConfigurationBuilder()
            .AddUserSecrets<AdminHubContext>()
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
        modelBuilder.HasDefaultSchema(SchemaName);

        modelBuilder.Entity<AdminHub>().ToTable("AdminHub");
        modelBuilder.Entity<AdminHubOfficer>().ToTable("AdminHubOfficer");
        modelBuilder.Entity<Area>().ToTable("Area");

        modelBuilder
            .Entity<AdminHub>()
            .Property(x => x.Id)
            .HasColumnType("uuid")
            .HasDefaultValueSql("uuid_generate_v4()")    
            .IsRequired();

        modelBuilder.Entity<AdminHub>()
            .HasIndex(p => p.Name)
            .IsUnique();
        
        modelBuilder
            .Entity<AdminHubOfficer>()
            .Property(x => x.Id)
            .HasColumnType("uuid")
            .HasDefaultValueSql("uuid_generate_v4()")    
            .IsRequired();
        
        modelBuilder.Entity<AdminHubOfficer>()
            .HasOne(p => p.AdminHub)
            .WithMany(p => p.AdminOfficers)
            .HasForeignKey(p => p.AdminHubId);

        modelBuilder.Entity<AdminHubOfficer>()
            .HasIndex(p => new {p.AdminHubId, p.UserAccountId})
            .IsUnique();

        modelBuilder
            .Entity<Area>()
            .Property(x => x.Id)
            .HasColumnType("uuid")
            .HasDefaultValueSql("uuid_generate_v4()")    
            .IsRequired();

        modelBuilder.Entity<Area>()
            .HasIndex(p => p.Name)
            .IsUnique();

        modelBuilder.Entity<Area>()
            .HasIndex(p => p.Code)
            .IsUnique();

        modelBuilder.Entity<Area>()
            .HasOne(p => p.AdminHub)
            .WithMany(p => p.Areas)
            .HasForeignKey(p=>p.AdminHubId);
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