namespace WordParserLibrary.Model.Schemas
{
    /// <summary>
    /// Jednostka systematyzująca: tytuł
    /// </summary>
    public class TitleDto : BaseEntityDto
    {
        public TitleDto()
        {
            UnitType = UnitType.Title;
            EIdPrefix = "tyt";
            DisplayLabel = "tyt.";
        }

        public List<DivisionDto> Divisions { get; set; } = new();
    }
}