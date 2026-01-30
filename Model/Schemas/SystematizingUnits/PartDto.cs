namespace WordParserLibrary.Model.Schemas
{
    /// <summary>
    /// Jednostka systematyzująca: część
    /// </summary>
    public class PartDto : BaseEntityDto
    {
        public PartDto()
        {
            UnitType = UnitType.Part;
            EIdPrefix = "cz";
            DisplayLabel = "cz.";
        }

        public List<BookDto> Books { get; set; } = new();
    }
}