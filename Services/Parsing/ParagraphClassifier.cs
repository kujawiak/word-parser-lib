using System;
using System.Text.RegularExpressions;

namespace WordParserLibrary.Services.Parsing
{
	public enum ParagraphKind
	{
		Article,
		Paragraph,
		Point,
		Letter,
		Tiret,
		Unknown
	}

	public sealed class ClassificationResult
	{
		public ParagraphKind Kind { get; set; } = ParagraphKind.Unknown;
		public string? StyleType { get; set; }
		public bool UsedFallback { get; set; }
		public bool StyleTextConflict { get; set; }
	}

	public sealed class ParagraphClassifier
	{
		public ClassificationResult Classify(string text, string? styleId)
		{
			var styleType = GetStyleType(styleId);
			var isArticleByText = IsArticleByText(text);
			var isParagraphByText = IsParagraphByText(text);
			var isPointByText = IsPointByText(text);
			var isLetterByText = IsLetterByText(text);
			var isTiretByText = IsTiretByText(text);

			var result = new ClassificationResult
			{
				StyleType = styleType
			};

			if (isArticleByText)
			{
				result.Kind = ParagraphKind.Article;
				result.UsedFallback = styleType == null || !styleType.Equals("ART", StringComparison.OrdinalIgnoreCase);
				result.StyleTextConflict = styleType != null && !styleType.Equals("ART", StringComparison.OrdinalIgnoreCase);
				return result;
			}

			if (isParagraphByText || (!isPointByText && !isLetterByText && !isTiretByText && styleType == "UST"))
			{
				result.Kind = ParagraphKind.Paragraph;
				result.UsedFallback = isParagraphByText;
				result.StyleTextConflict = isParagraphByText && styleType != null && styleType != "UST";
				return result;
			}

			if (isPointByText || (!isParagraphByText && !isLetterByText && !isTiretByText && styleType == "PKT"))
			{
				result.Kind = ParagraphKind.Point;
				result.UsedFallback = isPointByText;
				result.StyleTextConflict = isPointByText && styleType != null && styleType != "PKT";
				return result;
			}

			if (isLetterByText || (!isParagraphByText && !isPointByText && !isTiretByText && styleType == "LIT"))
			{
				result.Kind = ParagraphKind.Letter;
				result.UsedFallback = isLetterByText;
				result.StyleTextConflict = isLetterByText && styleType != null && styleType != "LIT";
				return result;
			}

			if (isTiretByText || (!isParagraphByText && !isPointByText && !isLetterByText && styleType == "TIR"))
			{
				result.Kind = ParagraphKind.Tiret;
				result.UsedFallback = isTiretByText;
				result.StyleTextConflict = isTiretByText && styleType != null && styleType != "TIR";
				return result;
			}

			return result;
		}

		public static string? GetStyleType(string? styleId)
		{
			if (string.IsNullOrEmpty(styleId))
			{
				return null;
			}

			if (styleId.StartsWith("ART", StringComparison.OrdinalIgnoreCase))
			{
				return "ART";
			}
			if (styleId.StartsWith("UST", StringComparison.OrdinalIgnoreCase))
			{
				return "UST";
			}
			if (styleId.StartsWith("PKT", StringComparison.OrdinalIgnoreCase))
			{
				return "PKT";
			}
			if (styleId.StartsWith("LIT", StringComparison.OrdinalIgnoreCase))
			{
				return "LIT";
			}
			if (styleId.StartsWith("TIR", StringComparison.OrdinalIgnoreCase))
			{
				return "TIR";
			}

			return null;
		}

		public static bool IsArticleByText(string text)
		{
			return Regex.IsMatch(text.Trim(), "^Art\\.?\\s*\\d+", RegexOptions.IgnoreCase);
		}

		public static bool IsParagraphByText(string text)
		{
			return Regex.IsMatch(text, "^\\d+[a-zA-Z]*\\.\\s+", RegexOptions.IgnoreCase);
		}

		public static bool IsPointByText(string text)
		{
			return Regex.IsMatch(text, "^\\d+[a-zA-Z]*\\)\\s+", RegexOptions.IgnoreCase);
		}

		public static bool IsLetterByText(string text)
		{
			return Regex.IsMatch(text, "^[a-zA-Z]{1,5}\\)\\s+", RegexOptions.IgnoreCase);
		}

		public static bool IsTiretByText(string text)
		{
			return Regex.IsMatch(text, "^\\u2013+\\s+");
		}
	}
}
