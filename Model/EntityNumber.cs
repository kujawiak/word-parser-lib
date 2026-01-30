using System;
using System.Collections.Generic;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Text.RegularExpressions;
using System.Text;

namespace WordParserLibrary.Model
{
    [System.Obsolete("Use EntityNumberDto in WordParserLibrary.Model.Schemas for DTO-level numbering and parsing.")]
    public class EntityNumber
    {
        public int NumericPart { get; set; }
        public string LexicalPart { get; set; } = string.Empty;
        public string Superscript { get; set; } = string.Empty;
        public EntityNumber(Paragraph paragraph)
        {
            //TODO Parse paragraph to extract numeric and lexical parts
            NumericPart = 0;
            LexicalPart = string.Empty;
            Superscript = string.Empty;
        }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            if (NumericPart > 0)
            {
                sb.Append(NumericPart);
            }
            if (!string.IsNullOrEmpty(LexicalPart))
            {
                sb.Append(LexicalPart);
            }
            if (!string.IsNullOrEmpty(Superscript))
            {
                sb.Append($"^{Superscript}");
            }
            return sb.ToString();
        }
    }
}