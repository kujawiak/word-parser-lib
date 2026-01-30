using DocumentFormat.OpenXml.Wordprocessing;
using Serilog;
using WordParserLibrary.Helpers;
using WordParserLibrary.Model;
using WordParserLibrary.Model.Schemas;
using WordParserLibrary.Services;

namespace WordParserLibrary.Services.EntityBuilders
{
    /// <summary>
    /// Builder odpowiedzialny za tworzenie ParagraphDto z paragrafu dokumentu.
    /// Wyekstrahowana logika z konstruktora Paragraph.
    /// </summary>
    public class ParagraphBuilder
    {
        private readonly LegalReferenceService _legalReferenceService;

        public ParagraphBuilder(LegalReferenceService? legalReferenceService = null)
        {
            _legalReferenceService = legalReferenceService ?? new LegalReferenceService();
        }

        /// <summary>
        /// Buduje ParagraphDto z paragrafu i informacji o artykule nadrzędnym.
        /// </summary>
        public ParagraphDto Build(Paragraph paragraph, ArticleDto parentArticle, DateTime effectiveDate)
        {
            var paragraphDto = new ParagraphDto
            {
                Guid = Guid.NewGuid(),
                UnitType = UnitType.Paragraph,
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
            paragraphDto.ContentText = paragraph.InnerText.Sanitize().Trim();
            var entityNumberService = new EntityNumberService();
            paragraphDto.Number = entityNumberService.Parse(paragraphDto.ContentText);

            // TODO: Obsługa błędów parsowania

            Log.Information("Paragraph: {Number} - {Content}", 
                paragraphDto.Number?.Value, 
                paragraphDto.ContentText.Substring(0, Math.Min(paragraphDto.ContentText.Length, 100)));

            var currentParagraph = paragraph;
            while (currentParagraph?.NextSibling<Paragraph>() is Paragraph nextParagraph)
            {
                string? styleId = nextParagraph.StyleId();
                if (string.IsNullOrEmpty(styleId))
                {
                    paragraphDto.Error = true;
                    paragraphDto.ErrorMessage = $"Unexpected paragraph style in paragraph: {paragraph.InnerText}";
                    Log.Error(paragraphDto.ErrorMessage);
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
                    var point = pointBuilder.Build(nextParagraph, paragraphDto, effectiveDate);
                    paragraphDto.Points.Add(point);
                }
                // TODO: Obsługa poprawek (Z)

                currentParagraph = nextParagraph;
            }

            paragraphDto.Context = _legalReferenceService.GetContext(paragraphDto);
            return paragraphDto;
        }
    }
}
