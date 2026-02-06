using ModelDto;
using DtoArticle = ModelDto.EditorialUnits.Article;
using DtoLetter = ModelDto.EditorialUnits.Letter;
using DtoParagraph = ModelDto.EditorialUnits.Paragraph;
using DtoPoint = ModelDto.EditorialUnits.Point;
using DtoTiret = ModelDto.EditorialUnits.Tiret;

namespace WordParserLibrary.Services.Parsing.Builders
{
	public sealed class TiretBuilder
	{
		public DtoTiret Build(DtoLetter letter, DtoPoint? point, DtoParagraph? paragraph, DtoArticle article, string text, int index)
		{
			var tiret = new DtoTiret
			{
				Parent = letter,
				Article = article,
				Paragraph = paragraph,
				Point = point,
				Letter = letter,
				ContentText = text,
				Number = ParsingFactories.CreateNumber(index.ToString())
			};

			letter.Tirets.Add(tiret);
			return tiret;
		}
	}
}
