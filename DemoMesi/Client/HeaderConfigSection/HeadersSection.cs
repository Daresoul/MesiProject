using System.Configuration;

namespace Client.HeaderConfigSection;

public class HeadersSection : ConfigurationSection
{
    [ConfigurationProperty("Headers", IsDefaultCollection = false)]
    [ConfigurationCollection(typeof(HeaderCollection), AddItemName = "add")]
    public HeaderCollection Headers
    {
        get
        {
            return (HeaderCollection)this["Headers"];
        }
    }
}