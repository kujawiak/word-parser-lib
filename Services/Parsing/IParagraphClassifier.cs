namespace WordParserLibrary.Services.Parsing
{
	/// <summary>
	/// Kontrakt klasyfikatora akapitow w parserze.
	/// </summary>
	public interface IParagraphClassifier
	{
		ClassificationResult Classify(string text, string? styleId);
	}
}
