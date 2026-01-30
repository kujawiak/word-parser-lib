namespace WordParserLibrary.Model.Schemas
{
    /// <summary>
    /// Jednostka systematyzująca: rozdział
    /// </summary>
    public class ChapterDto : BaseEntityDto
    {
        public ChapterDto()
        {
            UnitType = UnitType.Chapter;
            EIdPrefix = "rozdz";
            DisplayLabel = "rozdz.";
        }

        public List<SubchapterDto> Subchapters { get; set; } = new();
    }
}