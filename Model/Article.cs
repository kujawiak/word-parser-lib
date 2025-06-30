using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using Serilog;
using WordParserLibrary.Helpers;

namespace WordParserLibrary.Model
{
    /// <summary>
    /// Informacje o publikatorze (np. Dziennik Ustaw).
    /// </summary>
    public class JournalInfo
    {
        public int Year { get; set; }
        public List<int> Positions { get; set; } = new List<int>();
        public string SourceString { get; set; } // np. "Dz. U. z 2024 r. poz. 964"

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var position in Positions)
            {
                sb.AppendLine($"DU.{Year}.{position}");
            }
            return sb.ToString();
        }

        public string ToStringLong()
        {
            return $"Rok: {Year}, Pozycje: {string.Join(", ", Positions)} (Fragment źródłowy: \"{SourceString}\")";
        }
    }

    public class Article : BaseEntity, IXmlConvertible
    {
        LegalAct ParentLegalAct { get; set; }
        public bool IsAmending => Journals.Count > 0;
        public List<JournalInfo> Journals { get; set; } = new List<JournalInfo>();
        public List<Subsection> Subsections { get; set; } = new List<Subsection>();
        //TODO: For test purposes only, remove later
        public List<string> AllAmendments { get; set; } = new List<string>();

        public Article(Paragraph paragraph, LegalAct legalAct) : base(paragraph, null)
        {
            ParentLegalAct = legalAct;
            EffectiveDate = legalAct.EffectiveDate;
            EntityType = "ART";
            ParagraphParser paragraphParser = new ParagraphParser();
            paragraphParser.ParseParagraph(this);
            Log.Information("Article: {Number} - {Content}", Number, ContentText.Substring(0, Math.Min(ContentText.Length, 50)));
            // Każdy artykuł zawiera co najmniej jeden ustęp, którego treść jest zawarta w treści artykułu
            ContentText = String.Empty;
            var firstSubsection = new Subsection(paragraph, this);
            Subsections = [firstSubsection];
            while (paragraph.NextSibling<Paragraph>() is Paragraph nextParagraph
                    && nextParagraph.StyleId("ART") != true)
            {
                if (nextParagraph.StyleId("UST") == true)
                {
                    Subsections.Add(new Subsection(nextParagraph, this));
                }
                paragraph = nextParagraph;
            }
        }

        public XElement ToXML(bool generateGuids)
        {
            var newElement = new XElement(XmlConstants.Article,
                new XAttribute("id", Id));
            if (generateGuids) newElement.Add(new XAttribute("guid", Guid));
            newElement.AddFirst(new XElement(XmlConstants.Number, Number));
            if (IsAmending)
            {
                foreach (var journal in Journals)
                {
                    newElement.Add(new XElement("publication",
                        new XAttribute("year", journal.Year),
                        new XAttribute("positions", string.Join(",", journal.Positions))));
                }
            }
            foreach (var subsection in Subsections)
            {
                newElement.Add(subsection.ToXML(generateGuids));
            }
            return newElement;
        }

        public override string Id => $"art_{Number}";

        public Paragraph ToParagraph()
        {
            var p = new Paragraph()
            {
                ParagraphProperties = new ParagraphProperties(
                    new ParagraphStyleId { Val = "ARTartustawynprozporzdzenia" }
                )
            };
            p.Append(new Run(
                new RunProperties(new RunStyle { Val = "Ppogrubienie" }),
                new Text($"Art.\u00A0{Number}.\u00A0") { Space = SpaceProcessingModeValues.Preserve }
            ));
            if (Subsections.Count > 1)
            {
                p.Append(
                    new Run(
                        new Text($"{Subsections.First().Number}.\u00A0") { Space = SpaceProcessingModeValues.Preserve }
                    )
                );
            }
            p.Append(new Run(
                new Text(Subsections.First().ContentText) { Space = SpaceProcessingModeValues.Preserve }
            ));
            return p;
        }
    }
}