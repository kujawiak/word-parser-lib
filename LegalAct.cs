using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.IO.Packaging;
using System.Xml;
using WordParserLibrary.Model;
using Serilog;

namespace WordParserLibrary
{
    public class LegalAct
    {
        public WordprocessingDocument WordDocument  { get; }
        public MainDocumentPart MainPart { get; }
        public XmlDocument XmlDocument { get; set; }
        public DocumentProcessor DocumentProcessor { get; }
        public XmlGenerator XmlGenerator { get; }
        public CommentManager CommentManager { get; }
        public DocxGenerator DocxGenerator { get; set; }

        public Title Title { get; set; }
        public List<Article> Articles { get; set; } = new List<Article>();

        public LegalAct(WordprocessingDocument wordDoc)
        {
            LoggerConfig.ConfigureLogger();
            Log.Information("[LegalAct.Constructor]\tTworzenie instancji LegalAct");
            
            WordDocument = wordDoc;
            MainPart = WordDocument.MainDocumentPart ?? throw new InvalidOperationException("MainDocumentPart is null.");
            XmlDocument = new XmlDocument();

            DocumentProcessor = new DocumentProcessor(this);
            XmlGenerator = new XmlGenerator(this);
            CommentManager = new CommentManager(MainPart);
            DocxGenerator = new DocxGenerator(this);

            Title = new Title(MainPart.Document.Descendants<Paragraph>()
                                        .Where(p => p.ParagraphProperties != null 
                                                && p.StyleId("TYTUAKT") == true).FirstOrDefault() ?? throw new InvalidOperationException("Title paragraph not found"));                                        // Tytuł #1
            // Title.Parts.Add(new Part());                                // Dział #1
            // Title.Parts[0].Chapters.Add(new Chapter());                 // Rozdział #1
            // Title.Parts[0].Chapters[0].Sections.Add(new Section());     // Oddział #1

            foreach (var paragraph in MainPart.Document.Descendants<Paragraph>()
                                            .Where(p => p.InnerText.StartsWith("Art.") || p.InnerText.StartsWith("§"))
                                            .ToList())
            {
                if (paragraph.ParagraphProperties == null)
                {
                    Console.WriteLine("[CTOR]\tBrak właściwości paragrafu!");
                    CommentManager.AddComment(paragraph, "Brak właściwości paragrafu!");
                    continue;
                }
                if (paragraph.ParagraphProperties.ParagraphStyleId == null)
                {
                    Console.WriteLine("[CTOR]\tBrak stylu paragrafu!");
                    CommentManager.AddComment(paragraph, "Brak stylu paragrafu!");
                    continue;
                }
                
                var paragraphStyle = paragraph.ParagraphProperties?.ParagraphStyleId?.Val;

                if (paragraphStyle != null && paragraphStyle?.ToString()?.StartsWith("ART") == true)
                {
                    Articles.Add(new Article(paragraph));
                }
            }
        }
    
        public void Validate()
        {
            DocumentProcessor.CleanParagraphProperties();
            DocumentProcessor.MergeRuns();
            DocumentProcessor.MergeTexts();
        }

        public MemoryStream GetStream(List<string> stringList)
        {
            var memoryStream = new MemoryStream();
            if (stringList != null && stringList.Any())
            {
                if (stringList.Contains("REMOVE_COMMENTS"))
                {
                    CommentManager.RemoveSystemComments();
                }
                if (stringList.Contains("CLEANING"))
                {
                    DocumentProcessor.CleanParagraphProperties();
                }
                if (stringList.Contains("RUN_MERGE"))
                {
                    DocumentProcessor.MergeRuns();
                }
                if (stringList.Contains("TEXT_MERGE"))
                {
                    DocumentProcessor.MergeTexts();
                }
                if (stringList.Contains("VALIDATE"))
                {
                    Validate();
                }
                if (stringList.Contains("HYPERLINKS"))
                {
                    DocumentProcessor.ParseHyperlinks();
                }
                if (stringList.Contains("AMENDMENTS"))
                {
                    SaveAmendmentList();
                }
                if (stringList.Contains("XML"))
                {
                    XmlGenerator.Generate(true);
                }
            }
            WordDocument.Clone(memoryStream);
            return memoryStream;
        }

        private StringValue? GetStyleID(string styleName = "Normalny")
        {
            return MainPart.StyleDefinitionsPart?.Styles?.Descendants<Style>()
                                            .FirstOrDefault(s => s.StyleName?.Val == styleName)?.StyleId;
        }

        public void Save()
        {
            WordDocument.Save();
        }

        public void SaveAs(string newFilePath)
        {
            using (var newDoc = (WordprocessingDocument)WordDocument.Clone(newFilePath))
            {
                newDoc.CompressionOption = CompressionOption.Maximum;
                newDoc.Save();
                //System.IO.Compression.ZipFile.ExtractToDirectory(newFilePath, Path.GetDirectoryName(newFilePath));
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