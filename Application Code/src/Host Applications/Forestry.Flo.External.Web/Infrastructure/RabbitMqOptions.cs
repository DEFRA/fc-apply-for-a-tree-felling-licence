using System.ComponentModel.DataAnnotations;

namespace Forestry.Flo.External.Web.Infrastructure;

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

    /// <summary>
    /// Gets and sets the time in seconds for queue expiration.
    /// A connected bus with a receive endpoint on the queue is considered to have "activity" even if there are no messages received
    /// </summary>
    public double QueueExpiration { get; set; } = 18000; //expire the queue after 5hrs of no activity.

    /// <summary>
    /// Gets and sets the retry limit for consuming messages.
    /// </summary>
    public int RetryCount { get; set; } = 3;

    /// <summary>
    /// Gets and sets a fixed interval between retries.
    /// </summary>
    public int RetryIntervalMilliseconds { get; set; } = 10000;

    /// <summary>
    /// Gets and sets the amount of messages to prefetch from the RabbitMQ message broker.
    /// </summary>
    public int PrefetchCount { get; set; } = 16;
}