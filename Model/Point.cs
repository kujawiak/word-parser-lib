using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Wordprocessing;

namespace WordParserLibrary.Model
{
    public class Point : BaseEntity, IAmendable {
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
    }
}