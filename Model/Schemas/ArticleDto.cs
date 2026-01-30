namespace WordParserLibrary.Model.Schemas
{
    /// <summary>
    /// Model artykułu ustawy - zawiera definicję struktury bez logiki parsowania.
    /// Artykuł zawiera co najmniej jeden ustęp.
    /// </summary>
    public class ArticleDto : BaseEntityDto
    {
        public List<ParagraphDto> Paragraphs { get; set; } = new();
        public List<JournalInfoDto> Journals { get; set; } = new();
        public List<string> AllAmendments { get; set; } = new();

        public bool IsAmending => Journals.Count > 0;

        public ArticleDto()
        {
            UnitType = UnitType.Article;
            EIdPrefix = "art";
            DisplayLabel = "art.";
        }
    }
}
