namespace WordParserLibrary.Model
{
    public class AmendmentOperation
    {
        public AmendmentOperationType Type { get; set; }
        public LegalReference AmendmentTarget { get; set; } = new LegalReference();
        public string AmendmentObject { get; set; }
    }
}