using System.Xml.Linq;
using WordParserLibrary.Model.Schemas;

namespace WordParserLibrary.Services.Converters
{
    /// <summary>
    /// Konwerter dla ustępów - transformuje SubsectionDto do XML.
    /// </summary>
    public class SubsectionXmlConverter
    {
        public XElement ToXml(SubsectionDto subsection, bool generateGuids = false)
        {
            var newElement = new XElement(XmlConstants.Subsection,
                new XAttribute("id", subsection.Id));
            if (generateGuids) newElement.Add(new XAttribute("guid", subsection.Guid));
            newElement.AddFirst(new XElement(XmlConstants.Number, subsection.Number));
            newElement.Add(new XElement("text", subsection.ContentText));

            foreach (var point in subsection.Points)
            {
                var pointConverter = new PointXmlConverter();
                newElement.Add(pointConverter.ToXml(point, generateGuids));
            }

            // TODO: Dodać Amendment conversion
            foreach (var amendment in subsection.Amendments)
            {
                // newElement.Add(amendment.ToXML(generateGuids));
            }

            return newElement;
        }
    }
}
