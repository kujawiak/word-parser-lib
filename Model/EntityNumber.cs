using System;
using System.Collections.Generic;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Text.RegularExpressions;
using System.Text;

namespace WordParserLibrary.Model
{
    public class EntityNumber
    {
        public int NumericPart { get; set; }
        public string LexicalPart { get; set; }
        public string Superscript { get; set; }
        public string Subscript { get; set; }
        public EntityNumber(Paragraph paragraph)
        {
            //TODO Parse paragraph to extract numeric and lexical parts
            NumericPart = 0;
            LexicalPart = string.Empty;
            Superscript = string.Empty;
            Subscript = string.Empty;
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
            if (!string.IsNullOrEmpty(Subscript))
            {
                sb.Append($"_{Subscript}");
            }
            return sb.ToString();
        }
    }
}