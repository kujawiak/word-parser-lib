namespace WordParserLibrary.Model.Schemas
{
    /// <summary>
    /// Model punktu ustępu - zawiera definicję struktury bez logiki parsowania.
    /// Punkt zawiera litery lub tekst bezpośrednio.
    /// </summary>
    public class PointDto : BaseEntityDto
    {
        public List<LetterDto> Letters { get; set; } = new();
        public List<AmendmentDto> Amendments { get; set; } = new();

        public override string Id => $"{Parent?.Id ?? string.Empty}.pkt_{Number?.Value}";
    }
}
