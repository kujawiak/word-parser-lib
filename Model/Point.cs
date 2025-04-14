using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using DocumentFormat.OpenXml.Wordprocessing;

namespace WordParserLibrary.Model
{
    public class Point : BaseEntity, IAmendable, IXmlConvertible {
        public List<Letter> Letters { get; set; }
        public List<Amendment> Amendments { get; set; }
        public string Number { get; set; }
        public Point(Paragraph paragraph, Subsection parent) : base(paragraph, parent)
        {
            var parsedPoint = ParseOrdinal(Content);
            Number = parsedPoint[1].Value;
            Content = parsedPoint[2].Value;
            Letters = new List<Letter>();
            Amendments = new List<Amendment>();
            bool isAdjacent = true;
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
            var newElement = new XElement(XMLConstants.Point,
                new XAttribute("guid", Guid),
                new XAttribute("id", BuildId()));
            newElement.AddFirst(new XElement(XMLConstants.Number, Number));
            newElement.Add(new XElement("text", Content));
            foreach (var letter in Letters)
            {
                newElement.Add(letter.ToXML());
            }
            foreach (var amendmentOperation in AmendmentOperations)
            {
                newElement.Add(amendmentOperation.ToXML());
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