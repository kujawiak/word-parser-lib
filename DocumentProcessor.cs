using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;

namespace WordParserLibrary
{
    public class DocumentProcessor
    {
        private readonly LegalAct _legalAct;
        public MainDocumentPart MainPart { get; }

        public DocumentProcessor(LegalAct legalAct)
        {
            _legalAct = legalAct;
            MainPart = legalAct.MainPart;
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
                            _legalAct.CommentManager.AddComment(firstRun, "Styl paragrafu nie zdefiniowany!");
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
                Run newRun = null!;

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
                            newRun = null!;
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

        public int ParseHyperlinks()
        {
            int commentCount = 0;

            // Parsowanie paragrafów w treści dokumentu
            foreach (var paragraph in _legalAct.MainPart.Document.Descendants<Paragraph>())
            {
                commentCount += _legalAct.CommentManager.AddCommentsToHyperlinks(paragraph);
            }

            // Parsowanie przypisów dolnych
            if (MainPart.FootnotesPart != null)
            {
                foreach (var footnote in MainPart.FootnotesPart.Footnotes.Elements<Footnote>())
                {
                    foreach (var paragraph in footnote.Descendants<Paragraph>())
                    {
                        commentCount += _legalAct.CommentManager.AddCommentsToHyperlinks(paragraph);
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
                        commentCount += _legalAct.CommentManager.AddCommentsToHyperlinks(paragraph);
                    }
                }
            }

            return commentCount;
        }

        public void Validate()
        {
            CleanParagraphProperties();
            MergeRuns();
            MergeTexts();
        }
    }
}