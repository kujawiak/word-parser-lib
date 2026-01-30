namespace WordParserLibrary.Model.Schemas
{
    /// <summary>
    /// Model ustępu artykułu (Paragraph) - definicja struktury bez logiki parsowania.
    /// Paragraph zawiera co najmniej jeden punkt lub tekst bezpośrednio.
    /// </summary>
    public class ParagraphDto : BaseEntityDto
    {
        public List<PointDto> Points { get; set; } = new();
        public List<AmendmentDto> Amendments { get; set; } = new();

        /// <summary>
        /// Role może określać specjalne mapowania/nazewnictwo (np. "prg" dla paragrafu w kodeksie).
        /// </summary>
        public string? Role { get; set; }

        public ParagraphDto()
        {
            UnitType = UnitType.Paragraph;
            EIdPrefix = "ust";
            DisplayLabel = "ust.";
        }
    }
}