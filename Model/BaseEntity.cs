using System;
using System.Collections.Generic;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Text.RegularExpressions;

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
        public Paragraph? Paragraph { get; set; }
        public List<AmendmentOperation> AmendmentOperations { get; set; }
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
            }
            Paragraph = paragraph;
            Content = paragraph.InnerText.Sanitize();
            AmendmentOperations = new List<AmendmentOperation>();
            if (Article != null && Article.IsAmending) 
                TryParseAmendingOperation();
        }

        internal void TryParseAmendingOperation()
        {
            if (Content.Contains("uchyla się"))
            {
                System.Console.WriteLine("[BaseEntity] TryParseAmendingOperation: możliwa zmiana uchylająca: " + Content);
                var regex = new Regex(@"art\. (\d+[\w\-]*)");
                var matches = regex.Matches(Content);
                foreach (Match match in matches)
                {
                    var amendmentOperation = new AmendmentOperation
                    {
                        Type = AmendmentOperationType.Repealed
                    };
                    var articleNumber = match.Groups[1].Value;
                    amendmentOperation.AmendmentTarget = new AmendmentTarget
                    {
                        ActNumber = Article?.PublicationNumber,
                        ActYear = Article?.PublicationYear,
                        Article = articleNumber
                    };
                    AmendmentOperations.Add(amendmentOperation);
                }
            }
        }
    }

    public class AmendmentOperation
    {
        public AmendmentOperationType Type { get; set; }
        public AmendmentTarget AmendmentTarget { get; set; } = new AmendmentTarget();
    }

    public class AmendmentTarget
    {
        public string? ActNumber { get; set; }
        public string? ActYear { get; set; }
        public string? Article { get; set; }
        public string? Subsection { get; set; }
        public string? Point { get; set; }
        public string? Letter { get; set; }
        public string? Tiret { get; set; }
        public override string ToString()
        {
            return $"{ActNumber}/{ActYear} art. {Article} ust. {Subsection} pkt. {Point} lit. {Letter} tiret {Tiret}";
        }
    }

    public enum AmendmentOperationType
    {
        [EnumDescription("uchylenie")]
        Repealed
    }
}