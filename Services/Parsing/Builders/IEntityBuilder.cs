namespace WordParserLibrary.Services.Parsing.Builders
{
	public interface IEntityBuilder<in TInput, out TResult>
	{
		TResult Build(TInput input);
	}
}
