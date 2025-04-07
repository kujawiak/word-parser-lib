using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System;
using System.Collections;
using System.Diagnostics;
using System.IO.Packaging;
using System.Linq;
using System.Xml;
using WordParserLibrary.Model;

namespace WordParserLibrary
{
    public class LegalAct
    {
        public WordprocessingDocument _wordDoc
         { get; }
        public MainDocumentPart MainPart { get; }
        public DocumentSettingsPart? SettingsPart { get; }

        public Title Title { get; set; }
        public List<Article> Articles { get; set; } = new List<Article>();

        public LegalAct(WordprocessingDocument wordDoc)
        {
            _wordDoc = wordDoc;
            MainPart = _wordDoc.MainDocumentPart ?? throw new InvalidOperationException("MainDocumentPart is null.");
            Title = new Title(MainPart.Document.Descendants<Paragraph>()
                                        .Where(p => p.ParagraphProperties != null 
                                                && p.StyleId("TYTUAKT") == true).FirstOrDefault() ?? throw new InvalidOperationException("Title paragraph not found"));                                        // Tytuł #1
            // Title.Parts.Add(new Part());                                // Dział #1
            // Title.Parts[0].Chapters.Add(new Chapter());                 // Rozdział #1
            // Title.Parts[0].Chapters[0].Sections.Add(new Section());     // Oddział #1
            
            foreach (var paragraph in MainPart.Document.Descendants<Paragraph>()
                                                        .Where(p => p.InnerText.StartsWith("Art."))
                                                        .ToList())
            {
                if (paragraph.ParagraphProperties == null)
                {
                    Console.WriteLine("[CTOR]\tBrak właściwości paragrafu!");
                    continue;
                }
                if (paragraph.ParagraphProperties.ParagraphStyleId == null)
                {
                    Console.WriteLine("[CTOR]\tBrak stylu paragrafu!");
                    continue;
                }
                
                var paragraphStyle = paragraph.ParagraphProperties?.ParagraphStyleId?.Val;

                if (paragraphStyle != null && paragraphStyle?.ToString()?.StartsWith("ART") == true)
                {
                    // Console.WriteLine("[CTOR]\tZnaleziono artykuł w paragrafie: " + paragraph.InnerText);
                    Articles.Add(new Article(paragraph));
                }
            }
            Console.WriteLine("Znaleziono artykułów: " + Articles.Count);
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

        public int ParseHyperlinks()
        {
            int commentCount = 0;

            // Parsowanie paragrafów w treści dokumentu
            foreach (var paragraph in MainPart.Document.Descendants<Paragraph>())
            {
                commentCount += AddCommentsToHyperlinks(paragraph);
            }

            // Parsowanie przypisów dolnych
            if (MainPart.FootnotesPart != null)
            {
                foreach (var footnote in MainPart.FootnotesPart.Footnotes.Elements<Footnote>())
                {
                    foreach (var paragraph in footnote.Descendants<Paragraph>())
                    {
                        commentCount += AddCommentsToHyperlinks(paragraph);
                    }
                }
            }

            // Parsowanie przypisów końcowych
            if (MainPart.EndnotesPart != null)
            {
                foreach (var endnote in MainPart.EndnotesPart.Endnotes.Elements<Endnote>())
                {
                    foreach (var paragraph in endnote.Descendants<Paragraph>())
                    {
                        commentCount += AddCommentsToHyperlinks(paragraph);
                    }
                }
            }

            return commentCount;
        }

        private int AddCommentsToHyperlinks(Paragraph paragraph)
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

        private void AddComment(OpenXmlElement element, string commentText)
        {
            var commentPart = MainPart.WordprocessingCommentsPart;
            if (commentPart == null)
            {
                commentPart = MainPart.AddNewPart<WordprocessingCommentsPart>();
                commentPart.Comments = new Comments();
            }

            var commentId = commentPart.Comments.Elements<Comment>().Count().ToString();
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
                // else
                // {
                //     paragraph.InsertBefore(commentRangeStart, paragraph.FirstChild);
                // }

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
        }
 
        /// <summary>
        /// Cleans the properties of paragraphs within the main part of the document.
        /// This method performs the following actions:
        /// - Removes all BookmarkStart and BookmarkEnd elements.
        /// - Iterates through all paragraphs and processes each one:
        ///   - Clones the paragraph properties and retains only the paragraph style (pStyle).
        ///   - Removes all "rsid" attributes from runs.
        ///   - Removes all run properties except for styles (rStyle), vertical alignment (vertAlign), bold (b), and italic (i).
        ///   - Replaces the old paragraph with the new cleaned paragraph.
        /// </summary>
        public void CleanParagraphProperties()
        {
            OpenXmlElement root = MainPart.Document;

            root.Descendants<BookmarkStart>().ToList().ForEach(b => b.Remove());
            root.Descendants<BookmarkEnd>().ToList().ForEach(b => b.Remove());

            var paragraphs = root.Descendants<Paragraph>().ToList();
            
            foreach (var paragraph in paragraphs)
            {
                Console.WriteLine("[CLEANING]\tPrzetwarzanie paragrafu: " + paragraph.InnerText);
                var newParagraph = new Paragraph();
                var newParagraphId = Guid.NewGuid().ToString("N").Substring(0, 16);
                newParagraph.ParagraphId = newParagraphId;

                var paragraphProperties = paragraph.ParagraphProperties?.CloneNode(true) as ParagraphProperties;
                if (paragraphProperties != null)
                {
                    // Do nowego paragrafu przenieś tylko parametr pStyle
                    var pStyle = paragraphProperties.ParagraphStyleId;
                    paragraphProperties.RemoveAllChildren();
                    if (pStyle != null)
                    {
                        Console.WriteLine("[CLEANING]\tPrzenoszę styl paragrafu: " + pStyle.Val);
                        paragraphProperties.AppendChild(pStyle.CloneNode(true));
                    } else {
                        Console.WriteLine("[CLEANING]\tBrak stylu paragrafu!");
                        var firstRun = paragraph.Descendants<Run>().FirstOrDefault();
                        if (firstRun != null)
                            AddComment(firstRun, "Styl paragrafu nie zdefiniowany!");
                    }
                    newParagraph.ParagraphProperties = paragraphProperties;
                }

                foreach (var run in paragraph.Elements<Run>())
                {
                    // Usuń atrybuty rsid z runów
                    var rsidAttributes = run.GetAttributes().Where(a => a.LocalName.Contains("rsid")).ToList();
                    foreach (var rsidAttribute in rsidAttributes)
                    {
                        Console.WriteLine("[CLEANING]\tUsuwam atrybut: " + rsidAttribute.LocalName);
                        run.RemoveAttribute(rsidAttribute.LocalName, rsidAttribute.NamespaceUri);
                    }

                    // Usuń atrybuty poza stylami
                    var runProperties = run.RunProperties;
                    if (runProperties != null && runProperties.HasChildren)
                    {
                        var childrenToRemove = runProperties.Elements()
                            .Where(e => e.LocalName != "rStyle" && 
                                        e.LocalName != "vertAlign" && 
                                        e.LocalName != "b" && 
                                        e.LocalName != "i")
                            .ToList();
                        foreach (var child in childrenToRemove)
                        {
                            Console.WriteLine("[CLEANING]\tUsuwam element: " + child.OuterXml);
                            child.Remove();
                        }
                        if (!runProperties.HasChildren)
                        {
                            run.RunProperties = null;
                        }
                        // ReplaceFormattingWithStyle(run, runProperties);
                    }
                    newParagraph.AppendChild(run.CloneNode(true));
                }

                // Zamień stary paragraf na nowy
                paragraph.InsertAfterSelf(newParagraph);
                paragraph.Remove();
            }

            void ReplaceFormattingWithStyle(Run run, RunProperties runProperties)
            {
                if (runProperties.Elements<Italic>().Any())
                {
                    var rStyle = new RunStyle { Val = GetStyleID("_K_ - kursywa") };
                    runProperties.AppendChild(rStyle);
                    runProperties.Elements<Bold>().ToList().ForEach(e => e.Remove());
                    AddComment(run, "Zamieniono ręczne formatowanie kursywy na styl");
                } else if (runProperties.Elements<Bold>().Any())
                {
                    var rStyle = new RunStyle { Val = GetStyleID("_P_ - pogrubienie") };
                    runProperties.AppendChild(rStyle);
                    runProperties.Elements<Bold>().ToList().ForEach(e => e.Remove());
                    AddComment(run, "Zamieniono ręczne formatowanie pogrubienia na styl");
                }
                if (runProperties.Elements<VerticalTextAlignment>().Any() )
                {
                    var rStyle = new RunStyle { Val = GetStyleID("_IG_ - indeks górny") };
                    runProperties.AppendChild(rStyle);
                    runProperties.Elements<VerticalTextAlignment>().ToList().ForEach(e => e.Remove());
                    AddComment(run, "Zamieniono ręczne formatowanie indeksu górnego na styl");
                }
                if (runProperties.Elements<Bold>().Any())
                {
                    // AddComment(run, "Ręczne formatowanie pogrubienia");
                }
                if (runProperties.Elements<Italic>().Any())
                {
                    // AddComment(run, "Ręczne formatowanie kursywy");
                }
            }
        }

        public void MergeRuns()
        {
            var paragraphs = MainPart.Document.Descendants<Paragraph>()
                                                .Where(p => p.Elements<Run>().Count() > 1).ToList();

            foreach (var paragraph in paragraphs)
            {
                var runs = paragraph.Elements<Run>().ToList();
                Console.WriteLine("[RUN_MERGE]\tPrzetwarzanie paragrafu: " + paragraph.InnerText);
                Console.WriteLine("[RUN_MERGE]\tLiczba runów: " + runs.Count);
                Run newRun = null;

                foreach (var run in runs)
                {
                    if (run.RunProperties == null)
                    {
                        if (newRun == null)
                        {
                            newRun = new Run();
                        }
                        foreach (var child in run.Elements())
                        {
                            newRun.AppendChild(child.CloneNode(true));
                        }
                    }
                    else
                    {
                        if (newRun != null)
                        {
                            paragraph.AppendChild(newRun);
                            newRun = null;
                        }
                        paragraph.AppendChild(run.CloneNode(true));
                    }
                }

                if (newRun != null)
                {
                    paragraph.AppendChild(newRun);
                }

                // Remove all existing runs
                foreach (var run in runs)
                {
                    run.Remove();
                }
            }
        }

        public void MergeTexts()
        {
            var runs = MainPart.Document.Descendants<Run>().Where(r => r.Elements<Text>().Count() > 1).ToList();

            foreach (var run in runs)
            {
                var newRun = new Run();
                Text? previousText = null;

                foreach (var element in run.Elements())
                {
                    if (element is Text textElement)
                    {
                        if (previousText == null)
                        {
                            previousText = new Text { Space = SpaceProcessingModeValues.Preserve, Text = textElement.Text };
                            newRun.AppendChild(previousText);
                        }
                        else
                        {
                            previousText.Text += textElement.Text;
                        }
                    }
                    else
                    {
                        newRun.AppendChild(element.CloneNode(true));
                        previousText = null;
                    }
                }

                run.InsertAfterSelf(newRun);
                run.Remove();
            }
        }

        // -------------

        private StringValue? GetStyleID(string styleName = "Normalny")
        {
            return MainPart.StyleDefinitionsPart?.Styles?.Descendants<Style>()
                                            .FirstOrDefault(s => s.StyleName?.Val == styleName)?.StyleId;
        }
        
        public void Save()
        {
            _wordDoc.Save();
        }
        
        public void SaveAs(string newFilePath)
        {
            using (var newDoc = (WordprocessingDocument)_wordDoc.Clone(newFilePath))
            {
                newDoc.CompressionOption = CompressionOption.Maximum;
                newDoc.Save();
                //System.IO.Compression.ZipFile.ExtractToDirectory(newFilePath, Path.GetDirectoryName(newFilePath));
            }
        }
       
        public string GenerateXML()
        {
            CustomXmlPart xmlPart = MainPart.AddCustomXmlPart(CustomXmlPartType.CustomXml, "aktPrawny");
            var xmlDoc = new System.Xml.XmlDocument();
            var rootElement = xmlDoc.CreateElement(XMLConstants.Root);

            ProcessElements<Article>(xmlDoc, rootElement, Articles);

            xmlDoc.AppendChild(rootElement);

            using (var stream = xmlPart.GetStream(FileMode.Create, FileAccess.Write))
            {
                xmlDoc.Save(stream);
            }
            var xmlFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "LegalAct.xml");
            xmlDoc.Save(xmlFilePath);
            Console.WriteLine($"XML file saved at: {xmlFilePath}");

            using (var stringWriter = new StringWriter())
            using (var xmlTextWriter = XmlWriter.Create(stringWriter))
            {
                xmlDoc.WriteTo(xmlTextWriter);
                xmlTextWriter.Flush();
                return stringWriter.GetStringBuilder().ToString();
            }
        }

        private void ProcessElements<T>(XmlDocument xmlDoc, XmlElement parentElement, IEnumerable<T> elements)
        {
            var elementName = typeof(T).Name;
            string tagName = XMLConstants.GetTagName(elementName);
            foreach (var element in elements)
            {
                var entity = element as BaseEntity;
                if (entity == null) continue;
                // Console.WriteLine($"[XML]\t[{tagName}]\tPrzetwarzanie elementu: " + entity.Content);
                var xmlElement = xmlDoc.CreateElement(tagName);
                xmlElement.InnerText = entity.Content;
                //xmlElement.SetAttribute("debugLegalRef", entity.LegalReference.ToString());

                switch (typeof(T).Name)
                {
                    case nameof(Article):
                        var article = element as Article;
                        if (article != null)
                        {
                            // Treść artykułu z jednym ustępem w pierwszym ustępie
                            xmlElement.InnerText = string.Empty;
                            xmlElement.SetAttribute(XMLConstants.Number, article.Number);
                            xmlElement.SetAttribute(XMLConstants.Amending, article.IsAmending ? "1" : "0");
                            if (article.IsAmending)
                            {
                                xmlElement.SetAttribute(XMLConstants.PublicationYear, article.PublicationYear);
                                xmlElement.SetAttribute(XMLConstants.PublicationNumber, article.PublicationNumber);
                            }
                            ProcessElements<Subsection>(xmlDoc, xmlElement, article.Subsections);
                        }
                        break;
                    case nameof(Subsection):
                        var subsection = element as Subsection;
                        if (subsection != null)
                            xmlElement.SetAttribute(XMLConstants.Number, subsection.Number.ToString());
                        if (subsection?.Points != null)
                            ProcessElements<Point>(xmlDoc, xmlElement, subsection.Points);
                        break;
                    case nameof(Point):
                        var point = element as Point;
                        if (point != null)
                            xmlElement.SetAttribute(XMLConstants.Number, point.Number);
                        if (point?.Letters != null)
                            ProcessElements<Letter>(xmlDoc, xmlElement, point.Letters);
                        break;
                    case nameof(Letter):
                        var letter = element as Letter;
                        if (letter != null)
                            xmlElement.SetAttribute(XMLConstants.LetterOrdinal, letter.Ordinal);
                        if (letter?.Tirets != null)
                            ProcessElements<Tiret>(xmlDoc, xmlElement, letter.Tirets);
                        break;
                    case nameof(Tiret):
                        var tiret = element as Tiret;
                        if (tiret != null)
                            xmlElement.SetAttribute(XMLConstants.Number, tiret.Number.ToString());
                        break;
                    case nameof(Amendment):
                        // var amendment = element as Amendment;
                        // if (amendment != null)
                        // {
                        //     xmlElement.SetAttribute("ustawaZmieniana", amendment.LegalReference.ToString());
                        // }
                        break;
                    default:
                        Console.WriteLine($"[XML]\t[ERROR]\tNieobsługiwany typ elementu: {typeof(T).Name}");
                        break;
                }
                // Przetwarzanie AmendmentOperations
                if (entity.AmendmentOperations != null && entity.AmendmentOperations.Any())
                {
                    foreach (var amendmentOperation in entity.AmendmentOperations)
                    {
                        var amendmentOperationElement = xmlDoc.CreateElement(XMLConstants.AmendmentOperation);
                        amendmentOperationElement.SetAttribute("typ", amendmentOperation.Type.ToDescription());

                        if (amendmentOperation.AmendmentTarget != null)
                        {
                            amendmentOperationElement.SetAttribute("ustawa", amendmentOperation.AmendmentTarget.PublicationYear + ":" + amendmentOperation.AmendmentTarget.PublicationNumber);
                            if (amendmentOperation.AmendmentTarget.Article != null)
                                amendmentOperationElement.SetAttribute("artykul", amendmentOperation.AmendmentTarget.Article);
                            if (amendmentOperation.AmendmentTarget.Subsection != null)
                                amendmentOperationElement.SetAttribute("ustep", amendmentOperation.AmendmentTarget.Subsection);
                            if (amendmentOperation.AmendmentTarget.Point != null)
                                amendmentOperationElement.SetAttribute("punkt", amendmentOperation.AmendmentTarget.Point);
                            if (amendmentOperation.AmendmentTarget.Letter != null)
                                amendmentOperationElement.SetAttribute("litera", amendmentOperation.AmendmentTarget.Letter);
                            if (amendmentOperation.AmendmentObject != null)
                                amendmentOperationElement.SetAttribute("obiektZmianyId", amendmentOperation.AmendmentObject);
                            amendmentOperationElement.SetAttribute("obiektZmianyTyp", amendmentOperation.AmendmentObjectType.ToFriendlyString());
                        }

                        // Zagnieżdżenie Amendment pod AmendmentOperation
                        //if (entity is IAmendable amendable && amendable.Amendments.Any())
                        {
                            ProcessElements<Amendment>(xmlDoc, amendmentOperationElement, amendmentOperation.Amendments);
                        }

                        xmlElement.AppendChild(amendmentOperationElement);
                    }
                }
                parentElement.AppendChild(xmlElement);
            }
        }

        public void SaveAmendmentList()
        {
            var allAmendments = new List<string>();
            foreach (Article article in Articles)
            {
                if (article.AmendmentList != null && article.AmendmentList.Any())
                {
                    allAmendments.AddRange(article.AmendmentList);
                }
            }
            if (allAmendments.Any())
            {
                allAmendments = allAmendments.Distinct().ToList();
                var amendmentsFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "nowele.txt");
                File.WriteAllLines(amendmentsFilePath, allAmendments);
                Console.WriteLine($"Amendments list saved at: {amendmentsFilePath}");
            }
            else
            {
                Console.WriteLine("No amendments found to save.");
            }
        }
    }    
}   