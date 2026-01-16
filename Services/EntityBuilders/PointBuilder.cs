using DocumentFormat.OpenXml.Wordprocessing;
using Serilog;
using WordParserLibrary.Helpers;
using WordParserLibrary.Model;
using WordParserLibrary.Model.Schemas;

namespace WordParserLibrary.Services.EntityBuilders
{
    /// <summary>
    /// Builder odpowiedzialny za tworzenie PointDto z paragrafu dokumentu.
    /// Wyekstrahowana logika z konstruktora Point.
    /// </summary>
    public class PointBuilder
    {
        private readonly LegalReferenceService _legalReferenceService;

        public PointBuilder(LegalReferenceService? legalReferenceService = null)
        {
            _legalReferenceService = legalReferenceService ?? new LegalReferenceService();
        }

        /// <summary>
        /// Buduje PointDto z paragrafu i informacji o ustępie nadrzędnym.
        /// </summary>
        public PointDto Build(Paragraph paragraph, SubsectionDto parentSubsection, DateTime effectiveDate)
        {
            var point = new PointDto
            {
                Guid = Guid.NewGuid(),
                EntityType = "PKT",
                EffectiveDate = effectiveDate,
                Subsection = parentSubsection,
                Article = parentSubsection.Article,
                Parent = parentSubsection,
                LegalReference = new LegalReferenceDto
                {
                    PublicationNumber = parentSubsection.LegalReference?.PublicationNumber,
                    PublicationYear = parentSubsection.LegalReference?.PublicationYear,
                    Article = parentSubsection.LegalReference?.Article,
                    Subsection = parentSubsection.LegalReference?.Subsection,
                },
                Letters = new(),
                Amendments = new()
            };

            // TODO: Parse paragraph content - użyć nowego podejścia do parsowania
            point.ContentText = paragraph.InnerText.Sanitize().Trim();
            point.Number = new EntityNumberDto(point.ContentText);

            // TODO: Obsługa błędów parsowania

            Log.Information("Point: {Number} - {Content}", 
                point.Number?.Value, 
                point.ContentText.Substring(0, Math.Min(point.ContentText.Length, 100)));

            bool isAdjacent = true;
            var currentParagraph = paragraph;
            while (currentParagraph?.NextSibling<Paragraph>() is Paragraph nextParagraph)
            {
                string? styleId = nextParagraph.StyleId();
                if (string.IsNullOrEmpty(styleId))
                {
                    point.Error = true;
                    point.ErrorMessage = $"Unexpected paragraph style in paragraph: {paragraph.InnerText}";
                    Log.Error(point.ErrorMessage);
                    currentParagraph = nextParagraph;
                    continue;
                }

                if (styleId.StartsWith("PKT") || styleId.StartsWith("UST") || styleId.StartsWith("ART"))
                {
                    break;
                }
                else if (styleId.StartsWith("LIT"))
                {
                    var letterBuilder = new LetterBuilder(_legalReferenceService);
                    var letter = letterBuilder.Build(nextParagraph, point, effectiveDate);
                    point.Letters.Add(letter);
                }
                // TODO: Obsługa poprawek (Z)

                currentParagraph = nextParagraph;
            }

            point.Context = _legalReferenceService.GetContext(point);
            return point;
        }
    }
}
