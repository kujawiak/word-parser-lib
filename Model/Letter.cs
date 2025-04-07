using System;
using System.Collections.Generic;
using DocumentFormat.OpenXml.Wordprocessing;

namespace WordParserLibrary.Model
{
        public class Letter : BaseEntity, IAmendable {
        public List<Tiret> Tirets { get; set; }
        public string Ordinal { get; set; }
        public List<Amendment> Amendments { get; set; }
        public string AmendedArticle { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public AmendmentOperationType AmendmentOperationType { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public Letter(Paragraph paragraph, Point parent) : base(paragraph, parent)
        {
            var parsedLetter = ParseOrdinal(Content);
            Ordinal = parsedLetter[1].Value;
            Content = parsedLetter[2].Value;
            Tirets = new List<Tiret>();
            Amendments = new List<Amendment>();
            bool isAdjacent = true;
            var tiretCount = 1;
            while (paragraph.NextSibling() is Paragraph nextParagraph 
                    && nextParagraph.StyleId("LIT") != true
                    && nextParagraph.StyleId("PKT") != true
                    && nextParagraph.StyleId("UST") != true
                    && nextParagraph.StyleId("ART") != true)
            {
                if (nextParagraph.StyleId("TIR") == true)
                {
                    Tirets.Add(new Tiret(nextParagraph, this, tiretCount));
                    tiretCount++;
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
            if (Amendments.Any())
            {
                AmendmentBuilder ab = new AmendmentBuilder();
                AmendmentOperations = ab.Build(Amendments, this);
            }
        }

        private object ParseLetter(string content)
        {
            throw new NotImplementedException();
        }
    }
}