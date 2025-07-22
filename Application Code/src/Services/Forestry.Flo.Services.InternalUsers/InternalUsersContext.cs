using CSharpFunctionalExtensions;
using Forestry.Flo.Services.InternalUsers.Entities.UserAccount;
using Forestry.Flo.Services.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace Forestry.Flo.Services.InternalUsers;

public class InternalUsersContext : DbContext, IUnitOfWork
{
    private readonly ILogger<InternalUsersContext>? _logger;
    private const string SchemaName = "InternalUsers";

    public DbSet<UserAccount> UserAccounts { get; set; }

    public InternalUsersContext()
    {
    }

    public InternalUsersContext(DbContextOptions<InternalUsersContext> options) : base(options)
    {
    }

    public InternalUsersContext(DbContextOptions<InternalUsersContext> options, ILogger<InternalUsersContext> logger) : base(options)
    {
        _logger = logger;
    }
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);

        if (!optionsBuilder.IsConfigured)
        {
            var config = new ConfigurationBuilder()
            .AddUserSecrets<InternalUsersContext>()
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

        modelBuilder.Entity<UserAccount>().HasIndex(p => p.IdentityProviderId).IsUnique();
        modelBuilder.Entity<UserAccount>().HasIndex(p => p.Email).IsUnique();

        modelBuilder.HasDefaultSchema(SchemaName);

        modelBuilder
            .Entity<UserAccount>()
            .Property(x => x.Id)
            .HasColumnType("uuid")
            .HasDefaultValueSql("uuid_generate_v4()")    
            .IsRequired();

        modelBuilder
            .Entity<UserAccount>()
            .Property(e => e.AccountType)
            .HasConversion<string>();

        modelBuilder
            .Entity<UserAccount>()
            .Property(e => e.AccountTypeOther)
            .HasConversion<string>();
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