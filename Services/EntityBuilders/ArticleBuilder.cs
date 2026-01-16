using DocumentFormat.OpenXml.Wordprocessing;
using Serilog;
using WordParserLibrary.Helpers;
using WordParserLibrary.Model;
using WordParserLibrary.Model.Schemas;

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
                Subsections = new List<SubsectionDto>(),
                Journals = new List<JournalInfoDto>()
            };

            // Parse paragraph to extract article number and initial content
            article.Number = new EntityNumberDto(paragraph.InnerText.Sanitize().Trim());
            article.ContentText = paragraph.InnerText.Sanitize().Trim();

            Log.Information("Article: {Number} - {Content}", 
                article.Number?.Value, 
                article.ContentText.Substring(0, Math.Min(article.ContentText.Length, 50)));

            // Każdy artykuł zawiera co najmniej jeden ustęp
            article.ContentText = string.Empty;
            var firstSubsection = new SubsectionBuilder(_legalReferenceService)
                .Build(paragraph, article, effectiveDate);
            article.Subsections.Add(firstSubsection);

            // Szukaj kolejnych ustępów
            var currentParagraph = paragraph;
            while (currentParagraph?.NextSibling<Paragraph>() is Paragraph nextParagraph
                    && nextParagraph.StyleId("ART") != true)
            {
                if (nextParagraph.StyleId("UST") == true)
                {
                    var subsection = new SubsectionBuilder(_legalReferenceService)
                        .Build(nextParagraph, article, effectiveDate);
                    article.Subsections.Add(subsection);
                }
                currentParagraph = nextParagraph;
            }

            return article;
        }
    }
}
