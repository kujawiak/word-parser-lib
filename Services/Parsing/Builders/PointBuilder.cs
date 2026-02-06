using DtoArticle = ModelDto.EditorialUnits.Article;
using DtoParagraph = ModelDto.EditorialUnits.Paragraph;
using DtoPoint = ModelDto.EditorialUnits.Point;

namespace WordParserLibrary.Services.Parsing.Builders
{
	public sealed class PointBuilder
	{
		public DtoPoint Build(DtoParagraph paragraph, DtoArticle article, string text)
		{
			var point = new DtoPoint
			{
				Parent = paragraph,
				Article = article,
				Paragraph = paragraph,
				ContentText = text,
				Number = ParsingFactories.ParsePointNumber(text)
			};

			paragraph.Points.Add(point);
			return point;
		}

		public DtoPoint CreateImplicit(DtoParagraph? paragraph, DtoArticle article)
		{
			return ParsingFactories.CreateImplicitPoint(paragraph, article);
		}
	}
}
