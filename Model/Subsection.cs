using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using Serilog;

namespace WordParserLibrary.Model
{
     public class Subsection : BaseEntity, IAmendable, IXmlConvertible {
        public List<Point> Points { get; set; } = new List<Point>();
        public string Number { get; set; }  = string.Empty;
        public List<Amendment> Amendments { get; set; } = new List<Amendment>();

        public Subsection(Paragraph paragraph, Article article) : base(paragraph, article)
        {
            EntityType = "UST";
            EffectiveDate = article.EffectiveDate;
            ContentParser subsection = new ContentParser(this);
            subsection.ParseSubsection();
            if (subsection.ParserError)
            {
                Log.Error("Error parsing subsection: {ErrorMessage}", subsection.ErrorMessage);
                return;
            }
            Number = subsection.Number;
            ContentText = subsection.Content;
            bool isAdjacent = true;
            Log.Information("Subsection: {Number} - {Content}", Number, ContentText.Substring(0, Math.Min(ContentText.Length, 100)));
            while (paragraph.NextSibling() is Paragraph nextParagraph 
                    && nextParagraph.StyleId("UST") != true
                    && nextParagraph.StyleId("ART") != true)
            {
                if (nextParagraph.StyleId("PKT") == true)
                {
                    Points.Add(new Point(nextParagraph, this));
                    isAdjacent = false;
                }
                else if (nextParagraph.StyleId("Z") == true && isAdjacent)
                {
                    Amendments.Add(new Amendment(nextParagraph, this));
                    AmendmentOperations?.FirstOrDefault()?.Amendments.Add(new Amendment(nextParagraph, this));
                }
                else 
                {
                    isAdjacent = false;
                }
                paragraph = nextParagraph;
            }
            if (subsection.HasAmendmentOperation)
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
            var newElement = new XElement(XmlConstants.Subsection,
                new XAttribute("id", Id));
            if (generateGuids) newElement.Add(new XAttribute("guid", Guid));
            newElement.AddFirst(new XElement(XmlConstants.Number, Number));
            newElement.Add(new XElement("text", ContentText));
            foreach (var point in Points)
            {
                newElement.Add(point.ToXML(generateGuids));
            }
            foreach (var amendmentOperation in AmendmentOperations)
            {
                newElement.Add(amendmentOperation.ToXML(generateGuids));
            }
            return newElement;
        }

        public override string Id => $"{Parent?.Id ?? string.Empty}.ust_{Number}";

        public Paragraph ToParagraph()
        {
            var p = new Paragraph()
            {
                ParagraphProperties = new ParagraphProperties(
                    new ParagraphStyleId { Val = "USTustnpkodeksu" }
                )
            };
            p.Append(new Run(new Text($"{Number}.\u00A0")));
            p.Append(new Run(new Text($"{ContentText}")));
            return p;
        }
    }
}