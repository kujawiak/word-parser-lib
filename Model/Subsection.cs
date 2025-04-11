using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Wordprocessing;

namespace WordParserLibrary.Model
{
     public class Subsection : BaseEntity, IAmendable {
        public List<Point> Points { get; set; }
        public string Number { get; set; }
        public List<Amendment> Amendments { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Subsection"/> class.
        /// </summary>
        /// <param name="paragraph">The paragraph associated with this subsection.</param>
        /// <param name="article">The parent article of this subsection.</param>
        /// <param name="ordinal">The ordinal number of this subsection. Default is 1.</param>
        public Subsection(Paragraph paragraph, Article article) : base(paragraph, article)
        {
            var parsedSubsection = ParseSubsection(Content);
            Number = parsedSubsection[0];
            Content = parsedSubsection[1];
            Points = new List<Point>();
            Amendments = new List<Amendment>();
            bool isAdjacent = true;
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
        private string[] ParseSubsection(string text)
        {
            if (text.StartsWith("Art."))
            {
                // Dopasowanie do formatu: Art. X. Y. text
                var matchWithY = Regex.Match(text, @"^Art\.\s\d+\.\s(\d+\w*)\.\s(.*)$");
                if (matchWithY.Success)
                {
                    return [matchWithY.Groups[1].Value, matchWithY.Groups[2].Value];
                }

                // Dopasowanie do formatu: Art. X. text
                var matchWithoutY = Regex.Match(text, @"^Art\.\s\d+\.\s(.*)$");
                if (matchWithoutY.Success)
                {
                    return ["1", matchWithoutY.Groups[1].Value];
                }

                throw new FormatException("The text format is invalid for an article.");
            }
            else
            {
                // Dopasowanie do formatu: Y. text
                var match = Regex.Match(text, @"^(\d+\w*)\.\s(.*)$");
                if (match.Success)
                {
                    return [match.Groups[1].Value, match.Groups[2].Value];
                }

                throw new FormatException("The text format is invalid.");
            }
        }
    }
}