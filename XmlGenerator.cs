using System.Security;
using System.Xml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace WordParserLibrary
{
    public class XmlGenerator
    {        
        private readonly LegalAct _legalAct;
        public MainDocumentPart MainPart { get; set; }

        public XmlGenerator(LegalAct legalAct)
        {
            _legalAct = legalAct;
            MainPart = legalAct.MainPart;
        }

        public XmlDocument Generate(bool generateGuids = false)
        {
            CustomXmlPart xmlPart = MainPart.AddCustomXmlPart(CustomXmlPartType.CustomXml, "aktPrawny");
            var xmlDoc = new XmlDocument();
            var rootElement = xmlDoc.CreateElement(XmlConstants.Root);

            // foreach (var article in _legalAct.Articles)
            // {
            //     // Konwertuj XElement na XmlElement
            //     var articleElement = article.ToXML(generateGuids).ToXmlElement();
            //     var importedNode = xmlDoc.ImportNode(articleElement, true);
            //     rootElement.AppendChild(importedNode);
            // }

            xmlDoc.AppendChild(rootElement);

            using (var stream = xmlPart.GetStream(FileMode.Create, FileAccess.Write))
            {
                xmlDoc.Save(stream);
            }
            var xmlFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "LegalAct.xml");
            xmlDoc.Save(xmlFilePath);
            Console.WriteLine($"XML file saved at: {xmlFilePath}");

            _legalAct.XmlDocument = xmlDoc;
            return xmlDoc;
        }

        public string GenerateString(bool generateGuids = false)
        {
            var xmlDoc = Generate(generateGuids);
            using (var stringWriter = new StringWriter())
            {
                using (var xmlWriter = XmlWriter.Create(stringWriter))
                {
                    xmlDoc.WriteTo(xmlWriter);
                    xmlWriter.Flush();
                    return stringWriter.ToString();
                }
            }
        }
    }
}