namespace WordParserLibrary.Model.Schemas
{
    /// <summary>
    /// Bazowy DTO dla wszystkich encji modelu (artykuł, ustęp, punkt, litera, tiret).
    /// Zawiera czyste dane opisujące strukturę i zależności bez logiki biznesowej.
    /// </summary>
    public abstract class BaseEntityDto
    {
        public Guid Guid { get; set; } = Guid.NewGuid();
        public EntityNumberDto? Number { get; set; }
        public string ContentText { get; set; } = string.Empty;
        public string Context { get; set; } = string.Empty;
        public DateTime EffectiveDate { get; set; }
        public LegalReferenceDto? LegalReference { get; set; }
        public bool? Error { get; set; }
        public string? ErrorMessage { get; set; }
        public string EntityType { get; set; } = string.Empty;

        // Referencje do encji nadrzędnych
        public BaseEntityDto? Parent { get; set; }
        public ArticleDto? Article { get; set; }
        public SubsectionDto? Subsection { get; set; }
        public PointDto? Point { get; set; }
        public LetterDto? Letter { get; set; }
        public TiretDto? Tiret { get; set; }

        public abstract string Id { get; }
    }
}
