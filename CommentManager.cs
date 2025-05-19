using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing; // Added for the Comment type
using DocumentFormat.OpenXml; // Added for the OpenXmlElement type

namespace WordParserLibrary
{
    public class CommentManager
    {
        public MainDocumentPart MainPart { get; }


        public CommentManager(MainDocumentPart mainDocumentPart)
        {
            MainPart = mainDocumentPart;
        }

        public void RemoveSystemComments()
        {
            var commentPart = MainPart.WordprocessingCommentsPart;
            if (commentPart != null)
            {
                var comments = commentPart.Comments.Elements<Comment>().Where(c => c.Author == "System").ToList();
                foreach (var comment in comments)
                {
                    var commentId = comment.Id?.Value;
                    if (commentId == null) continue;

                    // Usuń zakresy komentarzy
                    var commentRangeStarts = MainPart.Document.Descendants<CommentRangeStart>().Where(c => c.Id == commentId).ToList();
                    var commentRangeEnds = MainPart.Document.Descendants<CommentRangeEnd>().Where(c => c.Id == commentId).ToList();
                    foreach (var rangeStart in commentRangeStarts)
                    {
                        rangeStart.Remove();
                    }
                    foreach (var rangeEnd in commentRangeEnds)
                    {
                        rangeEnd.Remove();
                    }

                    // Usuń odniesienia do komentarzy
                    var commentReferences = MainPart.Document.Descendants<CommentReference>().Where(c => c.Id == commentId).ToList();
                    foreach (var reference in commentReferences)
                    {
                        reference.Remove();
                    }

                    // Usuń komentarz
                    comment.Remove();
                }
            }
        }

        public void AddComment(OpenXmlElement element, string commentText)
        {
            var commentPart = MainPart.WordprocessingCommentsPart;
            if (commentPart == null)
            {
                commentPart = MainPart.AddNewPart<WordprocessingCommentsPart>();
                commentPart.Comments = new Comments();
            }

            var commentId = "sys_" + commentPart.Comments.Elements<Comment>().Count().ToString();
            var comment = new Comment { Id = commentId, Author = "System", Date = DateTime.Now };
            comment.AppendChild(new Paragraph(new Run(new Text(commentText))));
            commentPart.Comments.Append(comment);

            var commentRangeStart = new CommentRangeStart { Id = commentId };
            var commentRangeEnd = new CommentRangeEnd { Id = commentId };

            if (element is Run)
            {
                element.InsertBefore(commentRangeStart, element.FirstChild);
                element.InsertAfter(commentRangeEnd, element.LastChild);
            }
            else if (element is Paragraph paragraph)
            {
                var firstRun = paragraph.Elements<Run>().FirstOrDefault();
                var lastRun = paragraph.Elements<Run>().LastOrDefault();

                if (firstRun != null)
                {
                    firstRun.InsertBeforeSelf(commentRangeStart);
                    firstRun.InsertAfterSelf(commentRangeEnd);
                }
                else
                {
                    paragraph.InsertBefore(commentRangeStart, paragraph.FirstChild);
                }

                // if (lastRun != null)
                // {
                //     lastRun.InsertAfterSelf(commentRangeEnd);
                // }
                // else
                // {
                //     paragraph.AppendChild(commentRangeEnd);
                // }
            }

            var commentReference = new CommentReference { Id = commentId };
            element.AppendChild(commentReference);
            commentPart.Comments.Save();
        }


        public void CommentErrors(LegalAct legalAct)
        {
            foreach (var article in legalAct.Articles)
            {
                if (article.Error == true && article.ErrorMessage != null)
                {
                    var commentText = $"Błąd: {article.ErrorMessage}";
                    AddComment(article.Paragraph, commentText);
                }
                foreach (var subsection in article.Subsections)
                {
                    if (subsection.Error == true && subsection.ErrorMessage != null)
                    {
                        var commentText = $"Błąd: {subsection.ErrorMessage}";
                        AddComment(subsection.Paragraph, commentText);
                    }
                    foreach (var point in subsection.Points)
                    {
                        if (point.Error == true && point.ErrorMessage != null)
                        {
                            var commentText = $"Błąd: {point.ErrorMessage}";
                            AddComment(point.Paragraph, commentText);
                        }
                        foreach (var letter in point.Letters)
                        {
                            if (letter.Error == true && letter.ErrorMessage != null)
                            {
                                var commentText = $"Błąd: {letter.ErrorMessage}";
                                AddComment(letter.Paragraph, commentText);
                            }
                            if (letter.Tirets != null && letter.Tirets.Any())
                            {
                                foreach (var tiret in letter.Tirets)
                                {
                                    if (tiret.Error == true && tiret.ErrorMessage != null)
                                    {
                                        var commentText = $"Błąd: {tiret.ErrorMessage}";
                                        AddComment(tiret.Paragraph, commentText);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public int AddCommentsToHyperlinks(Paragraph paragraph)
        {
            int commentCount = 0;
            foreach (var hyperlink in paragraph.Descendants<Hyperlink>())
            {
                string? hyperlinkUri = null;
                if (hyperlink.Id != null)
                {
                    var relationship = MainPart.HyperlinkRelationships.FirstOrDefault(r => r.Id == hyperlink.Id);
                    if (relationship != null)
                    {
                        hyperlinkUri = relationship.Uri.ToString();
                    }
                }

                if (hyperlinkUri != null)
                {
                    var hyperlinkText = hyperlink.Descendants<Run>().Select(r => r.InnerText).FirstOrDefault();
                    var commentText = $"Hiperłącze: {hyperlinkUri}\nTekst: {hyperlinkText}";

                    Console.WriteLine("[HLINKS]\tDodawanie komentarza: " + commentText);

                    AddComment(hyperlink, commentText);

                    commentCount++;
                }
            }
            return commentCount;
        }
    }
}