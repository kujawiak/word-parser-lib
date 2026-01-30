namespace WordParserLibrary.Model.Schemas
{
    /// <summary>
    /// Model nowelizacji - pełna reprezentacja zmiany w akcie prawnym.
    /// Nowelizacja może dotyczyć uchylenia, dodania lub zmiany brzmienia jednostek redakcyjnych.
    /// </summary>
    public class AmendmentDto
    {
        /// <summary>
        /// Unikalny identyfikator nowelizacji.
        /// </summary>
        public Guid Guid { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Rodzaj operacji nowelizacyjnej (uchylenie, dodanie, zmiana brzmienia).
        /// </summary>
        public AmendmentOperationType OperationType { get; set; }

        /// <summary>
        /// Treść nowelizacji w przypadku dodania lub zmiany brzmienia.
        /// Może zawierać hierarchiczną strukturę (artykuł z ustępami, punktami, literami, tiretami).
        /// Null w przypadku uchylenia.
        /// </summary>
        public AmendmentContentDto? Content { get; set; }

        /// <summary>
        /// Informacja o ustawie, której dotyczy nowelizacja (rok i lista pozycji publikatora).
        /// </summary>
        public JournalInfoDto TargetLegalAct { get; set; } = new();

        /// <summary>
        /// Cel nowelizacji - jednostki redakcyjne których dotyczy zmiana.
        /// Może być lista celów (np. "w art. 5 ust. 2 pkt 1 otrzymuje brzmienie").
        /// </summary>
        public List<AmendmentTargetDto> Targets { get; set; } = new();

        /// <summary>
        /// Data wejścia w życie nowelizacji.
        /// </summary>
        public DateTime? EffectiveDate { get; set; }

        public override string ToString()
        {
            var targetStr = string.Join(", ", Targets.Select(t => t.ToString()));
            return $"{OperationType}: {targetStr} (wejście w życie: {EffectiveDate?.ToShortDateString() ?? "nieokreślone"})";
        }
    }

    /// <summary>
    /// Model treści nowelizacji - może zawierać dowolny fragment hierarchicznej struktury aktu prawnego.
    /// </summary>
    public class AmendmentContentDto
    {
        /// <summary>
        /// Typ obiektu będącego treścią nowelizacji (Artykuł, Ustęp, Punkt, Litera, Tiret).
        /// </summary>
        public AmendmentObjectType ObjectType { get; set; }

        /// <summary>
        /// Artykuły będące treścią nowelizacji (w przypadku dodania/zmiany pełnych artykułów).
        /// </summary>
        public List<ArticleDto> Articles { get; set; } = new();

        /// <summary>
        /// Ustępy będące treścią nowelizacji (w przypadku dodania/zmiany ustępów).
        /// </summary>
        public List<ParagraphDto> Paragraphs { get; set; } = new();

        /// <summary>
        /// Punkty będące treścią nowelizacji (w przypadku dodania/zmiany punktów).
        /// </summary>
        public List<PointDto> Points { get; set; } = new();

        /// <summary>
        /// Litery będące treścią nowelizacji (w przypadku dodania/zmiany liter).
        /// </summary>
        public List<LetterDto> Letters { get; set; } = new();

        /// <summary>
        /// Tirety będące treścią nowelizacji (w przypadku dodania/zmiany tiretów).
        /// </summary>
        public List<TiretDto> Tirets { get; set; } = new();

        /// <summary>
        /// Prosty tekst treści (dla przypadków, gdy nowelizacja nie ma struktury hierarchicznej).
        /// </summary>
        public string? PlainText { get; set; }

        public override string ToString()
        {
            if (!string.IsNullOrEmpty(PlainText))
                return PlainText;

            var counts = new List<string>();
            if (Articles.Any()) counts.Add($"{Articles.Count} art.");
            if (Paragraphs.Any()) counts.Add($"{Paragraphs.Count} ust.");
            if (Points.Any()) counts.Add($"{Points.Count} pkt.");
            if (Letters.Any()) counts.Add($"{Letters.Count} lit.");
            if (Tirets.Any()) counts.Add($"{Tirets.Count} tir.");

            return counts.Any() ? string.Join(", ", counts) : "brak treści";
        }
    }

    /// <summary>
    /// Model celu nowelizacji - adres jednostki redakcyjnej której dotyczy zmiana.
    /// Przykład: "w art. 5 ust. 2 pkt 1" → Article="5", Subsection="2", Point="1"
    /// </summary>
    public class AmendmentTargetDto
    {
        /// <summary>
        /// Numer artykułu (jeśli dotyczy).
        /// </summary>
        public string? Article { get; set; }

        /// <summary>
        /// Numer ustępu (jeśli dotyczy).
        /// </summary>
        public string? Subsection { get; set; }

        /// <summary>
        /// Numer punktu (jeśli dotyczy).
        /// </summary>
        public string? Point { get; set; }

        /// <summary>
        /// Oznaczenie litery (jeśli dotyczy).
        /// </summary>
        public string? Letter { get; set; }

        /// <summary>
        /// Numer tiretu (jeśli dotyczy).
        /// </summary>
        public string? Tiret { get; set; }

        /// <summary>
        /// Typ obiektu będącego celem nowelizacji.
        /// </summary>
        public AmendmentObjectType ObjectType { get; set; }

        /// <summary>
        /// Pełny identyfikator hierarchiczny celu (np. "art_5__ust_2__pkt_1").
        /// </summary>
        public string HierarchicalId
        {
            get
            {
                var parts = new List<string>();
                if (!string.IsNullOrEmpty(Article)) parts.Add($"art_{Article}");
                if (!string.IsNullOrEmpty(Subsection)) parts.Add($"ust_{Subsection}");
                if (!string.IsNullOrEmpty(Point)) parts.Add($"pkt_{Point}");
                if (!string.IsNullOrEmpty(Letter)) parts.Add($"lit_{Letter}");
                if (!string.IsNullOrEmpty(Tiret)) parts.Add($"tir_{Tiret}");
                return string.Join("__", parts);
            }
        }

        public override string ToString()
        {
            var parts = new List<string>();
            if (!string.IsNullOrEmpty(Article)) parts.Add($"art. {Article}");
            if (!string.IsNullOrEmpty(Subsection)) parts.Add($"ust. {Subsection}");
            if (!string.IsNullOrEmpty(Point)) parts.Add($"pkt {Point}");
            if (!string.IsNullOrEmpty(Letter)) parts.Add($"lit. {Letter}");
            if (!string.IsNullOrEmpty(Tiret)) parts.Add($"tiret {Tiret}");
            return string.Join(" ", parts);
        }
    }
}