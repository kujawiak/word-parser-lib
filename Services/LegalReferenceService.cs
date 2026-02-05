using System.Text;
using System.Text.RegularExpressions;
using ModelDto;

namespace WordParserLibrary.Services
{
    /// <summary>
    /// Serwis do zarządzania odniesieniami do aktów prawnych.
    /// Odpowiada za aktualizację referencji na podstawie treści encji.
    /// </summary>
    public class LegalReferenceService
    {
        /// <summary>
        /// Aktualizuje odniesienia w obiekcie LegalReferenceDto na podstawie treści encji.
        /// Szuka wzorców takich jak "art. 5", "ust. 2", "pkt 3a", "lit. b", "tiret 1".
        /// </summary>
        // public void UpdateLegalReference(LegalReferenceDto reference, string contentText)
        // {
        //     if (reference == null || string.IsNullOrEmpty(contentText))
        //         return;

        //     if (reference.Article == null)
        //     {
        //         var regex = new Regex(@"(?:po|Po|w|W)\s*art\.\s*([a-zA-Z0-9]+)");
        //         var match = regex.Match(contentText);
        //         if (match.Success) reference.Article = match.Groups[1].Value;
        //     }

        //     if (reference.Subsection == null)
        //     {
        //         var subsectionRegex = new Regex(@"(?:po|Po|w|W)\s*ust\.\s*([a-zA-Z0-9]+)");
        //         var subsectionMatch = subsectionRegex.Match(contentText);
        //         if (subsectionMatch.Success) reference.Subsection = subsectionMatch.Groups[1].Value;
        //     }

        //     if (reference.Point == null)
        //     {
        //         var pointRegex = new Regex(@"(?:po|Po|w|W)\s*pkt\s*([a-zA-Z0-9]+)");
        //         var pointMatch = pointRegex.Match(contentText);
        //         if (pointMatch.Success) reference.Point = pointMatch.Groups[1].Value;
        //     }

        //     if (reference.Letter == null)
        //     {
        //         var letterRegex = new Regex(@"(?:po|Po|w|W)\s*lit\.\s*([a-zA-Z])");
        //         var letterMatch = letterRegex.Match(contentText);
        //         if (letterMatch.Success) reference.Letter = letterMatch.Groups[1].Value;
        //     }
        // }

        /// <summary>
        /// Tworzy kontekst dla encji poprzez łączenie tekstów encji nadrzędnych.
        /// </summary>
        // public string GetContext(BaseEntityDto entity)
        // {
        //     var contextBuilder = new StringBuilder();
        //     var currentEntity = entity;

        //     while (currentEntity != null && !(currentEntity is ArticleDto))
        //     {
        //         if (!string.IsNullOrEmpty(currentEntity.ContentText))
        //         {
        //             contextBuilder.Insert(0, currentEntity.ContentText + " ");
        //         }
        //         currentEntity = currentEntity.Parent;
        //     }

        //     return contextBuilder.ToString().Trim();
        // }
    }
}
