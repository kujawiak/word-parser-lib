namespace WordParserLibrary.Model
{
    public enum AmendmentOperationType
    {
        [EnumDescription("uchylenie")]
        Repeal,
        [EnumDescription("dodanie")]
        Insertion,
        [EnumDescription("zmianaBrzmienia")]
        Modification
    }
}