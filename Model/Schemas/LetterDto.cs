namespace WordParserLibrary.Model.Schemas
{
    /// <summary>
    /// Model litery punktu - zawiera definicję struktury bez logiki parsowania.
    /// Litera zawiera tirets lub tekst bezpośrednio.
    /// </summary>
    public class LetterDto : BaseEntityDto
    {
        public string Ordinal { get; set; } = string.Empty;
        public List<TiretDto> Tirets { get; set; } = new();
        public List<AmendmentDto> Amendments { get; set; } = new();

        public override string Id => $"{Parent?.Id ?? string.Empty}.lit_{Ordinal}";
    }
}
