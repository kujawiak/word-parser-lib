using ModelDto;
using ModelDto.SystematizingUnits;
using DtoArticle = ModelDto.EditorialUnits.Article;
using DtoLetter = ModelDto.EditorialUnits.Letter;
using DtoParagraph = ModelDto.EditorialUnits.Paragraph;
using DtoPoint = ModelDto.EditorialUnits.Point;

namespace WordParserLibrary.Services.Parsing
{
	/// <summary>
	/// Kontekst parsowania przechowujacy aktualny stan drzewa encji.
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
	}
}
