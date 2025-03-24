using System;
using System.Collections.Generic;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Text.RegularExpressions;
using System.Text;

namespace WordParserLibrary.Model
{
    public class BaseEntity 
    {
        public BaseEntity? Parent { get; set; }
        public Article? Article { get; set; }
        public Subsection? Subsection { get; set; }
        public Point? Point { get; set; }
        public Letter? Letter { get; set; }
        public Tiret? Tiret { get; set; }
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Content { get; set; }
        public string Context { get; set; }
        public Paragraph? Paragraph { get; set; }
        public List<AmendmentOperation> AmendmentOperations { get; set; }
        public LegalReference LegalReference { get; set; } = new LegalReference();
        public BaseEntity(Paragraph paragraph, BaseEntity? parent)
        {
            if (parent != null)
            {
                Parent = parent;
                switch (parent)
                {
                    case Article article:
                        Article = article;
                        break;
                    case Subsection subsection:
                        Subsection = subsection;
                        Article = parent.Article;
                        break;
                    case Point point:
                        Point = point;
                        Article = parent.Article;
                        Subsection = parent.Subsection;
                        break;
                    case Letter letter:
                        Letter = letter;
                        Article = parent.Article;
                        Subsection = parent.Subsection;
                        Point = parent.Point;
                        break;
                    default:
                        throw new ArgumentException("Invalid parent type", nameof(parent));
                }
                LegalReference = new LegalReference
                {
                    PublicationNumber = parent.LegalReference.PublicationNumber,
                    PublicationYear = parent.LegalReference.PublicationYear,
                    Article = parent.LegalReference.Article,
                    Subsection = parent.LegalReference.Subsection,
                    Point = parent.LegalReference.Point,
                    Letter = parent.LegalReference.Letter,
                    Tiret = parent.LegalReference.Tiret
                };
            }
            Paragraph = paragraph;
            Content = paragraph.InnerText.Sanitize();
            Context = GetContext() ?? Content;
            AmendmentOperations = new List<AmendmentOperation>();
            if (Article != null && Article.IsAmending)
            {
                UpdateLegalReference();
                TryParseAmendingOperation();
            }
        }
        void UpdateLegalReference()
        {
            if (LegalReference.Article == null)
            {
                var regex = new Regex(@"(?:po|Po|w|W)\s*art\.\s*([a-zA-Z0-9]+)");
                var match = regex.Match(Content);
                if (match.Success) LegalReference.Article = match.Groups[1].Value;
            }
            if (LegalReference.Subsection == null)
            {
                var subsectionRegex = new Regex(@"(?:po|Po|w|W)\s*ust\.\s*([a-zA-Z0-9]+)");
                var subsectionMatch = subsectionRegex.Match(Content);
                if (subsectionMatch.Success) LegalReference.Subsection = subsectionMatch.Groups[1].Value;
            }
            if (LegalReference.Point == null)
            {
                var pointRegex = new Regex(@"(?:po|Po|w|W)\s*pkt\s*([a-zA-Z0-9]+)");
                var pointMatch = pointRegex.Match(Content);
                if (pointMatch.Success) LegalReference.Point = pointMatch.Groups[1].Value;
            }
            if (LegalReference.Letter == null)
            {
                //TODO: Weryfikacja przykładem
                var letterRegex = new Regex(@"(?:po|Po|w|W)\s*lit\.\s*([a-zA-Z])");
                var letterMatch = letterRegex.Match(Content);
                if (letterMatch.Success) LegalReference.Letter = letterMatch.Groups[1].Value;
            }
        }
        private string? GetContext()
        {
            var contextBuilder = new StringBuilder();
            var currentEntity = this;

            while (currentEntity != null && !(currentEntity is Article))
            {
                if (!string.IsNullOrEmpty(currentEntity.Content))
                {
                    contextBuilder.Insert(0, currentEntity.Content + " ");
                }
                currentEntity = currentEntity.Parent;
            }

            return contextBuilder.ToString().Trim();
        }
        
        public GroupCollection ParseOrdinal(string text)
        {
            var match = Regex.Match(text, @"^([^\)]+)\)[\s]?(.*)");
            return match.Groups;
        }

        internal void TryParseAmendingOperation()
        {
            if (Context.Contains("uchyla się"))
            {
                Console.WriteLine("[BaseEntity] TryParseAmendingOperation: możliwa zmiana uchylająca: " + Content);
            }

            string pattern = @"dodaje się (?<newObject>.*?) w brzmieniu:";
            Match match = Regex.Match(Context, pattern);
            if (match.Success)
            {
                var amendmentOperation = new AmendmentOperation
                {
                    Type = AmendmentOperationType.Insertion,
                    AmendmentTarget = LegalReference,
                    AmendmentObject = match.Groups["newObject"].Value
                };
                //TODO: Rozbić operację na liczbę dodawanych obiektów
                AmendmentOperations.Add(amendmentOperation);
            }
        }
    }
}