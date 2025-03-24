using System.Text.RegularExpressions;

namespace WordParserLibrary
{
    public static class StringExtensions
    {
        public static string Sanitize(this string input)
        {
            input = Regex.Replace(input, @"\s+", " ");
            return input.Replace("–", "-");
        }

        public static string ExtractOrdinal(this string input)
        {
            var match = Regex.Match(input, @"^([^\)]+)\)");
            return match.Success ? match.Groups[1].Value : "Unknown";
        }
    }   
}