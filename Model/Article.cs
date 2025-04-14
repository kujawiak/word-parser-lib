using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using DocumentFormat.OpenXml.Wordprocessing;

namespace WordParserLibrary.Model
{
    public class Article : BaseEntity, IXmlConvertible {
        public string Number { get; set; }
        public bool IsAmending { get; set; }
        public string? PublicationYear { get; set; }
        public string? PublicationNumber { get; set; }
        public List<Subsection> Subsections { get; set; }
        public List<string> AmendmentList { get; set; }

        public Article(Paragraph paragraph) : base(paragraph, null)
        {
            var parsedArticle = ParseArticle(Content);
            Number = parsedArticle[1].Value;
            Content = parsedArticle[2].Value;
            IsAmending = SetAmendment();
            AmendmentList = new List<string>();
            if (IsAmending)
            {
                LegalReference.PublicationNumber = PublicationNumber;
                LegalReference.PublicationYear = PublicationYear;
            }
            // Każdy artykuł zawiera co najmniej jeden ustęp, którego treść jest zawarta w treści artykułu
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

        GroupCollection ParseArticle(string text)
        {
            var match = Regex.Match(text, @"Art\. ([\w\d]+)+\.?\s*(.*)");
            return match.Groups;
        }

        bool SetAmendment()
        {
            var publication = new Regex(@"Dz\.\sU\.\sz\s(\d{4})\sr\.\spoz\.\s(\d+)");
            if (publication.Match(Content).Success)
            {
                PublicationYear = publication.Match(Content).Groups[1].Value;
                PublicationNumber = publication.Match(Content).Groups[2].Value;
                return true;
            } else {
                return false;
            }
        }

        public XElement ToXML()
        {
            var newElement = new XElement(XMLConstants.Article,
                new XAttribute("guid", Guid),
                new XAttribute("id", BuildId()));
            newElement.AddFirst(new XElement(XMLConstants.Number, Number));
            if (IsAmending)
            {
                newElement.Add(
                new XElement("publication",
                    new XAttribute("year", PublicationYear),
                    new XAttribute("number", PublicationNumber)));
            }
            foreach (var subsection in Subsections)
            {
                newElement.Add(subsection.ToXML());
            }
            return newElement;
        }

        public string BuildId()
        {
            //var parentId = (Article)Parent?.GetId();
            return $"art_{Number}";
        }
    }
}