using CSharpFunctionalExtensions;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Notifications.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Forestry.Flo.Services.Notifications;

public class NotificationsContext : DbContext, IUnitOfWork
{
    private readonly ILogger<NotificationsContext>? _logger;
    public const string SchemaName = "Notifications";

    public NotificationsContext()
    {
    }

    public NotificationsContext(DbContextOptions<NotificationsContext> options) : base(options)
    {
    }
    
    public NotificationsContext(DbContextOptions<NotificationsContext> options, ILogger<NotificationsContext> logger) : base(options)
    {
        _logger = logger;
    }

    public DbSet<NotificationHistory> NotificationHistories { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);

        if (!optionsBuilder.IsConfigured)
        {
            var config = new ConfigurationBuilder()
            .AddUserSecrets<NotificationsContext>()
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
    
        modelBuilder.Entity<NotificationHistory>().ToTable("NotificationHistory");

        modelBuilder.HasDefaultSchema(SchemaName);

        modelBuilder
            .Entity<NotificationHistory>()
            .Property(x => x.Id)
            .HasColumnType("uuid")
            .HasDefaultValueSql("uuid_generate_v4()")    
            .IsRequired();
        
        modelBuilder
            .Entity<NotificationHistory>()
            .Property(e => e.NotificationType)
            .HasConversion<string>();
        
        // Ensure enum NotificationStatus is stored/read as string to match column type
        modelBuilder
            .Entity<NotificationHistory>()
            .Property(e => e.Status)
            .HasConversion<string>();
        
    }

    public async Task<UnitResult<UserDbErrorReason>> SaveEntitiesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await base.SaveChangesAsync(cancellationToken);
            return UnitResult.Success<UserDbErrorReason>();
        }
        catch (DbUpdateException ex)
        {
            _logger?.LogError(
                "Error occured during the entity update, exception: {Exception}",
                ex.ToString());
            return UnitResult.Failure(UserDbErrorReason.General);
        }
    }
}