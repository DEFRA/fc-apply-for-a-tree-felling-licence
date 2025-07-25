﻿using System.Net;
using System.Net.Mail;
using FluentEmail.Core.Interfaces;
using FluentEmail.Smtp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Forestry.Flo.Services.Notifications;

public static class FluentEmailSmtpBuilderExtensions
{
    public static FluentEmailServicesBuilder AddSmtpSender(this FluentEmailServicesBuilder builder, SmtpClient smtpClient)
    {
        builder.Services.TryAdd(ServiceDescriptor.Singleton<ISender>(_ => new SmtpSender(smtpClient)));
        return builder;
    }

    public static FluentEmailServicesBuilder AddSmtpSender(this FluentEmailServicesBuilder builder, string host, int port) => AddSmtpSender(builder, () => new SmtpClient(host, port));

    public static FluentEmailServicesBuilder AddSmtpSender(this FluentEmailServicesBuilder builder, string host, int port, string username, string password) => AddSmtpSender(builder,
        () => new SmtpClient(host, port) { EnableSsl = true, Credentials = new NetworkCredential (username, password) });

    public static FluentEmailServicesBuilder AddSmtpSender(this FluentEmailServicesBuilder builder, Func<SmtpClient> clientFactory)
    {
        builder.Services.TryAdd(ServiceDescriptor.Singleton<ISender>(_ => new SmtpSender(clientFactory)));
        return builder;
    }
}