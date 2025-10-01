using System;
using System.Collections.Generic;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Text.RegularExpressions;
using System.Text;

namespace WordParserLibrary.Model
{
    public abstract class BaseEntity 
    {
        public BaseEntity? Parent { get; set; }
        public Article? Article { get; set; }
        public Subsection? Subsection { get; set; }
        public Point? Point { get; set; }
        public Letter? Letter { get; set; }
        public Tiret? Tiret { get; set; }
        public Guid Guid { get; set; } = Guid.NewGuid();
        public EntityNumber Number { get; set; }
        public string ContentText { get; set; }
        public string Context { get; set; }
        public Paragraph Paragraph { get; set; }
        public List<AmendmentOperation> AmendmentOperations { get; set; }
        public LegalReference LegalReference { get; set; } = new LegalReference(); // TODO: remove when AI method of amendment identification is introduced
        public bool? Error { get; set; }
        public string? ErrorMessage { get; set; }
        public string EntityType { get; set; } = string.Empty;
        public DateTime EffectiveDate { get; set; }

        public BaseEntity(Paragraph paragraph, BaseEntity? parent)
        {
            if (parent != null)
            {
                Parent = parent;
                EffectiveDate = parent.EffectiveDate;
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
                    case Tiret tiret:
                        Tiret = tiret;
                        Article = parent.Article;
                        Subsection = parent.Subsection;
                        Point = parent.Point;
                        Letter = parent.Letter;
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
            ContentText = paragraph.InnerText.Sanitize().Trim();
            Context = GetContext() ?? ContentText;
            Number = new EntityNumber(paragraph);
            AmendmentOperations = new List<AmendmentOperation>();
            if (Article != null && Article.IsAmending && Paragraph.StyleId("Z") == false)
            {
                UpdateLegalReference();
            }
        }
        void UpdateLegalReference()
        {
            if (LegalReference.Article == null)
            {
                var regex = new Regex(@"(?:po|Po|w|W)\s*art\.\s*([a-zA-Z0-9]+)");
                var match = regex.Match(ContentText);
                if (match.Success) LegalReference.Article = match.Groups[1].Value;
            }
            if (LegalReference.Subsection == null)
            {
                var subsectionRegex = new Regex(@"(?:po|Po|w|W)\s*ust\.\s*([a-zA-Z0-9]+)");
                var subsectionMatch = subsectionRegex.Match(ContentText);
                if (subsectionMatch.Success) LegalReference.Subsection = subsectionMatch.Groups[1].Value;
            }
            if (LegalReference.Point == null)
            {
                var pointRegex = new Regex(@"(?:po|Po|w|W)\s*pkt\s*([a-zA-Z0-9]+)");
                var pointMatch = pointRegex.Match(ContentText);
                if (pointMatch.Success) LegalReference.Point = pointMatch.Groups[1].Value;
            }
            if (LegalReference.Letter == null)
            {
                //TODO: Weryfikacja przykładem
                var letterRegex = new Regex(@"(?:po|Po|w|W)\s*lit\.\s*([a-zA-Z])");
                var letterMatch = letterRegex.Match(ContentText);
                if (letterMatch.Success) LegalReference.Letter = letterMatch.Groups[1].Value;
            }
        }
        private string? GetContext()
        {
            var contextBuilder = new StringBuilder();
            var currentEntity = this;

            while (currentEntity != null && !(currentEntity is Article))
            {
                if (!string.IsNullOrEmpty(currentEntity.ContentText))
                {
                    contextBuilder.Insert(0, currentEntity.ContentText + " ");
                }
                currentEntity = currentEntity.Parent;
            }

            return contextBuilder.ToString().Trim();
        }
        public abstract string Id { get; }
    }
}