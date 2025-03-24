namespace WordParserLibrary.Model
{
    public class LegalReference
    {
        public string? Article { get; set; }
        public string? Subsection { get; set; }
        public string? Point { get; set; }
        public string? Letter { get; set; }
        public string? Tiret { get; set; }
        public string? PublicationNumber { get; set; }
        public string? PublicationYear { get; set; }

        public override string ToString()
        {
            var parts = new List<string>();
            if (!string.IsNullOrEmpty(PublicationNumber)) parts.Add($"{PublicationNumber}");
            if (!string.IsNullOrEmpty(PublicationYear)) parts.Add($"{PublicationYear}");
            if (!string.IsNullOrEmpty(Article)) parts.Add($"art. {Article}");
            if (!string.IsNullOrEmpty(Subsection)) parts.Add($"ust. {Subsection}");
            if (!string.IsNullOrEmpty(Point)) parts.Add($"pkt. {Point}");
            if (!string.IsNullOrEmpty(Letter)) parts.Add($"lit. {Letter}");
            if (!string.IsNullOrEmpty(Tiret)) parts.Add($"tiret {Tiret}");
            return string.Join("|", parts);
        }
    }
}