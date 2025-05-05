using System.Security;
using System.Xml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using WordParserLibrary.Model;

namespace WordParserLibrary
{
    public class DocxGenerator
    {        
        private readonly LegalAct _legalAct;

        public DocxGenerator(LegalAct legalAct)
        {
            _legalAct = legalAct;
        }

        public XmlDocument Generate(bool generateGuids = false)
        {
            CustomXmlPart xmlPart = _legalAct.MainPart.AddCustomXmlPart(CustomXmlPartType.CustomXml, "aktPrawny");
            var xmlDoc = new XmlDocument();
            var rootElement = xmlDoc.CreateElement(XmlConstants.Root);

            foreach (var paragraph in _legalAct.MainPart.Document.Descendants<Paragraph>()
                                                        .Where(p => p.InnerText.StartsWith("Art."))
                                                        .ToList())
            {
                if (paragraph.ParagraphProperties == null)
                {
                    Console.WriteLine("[CTOR]\tBrak właściwości paragrafu!");
                    _legalAct.CommentManager.AddComment(paragraph, "Brak właściwości paragrafu!");
                    continue;
                }
                if (paragraph.ParagraphProperties.ParagraphStyleId == null)
                {
                    Console.WriteLine("[CTOR]\tBrak stylu paragrafu!");
                    _legalAct.CommentManager.AddComment(paragraph, "Brak stylu paragrafu!");
                    continue;
                }
                
                var paragraphStyle = paragraph.ParagraphProperties?.ParagraphStyleId?.Val;

                if (paragraphStyle != null && paragraphStyle?.ToString()?.StartsWith("ART") == true)
                {
                    // Konwertuj XElement na XmlElement
                    var articleElement = new Article(paragraph).ToXML(generateGuids).ToXmlElement();
                    var importedNode = xmlDoc.ImportNode(articleElement, true);
                    rootElement.AppendChild(importedNode);
                }
            }
            xmlDoc.AppendChild(rootElement);

            using (var stream = xmlPart.GetStream(FileMode.Create, FileAccess.Write))
            {
                xmlDoc.Save(stream);
            }
            var xmlFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "LegalAct.xml");
            xmlDoc.Save(xmlFilePath);
            Console.WriteLine($"XML file saved at: {xmlFilePath}");

            return xmlDoc;
        }

    }
}