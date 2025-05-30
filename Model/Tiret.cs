using System;
using System.Collections.Generic;
using System.Xml.Linq;
using DocumentFormat.OpenXml.Wordprocessing;
using Serilog;

namespace WordParserLibrary.Model
{
        public class Tiret : BaseEntity, IAmendable, IXmlConvertible {
        public int Number { get; set; } = 1;
        public List<Amendment> Amendments { get; set; } = new List<Amendment>();
        public Tiret(Paragraph paragraph, Letter parent, int ordinal = 1) : base(paragraph, parent)
        {
            EntityType = "TIR";
            EffectiveDate = parent.EffectiveDate;
            ContentParser tiret = new ContentParser(this);
            ContentText = tiret.Content;
            Number = ordinal;
            bool isAdjacent = true;
            Log.Information("Tiret: {Number} - {Content}", Number, ContentText.Substring(0, Math.Min(ContentText.Length, 100)));
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
            if (tiret.HasAmendmentOperation)
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
            var newElement = new XElement(XmlConstants.Tiret,
                new XAttribute("id", Id));
            if (generateGuids) newElement.Add(new XAttribute("guid", Guid));
            newElement.AddFirst(new XElement(XmlConstants.Number, Number));
            newElement.Add(new XElement("text", ContentText));
            foreach (var amendmentOperation in AmendmentOperations)
            {
                newElement.Add(amendmentOperation.ToXML(generateGuids));
            }
            return newElement;
        }

        public override string Id => $"{Parent?.Id ?? string.Empty}.tir_{Number}";
    }
}