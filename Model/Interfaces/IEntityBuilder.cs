using WordParserLibrary.Model.Schemas;

namespace WordParserLibrary.Model.Interfaces
{
    /// <summary>
    /// Interfejs dla budowniczów encji modelu.
    /// Ujednolica proces tworzenia encji z logiki parsowania dokumentu.
    /// </summary>
    public interface IEntityBuilder<out TDto> where TDto : BaseEntityDto
    {
        /// <summary>
        /// Buduje encję DTO z paragrafu dokumentu.
        /// </summary>
        TDto Build(DocumentFormat.OpenXml.Wordprocessing.Paragraph paragraph);
    }

    /// <summary>
    /// Specjalna wersja dla ArticleDto, która wymaga referencji do LegalAct.
    /// </summary>
    public interface IArticleBuilder
    {
        ArticleDto Build(DocumentFormat.OpenXml.Wordprocessing.Paragraph paragraph, object legalAct);
    }
}
