using DocumentFormat.OpenXml.Wordprocessing;
using Serilog;
using WordParserLibrary.Helpers;
using WordParserLibrary.Model;
using WordParserLibrary.Model.Schemas;
using WordParserLibrary.Services;

namespace WordParserLibrary.Services.EntityBuilders
{
    /// <summary>
    /// Builder odpowiedzialny za tworzenie TiretDto z paragrafu dokumentu.
    /// Wyekstrahowana logika z konstruktora Tiret.
    /// </summary>
    public class TiretBuilder
    {
        private readonly LegalReferenceService _legalReferenceService;

        public TiretBuilder(LegalReferenceService? legalReferenceService = null)
        {
            _legalReferenceService = legalReferenceService ?? new LegalReferenceService();
        }

        /// <summary>
        /// Buduje TiretDto z paragrafu i informacji o literze nadrzędnej.
        /// </summary>
        public TiretDto Build(Paragraph paragraph, LetterDto parentLetter, DateTime effectiveDate, int ordinal = 1)
        {
            var entityNumberService = new EntityNumberService();
            var parsedNumber = entityNumberService.Parse(ordinal.ToString());

            var tiret = new TiretDto
            {
                Guid = Guid.NewGuid(),
                UnitType = UnitType.Tiret,
                EffectiveDate = effectiveDate,
                Letter = parentLetter,
                Point = parentLetter.Point,
                Paragraph = parentLetter.Paragraph,
                Article = parentLetter.Article,
                Parent = parentLetter,
                Number = parsedNumber,
                LegalReference = new LegalReferenceDto
                {
                    PublicationNumber = parentLetter.LegalReference?.PublicationNumber,
                    PublicationYear = parentLetter.LegalReference?.PublicationYear,
                    Article = parentLetter.LegalReference?.Article,
                    Subsection = parentLetter.LegalReference?.Subsection,
                    Point = parentLetter.LegalReference?.Point,
                    Letter = parentLetter.LegalReference?.Letter,
                },
                Amendments = new()
            };

            // TODO: Parse paragraph content - użyć nowego podejścia do parsowania
            tiret.ContentText = paragraph.InnerText.Sanitize().Trim();

            // TODO: Obsługa błędów parsowania

            Log.Information("Tiret: {Number} - {Content}", 
                tiret.Number?.Value, 
                tiret.ContentText.Substring(0, Math.Min(tiret.ContentText.Length, 100)));

            var currentParagraph = paragraph;
            while (currentParagraph?.NextSibling() is Paragraph nextParagraph
                    && nextParagraph.StyleId("TIR") != true
                    && nextParagraph.StyleId("LIT") != true
                    && nextParagraph.StyleId("PKT") != true
                    && nextParagraph.StyleId("UST") != true
                    && nextParagraph.StyleId("ART") != true)
            {
                // TODO: Obsługa poprawek (Z)
                currentParagraph = nextParagraph;
            }

            tiret.Context = _legalReferenceService.GetContext(tiret);
            return tiret;
        }
    }
}
