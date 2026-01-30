namespace WordParserLibrary.Model.Schemas
{
    /// <summary>
    /// Model poprawki/zmiany - dane opisujące zmianę w akcie prawnym.
    /// </summary>
    public class AmendmentDto
    {
        public Guid Guid { get; set; } = Guid.NewGuid();
        public string ContentText { get; set; } = string.Empty;
        public string AmendmentType { get; set; } = string.Empty;

        public override string ToString() => $"{AmendmentType}: {ContentText}";
    }
}