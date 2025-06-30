using System.Xml.Linq;
using DocumentFormat.OpenXml.Wordprocessing;
using Serilog;

namespace WordParserLibrary.Model
{
        public class Letter : BaseEntity, IAmendable, IXmlConvertible {
        public List<Tiret> Tirets { get; set; } = new List<Tiret>();
        public string Ordinal { get; set; } = string.Empty;
        public List<Amendment> Amendments { get; set; } = new List<Amendment>();
        public string AmendedArticle { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public AmendmentOperationType AmendmentOperationType { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public Letter(Paragraph paragraph, Point parent) : base(paragraph, parent)
        {
            EntityType = "LIT";
            EffectiveDate = parent.EffectiveDate;
            ContentParser letter = new ContentParser(this);
            letter.ParseOrdinal();
            if (letter.ParserError)
            {
                Log.Error("Error parsing article: {ErrorMessage}", letter.ErrorMessage);
                return;
            }
            Ordinal = letter.Number.ToString();
            ContentText = letter.Content;
            Log.Information("Letter: {Ordinal} - {Content}", Ordinal, ContentText.Substring(0, Math.Min(ContentText.Length, 100)));
            bool isAdjacent = true;
            var tiretCount = 1;
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
                if (styleId.StartsWith("LIT") || styleId.StartsWith("PKT") || styleId.StartsWith("UST") || styleId.StartsWith("ART"))
                {
                    break;
                }
                else if (styleId.StartsWith("TIR") == true)
                {
                    Tirets.Add(new Tiret(nextParagraph, this, tiretCount));
                    tiretCount++;
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
            if (letter.HasAmendmentOperation)
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
            var newElement = new XElement(XmlConstants.Letter,
                new XAttribute("id", Id));
            if (generateGuids) newElement.Add(new XAttribute("guid", Guid));
            newElement.AddFirst(new XElement(XmlConstants.Number, Ordinal));
            newElement.Add(new XElement("text", ContentText));
            foreach (var tiret in Tirets)
            {
                newElement.Add(tiret.ToXML(generateGuids));
            }
            foreach (var amendmentOperation in AmendmentOperations)
            {
                newElement.Add(amendmentOperation.ToXML(generateGuids));
            }
            return newElement;
        }

        public override string Id => $"{Parent?.Id ?? string.Empty}.lit_{Ordinal}";

        public Paragraph ToParagraph()
        {
            var p = new Paragraph()
            {
                ParagraphProperties = new ParagraphProperties(
                    new ParagraphStyleId { Val = "LITlitera" }
                )
            };
            p.Append(new Run(new Text($"{Ordinal})")));
            p.Append(new Run(new TabChar()));
            p.Append(new Run(new Text(ContentText)));
            return p;
        }
    }
}