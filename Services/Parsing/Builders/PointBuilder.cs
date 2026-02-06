using DtoArticle = ModelDto.EditorialUnits.Article;
using DtoParagraph = ModelDto.EditorialUnits.Paragraph;
using DtoPoint = ModelDto.EditorialUnits.Point;

namespace WordParserLibrary.Services.Parsing.Builders
{
	public sealed record PointBuildInput(DtoParagraph Paragraph, DtoArticle Article, string Text);
	public sealed record PointEnsureResult(DtoPoint Point, bool CreatedImplicit);

	public sealed class PointBuilder : IEntityBuilder<PointBuildInput, DtoPoint>
	{
		public DtoPoint Build(PointBuildInput input)
		{
			var paragraph = input.Paragraph;
			var article = input.Article;
			var text = input.Text;
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

		public DtoPoint Build(DtoParagraph paragraph, DtoArticle article, string text)
		{
			return Build(new PointBuildInput(paragraph, article, text));
		}

		public PointEnsureResult EnsureForLetter(DtoParagraph? paragraph, DtoArticle article, DtoPoint? currentPoint)
		{
			if (currentPoint != null)
			{
				return new PointEnsureResult(currentPoint, false);
			}

			var point = ParsingFactories.CreateImplicitPoint(paragraph, article);
			paragraph?.Points.Add(point);
			return new PointEnsureResult(point, true);
		}

		public DtoPoint CreateImplicit(DtoParagraph? paragraph, DtoArticle article)
		{
			return ParsingFactories.CreateImplicitPoint(paragraph, article);
		}
	}
}
