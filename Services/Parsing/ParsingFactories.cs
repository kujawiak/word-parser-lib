using System.Text.RegularExpressions;
using ModelDto;
using DtoArticle = ModelDto.EditorialUnits.Article;
using DtoLetter = ModelDto.EditorialUnits.Letter;
using DtoParagraph = ModelDto.EditorialUnits.Paragraph;
using DtoPoint = ModelDto.EditorialUnits.Point;

namespace WordParserLibrary.Services.Parsing
{
	/// <summary>
	/// Fabryki pomocnicze dla parsowania: tworzenie encji i parsowanie numerow.
	/// </summary>
	public static class ParsingFactories
	{
		public static DtoParagraph CreateImplicitParagraph(DtoArticle article)
		{
			return new DtoParagraph
			{
				Parent = article,
				Article = article,
				ContentText = string.Empty,
				IsImplicit = true
			};
		}

		public static DtoParagraph CreateParagraphFromArticleTail(DtoArticle article, string tail)
		{
			var normalizedTail = tail?.Trim() ?? string.Empty;
			if (string.IsNullOrWhiteSpace(normalizedTail))
			{
				return CreateImplicitParagraph(article);
			}

			var match = Regex.Match(normalizedTail, "^(\\d+[a-zA-Z]*)\\.\\s+", RegexOptions.IgnoreCase);
			if (match.Success)
			{
				return new DtoParagraph
				{
					Parent = article,
					Article = article,
					ContentText = normalizedTail,
					Number = CreateNumber(match.Groups[1].Value),
					IsImplicit = false
				};
			}

			return new DtoParagraph
			{
				Parent = article,
				Article = article,
				ContentText = normalizedTail,
				IsImplicit = true
			};
		}

		public static DtoPoint CreateImplicitPoint(DtoParagraph? paragraph, DtoArticle article)
		{
			return new DtoPoint
			{
				Parent = paragraph,
				Article = article,
				Paragraph = paragraph,
				ContentText = string.Empty
			};
		}

		public static DtoLetter CreateImplicitLetter(DtoPoint point, DtoParagraph? paragraph, DtoArticle article)
		{
			return new DtoLetter
			{
				Parent = point,
				Article = article,
				Paragraph = paragraph,
				Point = point,
				ContentText = string.Empty
			};
		}

		public static EntityNumber? ParseArticleNumber(string? text)
		{
			if (string.IsNullOrWhiteSpace(text))
			{
				return null;
			}

			var match = Regex.Match(text, "^Art\\.?\\s*(\\d+[a-zA-Z]*)", RegexOptions.IgnoreCase);
			if (match.Success)
			{
				return CreateNumber(match.Groups[1].Value);
			}

			return null;
		}

		public static EntityNumber? ParseParagraphNumber(string? text)
		{
			if (string.IsNullOrWhiteSpace(text))
			{
				return null;
			}

			var match = Regex.Match(text.Trim(), "^(\\d+[a-zA-Z]*)\\.\\s+", RegexOptions.IgnoreCase);
			if (match.Success)
			{
				return CreateNumber(match.Groups[1].Value);
			}

			return null;
		}

		public static EntityNumber? ParsePointNumber(string? text)
		{
			if (string.IsNullOrWhiteSpace(text))
			{
				return null;
			}

			var match = Regex.Match(text.Trim(), "^(\\d+[a-zA-Z]*)\\)\\s*", RegexOptions.IgnoreCase);
			if (match.Success)
			{
				return CreateNumber(match.Groups[1].Value);
			}

			return null;
		}

		public static EntityNumber? ParseLetterNumber(string? text)
		{
			if (string.IsNullOrWhiteSpace(text))
			{
				return null;
			}

			var match = Regex.Match(text.Trim(), "^([a-zA-Z]{1,5})\\)\\s*", RegexOptions.IgnoreCase);
			return match.Success ? CreateNumber(match.Groups[1].Value) : null;
		}

		public static EntityNumber CreateNumber(string rawValue)
		{
			var number = new EntityNumber
			{
				RawValue = rawValue,
				Value = rawValue
			};

			var match = Regex.Match(rawValue, "^(\\d+)([a-zA-Z]*)$");
			if (match.Success)
			{
				if (int.TryParse(match.Groups[1].Value, out var numeric))
				{
					number.NumericPart = numeric;
				}
				number.LexicalPart = match.Groups[2].Value;
			}

			return number;
		}

		public static string GetArticleTail(string text)
		{
			var match = Regex.Match(text.Trim(), "^Art\\.?\\s*\\d+[a-zA-Z]*\\.?\\s*(.*)$", RegexOptions.IgnoreCase);
			return match.Success ? match.Groups[1].Value : string.Empty;
		}
	}
}
