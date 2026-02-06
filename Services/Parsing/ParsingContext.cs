using ModelDto;
using ModelDto.SystematizingUnits;
using DtoArticle = ModelDto.EditorialUnits.Article;
using DtoLetter = ModelDto.EditorialUnits.Letter;
using DtoParagraph = ModelDto.EditorialUnits.Paragraph;
using DtoPoint = ModelDto.EditorialUnits.Point;

namespace WordParserLibrary.Services.Parsing
{
	/// <summary>
	/// Kontekst parsowania przechowujacy aktualny stan drzewa encji
	/// oraz biezaca pozycje strukturalna w hierarchii jednostek redakcyjnych.
	/// </summary>
	public sealed class ParsingContext
	{
		public ParsingContext(LegalDocument document, Subchapter subchapter)
		{
			Document = document;
			Subchapter = subchapter;
		}

		public LegalDocument Document { get; }
		public Subchapter Subchapter { get; }
		public DtoArticle? CurrentArticle { get; set; }
		public DtoParagraph? CurrentParagraph { get; set; }
		public DtoPoint? CurrentPoint { get; set; }
		public DtoLetter? CurrentLetter { get; set; }
		public int CurrentTiretIndex { get; set; }

		/// <summary>
		/// Serwis do budowania i aktualizacji referencji strukturalnych
		/// w kontekscie nowelizacji.
		/// </summary>
		public LegalReferenceService ReferenceService { get; } = new();

		/// <summary>
		/// Biezaca pozycja strukturalna w hierarchii jednostek redakcyjnych
		/// (art. -> ust. -> pkt -> lit. -> tiret). Aktualizowana przez orkiestrator
		/// po kazdym zbudowaniu encji.
		/// </summary>
		public StructuralReference CurrentStructuralReference { get; } = new();

		/// <summary>
		/// Wykryte cele nowelizacji w tresci jednostek redakcyjnych.
		/// Klucz: Guid encji, Wartosc: wykryty cel (referencja strukturalna z RawText).
		/// Wypelniane przez orkiestrator podczas parsowania encji IHasAmendments.
		/// </summary>
		public Dictionary<Guid, StructuralAmendmentReference> DetectedAmendmentTargets { get; } = new();

		/// <summary>
		/// Czy aktualnie przetwarza akapity wewnatrz cudzysłowu (treść nowelizacji).
		/// Akapity ze stylami "Z/..." sa automatycznie traktowane jako czesć nowelizacji.
		/// </summary>
		public bool InsideQuotation { get; set; }
	}
}
