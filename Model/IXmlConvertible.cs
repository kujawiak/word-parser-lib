using System.Xml.Linq;

namespace WordParserLibrary.Model
{
    public interface IXmlConvertible
    {
        XElement ToXML(bool generateGuids);
    }
}
