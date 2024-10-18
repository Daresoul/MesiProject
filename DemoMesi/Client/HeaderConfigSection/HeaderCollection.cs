using System.Configuration;

namespace Client.HeaderConfigSection;

public class HeaderCollection : ConfigurationElementCollection
{
    protected override ConfigurationElement CreateNewElement()
    {
        return new HeaderElement();
    }

    protected override object GetElementKey(ConfigurationElement element)
    {
        return ((HeaderElement)element).Key;
    }

    public void Add(HeaderElement element)
    {
        BaseAdd(element);
    }

    public void Remove(string key)
    {
        BaseRemove(key);
    }
}