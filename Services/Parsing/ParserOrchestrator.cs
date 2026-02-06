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
				var result = _articleBuilder.Build(context.Subchapter, text);
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
					context.CurrentParagraph = _paragraphBuilder.Build(context.CurrentArticle, context.CurrentParagraph, text);
					ValidationReporter.AddClassificationWarning(context.CurrentParagraph, classification, "UST");
					context.CurrentPoint = null;
					context.CurrentLetter = null;
					context.CurrentTiretIndex = 0;
					break;
				case ParagraphKind.Point:
					if (context.CurrentParagraph == null)
					{
						context.CurrentParagraph = _paragraphBuilder.CreateImplicit(context.CurrentArticle);
						context.CurrentArticle.Paragraphs.Add(context.CurrentParagraph);
					}
					context.CurrentPoint = _pointBuilder.Build(context.CurrentParagraph, context.CurrentArticle, text);
					ValidationReporter.AddClassificationWarning(context.CurrentPoint, classification, "PKT");
					context.CurrentLetter = null;
					context.CurrentTiretIndex = 0;
					break;
				case ParagraphKind.Letter:
					if (context.CurrentPoint == null)
					{
						context.CurrentPoint = _pointBuilder.CreateImplicit(context.CurrentParagraph, context.CurrentArticle);
						context.CurrentParagraph?.Points.Add(context.CurrentPoint);
						ValidationReporter.AddValidationMessage(context.CurrentPoint, ValidationLevel.Warning,
							"Brak jawnego punktu; utworzono niejawny punkt na podstawie struktury.");
					}
					context.CurrentLetter = _letterBuilder.Build(context.CurrentPoint, context.CurrentParagraph, context.CurrentArticle, text);
					ValidationReporter.AddClassificationWarning(context.CurrentLetter, classification, "LIT");
					context.CurrentTiretIndex = 0;
					break;
				case ParagraphKind.Tiret:
					if (context.CurrentLetter == null)
					{
						context.CurrentPoint ??= _pointBuilder.CreateImplicit(context.CurrentParagraph, context.CurrentArticle);
						context.CurrentParagraph?.Points.Add(context.CurrentPoint);
						context.CurrentLetter = _letterBuilder.CreateImplicit(context.CurrentPoint, context.CurrentParagraph, context.CurrentArticle);
						context.CurrentPoint!.Letters.Add(context.CurrentLetter);
						ValidationReporter.AddValidationMessage(context.CurrentLetter, ValidationLevel.Warning,
							"Brak jawnej litery; utworzono niejawna litere na podstawie struktury.");
					}
					context.CurrentTiretIndex++;
					_tiretBuilder.Build(context.CurrentLetter, context.CurrentPoint, context.CurrentParagraph, context.CurrentArticle,
						text, context.CurrentTiretIndex);
					ValidationReporter.AddClassificationWarning(context.CurrentLetter.Tirets[^1], classification, "TIR");
					break;
				default:
					break;
			}
		}
	}
}
