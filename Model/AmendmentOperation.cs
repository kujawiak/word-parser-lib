namespace WordParserLibrary.Model
{
    public class AmendmentOperation
    {
        public AmendmentOperationType Type { get; set; }
        public LegalReference AmendmentTarget { get; set; } = new LegalReference();
        public string AmendmentObject { get; set; } = string.Empty;
        public AmendmentObjectType AmendmentObjectType { get; set; }
        public List<Amendment> Amendments { get; set; } = new List<Amendment>();

        public override string ToString()
        {
            return $"{Type}, {AmendmentTarget}, {AmendmentObject}";
        }
    }
}