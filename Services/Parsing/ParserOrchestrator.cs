using DocumentFormat.OpenXml.Wordprocessing;
using ModelDto;
using WordParserLibrary.Helpers;
using WordParserLibrary.Services.Parsing.Builders;
using Serilog;

namespace WordParserLibrary.Services.Parsing
{
	/// <summary>
	/// Orkiestrator parsowania: klasyfikuje akapity, deleguje budowanie encji
	/// i aktualizuje kontekst nowelizacji (pozycje strukturalna i wykryte cele).
	/// </summary>
	public sealed class ParserOrchestrator
	{
		private readonly ParagraphClassifier _classifier = new();
		private readonly ArticleBuilder _articleBuilder = new();
		private readonly ParagraphBuilder _paragraphBuilder = new();
		private readonly PointBuilder _pointBuilder = new();
		private readonly LetterBuilder _letterBuilder = new();
		private readonly TiretBuilder _tiretBuilder = new();
		private readonly NumberingContinuityValidator _numberingValidator = new();

		/// <summary>
		/// Przetwarza pojedynczy akapit i aktualizuje stan kontekstu.
		/// </summary>
		public void ProcessParagraph(Paragraph paragraph, ParsingContext context)
		{
			var text = paragraph.InnerText.Sanitize().Trim();
			if (string.IsNullOrEmpty(text))
			{
				return;
			}

			var styleId = paragraph.StyleId();
			var classification = _classifier.Classify(text, styleId);

			// Sledzenie cudzysłowow: aktualizuj stan kontekstu
			UpdateQuotationState(context, text, classification);

			// Pomijaj akapity w nowelizacji (styl Z/... lub wewnatrz cudzysłowow)
			if (classification.IsAmendmentContent || context.InsideQuotation)
			{
				Log.Debug("Pominieto akapit nowelizacji: styl={StyleId}, insideQuote={InsideQuote}, text={Text}",
					styleId, context.InsideQuotation, text.Length > 50 ? text.Substring(0, 50) + "..." : text);
				return;
			}

			if (classification.Kind == ParagraphKind.Article)
			{
				var result = _articleBuilder.Build(new ArticleBuildInput(context.Subchapter, text));
				_numberingValidator.ValidateArticle(result.Article);
				context.CurrentArticle = result.Article;
				context.CurrentParagraph = result.Paragraph;
				context.CurrentPoint = null;
				context.CurrentLetter = null;
				context.CurrentTiretIndex = 0;

				UpdateStructuralReference(context, result.Article);
				if (result.Paragraph != null && !result.Paragraph.IsImplicit)
				{
					UpdateStructuralReference(context, result.Paragraph);
					DetectAmendmentTargets(context, result.Paragraph);
				}
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
					_numberingValidator.ValidateParagraph(context.CurrentParagraph);
					ValidationReporter.AddClassificationWarning(context.CurrentParagraph, classification, "UST");
					context.CurrentPoint = null;
					context.CurrentLetter = null;
					context.CurrentTiretIndex = 0;

					UpdateStructuralReference(context, context.CurrentParagraph);
					DetectAmendmentTargets(context, context.CurrentParagraph);
					break;
				case ParagraphKind.Point:
					var ensuredParagraph = _paragraphBuilder.EnsureForPoint(context.CurrentArticle, context.CurrentParagraph);
					context.CurrentParagraph = ensuredParagraph.Paragraph;
					context.CurrentPoint = _pointBuilder.Build(new PointBuildInput(context.CurrentParagraph, context.CurrentArticle, text));
					_numberingValidator.ValidatePoint(context.CurrentPoint);
					ValidationReporter.AddClassificationWarning(context.CurrentPoint, classification, "PKT");
					context.CurrentLetter = null;
					context.CurrentTiretIndex = 0;

					UpdateStructuralReference(context, context.CurrentPoint);
					DetectAmendmentTargets(context, context.CurrentPoint);
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
					_numberingValidator.ValidateLetter(context.CurrentLetter);
					ValidationReporter.AddClassificationWarning(context.CurrentLetter, classification, "LIT");
					context.CurrentTiretIndex = 0;

					UpdateStructuralReference(context, context.CurrentLetter);
					DetectAmendmentTargets(context, context.CurrentLetter);
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
					var tiret = _tiretBuilder.Build(new TiretBuildInput(context.CurrentLetter, context.CurrentPoint, context.CurrentParagraph,
						context.CurrentArticle, text, context.CurrentTiretIndex));
					ValidationReporter.AddClassificationWarning(tiret, classification, "TIR");

					UpdateStructuralReference(context, tiret);
					DetectAmendmentTargets(context, tiret);
					break;
				default:
					break;
			}
		}

		/// <summary>
		/// Aktualizuje biezaca pozycje strukturalna w kontekscie na podstawie
		/// numeru zbudowanej encji. Ustawienie poziomu resetuje podrzedne
		/// (np. SetArticle zeruje ust/pkt/lit/tir).
		/// </summary>
		private static void UpdateStructuralReference(ParsingContext context, BaseEntity entity)
		{
			var numberValue = entity.Number?.Value;
			if (string.IsNullOrEmpty(numberValue))
				return;

			var reference = context.CurrentStructuralReference;
			switch (entity.UnitType)
			{
				case UnitType.Article:
					reference.SetArticle(numberValue);
					break;
				case UnitType.Paragraph:
					reference.SetParagraph(numberValue);
					break;
				case UnitType.Point:
					reference.SetPoint(numberValue);
					break;
				case UnitType.Letter:
					reference.SetLetter(numberValue);
					break;
				case UnitType.Tiret:
					reference.SetTiret(numberValue);
					break;
			}
		}

		/// <summary>
		/// Wykrywa cele nowelizacji w tresci encji implementujacej IHasAmendments.
		/// Parsuje wzorce typu "w art. 5", "po ust. 2" itp. i zapisuje
		/// wykryty cel w kontekscie (DetectedAmendmentTargets).
		/// </summary>
		private static void DetectAmendmentTargets(ParsingContext context, BaseEntity entity)
		{
			if (entity is not IHasAmendments)
				return;

			if (string.IsNullOrWhiteSpace(entity.ContentText))
				return;

			var targetRef = new StructuralReference();
			context.ReferenceService.UpdateLegalReference(targetRef, entity.ContentText);

			// Sprawdz czy wykryto jakikolwiek cel nowelizacji
			if (targetRef.Article == null && targetRef.Paragraph == null &&
				targetRef.Point == null && targetRef.Letter == null && targetRef.Tiret == null)
			{
				return;
			}

			var amendmentRef = new StructuralAmendmentReference
			{
				Structure = targetRef,
				RawText = entity.ContentText
			};

			context.DetectedAmendmentTargets[entity.Guid] = amendmentRef;

			Log.Debug("Wykryto cel nowelizacji w {UnitType} [{EntityId}]: {AmendmentTarget}",
				entity.UnitType, entity.Id, amendmentRef);
		}

		/// <summary>
		/// Aktualizuje stan kontekstu czy jestesmy wewnatrz cudzysłowu (treść nowelizacji).
		/// Zwroty typu "otrzymuje brzmienie:" lub "w brzmieniu:" otwieraja cudzysłów,
		/// a zamkniety cudzysłów konczy nowelizacje.
		/// </summary>
		private static void UpdateQuotationState(ParsingContext context, string text, ClassificationResult classification)
		{
			// Automatycznie traktuj akapity ze stylami Z/... jako nowelizacje
			if (classification.IsAmendmentContent)
			{
				context.InsideQuotation = true;
				return;
			}

			// Sprawdz czy tekst zawiera zwroty otwierajace nowelizacje
			if (text.Contains("otrzymuje brzmienie:", StringComparison.OrdinalIgnoreCase) ||
				text.Contains("w brzmieniu:", StringComparison.OrdinalIgnoreCase))
			{
				// Rozpoczynamy nowelizacje - spodziewamy sie cudzysłowow
				context.InsideQuotation = true;
				Log.Debug("Wykryto rozpoczecie nowelizacji: {Trigger}", text.Length > 80 ? text.Substring(0, 80) + "..." : text);
				return;
			}

			// Zlicz cudzysłowy w tekscie
			int openQuotes = 0;
			int closeQuotes = 0;
			foreach (char c in text)
			{
				if (c == '"' || c == '\u201C') // " lub cudzysłow otwierajacy
					openQuotes++;
				else if (c == '"' || c == '\u201D') // " lub cudzysłow zamykajacy
					closeQuotes++;
			}

			// Prosta logika: jezeli zamykamy więcej niż otwieramy, wychodzimy z nowelizacji
			if (context.InsideQuotation && closeQuotes > openQuotes)
			{
				context.InsideQuotation = false;
				Log.Debug("Zamknieto nowelizacje (cudzysłow zamykajacy)");
			}
		}
	}
}
