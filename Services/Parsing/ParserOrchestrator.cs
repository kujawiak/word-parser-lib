using DocumentFormat.OpenXml.Wordprocessing;
using ModelDto;
using WordParserLibrary.Helpers;
using WordParserLibrary.Services.Parsing.Builders;

namespace WordParserLibrary.Services.Parsing
{
	public sealed class ParserOrchestrator
	{
		private readonly ParagraphClassifier _classifier = new();
		private readonly ArticleBuilder _articleBuilder = new();
		private readonly ParagraphBuilder _paragraphBuilder = new();
		private readonly PointBuilder _pointBuilder = new();
		private readonly LetterBuilder _letterBuilder = new();
		private readonly TiretBuilder _tiretBuilder = new();

		public void ProcessParagraph(Paragraph paragraph, ParsingContext context)
		{
			var text = paragraph.InnerText.Sanitize().Trim();
			if (string.IsNullOrEmpty(text))
			{
				return;
			}

			var styleId = paragraph.StyleId();
			var classification = _classifier.Classify(text, styleId);

			if (classification.Kind == ParagraphKind.Article)
			{
				var result = _articleBuilder.Build(new ArticleBuildInput(context.Subchapter, text));
				context.CurrentArticle = result.Article;
				context.CurrentParagraph = result.Paragraph;
				context.CurrentPoint = null;
				context.CurrentLetter = null;
				context.CurrentTiretIndex = 0;
				return;
			}

			if (context.CurrentArticle == null)
			{
				return;
			}

			switch (classification.Kind)
			{
				case ParagraphKind.Paragraph:
					context.CurrentParagraph = _paragraphBuilder.Build(new ParagraphBuildInput(context.CurrentArticle, context.CurrentParagraph, text));
					ValidationReporter.AddClassificationWarning(context.CurrentParagraph, classification, "UST");
					context.CurrentPoint = null;
					context.CurrentLetter = null;
					context.CurrentTiretIndex = 0;
					break;
				case ParagraphKind.Point:
					var ensuredParagraph = _paragraphBuilder.EnsureForPoint(context.CurrentArticle, context.CurrentParagraph);
					context.CurrentParagraph = ensuredParagraph.Paragraph;
					context.CurrentPoint = _pointBuilder.Build(new PointBuildInput(context.CurrentParagraph, context.CurrentArticle, text));
					ValidationReporter.AddClassificationWarning(context.CurrentPoint, classification, "PKT");
					context.CurrentLetter = null;
					context.CurrentTiretIndex = 0;
					break;
				case ParagraphKind.Letter:
					var ensuredPoint = _pointBuilder.EnsureForLetter(context.CurrentParagraph, context.CurrentArticle, context.CurrentPoint);
					context.CurrentPoint = ensuredPoint.Point;
					if (ensuredPoint.CreatedImplicit)
					{
						ValidationReporter.AddValidationMessage(context.CurrentPoint, ValidationLevel.Warning,
							"Brak jawnego punktu; utworzono niejawny punkt na podstawie struktury.");
					}
					context.CurrentLetter = _letterBuilder.Build(new LetterBuildInput(context.CurrentPoint, context.CurrentParagraph, context.CurrentArticle, text));
					ValidationReporter.AddClassificationWarning(context.CurrentLetter, classification, "LIT");
					context.CurrentTiretIndex = 0;
					break;
				case ParagraphKind.Tiret:
					var ensuredPointForTiret = _pointBuilder.EnsureForLetter(context.CurrentParagraph, context.CurrentArticle, context.CurrentPoint);
					context.CurrentPoint = ensuredPointForTiret.Point;
					if (ensuredPointForTiret.CreatedImplicit)
					{
						ValidationReporter.AddValidationMessage(context.CurrentPoint, ValidationLevel.Warning,
							"Brak jawnego punktu; utworzono niejawny punkt na podstawie struktury.");
					}

					var ensuredLetter = _letterBuilder.EnsureForTiret(context.CurrentPoint, context.CurrentParagraph,
						context.CurrentArticle, context.CurrentLetter);
					context.CurrentLetter = ensuredLetter.Letter;
					if (ensuredLetter.CreatedImplicit)
					{
						ValidationReporter.AddValidationMessage(context.CurrentLetter, ValidationLevel.Warning,
							"Brak jawnej litery; utworzono niejawna litere na podstawie struktury.");
					}
					context.CurrentTiretIndex++;
					_tiretBuilder.Build(new TiretBuildInput(context.CurrentLetter, context.CurrentPoint, context.CurrentParagraph,
						context.CurrentArticle, text, context.CurrentTiretIndex));
					ValidationReporter.AddClassificationWarning(context.CurrentLetter.Tirets[^1], classification, "TIR");
					break;
				default:
					break;
			}
		}
	}
}
