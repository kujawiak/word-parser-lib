namespace WordParserLibrary.Model.Schemas
{
    /// <summary>
    /// Model litery punktu - zawiera definicję struktury bez logiki parsowania.
    /// Litera zawiera tirets lub tekst bezpośrednio.
    /// Numer litery przechowywany jest w EntityNumberDto (dziedziczony z BaseEntityDto),
    /// gdzie część liczbowa jest pusta, a wartość zawiera symbol litery (np. "a", "b", "aa" itp.).
    /// </summary>
    public class LetterDto : BaseEntityDto
    {
        public List<TiretDto> Tirets { get; set; } = new();
        public List<AmendmentDto> Amendments { get; set; } = new();

        public LetterDto()
        {
            UnitType = UnitType.Letter;
            EIdPrefix = "lit";
            DisplayLabel = "lit";
        }
    }
}
