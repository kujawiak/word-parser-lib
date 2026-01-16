using System.Xml.Linq;
using WordParserLibrary.Model.Schemas;

namespace WordParserLibrary.Model.Interfaces
{
    /// <summary>
    /// Interfejs dla konwerterów XML.
    /// Ujednolica proces konwersji encji DTO do formatu XML.
    /// </summary>
    public interface IXmlConverter<in TDto> where TDto : BaseEntityDto
    {
        XElement ToXml(TDto entity, bool generateGuids = false);
    }
}
