using System.Text.RegularExpressions;
using WordParserLibrary.Services;

namespace WordParserLibrary.Model
{
    public class ContentParser
    {
        private BaseEntity entity = null!;
        public WordParserLibrary.Model.Schemas.EntityNumberDto Number { get; private set; } = new();
        public string Content { get; private set; } = string.Empty;

        public bool ParserError { get; private set; } = false;
        public string ErrorMessage { get; private set; } = string.Empty;
        public bool HasAmendmentOperation { get; internal set; } = false;

        private static readonly EntityNumberService _entityNumberService = new();

        public ContentParser(BaseEntity entity)
        {
            this.entity = entity;
            HasAmendmentOperation = entity.ContentText.Contains("uchyla się");
            // Logika parsowania numeru została przeniesiona do EntityNumberService
            Number = _entityNumberService.Parse(entity.ContentText) ?? new WordParserLibrary.Model.Schemas.EntityNumberDto();
            // Ensure the entity's number is set
            entity.Number = Number;
        }
        
        public ContentParser ParseSubsection()
        {
            var text = entity.ContentText.Trim();
            if (text.StartsWith("Art.") || text.StartsWith("§"))
            {
                // Dopasowanie do formatu: Art. X. Y. text
                var matchWithY = Regex.Match(text, @"^(Art\.|§)\s\d+\.\s(\d+\w*)\.\s(.*)$");
                if (matchWithY.Success)
                {
                    Number.LexicalPart = matchWithY.Groups[2].Value;
                    Content = matchWithY.Groups[3].Value;
                    return this;
                }

                // Dopasowanie do formatu: Art. X. text
                var matchWithoutY = Regex.Match(text, @"^(Art\.|§)\s\d+\.\s?(.*)$");
                if (matchWithoutY.Success)
                {
                    Number.NumericPart = 1; // Domyślnie ustawiamy numer na 1, jeśli nie ma Y
                    Content = matchWithoutY.Groups[2].Value;
                    return this;
                }

                // throw new FormatException("The text format is invalid.");
                entity.Error = ParserError = true;
                entity.ErrorMessage = ErrorMessage = "Oczekiwano formatu: Art. X. Y. text lub Art. X. text.\nMożliwy błędny styl paragrafu.";
                return this;
            }
            else
            {
                // Dopasowanie do formatu: Y. text
                var match = Regex.Match(text, @"^(\d+\w*)\.\s?(.*)$");
                if (match.Success)
                {
                    Number.LexicalPart = match.Groups[1].Value;
                    Content = match.Groups[2].Value;
                    return this;
                }

                //throw new FormatException("The text format is invalid.");
                entity.Error = ParserError = true;
                entity.ErrorMessage = ErrorMessage = "Oczekiwano formatu: Y. text.\nMożliwy błędny styl paragrafu.";
                return this;
            }

            // var match = Regex.Match(text, @"^(?<number>\d+)\s*(?<content>.*)$");
            // if (!match.Success)
            //     throw new FormatException($"Invalid subsection format: {text}");

            // var number = match.Groups["number"].Value;
            // var content = match.Groups["content"].Value.Trim();

            // return new TextSegment(number, content);
        }

        public ContentParser ParseOrdinal()
        {
            var text = entity.ContentText.Trim();
            var match = Regex.Match(text, @"^([^\)]+)\)[\s]?(.*)");
            if (match.Success)
            {
                Number.LexicalPart = match.Groups[1].Value;
                Content = match.Groups[2].Value;
                return this;
            }
            entity.Error = ParserError = true;
            entity.ErrorMessage = ErrorMessage = "Oczekiwano formatu: X) text.\nMożliwy błędny styl paragrafu.";
            return this;
        }
    }
}