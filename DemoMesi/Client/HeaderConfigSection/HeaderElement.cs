using System.Configuration;

namespace Client;

public class HeaderElement : ConfigurationElement
{
    [ConfigurationProperty("key", IsRequired = true)]
    public string Key
    {
        get { return (string)this["key"]; }
        set { this["key"] = value; }
    }

    [ConfigurationProperty("value", IsRequired = true)]
    public string Value
    {
        get { return (string)this["value"]; }
        set { this["value"] = value; }
    }
}