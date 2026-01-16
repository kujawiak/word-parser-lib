namespace WordParserLibrary.Model.Schemas
{
    /// <summary>
    /// Model tiret litery - zawiera definicję struktury bez logiki parsowania.
    /// Tiret to najmniejsza jednostka struktury aktu prawnego.
    /// </summary>
    public class TiretDto : BaseEntityDto
    {
        public new int Number { get; set; } = 1;
        public List<AmendmentDto> Amendments { get; set; } = new();

        public override string Id => $"{Parent?.Id ?? string.Empty}.tir_{Number}";
    }
}
