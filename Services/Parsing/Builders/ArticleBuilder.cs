using ModelDto.SystematizingUnits;
using DtoArticle = ModelDto.EditorialUnits.Article;
using DtoParagraph = ModelDto.EditorialUnits.Paragraph;

namespace WordParserLibrary.Services.Parsing.Builders
{
	public sealed class ArticleBuildResult
	{
		public ArticleBuildResult(DtoArticle article, DtoParagraph paragraph)
		{
			Article = article;
			Paragraph = paragraph;
		}

		public DtoArticle Article { get; }
		public DtoParagraph Paragraph { get; }
	}

	public sealed class ArticleBuilder
	{
		public ArticleBuildResult Build(Subchapter subchapter, string text)
		{
			var article = new DtoArticle
			{
				Parent = subchapter,
				ContentText = text,
				Number = ParsingFactories.ParseArticleNumber(text)
			};

			subchapter.Articles.Add(article);

			var articleTail = ParsingFactories.GetArticleTail(text);
			var paragraph = ParsingFactories.CreateParagraphFromArticleTail(article, articleTail);
			article.Paragraphs.Add(paragraph);

			return new ArticleBuildResult(article, paragraph);
		}
	}
}
