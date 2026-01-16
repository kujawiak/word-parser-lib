namespace WordParserLibrary.Model.Schemas
{
    /// <summary>
    /// Model opisujący numer encji (artykuł, ustęp, punkt, litera, tiret).
    /// </summary>
    public class EntityNumberDto
    {
        public string Value { get; set; } = string.Empty;
        public string? RawValue { get; set; }

        public EntityNumberDto() { }

        public EntityNumberDto(string value)
        {
            Value = value;
            RawValue = value;
        }

        public override string ToString() => Value;
    }
}
