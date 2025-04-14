using System.Xml;
using System.Xml.Linq;

public static class XmlExtensions
{
    public static XmlElement ToXmlElement(this XElement xElement)
    {
        var xmlDocument = new XmlDocument();
        using (var reader = xElement.CreateReader())
        {
            xmlDocument.Load(reader);
        }
        return xmlDocument.DocumentElement!;
    }
}