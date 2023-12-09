// TODO: add validation of the filter settings
using System.ComponentModel.DataAnnotations;

public class SubscriptionFilter
{

    [Required]
    public string Type { get; init; } = string.Empty;  //"EventType", "StreamName"

    // TODO: add validation of the expression
    [Required]
    public string Expression { get; init; } = string.Empty; // either  "prefix:prefix" or regex: "regex:^my-stream"
}