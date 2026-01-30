using System.Xml.Linq;
using DocumentFormat.OpenXml.Wordprocessing;
using Serilog;

namespace WordParserLibrary.Model
{
    public class Point : BaseEntity, IAmendable, IXmlConvertible {
        public List<Letter> Letters { get; set; } = new List<Letter>();
        public List<Amendment> Amendments { get; set; } = new List<Amendment>();
        public Point(Paragraph paragraph, Subsection parent) : base(paragraph, parent)
        {
            EffectiveDate = parent.EffectiveDate;
            ContentParser point = new ContentParser(this);
            point.ParseOrdinal();
            if (point.ParserError)
            {
                Log.Error("Error parsing article: {ErrorMessage}", point.ErrorMessage);
                return;
            }
            ContentText = point.Content;
            bool isAdjacent = true;
            Log.Information("Point: {Number} - {Content}", Number, ContentText.Substring(0, Math.Min(ContentText.Length, 100)));
            while (paragraph.NextSibling<Paragraph>() is Paragraph nextParagraph)
            {                
                string? styleId = nextParagraph.StyleId();
                if (string.IsNullOrEmpty(styleId))
                {
                    Error = true;
                    ErrorMessage = $"Unexpected paragraph style in paragraph: {paragraph.InnerText}";
                    Log.Error(ErrorMessage);
                    paragraph = nextParagraph;
                    continue;
                }
                if (styleId.StartsWith("PKT") || styleId.StartsWith("UST") || styleId.StartsWith("ART"))
                {
                    break;
                }
                else if (styleId.StartsWith("LIT") == true)
                {
                    Letters.Add(new Letter(nextParagraph, this));
                    isAdjacent = false;
                }
                else if (styleId.StartsWith("Z") == true && isAdjacent == true)
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
            var newElement = new XElement(XmlConstants.Point,
                new XAttribute("id", Id));
            if (generateGuids) newElement.Add(new XAttribute("guid", Guid));
            newElement.AddFirst(new XElement(XmlConstants.Number, Number));
            newElement.Add(new XElement("text", ContentText));
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

        public override string Id => $"{Parent?.Id ?? string.Empty}.pkt_{Number}";

        public Paragraph ToParagraph()
        {
            var p = new Paragraph()
            {
                ParagraphProperties = new ParagraphProperties(
                   new ParagraphStyleId { Val = "PKTpunkt" }
               )
            };
            p.Append(new Run(new Text($"{Number})")));
            p.Append(new Run(new TabChar()));
            p.Append(new Run(new Text(ContentText)));
            return p;
        }
    }
}