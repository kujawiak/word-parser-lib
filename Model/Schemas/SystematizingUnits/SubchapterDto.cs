namespace WordParserLibrary.Model.Schemas
{
    /// <summary>
    /// Jednostka systematyzująca: oddział
    /// </summary>
    public class SubchapterDto : BaseEntityDto
    {
        public SubchapterDto()
        {
            UnitType = UnitType.Subchapter;
            EIdPrefix = "oddz";
            DisplayLabel = "oddz.";
        }

        // oddziały zazwyczaj zawierają artykuły
        public List<ArticleDto> Articles { get; set; } = new();
    }
}