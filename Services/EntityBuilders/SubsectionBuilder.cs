using DocumentFormat.OpenXml.Wordprocessing;
using Serilog;
using WordParserLibrary.Helpers;
using WordParserLibrary.Model;
using WordParserLibrary.Model.Schemas;

namespace WordParserLibrary.Services.EntityBuilders
{
    /// <summary>
    /// Builder odpowiedzialny za tworzenie SubsectionDto z paragrafu dokumentu.
    /// Wyekstrahowana logika z konstruktora Subsection.
    /// </summary>
    public class SubsectionBuilder
    {
        private readonly LegalReferenceService _legalReferenceService;

        public SubsectionBuilder(LegalReferenceService? legalReferenceService = null)
        {
            _legalReferenceService = legalReferenceService ?? new LegalReferenceService();
        }

        /// <summary>
        /// Buduje SubsectionDto z paragrafu i informacji o artykule nadrzędnym.
        /// </summary>
        public SubsectionDto Build(Paragraph paragraph, ArticleDto parentArticle, DateTime effectiveDate)
        {
            var subsection = new SubsectionDto
            {
                Guid = Guid.NewGuid(),
                EntityType = "UST",
                EffectiveDate = effectiveDate,
                Article = parentArticle,
                Parent = parentArticle,
                LegalReference = new LegalReferenceDto
                {
                    PublicationNumber = parentArticle.LegalReference?.PublicationNumber,
                    PublicationYear = parentArticle.LegalReference?.PublicationYear,
                    Article = parentArticle.LegalReference?.Article,
                },
                Points = new List<PointDto>(),
                Amendments = new List<AmendmentDto>()
            };

            // Parse paragraph content
            subsection.ContentText = paragraph.InnerText.Sanitize().Trim();
            subsection.Number = new EntityNumberDto(subsection.ContentText);

            // TODO: Obsługa błędów parsowania

            Log.Information("Subsection: {Number} - {Content}", 
                subsection.Number?.Value, 
                subsection.ContentText.Substring(0, Math.Min(subsection.ContentText.Length, 100)));

            var currentParagraph = paragraph;
            while (currentParagraph?.NextSibling<Paragraph>() is Paragraph nextParagraph)
            {
                string? styleId = nextParagraph.StyleId();
                if (string.IsNullOrEmpty(styleId))
                {
                    subsection.Error = true;
                    subsection.ErrorMessage = $"Unexpected paragraph style in paragraph: {paragraph.InnerText}";
                    Log.Error(subsection.ErrorMessage);
                    currentParagraph = nextParagraph;
                    continue;
                }

                if (styleId.StartsWith("UST") || styleId.StartsWith("ART"))
                {
                    break;
                }
                else if (styleId.StartsWith("PKT"))
                {
                    var pointBuilder = new PointBuilder(_legalReferenceService);
                    var point = pointBuilder.Build(nextParagraph, subsection, effectiveDate);
                    subsection.Points.Add(point);
                }
                // TODO: Obsługa poprawek (Z)

                currentParagraph = nextParagraph;
            }

            subsection.Context = _legalReferenceService.GetContext(subsection);
            return subsection;
        }
    }
}
