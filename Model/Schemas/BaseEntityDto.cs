namespace WordParserLibrary.Model.Schemas
{
    /// <summary>
    /// Bazowy DTO dla wszystkich encji modelu (artykuł, ustęp, punkt, litera, tiret, jednostki systematyzujące).
    /// Zawiera dane opisujące strukturę i zależności bez logiki parsowania.
    /// Wprowadza: UnitType, DisplayLabel, EIdPrefix oraz centralne budowanie stabilnego eId.
    /// </summary>
    public enum UnitType
    {
        Unknown,
        // editorial units
        Article,
        Paragraph, // ustęp
        Point,
        Letter,
        Tiret,
        // systematizing units
        Part,
        Book,
        Title,
        Division,
        Chapter,
        Subchapter
    }

    public abstract class BaseEntityDto
    {
        public Guid Guid { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Typ jednostki semantycznej (np. Article/Paragraph/Point/Letter/Tiret/...)
        /// </summary>
        public UnitType UnitType { get; set; } = UnitType.Unknown;

        /// <summary>
        /// Numer encji (np. 10 dla artykułu, 2 dla ustępu, f dla litery).
        /// Zawiera rozbicie na komponenty: część liczbowa, tekstowa i indeks górny.
        /// </summary>
        public EntityNumberDto? Number { get; set; }

        public string ContentText { get; set; } = string.Empty;
        public string Context { get; set; } = string.Empty;
        public DateTime EffectiveDate { get; set; }
        public LegalReferenceDto? LegalReference { get; set; }
        public bool? Error { get; set; }
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Etykieta do wyświetlenia (np. "art.", "ust.", "§")
        /// </summary>
        public string DisplayLabel { get; set; } = string.Empty;

        /// <summary>
        /// Prefiks używany przy generowaniu eId (np. "art"/"ust"/"pkt"/"lit"/"tir"/"cz"/"ks" ...).
        /// Można go nadpisać w klasach pochodnych jeżeli konieczne.
        /// </summary>
        public string EIdPrefix { get; set; } = string.Empty;

        // Referencje do encji nadrzędnych (ułatwiają nawigację w modelu)
        public BaseEntityDto? Parent { get; set; }
        public ArticleDto? Article { get; set; }
        public ParagraphDto? Paragraph { get; set; }
        public PointDto? Point { get; set; }
        public LetterDto? Letter { get; set; }
        public TiretDto? Tiret { get; set; }

        /// <summary>
        /// Lokalny segment identyfikatora (np. "art_10" albo "lit_a").
        /// Można nadpisać w klasach pochodnych, gdy format różni się (np. litera/ordinal).
        /// </summary>
        protected virtual string GetLocalIdSegment()
        {
            if (string.IsNullOrEmpty(EIdPrefix)) return string.Empty;

            // prefer specjalne pola
            if (this is LetterDto l)
            {
                return string.IsNullOrEmpty(l.Number?.Value) ? EIdPrefix : $"{EIdPrefix}_{l.Number.Value}";
            }

            if (this is TiretDto t)
            {
                return string.IsNullOrEmpty(t.Number?.Value) ? EIdPrefix : $"{EIdPrefix}_{t.Number.Value}";
            }

            if (!string.IsNullOrEmpty(Number?.Value))
            {
                return $"{EIdPrefix}_{Number.Value}";
            }

            return EIdPrefix;
        }

        /// <summary>
        /// Hierarchiczne, stabilne i parsowalne Id (eId) budowane od korzenia w dół,
        /// używa separatora "__" zgodnie z założeniem (np. "art_10__ust_2__pkt_3").
        /// </summary>
        public virtual string Id
        {
            get
            {
                var parts = new List<string>();
                BaseEntityDto? current = this;
                while (current != null)
                {
                    var seg = current.GetLocalIdSegment();
                    if (!string.IsNullOrEmpty(seg)) parts.Add(seg);
                    current = current.Parent;
                }
                parts.Reverse();
                return string.Join("__", parts);
            }
        }
    }
}
