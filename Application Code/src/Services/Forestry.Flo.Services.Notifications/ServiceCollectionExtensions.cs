﻿using Forestry.Flo.Services.Common;
using Forestry.Flo.Services.Common.Services;
using Forestry.Flo.Services.Notifications.Configuration;
using Forestry.Flo.Services.Notifications.Repositories;
using Forestry.Flo.Services.Notifications.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Notify.Client;
using Notify.Interfaces;

namespace Forestry.Flo.Services.Notifications;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNotificationsServices(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<DbContextOptionsBuilder> options)
    {
        //services.AddNotificationsBySmtp(configuration);
        services.AddNotificationsByGovUkNotify(configuration);

        services.AddDbContextFactory<NotificationsContext>(options);
        services.AddSingleton<IDbContextFactorySource<NotificationsContext>,
            CustomDbContextFactorySource<NotificationsContext>>();
        services.AddScoped<INotificationHistoryRepository, NotificationHistoryRepository>();

        services.AddScoped<IRetrieveNotificationHistory, NotificationHistoryService>();
        services.AddScoped<IActivityFeedService, ActivityFeedService>();

        return services;
    }

    private static IServiceCollection AddNotificationsBySmtp(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var configSection = configuration.GetSection("Notifications");
        var config = configSection.Get<NotificationsOptions>();
        services.Configure<NotificationsOptions>(configSection);

        if (string.IsNullOrWhiteSpace(config.Smtp.Username) || string.IsNullOrWhiteSpace(config.Smtp.Password))
        {
            services
                .AddFluentEmail(config.DefaultFromAddress, config.DefaultFromName ?? "")
                .AddSmtpSender(config.Smtp.Host, config.Smtp.Port)
                .AddRazorRenderer();
        }
        else
        {
            services
                .AddFluentEmail(config.DefaultFromAddress, config.DefaultFromName ?? "")
                .AddSmtpSender(config.Smtp.Host, config.Smtp.Port, config.Smtp.Username, config.Smtp.Password)
                .AddRazorRenderer();
        }
        services.AddScoped<ISendNotifications, EmailService>();

        return services;
    }

    private static IServiceCollection AddNotificationsByGovUkNotify(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var configSection = configuration.GetSection("GovUkNotifyOptions");
        var config = configSection.Get<GovUkNotifyOptions>();
        services.Configure<GovUkNotifyOptions>(configSection);

        var client = new NotificationClient(config.ApiKey);
        services.AddSingleton<IAsyncNotificationClient>(client);

        services.AddScoped<ISendNotifications, SendNotificationsByGovUkNotify>();

        return services;
    }
}