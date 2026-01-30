namespace WordParserLibrary.Model.Schemas
{
    /// <summary>
    /// Jednostka systematyzująca: księga
    /// </summary>
    public class BookDto : BaseEntityDto
    {
        public BookDto()
        {
            UnitType = UnitType.Book;
            EIdPrefix = "ks";
            DisplayLabel = "ks.";
        }

        public List<TitleDto> Titles { get; set; } = new();
    }
}