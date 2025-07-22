using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.Services.Notifications.Configuration;

public class NotificationsOptions
{
    /// <summary>
    /// Gets and sets the default from email address for sending notifications.
    /// </summary>
    [Required]
    public string DefaultFromAddress { get; set; }

    /// <summary>
    /// Gets and sets the default from name for sending notifications.
    /// </summary>
    public string? DefaultFromName { get; set; }

    /// <summary>
    /// Gets and sets an optional copy to address that all sent notifications will be sent to.
    /// </summary>
    public string? CopyToAddress { get; set; }

    /// <summary>
    /// Gets and sets the options to send notifications as emails via SMTP.
    /// </summary>
    public SmtpOptions Smtp { get; set; }

    public class SmtpOptions
    {
        /// <summary>
        /// Gets and sets the host address for the SMTP server to send emails via.
        /// </summary>
        [Required]
        public string Host { get; set; }

        /// <summary>
        /// Gets and sets the host port for the SMTP server to send emails via.
        /// </summary>
        [Required]
        public int Port { get; set; }

        /// <summary>
        /// Gets and sets the username of the network credentials required to send via this SMTP server.
        /// </summary>
        /// <remarks>
        /// For SSL to be enabled on the connection then both <see cref="Username"/> and <see cref="Password"/>
        /// must be provided, otherwise both will be ignored and the connection will be unsecured.
        /// </remarks>
        public string? Username { get; set; }

        /// <summary>
        /// Gets and sets the password of the network credentials required to send via this SMTP server.
        /// </summary>
        /// <remarks>
        /// For SSL to be enabled on the connection then both <see cref="Username"/> and <see cref="Password"/>
        /// must be provided, otherwise both will be ignored and the connection will be unsecured.
        /// </remarks>
        public string? Password { get; set; }
    }
}