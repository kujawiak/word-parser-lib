namespace WordParserLibrary.Model.Schemas
{
    /// <summary>
    /// Model artykułu ustawy - zawiera definicję struktury bez logiki parsowania.
    /// Artykuł zawiera co najmniej jeden ustęp.
    /// </summary>
    public class ArticleDto : BaseEntityDto
    {
        public List<SubsectionDto> Subsections { get; set; } = new();
        public List<JournalInfoDto> Journals { get; set; } = new();
        public List<string> AllAmendments { get; set; } = new();

        public bool IsAmending => Journals.Count > 0;

        public override string Id => $"art_{Number?.Value}";
    }

    /// <summary>
    /// Model informacji o publikatorze (np. Dziennik Ustaw).
    /// </summary>
    public class JournalInfoDto
    {
        public int Year { get; set; }
        public List<int> Positions { get; set; } = new();
        public string SourceString { get; set; } = string.Empty;

        public override string ToString()
        {
            var sb = new System.Text.StringBuilder();
            foreach (var position in Positions)
            {
                sb.AppendLine($"DU.{Year}.{position}");
            }
            return sb.ToString();
        }

        public string ToStringLong()
        {
            return $"Rok: {Year}, Pozycje: {string.Join(", ", Positions)} (Fragment źródłowy: \"{SourceString}\")";
        }
    }
}
