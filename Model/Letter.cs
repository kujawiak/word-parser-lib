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
            ContentParser letter = new ContentParser(this);
            letter.ParseOrdinal();
            if (letter.ParserError)
            {
                Log.Error("Error parsing article: {ErrorMessage}", letter.ErrorMessage);
                return;
            }
            Ordinal = letter.Number;
            Content = letter.Content;
            Log.Information("Letter: {Ordinal} - {Content}", Ordinal, Content.Substring(0, Math.Min(Content.Length, 100)));
            bool isAdjacent = true;
            var tiretCount = 1;
            while (paragraph.NextSibling() is Paragraph nextParagraph 
                    && nextParagraph.StyleId("LIT") != true
                    && nextParagraph.StyleId("PKT") != true
                    && nextParagraph.StyleId("UST") != true
                    && nextParagraph.StyleId("ART") != true)
            {
                if (nextParagraph.StyleId("TIR") == true)
                {
                    Tirets.Add(new Tiret(nextParagraph, this, tiretCount));
                    tiretCount++;
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
            var newElement = new XElement(XMLConstants.Letter,
                new XAttribute("id", BuildId()));
            if (generateGuids) newElement.Add(new XAttribute("guid", Guid));
            newElement.AddFirst(new XElement(XMLConstants.Number, Ordinal));
            newElement.Add(new XElement("text", Content));
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
        public string BuildId()
        {
            var parentId = (Parent as Point)?.BuildId();
            return parentId != null ? $"{parentId}.lit_{Ordinal}" : $"lit_{Ordinal}";
        }
    }
}