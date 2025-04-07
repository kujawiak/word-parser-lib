namespace WordParserLibrary.Model
{
    public enum AmendmentObjectType
    {
        Article,
        Subsection,
        Point,
        Letter,
        Tiret,
        None
    }

    public static class AmendmentObjectTypeExtensions
    {
        public static string ToFriendlyString(this AmendmentObjectType type)
        {
            return type switch
            {
                AmendmentObjectType.Article => "artykul",
                AmendmentObjectType.Subsection => "ustep",
                AmendmentObjectType.Point => "punkt",
                AmendmentObjectType.Letter => "litera",
                AmendmentObjectType.Tiret => "tiret",
                _ => "None"
            };
        }
    }
}