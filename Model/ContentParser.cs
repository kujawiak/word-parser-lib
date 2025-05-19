using System.Text.RegularExpressions;

namespace WordParserLibrary.Model
{
    public class ContentParser
    {
        private BaseEntity entity = null!;
        public string Number { get; private set; } = string.Empty;
        public string Content { get; private set; } = string.Empty;
        public string PublicationYear { get; private set; } = string.Empty;
        public string PublicationNumber { get; private set; } = string.Empty;

        public bool ParserError { get; private set; } = false;
        public string ErrorMessage { get; private set; } = string.Empty;
        public bool HasAmendmentOperation { get; internal set; } = false;

        public ContentParser(BaseEntity entity)
        {
            this.entity = entity;
            HasAmendmentOperation = entity.ContentText.Contains("uchyla się");
        }

        public ContentParser ParseArticle()
        {
            var text = entity.ContentText.Trim();
            var match = Regex.Match(text, @"Art\.\s([\w\d]+)+\.?\s*(.*)");
            if (match.Success)
            {
                Number = match.Groups[1].Value;
                Content = match.Groups[2].Value.Trim();
            }
            else
            {
                // throw new FormatException("The text format is invalid.");
                entity.Error = ParserError = true;
                entity.ErrorMessage = ErrorMessage = "Oczekiwano formatu: Art. X.\nMożliwy błędny styl artykułu.";
                return this;
            }
            
            var publication = new Regex(@"Dz\.\sU\.\sz\s(\d{4})\sr\.\spoz\.\s(\d+)");
            if (publication.Match(Content).Success)
            {
                PublicationYear = publication.Match(Content).Groups[1].Value;
                PublicationNumber = publication.Match(Content).Groups[2].Value;
            }

            return this;
        }
        
        public ContentParser ParseSubsection()
        {
            var text = entity.ContentText.Trim();
            if (text.StartsWith("Art."))
            {
                // Dopasowanie do formatu: Art. X. Y. text
                var matchWithY = Regex.Match(text, @"^Art\.\s\d+\.\s(\d+\w*)\.\s(.*)$");
                if (matchWithY.Success)
                {
                    Number = matchWithY.Groups[1].Value;
                    Content = matchWithY.Groups[2].Value;
                    return this;
                }

                // Dopasowanie do formatu: Art. X. text
                var matchWithoutY = Regex.Match(text, @"^Art\.\s\d+\.\s?(.*)$");
                if (matchWithoutY.Success)
                {
                    Number = "1"; // Domyślnie ustawiamy numer na 1, jeśli nie ma Y
                    Content = matchWithoutY.Groups[1].Value;
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
                    Number = match.Groups[1].Value;
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
                Number = match.Groups[1].Value;
                Content = match.Groups[2].Value;
                return this;
            }
            entity.Error = ParserError = true;
            entity.ErrorMessage = ErrorMessage = "Oczekiwano formatu: X) text.\nMożliwy błędny styl paragrafu.";
            return this;
        }
    }
}