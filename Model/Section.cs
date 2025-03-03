using System;
using System.Collections.Generic;
using DocumentFormat.OpenXml.Wordprocessing;

namespace WordParserLibrary.Model
{
    // Oddział
    public class Section 
    {
        Chapter Parent { get; set; }
        public string Number { get; set; }
        public List<Article> Articles { get; set; }

        internal void AddArticle(Paragraph paragraph)
        {
            var article = new Article(paragraph);
            Articles.Add(article);
        }
    }
}