namespace WordParserLibrary.Model.Schemas
{
    /// <summary>
    /// Jednostka systematyzująca: dział
    /// </summary>
    public class DivisionDto : BaseEntityDto
    {
        public DivisionDto()
        {
            UnitType = UnitType.Division;
            EIdPrefix = "dz";
            DisplayLabel = "dz.";
        }

        public List<ChapterDto> Chapters { get; set; } = new();
    }
}