namespace WordParserLibrary.Model.Schemas
{
    /// <summary>
    /// Model ustępu artykułu - zawiera definicję struktury bez logiki parsowania.
    /// Ustęp zawiera co najmniej jeden punkt lub tekst bezpośrednio.
    /// </summary>
    public class SubsectionDto : BaseEntityDto
    {
        public List<PointDto> Points { get; set; } = new();
        public List<AmendmentDto> Amendments { get; set; } = new();

        public override string Id => $"{Parent?.Id ?? string.Empty}.ust_{Number?.Value}";
    }

    /// <summary>
    /// Model poprawki/zmiany - dane opisujące zmianę w akcie prawnym.
    /// </summary>
    public class AmendmentDto
    {
        public Guid Guid { get; set; } = Guid.NewGuid();
        public string ContentText { get; set; } = string.Empty;
        public string AmendmentType { get; set; } = string.Empty;
    }
}
