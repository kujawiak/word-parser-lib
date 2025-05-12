using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using Serilog;

namespace WordParserLibrary.Model
{
    public class Article : BaseEntity, IXmlConvertible {
        public string Number { get; set; } = string.Empty;
        public bool IsAmending { get; set; } = false;
        public string PublicationYear { get; set; } = string.Empty;
        public string PublicationNumber { get; set; } = string.Empty;
        public List<Subsection> Subsections { get; set; } = new List<Subsection>();
        //TODO: For test purposes only, remove later
        public List<string> AmendmentList { get; set; } = new List<string>();

        public Article(Paragraph paragraph) : base(paragraph, null)
        {
            ContentParser article = new ContentParser(this);
            article.ParseArticle();
            if (article.ParserError)
            {
                Log.Error("Error parsing article: {ErrorMessage}", article.ErrorMessage);
                return;
            }
            Number = article.Number;
            IsAmending = !string.IsNullOrEmpty(article.PublicationNumber) && !string.IsNullOrEmpty(article.PublicationYear);
            if (IsAmending)
            {
                PublicationNumber = LegalReference.PublicationNumber = article.PublicationNumber;
                PublicationYear = LegalReference.PublicationYear = article.PublicationYear;
            }
            // Każdy artykuł zawiera co najmniej jeden ustęp, którego treść jest zawarta w treści artykułu
            Content = String.Empty;
            Log.Information("Article: {Number} - {Content}", Number, Content.Substring(0, Math.Min(Content.Length, 50)));
            var firstSubsection = new Subsection(paragraph, this);
            Subsections = [firstSubsection];
            while (paragraph.NextSibling() is Paragraph nextParagraph 
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
                new XAttribute("id", BuildId()));
            if (generateGuids) newElement.Add(new XAttribute("guid", Guid));
            newElement.AddFirst(new XElement(XmlConstants.Number, Number));
            if (IsAmending)
            {
                newElement.Add(
                new XElement("publication",
                    new XAttribute("year", PublicationYear),
                    new XAttribute("number", PublicationNumber)));
            }
            foreach (var subsection in Subsections)
            {
                newElement.Add(subsection.ToXML(generateGuids));
            }
            return newElement;
        }

        public string BuildId()
        {
            return $"art_{Number}";
        }

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
                new Text(Subsections.First().Content) { Space = SpaceProcessingModeValues.Preserve }
            ));
            return p;
        }
    }
}