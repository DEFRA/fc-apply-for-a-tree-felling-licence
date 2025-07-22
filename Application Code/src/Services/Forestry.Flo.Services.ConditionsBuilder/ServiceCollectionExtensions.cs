using Ardalis.GuardClauses;
using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.ConditionsBuilder.Configuration;
using Forestry.Flo.Services.ConditionsBuilder.Repositories;
using Forestry.Flo.Services.ConditionsBuilder.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Forestry.Flo.Services.ConditionsBuilder;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddConditionsBuilderServices(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<DbContextOptionsBuilder> options)
    {
        Guard.Against.Null(services);
        Guard.Against.Null(configuration);
        Guard.Against.Null(options);

        services.AddDbContextFactory<ConditionsBuilderContext>(options);
        services.AddSingleton<IDbContextFactorySource<ConditionsBuilderContext>, CustomDbContextFactorySource<ConditionsBuilderContext>>();

        services.Configure<ConditionsBuilderOptions>(configuration.GetSection("ConditionsBuilder"));

        services.AddScoped<IBuildCondition, RestockByPlantingConditionBuilder>();
        services.AddScoped<IBuildCondition, NaturalRegenerationConditionBuilder>();
        services.AddScoped<IBuildCondition, CoppiceRegrowthConditionBuilder>();

        services.AddScoped<IConditionsBuilderRepository, ConditionsBuilderRepository>();
        services.AddScoped<ICalculateConditions, CalculateConditionsService>();

        return services;
    }
}