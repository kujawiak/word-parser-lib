using DocumentFormat.OpenXml.Wordprocessing;
using Serilog;
using WordParserLibrary.Helpers;
using WordParserLibrary.Model;
using WordParserLibrary.Model.Schemas;
using WordParserLibrary.Services;

namespace WordParserLibrary.Services.EntityBuilders
{
    /// <summary>
    /// Builder odpowiedzialny za tworzenie ArticleDto z paragrafu dokumentu.
    /// Wyekstrahowana logika z konstruktora Article.
    /// </summary>
    public class ArticleBuilder
    {
        private readonly LegalReferenceService _legalReferenceService;

        public ArticleBuilder(LegalReferenceService? legalReferenceService = null)
        {
            _legalReferenceService = legalReferenceService ?? new LegalReferenceService();
        }

        /// <summary>
        /// Buduje ArticleDto z paragrafu i informacji o LegalAct.
        /// </summary>
        public ArticleDto Build(Paragraph paragraph, DateTime effectiveDate)
        {
            var article = new ArticleDto
            {
                Guid = Guid.NewGuid(),
                EntityType = "ART",
                EffectiveDate = effectiveDate,
                LegalReference = new LegalReferenceDto(),
                Paragraphs = new List<ParagraphDto>(),
                Journals = new List<JournalInfoDto>()
            };

            // Parse paragraph to extract article number and initial content
            var entityNumberService = new EntityNumberService();
            var parsedNumber = paragraph.InnerText.Sanitize().Trim();
            article.Number = entityNumberService.Parse(parsedNumber);
            article.NumberDto = article.Number;
            article.ContentText = paragraph.InnerText.Sanitize().Trim();

            Log.Information("Article: {Number} - {Content}", 
                article.Number?.Value, 
                article.ContentText.Substring(0, Math.Min(article.ContentText.Length, 50)));

            // Każdy artykuł zawiera co najmniej jeden ustęp (Paragraph)
            article.ContentText = string.Empty;
            var firstParagraph = new ParagraphBuilder(_legalReferenceService)
                .Build(paragraph, article, effectiveDate);
            article.Paragraphs.Add(firstParagraph);

            // Szukaj kolejnych ustępów
            var currentParagraph = paragraph;
            while (currentParagraph?.NextSibling<Paragraph>() is Paragraph nextParagraph
                    && nextParagraph.StyleId("ART") != true)
            {
                if (nextParagraph.StyleId("UST") == true)
                {
                    var paragraphDto = new ParagraphBuilder(_legalReferenceService)
                        .Build(nextParagraph, article, effectiveDate);
                    article.Paragraphs.Add(paragraphDto);
                }
                currentParagraph = nextParagraph;
            }

            return article;
        }
    }
}
