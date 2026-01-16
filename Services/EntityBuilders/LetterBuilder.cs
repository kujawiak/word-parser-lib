using DocumentFormat.OpenXml.Wordprocessing;
using Serilog;
using WordParserLibrary.Helpers;
using WordParserLibrary.Model;
using WordParserLibrary.Model.Schemas;

namespace WordParserLibrary.Services.EntityBuilders
{
    /// <summary>
    /// Builder odpowiedzialny za tworzenie LetterDto z paragrafu dokumentu.
    /// Wyekstrahowana logika z konstruktora Letter.
    /// </summary>
    public class LetterBuilder
    {
        private readonly LegalReferenceService _legalReferenceService;

        public LetterBuilder(LegalReferenceService? legalReferenceService = null)
        {
            _legalReferenceService = legalReferenceService ?? new LegalReferenceService();
        }

        /// <summary>
        /// Buduje LetterDto z paragrafu i informacji o punkcie nadrzędnym.
        /// </summary>
        public LetterDto Build(Paragraph paragraph, PointDto parentPoint, DateTime effectiveDate)
        {
            var letter = new LetterDto
            {
                Guid = Guid.NewGuid(),
                EntityType = "LIT",
                EffectiveDate = effectiveDate,
                Point = parentPoint,
                Subsection = parentPoint.Subsection,
                Article = parentPoint.Article,
                Parent = parentPoint,
                LegalReference = new LegalReferenceDto
                {
                    PublicationNumber = parentPoint.LegalReference?.PublicationNumber,
                    PublicationYear = parentPoint.LegalReference?.PublicationYear,
                    Article = parentPoint.LegalReference?.Article,
                    Subsection = parentPoint.LegalReference?.Subsection,
                    Point = parentPoint.LegalReference?.Point,
                },
                Tirets = new(),
                Amendments = new()
            };

            // TODO: Parse paragraph content - użyć nowego podejścia do parsowania
            letter.ContentText = paragraph.InnerText.Sanitize().Trim();
            letter.Ordinal = letter.ContentText;

            // TODO: Obsługa błędów parsowania

            Log.Information("Letter: {Ordinal} - {Content}", 
                letter.Ordinal, 
                letter.ContentText.Substring(0, Math.Min(letter.ContentText.Length, 100)));

            var currentParagraph = paragraph;
            int tiretNumber = 1;
            while (currentParagraph?.NextSibling<Paragraph>() is Paragraph nextParagraph)
            {
                string? styleId = nextParagraph.StyleId();
                if (string.IsNullOrEmpty(styleId))
                {
                    letter.Error = true;
                    letter.ErrorMessage = $"Unexpected paragraph style in paragraph: {paragraph.InnerText}";
                    Log.Error(letter.ErrorMessage);
                    currentParagraph = nextParagraph;
                    continue;
                }

                if (styleId.StartsWith("LIT") || styleId.StartsWith("PKT") || styleId.StartsWith("UST") || styleId.StartsWith("ART"))
                {
                    break;
                }
                else if (styleId.StartsWith("TIR"))
                {
                    var tiretBuilder = new TiretBuilder(_legalReferenceService);
                    var tiret = tiretBuilder.Build(nextParagraph, letter, effectiveDate, tiretNumber++);
                    letter.Tirets.Add(tiret);
                }
                // TODO: Obsługa poprawek (Z)

                currentParagraph = nextParagraph;
            }

            letter.Context = _legalReferenceService.GetContext(letter);
            return letter;
        }
    }
}
