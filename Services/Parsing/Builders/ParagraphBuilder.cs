using DtoArticle = ModelDto.EditorialUnits.Article;
using DtoParagraph = ModelDto.EditorialUnits.Paragraph;

namespace WordParserLibrary.Services.Parsing.Builders
{
	public sealed class ParagraphBuilder
	{
		public DtoParagraph Build(DtoArticle article, DtoParagraph? currentParagraph, string text)
		{
			if (currentParagraph != null && currentParagraph.IsImplicit &&
				string.IsNullOrWhiteSpace(currentParagraph.ContentText) &&
				currentParagraph.Points.Count == 0)
			{
				currentParagraph.ContentText = text;
				currentParagraph.Number = ParsingFactories.ParseParagraphNumber(text);
				currentParagraph.IsImplicit = false;
				return currentParagraph;
			}

			var paragraph = new DtoParagraph
			{
				Parent = article,
				Article = article,
				ContentText = text,
				Number = ParsingFactories.ParseParagraphNumber(text),
				IsImplicit = false
			};
			article.Paragraphs.Add(paragraph);
			return paragraph;
		}

		public DtoParagraph CreateImplicit(DtoArticle article)
		{
			return ParsingFactories.CreateImplicitParagraph(article);
		}
	}
}
