using System.Xml.Linq;
using DocumentFormat.OpenXml.Wordprocessing;
using Serilog;

namespace WordParserLibrary.Model
{
    public class Point : BaseEntity, IAmendable, IXmlConvertible {
        public List<Letter> Letters { get; set; } = new List<Letter>();
        public List<Amendment> Amendments { get; set; } = new List<Amendment>();
        public string Number { get; set; } = string.Empty;
        public Point(Paragraph paragraph, Subsection parent) : base(paragraph, parent)
        {
            ContentParser point = new ContentParser(this);
            point.ParseOrdinal();
            if (point.ParserError)
            {
                Log.Error("Error parsing article: {ErrorMessage}", point.ErrorMessage);
                return;
            }
            Number = point.Number;
            Content = point.Content;
            bool isAdjacent = true;
            Log.Information("Point: {Number} - {Content}", Number, Content.Substring(0, Math.Min(Content.Length, 100)));
            while (paragraph.NextSibling() is Paragraph nextParagraph 
                    && nextParagraph.StyleId("PKT") != true
                    && nextParagraph.StyleId("UST") != true
                    && nextParagraph.StyleId("ART") != true)
            {                
                if (nextParagraph.StyleId("LIT") == true)
                {
                    Letters.Add(new Letter(nextParagraph, this));
                    isAdjacent = false;
                }
                else if (nextParagraph.StyleId("Z") == true && isAdjacent == true)
                {
                    Amendments.Add(new Amendment(nextParagraph, this));
                }
                else 
                {
                    isAdjacent = false;
                }
                paragraph = nextParagraph;
            }
            if (point.HasAmendmentOperation)
            {
                Amendments.Add(new Amendment(paragraph, this));
            }
            if (Amendments.Any())
            {
                AmendmentBuilder ab = new AmendmentBuilder();
                AmendmentOperations = ab.Build(Amendments, this);
            }
        }

        public XElement ToXML(bool generateGuids)
        {
            var newElement = new XElement(XMLConstants.Point,
                new XAttribute("id", BuildId()));
            if (generateGuids) newElement.Add(new XAttribute("guid", Guid));
            newElement.AddFirst(new XElement(XMLConstants.Number, Number));
            newElement.Add(new XElement("text", Content));
            foreach (var letter in Letters)
            {
                newElement.Add(letter.ToXML(generateGuids));
            }
            foreach (var amendmentOperation in AmendmentOperations)
            {
                newElement.Add(amendmentOperation.ToXML(generateGuids));
            }
            return newElement;
        }

        public string BuildId()
        {
            var parentId = (Parent as Subsection)?.BuildId();
            return parentId != null ? $"{parentId}.pkt_{Number}" : $"pkt_{Number}";
        }
    }
}