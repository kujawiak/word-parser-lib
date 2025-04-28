using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Xml.Linq;
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
            ContentParser subsection = new ContentParser(this);
            subsection.ParseSubsection();
            if (subsection.ParserError)
                return;
            Number = subsection.Number;
            Content = subsection.Content;
            bool isAdjacent = true;
            Log.Debug("Subsection: {Number} - {Content}", Number, Content.Substring(0, Math.Min(Content.Length, 50)));
            while (paragraph.NextSibling() is Paragraph nextParagraph 
                    && nextParagraph.StyleId("UST") != true
                    && nextParagraph.StyleId("ART") != true
                    && isAdjacent)
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
            var newElement = new XElement(XMLConstants.Subsection,
                new XAttribute("id", BuildId()));
            if (generateGuids) newElement.Add(new XAttribute("guid", Guid));
            newElement.AddFirst(new XElement(XMLConstants.Number, Number));
            newElement.Add(new XElement("text", Content));
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

        public string BuildId()
        {
            var parentId = (Parent as Article)?.BuildId();
            return parentId != null ? $"{parentId}.ust_{Number}" : $"ust_{Number}";
        }
    }

    public class ContentParser
    {
        private BaseEntity entity = null!;
        public string Number { get; private set; } = string.Empty;
        public string Content { get; private set; } = string.Empty;
        public bool ParserError { get; private set; } = false;

        public ContentParser(BaseEntity entity)
        {
            this.entity = entity;
        }
        
        public ContentParser ParseSubsection()
        {
            var text = entity.Content.Trim();
            if (text.StartsWith("Art."))
            {
                // Dopasowanie do formatu: Art. X. Y. text
                var matchWithY = Regex.Match(text, @"^Art\.\s\d+\.\s(\d+\w*)\.\s(.*)$");
                if (matchWithY.Success)
                {
                    Number = matchWithY.Groups[1].Value;
                    Content = matchWithY.Groups[2].Value;
                    return this;
                }

                // Dopasowanie do formatu: Art. X. text
                var matchWithoutY = Regex.Match(text, @"^Art\.\s\d+\.\s?(.*)$");
                if (matchWithoutY.Success)
                {
                    Number = "1"; // Domyślnie ustawiamy numer na 1, jeśli nie ma Y
                    Content = matchWithoutY.Groups[1].Value;
                    return this;
                }

                // throw new FormatException("The text format is invalid.");
                ParserError = true;
                entity.Error = true;
                entity.ErrorMessage = "Oczekiwano formatu: Art. X. Y. text lub Art. X. text.\nMożliwy błędny styl paragrafu.";
                return this;
            }
            else
            {
                // Dopasowanie do formatu: Y. text
                var match = Regex.Match(text, @"^(\d+\w*)\.\s?(.*)$");
                if (match.Success)
                {
                    Number = match.Groups[1].Value;
                    Content = match.Groups[2].Value;
                    return this;
                }

                //throw new FormatException("The text format is invalid.");
                ParserError = true;
                entity.Error = true;
                entity.ErrorMessage = "Oczekiwano formatu: Y. text.\nMożliwy błędny styl paragrafu.";
                return this;
            }

            // var match = Regex.Match(text, @"^(?<number>\d+)\s*(?<content>.*)$");
            // if (!match.Success)
            //     throw new FormatException($"Invalid subsection format: {text}");

            // var number = match.Groups["number"].Value;
            // var content = match.Groups["content"].Value.Trim();

            // return new TextSegment(number, content);
        }
    }
}