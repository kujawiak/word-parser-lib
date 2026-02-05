using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using Word = DocumentFormat.OpenXml.Wordprocessing;
using System.IO.Packaging;
using System.Xml;
using Serilog;
using Dto = ModelDto.EditorialUnits;

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
        public XlsxGenerator XlsxGenerator { get; set; }
        public object? Title { get; set; }
        public DateTime EffectiveDate { get; set; }

        public List<Dto.Article> Articles { get; set; } = new List<Dto.Article>();

        public LegalAct(WordprocessingDocument wordDocument)
        {
            LoggerConfig.ConfigureLogger();
            Log.Information("[LegalAct.Constructor]\tTworzenie instancji LegalAct");

            WordDocument = wordDocument;
            MainPart = WordDocument.MainDocumentPart ?? throw new InvalidOperationException("MainDocumentPart is null.");
            XmlDocument = new XmlDocument();

            DocumentProcessor = new DocumentProcessor(this);
            XmlGenerator = new XmlGenerator(this);
            CommentManager = new CommentManager(MainPart);
            DocxGenerator = new DocxGenerator(this);
            XlsxGenerator = new XlsxGenerator(this);

            // TODO: Restore Title initialization when Title class is refactored
            // Title = new Title(MainPart.Document.Descendants<Paragraph>()
            //                             .Where(p => p.ParagraphProperties != null
            //                                     && p.StyleId("TYTUAKT") == true).FirstOrDefault() ?? throw new InvalidOperationException("Title paragraph not found"));
            Title = null;

            var dateParagraph = MainPart.Document.Descendants<Word.Paragraph>()
                                        .Where(p => p.ParagraphProperties != null && p.StyleId("DATAAKTU") == true)
                                        .FirstOrDefault() ?? throw new InvalidOperationException("Effective date paragraph not found");

            EffectiveDate = dateParagraph.InnerText.ExtractDate();
            Log.Information("[LegalAct.Constructor]\tData wejścia w życie: {EffectiveDate}", EffectiveDate);

            foreach (var paragraph in MainPart.Document.Descendants<Word.Paragraph>()
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

                if (paragraph.StyleId("ART") == true)
                {
                    // Articles.Add(new Article(paragraph, this));
                }
            }

            // SaveAmendmentList();
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
                    DocumentProcessor.CleanParagraphProperties();
                    DocumentProcessor.MergeRuns();
                    DocumentProcessor.MergeTexts();
                    DocumentProcessor.Validate();   
                }
                if (stringList.Contains("HYPERLINKS"))
                {
                    DocumentProcessor.ParseHyperlinks();
                }
                if (stringList.Contains("AMENDMENTS"))
                {
                    // SaveAmendmentList();
                }
                if (stringList.Contains("XML"))
                {
                    XmlGenerator.Generate(true);
                }
            }
            WordDocument.Clone(memoryStream);
            return memoryStream;
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

        // public void SaveAmendmentList()
        // {
        //     var allAmendments = new List<string>();
        //     foreach (Article article in Articles)
        //     {
        //         if (article.AllAmendments != null && article.AllAmendments.Any())
        //         {
        //             allAmendments.AddRange(article.AllAmendments);
        //         }
        //     }
        //     if (allAmendments.Any())
        //     {
        //         allAmendments = allAmendments.Distinct().ToList();
        //         var amendmentsFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "nowele.txt");
        //         File.WriteAllLines(amendmentsFilePath, allAmendments);
        //         Console.WriteLine($"Amendments list saved at: {amendmentsFilePath}");
        //     }
        //     else
        //     {
        //         Console.WriteLine("No amendments found to save.");
        //     }
        // }
    }    
}   