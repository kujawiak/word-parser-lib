using System;
using System.Collections.Generic;
using DocumentFormat.OpenXml.Wordprocessing;

namespace WordParserLibrary.Model
{
    public class Amendment : BaseEntity
    {
        public Amendment(Paragraph paragraph, BaseEntity parent) : base(paragraph, parent)
        {
            Article = parent.Article ?? (parent as Article);
            Subsection = parent.Subsection ?? (parent as Subsection);
            Point = parent.Point ?? (parent as Point);
            Letter = parent.Letter ?? (parent as Letter);
            Tiret = parent.Tiret ?? (parent as Tiret);
            Parent = parent;
            Paragraph = paragraph;
        }
    }
}