using DocumentFormat.OpenXml.Wordprocessing;
using Serilog;
using WordParserLibrary.Helpers;
using ModelDto;
using WordParserLibrary.Services;

namespace WordParserLibrary.Services.EntityBuilders
{
    /// <summary>
    /// Builder odpowiedzialny za tworzenie PointDto z paragrafu dokumentu.
    /// Wyekstrahowana logika z konstruktora Point.
    /// </summary>
    public class PointBuilder
    {
        // private readonly LegalReferenceService _legalReferenceService;

        // public PointBuilder(LegalReferenceService? legalReferenceService = null)
        // {
        //     _legalReferenceService = legalReferenceService ?? new LegalReferenceService();
        // }

        // /// <summary>
        // /// Buduje PointDto z paragrafu i informacji o ustępie nadrzędnym.
        // /// </summary>
        // public PointDto Build(Paragraph paragraph, ParagraphDto parentParagraph, DateTime effectiveDate)
        // {
        //     var point = new PointDto
        //     {
        //         Guid = Guid.NewGuid(),
        //         UnitType = UnitType.Point,
        //         EffectiveDate = effectiveDate,
        //         Paragraph = parentParagraph,
        //         Article = parentParagraph.Article,
        //         Parent = parentParagraph,
        //         LegalReference = new LegalReferenceDto
        //         {
        //             PublicationNumber = parentParagraph.LegalReference?.PublicationNumber,
        //             PublicationYear = parentParagraph.LegalReference?.PublicationYear,
        //             Article = parentParagraph.LegalReference?.Article,
        //             Subsection = parentParagraph.LegalReference?.Subsection,
        //         },
        //         Letters = new(),
        //         Amendments = new()
        //     };

        //     // TODO: Parse paragraph content - użyć nowego podejścia do parsowania
        //     point.ContentText = paragraph.InnerText.Sanitize().Trim();
        //     var entityNumberService = new EntityNumberService();
        //     point.Number = entityNumberService.Parse(point.ContentText);

        //     // TODO: Obsługa błędów parsowania

        //     Log.Information("Point: {Number} - {Content}", 
        //         point.Number?.Value, 
        //         point.ContentText.Substring(0, Math.Min(point.ContentText.Length, 100)));

        //     bool isAdjacent = true;
        //     var currentParagraph = paragraph;
        //     while (currentParagraph?.NextSibling<Paragraph>() is Paragraph nextParagraph)
        //     {
        //         string? styleId = nextParagraph.StyleId();
        //         if (string.IsNullOrEmpty(styleId))
        //         {
        //             var message = $"Unexpected paragraph style in paragraph: {paragraph.InnerText}";
        //             point.ValidationMessages.Add(new ValidationMessage(ValidationLevel.Error, message));
        //             Log.Error(message);
        //             currentParagraph = nextParagraph;
        //             continue;
        //         }

        //         if (styleId.StartsWith("PKT") || styleId.StartsWith("UST") || styleId.StartsWith("ART"))
        //         {
        //             break;
        //         }
        //         else if (styleId.StartsWith("LIT"))
        //         {
        //             var letterBuilder = new LetterBuilder(_legalReferenceService);
        //             var letter = letterBuilder.Build(nextParagraph, point, effectiveDate);
        //             point.Letters.Add(letter);
        //         }
        //         // TODO: Obsługa poprawek (Z)

        //         currentParagraph = nextParagraph;
        //     }

        //     point.Context = _legalReferenceService.GetContext(point);
        //     return point;
        // }
    }
}
