using Forestry.Flo.Services.Common.Auditing.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Forestry.Flo.Services.Common.Auditing;

/// <summary>
/// A <see cref="DbContext"/> to store audit data.
/// </summary>
public class AuditDataContext : DbContext
{
    private const string SchemaName = "Audit";

    /// <summary>
    /// The collection of raised audit events.
    /// </summary>
    public DbSet<AuditEventEntity> AuditEvents { get; set; }

    public AuditDataContext()
    {

    }

    public AuditDataContext(DbContextOptions<AuditDataContext> options) : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);

        if (!optionsBuilder.IsConfigured)
        {
            var config = new ConfigurationBuilder()
            .AddUserSecrets<AuditDataContext>()
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

        modelBuilder.Entity<AuditEventEntity>().ToTable("AuditEvent");

        modelBuilder.HasDefaultSchema(SchemaName);
        
        modelBuilder
            .Entity<AuditEventEntity>()
            .Property(x => x.Id)
            .HasColumnType("uuid")
            .HasDefaultValueSql("uuid_generate_v4()")    
            .IsRequired();

        modelBuilder
            .Entity<AuditEventEntity>()
            .Property(e => e.SourceEntityType)
            .HasConversion<string>();

        modelBuilder
            .Entity<AuditEventEntity>()
            .Property(e => e.ActorType)
            .HasConversion<string>();
    }
}