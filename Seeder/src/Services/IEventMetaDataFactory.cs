using System.Diagnostics;

namespace Kuna.EventStore.Seeder.Services;

public interface IEventMetadataFactory
{
    IDictionary<string, string> Get();
}

public class EventMetadataFactory : IEventMetadataFactory
{
    public IDictionary<string, string> Get()
    {
        var toReturn = new Dictionary<string, string>();

        if (Activity.Current?.Id != null)
        {
            toReturn.Add("CorrelationId", Activity.Current.Id);
        }

        return toReturn;
    }
}
