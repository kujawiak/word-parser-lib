using System;
using System.Linq;
using DocumentFormat.OpenXml.Packaging;
using Word = DocumentFormat.OpenXml.Wordprocessing;
using ModelDto;
using ModelDto.EditorialUnits;
using ModelDto.SystematizingUnits;
using Serilog;

namespace WordParserLibrary
{
	public static class LegalDocumentParser
	{
		public static LegalDocument Parse(string filePath)
		{
			using var wordDoc = WordprocessingDocument.Open(filePath, false);
			return Parse(wordDoc);
		}

		public static LegalDocument Parse(WordprocessingDocument wordDocument)
		{
			var mainPart = wordDocument.MainDocumentPart ??
				throw new InvalidOperationException("MainDocumentPart is null.");

			var document = new LegalDocument();
			var subchapter = GetDefaultSubchapter(document);

			foreach (var paragraph in mainPart.Document.Descendants<Word.Paragraph>())
			{
				if (!IsArticleParagraph(paragraph))
				{
					continue;
				}

				var article = new Article
				{
					Parent = subchapter,
					ContentText = paragraph.InnerText.Sanitize().Trim()
				};

				subchapter.Articles.Add(article);
			}

			return document;
		}

		private static Subchapter GetDefaultSubchapter(LegalDocument document)
		{
			var part = document.RootPart;
			var book = part.Books.First();
			var title = book.Titles.First();
			var division = title.Divisions.First();
			var chapter = division.Chapters.First();
			return chapter.Subchapters.First();
		}

		private static bool IsArticleParagraph(Word.Paragraph paragraph)
		{
            Log.Debug("[IsArticleParagraph] Styl: {StyleId} paragraf: {ParagraphText}", paragraph.StyleId(), paragraph.InnerText);
			if (paragraph.StyleId("ART") == true)
			{
				return true;
			}

			var text = paragraph.InnerText?.Trim();
			if (string.IsNullOrEmpty(text))
			{
				return false;
			}

			return text.StartsWith("Art.", StringComparison.Ordinal);
		}
	}
}
