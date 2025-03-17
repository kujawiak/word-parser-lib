
namespace WordParserLibrary
{
    public static class XMLConstants
    {
        public const string Root = "ustawa";
        public const string Title = "tytul";
        public const string Article = "artykul";
        public const string Subsection = "ustep";
        public const string Point = "punkt";
        public const string Letter = "litera";
        public const string Tiret = "tiret";
        public const string Amendment = "nowelizacja";

        public const string Number = "numer";
        public const string Amending = "nowelizujacy";
        public const string PublicationYear = "publikatorRok";
        public const string PublicationNumber = "publikatorNumer";
        public const string LetterOrdinal = "litera";

        public const string AmendmentOperation = "pn";

        internal static string GetTagName(string elementName)
        {
            return elementName switch
            {
                nameof(Article) => Article,
                nameof(Title) => Title,
                nameof(Subsection) => Subsection,
                nameof(Point) => Point,
                nameof(Letter) => Letter,
                nameof(Tiret) => Tiret,
                nameof(Amendment) => Amendment,
                nameof(Number) => Number,
                nameof(Amending) => Amending,
                nameof(PublicationYear) => PublicationYear,
                nameof(PublicationNumber) => PublicationNumber,
                nameof(LetterOrdinal) => LetterOrdinal,
                nameof(AmendmentOperation) => AmendmentOperation,
                _ => throw new ArgumentException("Invalid element name", nameof(elementName))
            };
        }
    }
}