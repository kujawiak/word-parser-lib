using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Validation;
using DocumentFormat.OpenXml.Wordprocessing;
using Serilog;
using System.IO;

namespace WordParserLibrary
{
    public class DocxGenerator
    {
        private readonly LegalAct legalAct;
        private CommentManager commentManager;

        public DocxGenerator(LegalAct legalAct)
        {
            this.legalAct = legalAct;
        }

        public void Generate()
        {
            // var templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "template_0.dotm");
            var templatePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "Templates", "Szablon aktu prawnego 4_0.dotm");

            // var outputPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "generated.docx");
            var outputPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "generated.docx");

            if (!File.Exists(templatePath))
            {
                throw new FileNotFoundException($"Template file not found at: {templatePath}");
            }
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }

            File.Copy(templatePath, outputPath);

            //using (var document = WordprocessingDocument.CreateFromTemplate(templatePath, false))
            using (var document = WordprocessingDocument.Open(outputPath, true))
            {
                if (document.MainDocumentPart != null)
                {
                    // Remove the VBA project part (macros)
                    var vbaPart = document.MainDocumentPart.VbaProjectPart;
                    if (vbaPart != null)
                    {
                        document.MainDocumentPart.DeletePart(vbaPart);
                    }

                    // Change the document type to Document (from Macro-Enabled Template)
                    document.ChangeDocumentType(DocumentFormat.OpenXml.WordprocessingDocumentType.Document);
                }
                
                // document.Clone(outputPath);

                commentManager = new CommentManager(document.MainDocumentPart);

                if (document.MainDocumentPart.WordprocessingCommentsPart == null)
                {
                    var commentPart = document.MainDocumentPart.AddNewPart<WordprocessingCommentsPart>();
                    commentPart.Comments = new Comments();
                    commentPart.Comments.Save();
                }

                ValidateDocx(document);
                if (document.MainDocumentPart == null)
                {
                    throw new InvalidOperationException("MainDocumentPart is null in the template.");
                }
                var stylesPart = document.MainDocumentPart.StyleDefinitionsPart;
                if (stylesPart == null)
                {
                    throw new InvalidOperationException("StyleDefinitionsPart is null in the template.");
                }
                var styles = stylesPart.Styles != null
                    ? stylesPart.Styles.Elements<Style>()
                        .Select(s => s.StyleId)
                        .Where(id => id != null)
                        .Select(id => new StringValue(id.Value))
                        .ToList()
                    : new List<StringValue>();

                var mainPart = document.MainDocumentPart;
                if (mainPart == null)
                {
                    throw new InvalidOperationException("MainDocumentPart is null in the template.");
                }
                var body = mainPart.Document.Body;
                if (body == null)
                {
                    throw new InvalidOperationException("Body is null in the template.");
                }

                // Wyczyść istniejącą zawartość dokumentu
                body.RemoveAllChildren();

                // Dodaj artykuły do dokumentu
                // foreach (var article in legalAct.Articles)
                // {
                //     AddArticle(article, body, styles);
                // }
                document.Save();
            }
            
            Console.WriteLine($"Document generated and saved at: {outputPath}");
        }

        
        public void ValidateDocx(WordprocessingDocument document)
        {
            OpenXmlValidator validator = new OpenXmlValidator();
            var errors = validator.Validate(document);

            foreach (var error in errors)
            {
                Console.WriteLine($"Błąd: {error.Description}");
                Console.WriteLine($"Część: {error.Part.Uri}");
                Console.WriteLine($"Ścieżka: {error.Path.XPath}");
                Console.WriteLine($"Typ błędu: {error.ErrorType}");
                Console.WriteLine();
            }

            if (!errors.Any())
            {
                Console.WriteLine("Dokument jest poprawny.");
            }
        }
        
        // private void AddArticle(Article article, Body body, List<StringValue> styles)
        // {
        //     // var style = styles.FirstOrDefault(s => s.Value != null && s.Value.StartsWith("ART"));
        //     Paragraph p = article.ToParagraph();
        //     body.AppendChild(p);
        //     if (article.Subsections.First().Amendments.Any())
        //     {
        //         AddAmendments(article.Subsections.First().Amendments, body, styles);
        //     }
        //     if (article.Subsections.First().Points.Any())
        //     {
        //         foreach (var point in article.Subsections.First().Points)
        //         {
        //             AddPoint(point, body, styles);
        //         }
        //     }
        //     if (article.Subsections.Count > 1)
        //     {
        //         foreach (var subsection in article.Subsections)
        //         {
        //             if (article.Subsections.IndexOf(subsection) != 0)
        //             {
        //                 AddSubsection(subsection, body, styles);
        //             }
        //         }
        //     }
        // }

        // private void AddAmendments(List<Amendment> amendments, Body body, List<StringValue> styles)
        // {
        //     foreach (var amendment in amendments)
        //     {
        //         var p = new Paragraph();
                
        //         var style = styles.FirstOrDefault(s => s.Value != null && s.Value.StartsWith(amendment.StyleValue));
        //         Log.Debug("Znaleziono styl: {Style}", style);
        //         if (style == null)
        //         {
        //             Log.Warning("Nie znaleziono stylu dla poprawki: {Amendment}", amendment);
        //             continue;
        //         }
        //         p.ParagraphProperties = new ParagraphProperties(new ParagraphStyleId { Val = style });
        //         // p.ParagraphProperties.Shading = new Shading
        //         // {
        //         //     Val = ShadingPatternValues.Clear,
        //         //     Color = "auto",
        //         //     Fill = "D9EAF7" // Light blue background color
        //         // };
        //         p.Append(new Run(new Text(amendment.ContentText)));
        //         if (amendment.Operation != null)
        //         {
        //             commentManager.AddComment(p, amendment.Operation.ToString());
        //         }
        //         else
        //         {
        //             commentManager.AddComment(p, "BŁAD: Nie znaleziono operacji; " + amendment.LegalReference.ToString());
        //         }
                
        //         body.AppendChild(p);
        //     }
        // }

        // private void AddSubsection(Subsection subsection, Body body, List<StringValue> styles)
        // {
        //     // var style = styles.FirstOrDefault(s => s.Value != null && s.Value.StartsWith("UST"));
        //     var p = subsection.ToParagraph();
        //     body.AppendChild(p);
        //     if (subsection.Amendments.Any())
        //     {
        //         AddAmendments(subsection.Amendments, body, styles);
        //     }
        //     foreach (var point in subsection.Points)
        //     {
        //         AddPoint(point, body, styles);
        //     }
        // }
        
        // private void AddPoint(Point point, Body body, List<StringValue> styles)
        // {
        //     // var style = styles.FirstOrDefault(s => s.Value != null && s.Value.StartsWith("PKT"));
        //     var p = point.ToParagraph();
        //     body.AppendChild(p);
        //     if (point.Amendments.Any())
        //     {
        //         AddAmendments(point.Amendments, body, styles);
        //     }
        //     foreach (var letter in point.Letters)
        //     {
        //         AddLetter(letter, body, styles);
        //     }
        // }

        // private void AddLetter(Letter letter, Body body, List<StringValue> styles)
        // {
        //     // var style = styles.FirstOrDefault(s => s.Value != null && s.Value.StartsWith("LIT"));
        //     var p = letter.ToParagraph();
        //     body.AppendChild(p);
        //     if (letter.Amendments.Any())
        //     {
        //         AddAmendments(letter.Amendments, body, styles);
        //     }
        //     foreach (var tiret in letter.Tirets)
        //     {
        //         AddTiret(tiret, body);
        //     }
        // }

        // private void AddTiret(Tiret tiret, Body body)
        // {
        //     var tiretParagraph = new Paragraph(new Run(new Text(tiret.ContentText)));
        //     // tiretParagraph.ParagraphProperties = new ParagraphProperties(new ParagraphStyleId { Val = "TiretStyle" });
        //     body.AppendChild(tiretParagraph);
        // }
    }
}