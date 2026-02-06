using DtoArticle = ModelDto.EditorialUnits.Article;
using DtoLetter = ModelDto.EditorialUnits.Letter;
using DtoParagraph = ModelDto.EditorialUnits.Paragraph;
using DtoPoint = ModelDto.EditorialUnits.Point;

namespace WordParserLibrary.Services.Parsing.Builders
{
	public sealed class LetterBuilder
	{
		public DtoLetter Build(DtoPoint point, DtoParagraph? paragraph, DtoArticle article, string text)
		{
			var letter = new DtoLetter
			{
				Parent = point,
				Article = article,
				Paragraph = paragraph,
				Point = point,
				ContentText = text,
				Number = ParsingFactories.ParseLetterNumber(text)
			};

			point.Letters.Add(letter);
			return letter;
		}

		public DtoLetter CreateImplicit(DtoPoint point, DtoParagraph? paragraph, DtoArticle article)
		{
			return ParsingFactories.CreateImplicitLetter(point, paragraph, article);
		}
	}
}
