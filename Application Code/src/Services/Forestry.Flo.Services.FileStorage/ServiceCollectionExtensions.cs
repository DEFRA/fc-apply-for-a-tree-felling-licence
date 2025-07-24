using Ardalis.GuardClauses;
using Azure.Storage.Blobs;
using FileSignatures;
using Forestry.Flo.Services.FileStorage.Configuration;
using Forestry.Flo.Services.FileStorage.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Azure;

namespace Forestry.Flo.Services.FileStorage
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds all required services to the provided <see cref="ServiceCollection"/> made available for the file storage service.
        /// </summary>
        /// <param name="services">The collection of services to register against.</param>
        /// <param name="configuration">Service configuration</param>
        /// <param name="environment">Host environment</param>
        public static IServiceCollection AddFileStorageServices(
            this IServiceCollection services,
            IConfiguration configuration,
            IHostEnvironment environment)
        {
            Guard.Against.Null(services);
            Guard.Against.Null(configuration);

            services.Configure<QuicksilvaVirusScannerServiceOptions>(configuration.GetSection("QuicksilvaVirusScannerService"));
            

            services.AddTransient<FileValidator>();
            services.AddSingleton<FileTypesProvider>();
            services.AddSingleton<IFileFormatInspector>(new FileFormatInspector()); //must be new(), as this registers all formats

            services.Configure<AzureFileStorageOptions>(configuration.GetSection("FileStorage"));
            services.AddScoped<IFileStorageService, AzureFileStorageService>();
            services.AddAzureClients(cb =>
            {
                cb.AddBlobServiceClient(configuration.GetSection("FileStorage")["ConnectionString"]);
            });


            if (environment.IsDevelopment())
            {
                services.AddScoped<IVirusScannerService, AlwaysVirusFreeScannerService>();
            }
            else
            {
                services.AddHttpClient("QuicksilvaVirusScannerServiceClient");
                services.AddScoped<IVirusScannerService, QuicksilvaVirusScannerService>();

            }
            return services;
        }
    }
}
