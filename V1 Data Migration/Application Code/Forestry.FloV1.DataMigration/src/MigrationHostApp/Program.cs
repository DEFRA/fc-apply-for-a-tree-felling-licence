using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MigrationHostApp;
using MigrationService.Services;
using Serilog;
using NodaTime;
using DataMigrationConfiguration = MigrationService.Configuration.DataMigrationConfiguration;
using MigrationHostApp.MigrationModules;

/*
 *  Example usage:
 *
 *  To pre-validate a migration of the ExternalUsers migration process:
 *  > .\flo-data-migrater.exe prevalidate --migration ExternalUsers
 *
 *  To run the migration of all users from flov1 into flov2 as ExternalUsers, in test mode (-t):
 *  > .\flo-data-migrater.exe migrate --migration ExternalUsers -t
 *
 *  Or, performing the above, not in test mode:
 *
 *  > .\flo-data-migrater.exe migrate --migration ExternalUsers
 *
 *  Or, performing migration - without performing the pre-validation step first (-d)
 *  > .\flo-data-migrater.exe migrate --migration ExternalUsers -d
 *
 *  Or, performing migration - but constructing FLOv2 username/email-address with a specified
 *  email with +flov1User id suffix (-e {emailaddress}), i.e. paul.winslade+32012@qxlva.com
 *  > .\flo-data-migrater.exe migrate --migration ExternalUsers -e paul.winslade@qxlva.com
 *
 *  Or, to reset the target flov2 environment (i.e. delete all records from flov2 db tables)
 *  > .\flo-data-migrater.exe reset
 *
 *
 *  etc.
 *
*/

var stopWatch = new Stopwatch();

using var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration(cfg =>
    {
        cfg.AddJsonFile("appsettings.json");
    })
    .UseSerilog((context, loggerConfiguration) =>
    {
        loggerConfiguration.ReadFrom.Configuration(context.Configuration).Enrich.WithThreadId();
    }
    )
    .ConfigureServices((context, services) =>
    {
        services.Configure<DataMigrationConfiguration>(context.Configuration.GetSection("DataMigrationConfiguration"));
        services.AddScoped<DataMigrationEngine>();
        services.AddScoped<IDatabaseServiceV1, DatabaseServiceV1>();
        services.AddScoped<IDatabaseServiceV2, DatabaseServiceV2>();
        services.AddScoped<ResetFloV2Service>();
        services.AddSingleton<IClock>(SystemClock.Instance);
        services.AddSingleton<ExternalUsersMigrator>();
        services.AddSingleton<ManagedOwnersMigrator>();
        //services.AddScoped<IFileStore, AzureBlobStorageService>(); //LocalDiskFileStore
    })
    .ConfigureLogging((context, cfg) =>
    {
        cfg.ClearProviders();
        cfg.AddConfiguration(context.Configuration.GetSection("Logging"));
        cfg.AddConsole();
    })
    .Build();

using var scope = host.Services.CreateScope();

var dataMigrationEngine = scope.ServiceProvider.GetRequiredService<DataMigrationEngine>();

var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger<Program>();

stopWatch.Start();

await dataMigrationEngine.ExecuteAsync(args, new CancellationToken());

stopWatch.Stop();

logger.LogInformation("Run finished - the total elapsed time was [{elapsedTime}].", stopWatch.Elapsed.FormatTimeSpanForDisplay());