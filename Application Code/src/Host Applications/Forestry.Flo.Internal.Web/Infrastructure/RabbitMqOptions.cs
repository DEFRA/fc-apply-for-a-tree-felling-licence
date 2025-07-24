using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.Internal.Web.Infrastructure;

/// <summary>
/// Options class for configuring connections to a RabbitMQ instance.
/// </summary>
public class RabbitMqOptions
{
    /// <summary>
    /// Gets and sets a <see cref="Uri"/> for accessing a RabbitMQ instance.
    /// </summary>
    [Required]
    public Uri? Url { get; set; }

    /// <summary>
    /// Gets and sets a textual representation of a username for authenticating against a RabbitMQ instance.
    /// </summary>
    [Required]
    public string? Username { get; set; }

    /// <summary>
    /// Gets and sets a textual representation of a password for authenticating against a RabbitMQ instance.
    /// </summary>
    [Required]
    public string? Password { get; set; }
}