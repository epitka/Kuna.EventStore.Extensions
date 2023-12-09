using System.ComponentModel.DataAnnotations;

namespace Kuna.EventStore.Subscriptions.Catchup;

public class EventStoreSubscriptionSettings
{
    public const string SectionName = "EventStore";

    public SubscriptionFilter? Filter { get; set; }  // optional, if not provided then all events will be processed

    [Required]
    public string SubscriptionName { get; set; } = string.Empty;

    [Required]
    public int HeartbeatIntervalInSeconds { get; set; }

    [Required]
    public int HeartbeatTimeoutInSeconds { get; set; }

    [Required]
    public int MaxLiveQueueSize { get; set; }

    [Required]
    public int ReadBatchSize { get; set; }

    [Required]
    public int MaxConnectionRetries { get; set; }

    [Required]
    public int MaxSearchWindow { get; set; }

    public string? User { get; set; }

    public string? Secret { get; set; }
}