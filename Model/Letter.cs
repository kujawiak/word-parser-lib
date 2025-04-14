using System;
using System.Collections.Generic;
using System.Xml.Linq;
using DocumentFormat.OpenXml.Wordprocessing;

namespace WordParserLibrary.Model
{
        public class Letter : BaseEntity, IAmendable, IXmlConvertible {
        public List<Tiret> Tirets { get; set; }
        public string Ordinal { get; set; }
        public List<Amendment> Amendments { get; set; }
        public string AmendedArticle { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public AmendmentOperationType AmendmentOperationType { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public Letter(Paragraph paragraph, Point parent) : base(paragraph, parent)
        {
            var parsedLetter = ParseOrdinal(Content);
            Ordinal = parsedLetter[1].Value;
            Content = parsedLetter[2].Value;
            Tirets = new List<Tiret>();
            Amendments = new List<Amendment>();
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
            if (IsAmendmentOperation())
            {
                Amendments.Add(new Amendment(paragraph, this));
            }
            if (Amendments.Any())
            {
                AmendmentBuilder ab = new AmendmentBuilder();
                AmendmentOperations = ab.Build(Amendments, this);
            }
        }
    
        public XElement ToXML()
        {
            var newElement = new XElement(XMLConstants.Letter,
                new XAttribute("guid", Guid),
                new XAttribute("id", BuildId()));
            newElement.AddFirst(new XElement(XMLConstants.Number, Ordinal));
            newElement.Add(new XElement("text", Content));
            foreach (var tiret in Tirets)
            {
                newElement.Add(tiret.ToXML());
            }
            foreach (var amendmentOperation in AmendmentOperations)
            {
                newElement.Add(amendmentOperation.ToXML());
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