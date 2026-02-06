namespace WordParserLibrary.Services.Parsing
{
	public interface IParagraphClassifier
	{
		ClassificationResult Classify(string text, string? styleId);
	}
}
