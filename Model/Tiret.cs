using System;
using System.Collections.Generic;
using System.Xml.Linq;
using DocumentFormat.OpenXml.Wordprocessing;

namespace WordParserLibrary.Model
{
        public class Tiret : BaseEntity, IAmendable, IXmlConvertible {
        public int Number { get; set; }
        public List<Amendment> Amendments { get; set; }
        public Tiret(Paragraph paragraph, Letter parent, int ordinal = 1) : base(paragraph, parent)
        {
            Number = ordinal;
            Amendments = new List<Amendment>();
            bool isAdjacent = true;
            while (paragraph.NextSibling() is Paragraph nextParagraph 
                    && nextParagraph.StyleId("TIR") != true
                    && nextParagraph.StyleId("LIT") != true
                    && nextParagraph.StyleId("PKT") != true
                    && nextParagraph.StyleId("UST") != true
                    && nextParagraph.StyleId("ART") != true)
            {
                if (nextParagraph.StyleId("Z") == true && isAdjacent == true)
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
    
        public XElement ToXML(bool generateGuids)
        {
            var newElement = new XElement(XMLConstants.Tiret,
                new XAttribute("id", BuildId()));
            if (generateGuids) newElement.Add(new XAttribute("guid", Guid));
            newElement.AddFirst(new XElement(XMLConstants.Number, Number));
            newElement.Add(new XElement("text", Content));
            foreach (var amendmentOperation in AmendmentOperations)
            {
                newElement.Add(amendmentOperation.ToXML(generateGuids));
            }
            return newElement;
        }
        public string BuildId()
        {
            var parentId = (Parent as Letter)?.BuildId();
            return parentId != null ? $"{parentId}.tir_{Number}" : $"tir_{Number}";
        }
    }
}