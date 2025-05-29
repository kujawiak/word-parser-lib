using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using WordParserLibrary.Model;

// --- Modele danych ---

/// <summary>
/// Typ operacji nowelizacyjnej.
/// </summary>
public enum AmendingOperation
{
    MODIFICATION, // "otrzymuje brzmienie", "zmienia się"
    ADDITION,     // "dodaje się"
    DELETION,     // "skreśla się", "uchyla się"
    REPLACEMENT,  // "zastępuje się" (np. wyraz innym wyrazem)
    REFERENCE     // Odniesienie, bez bezpośredniej zmiany treści
}



/// <summary>
/// Informacje o nowelizowanej jednostce redakcyjnej.
/// </summary>
public class AmendedUnitInfo
{
    public string Article { get; set; }
    public string Subsection { get; set; } // Ustęp
    public string Point { get; set; }
    public string Letter { get; set; }
    public string Tiret { get; set; } // Tiret

    public AmendingOperation Operation { get; set; }
    public string OriginalMatchedText { get; set; } // Fragment tekstu, który został dopasowany, np. "ust. 13b"
    public string TaggedMatchedText { get; set; } // Fragment tekstu z tagami, np. "<sub>ust. 13b</sub>"

    public string FullAddress
    {
        get
        {
            var parts = new List<string>();
            if (!string.IsNullOrEmpty(Article)) parts.Add($"art. {Article}");
            if (!string.IsNullOrEmpty(Subsection)) parts.Add($"ust. {Subsection}");
            if (!string.IsNullOrEmpty(Point)) parts.Add($"pkt {Point}");
            if (!string.IsNullOrEmpty(Letter)) parts.Add($"lit. {Letter}");
            if (!string.IsNullOrEmpty(Tiret)) parts.Add($"tiret {Tiret}");
            return string.Join(" ", parts);
        }
    }

    public override string ToString()
    {
        return $"Adres: [{FullAddress}], Operacja: {Operation}, Dopasowano: '{OriginalMatchedText}' -> '{TaggedMatchedText}'";
    }
}

/// <summary>
/// Wynik parsowania dla pojedynczego akapitu (lub fragmentu tekstu).
/// </summary>
public class ParagraphParseResult
{
    public string OriginalText { get; set; }
    public string ProcessedText { get; set; } // Tekst po wszystkich transformacjach (tagowaniu)
    public List<JournalInfo> Journals { get; set; } = new List<JournalInfo>();
    public List<AmendedUnitInfo> AmendedUnits { get; set; } = new List<AmendedUnitInfo>();
}

public class ParagraphParser
{
    private string _currentArticleContext; // Kontekst artykułu dla jednostek niższego rzędu

    // Regex do parsowania publikatorów (Dz. U., Dz. Urz.)
    // (?:Dz\.\s*U\.\s*(?:z\s*(?<year>\d{4})\s*r\.?)?\s*poz\.\s*(?<positions>[\d,\sandi]+(?:,\s*[\d,\sandi]+)*))
    // Ulepszony regex dla pozycji, aby lepiej łapać listy
    private static readonly Regex JournalRegex = new Regex(
        @"(?<source>Dz\.\s*U\.\s*(?:z\s*(?<year>\d{4})\s*r\.?)?\s*poz\.\s*(?<positions>[\d,\sandi]+(?:,\s*[\d,\sandi]+)*))",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );
    //@"(?<source>(?:Dz\.\s*U\.\s*(?:z\s*(?<year>\d{4})\s*r\.?)?\s*poz\.\s*(?<positions>[\d,\sandi]+(?:,\s*[\d,\sandi]+)*))" + // Dz.U.
    //@"|(?:Dz\.\s*Urz\.\s*(?<publisherShort>[A-ZŁŚŻŹĆŃĘÓĄ\s]+?)\s*(?:z\s*(?<yearUrzedowy>\d{4})\s*r\.?)?\s*(?:Nr\s*(?<numberUrzedowy>\d+)\s*)?poz\.\s*(?<positionsUrzedowy>[\d,\sandi]+(?:,\s*[\d,\sandi]+)*)))", // Dz.Urz.

    public void ParseParagraph(BaseEntity entity)
    {
        if (entity is Article)
        {
            ParseJournalReferences(entity as Article);
            ParseOrdinalNumber(entity as Article);
        }
    }

    private void ParseOrdinalNumber(Article article)
    {
        if (article == null || string.IsNullOrEmpty(article.ContentText))
            return;
        var match = Regex.Match(article.ContentText, @"^(Art\.|§)\s*([\w\d]+)\.?\s*(.*)");
        if (match.Success)
        {
            article.Number = match.Groups[2].Value.Trim();
            article.ContentText = match.Groups[3].Value.Trim();
        }
        else
        {
            throw new FormatException("Oczekiwano formatu: Art. X.\nMożliwy błędny styl artykułu.");
        }
    }

    public ParagraphParseResult ParseParagraph(string text, string currentArticleContext = null)
    {
        _currentArticleContext = currentArticleContext;
        var result = new ParagraphParseResult
        {
            OriginalText = text,
            ProcessedText = text // Inicjalnie ProcessedText jest taki sam jak OriginalText
        };

        // Kolejność operacji jest ważna, aby uniknąć konfliktów między regexami
        // i aby tagowanie nie zakłócało kolejnych dopasowań.

        // 1. Parsowanie informacji o publikatorach
        //ParseJournalReferences(result);

        // 2. Parsowanie złożonych zmian (np. "w ust. X pkt Y-Z otrzymują brzmienie:")
        // Ten regex powinien być wywoływany przed prostszymi, aby złapać bardziej specyficzne konstrukcje.
        ParseComplexAmendmentWithRange(result, "ust", "pkt", "<sub>", "<point>", _currentArticleContext);
        ParseComplexAmendmentWithRange(result, "art", "ust", "<art>", "<sub>", null); // Dla "w art. X ust. Y..."

        // 3. Parsowanie prostych zmian (np. "ust. X otrzymuje brzmienie:", "art. Y zmienia się")
        // Dla ustępów (z kontekstem artykułu)
        ParseSimpleAmendment(result, "ust", @"(ust\.\s*(?<id>[\w\d\.]+))", "<sub>", parentArticle: _currentArticleContext);
        // Dla punktów (wymaga kontekstu artykułu i ustępu - bardziej złożone, na razie uproszczenie)
        // ParseSimpleAmendment(result, "pkt", @"(pkt\.\s*(?<id>[\w\d\.]+))", "<point>", parentArticle: _currentArticleContext, parentSubsection: _currentSubsectionContext);
        // Dla artykułów (gdy np. "art. X otrzymuje brzmienie:")
        ParseSimpleAmendment(result, "art", @"(art\.\s*(?<id>[\w\d\.]+))", "<art>");


        // 4. Parsowanie odniesień do artykułów (np. "w art. X"), jeśli nie zostały już objęte
        // Ten regex jest bardziej ogólny i powinien być stosowany ostrożnie, aby nie tagować czegoś, co już zostało otagowane
        // lub co jest częścią większej, jeszcze nieprzetworzonej struktury.
        // Na potrzeby przykładu "w art. 2" -> tag <art>art. 2</art>
        ParseStandaloneArticleReference(result);


        return result;
    }

    private void ParseJournalReferences(Article article)
    {
        if (article == null || string.IsNullOrEmpty(article.ContentText))
            return;

        var currentYear = DateTime.Now.Year;
        var journalsByYear = new Dictionary<int, JournalInfo>();

        // Przeszukaj tekst artykułu pod kątem publikatorów "Dz. U."
        foreach (Match m in JournalRegex.Matches(article.ContentText))
        {
            string sourceMatch = m.Groups["source"].Value;
            string yearStr = m.Groups["year"].Success ? m.Groups["year"].Value : null;
            int year = string.IsNullOrEmpty(yearStr) ? currentYear : int.Parse(yearStr);

            string positionsStr = m.Groups["positions"].Success ? m.Groups["positions"].Value : "";

            if (!journalsByYear.TryGetValue(year, out var journalInfo))
            {
                journalInfo = new JournalInfo { Year = year, SourceString = sourceMatch };
                journalsByYear[year] = journalInfo;
                article.Journals.Add(journalInfo); // <-- Uzupełniamy listę Journals w Article
            }
            else
            {
                if (sourceMatch.Length > journalInfo.SourceString.Length)
                {
                    journalInfo.SourceString = sourceMatch;
                }
            }

            var positionNumbers = positionsStr.Split(new[] { ',', ' ', 'i' }, StringSplitOptions.RemoveEmptyEntries)
                                            .Select(s => int.TryParse(s.Trim(), out int num) ? num : (int?)null)
                                            .Where(n => n.HasValue)
                                            .Select(n => n.Value)
                                            .ToList();
            journalInfo.Positions.AddRange(positionNumbers);
            journalInfo.Positions = journalInfo.Positions.Distinct().OrderBy(p => p).ToList();
        }
    }
    private void ParseJournalReferences(ParagraphParseResult result)
    {
        var currentYear = DateTime.Now.Year;
        // Używamy słownika, aby grupować pozycje dla tego samego roku z różnych dopasowań (choć rzadkie dla jednego akapitu)
        var journalsByYearAndType = new Dictionary<Tuple<int, string>, JournalInfo>();

        result.ProcessedText = JournalRegex.Replace(result.ProcessedText, m =>
        {
            string sourceMatch = m.Groups["source"].Value;
            string yearStr = m.Groups["year"].Success ? m.Groups["year"].Value : (m.Groups["yearUrzedowy"].Success ? m.Groups["yearUrzedowy"].Value : null);
            int year = string.IsNullOrEmpty(yearStr) ? currentYear : int.Parse(yearStr);

            string positionsStr = m.Groups["positions"].Success ? m.Groups["positions"].Value : m.Groups["positionsUrzedowy"].Value;

            // Klucz dla słownika, aby odróżnić np. Dz.U. od Dz.Urz. tego samego roku
            string publisherType = m.Groups["publisherShort"].Success ? m.Groups["publisherShort"].Value.Trim() : "Dz.U.";
            var key = Tuple.Create(year, publisherType);

            if (!journalsByYearAndType.TryGetValue(key, out var journalInfo))
            {
                journalInfo = new JournalInfo { Year = year, SourceString = sourceMatch };
                journalsByYearAndType[key] = journalInfo;
                result.Journals.Add(journalInfo); // Dodajemy do listy wyników od razu
            }
            else
            {
                // Jeśli już istnieje wpis dla tego roku i typu, możemy zaktualizować SourceString, jeśli nowe dopasowanie jest pełniejsze
                // lub po prostu dodać pozycje. Dla uproszczenia, zakładamy, że pierwsze dopasowanie jest reprezentatywne dla SourceString.
                // Jeśli to kolejne wystąpienie tego samego publikatora w tekście, SourceString może być inny.
                // Można by dodać logikę tworzenia nowego JournalInfo jeśli SourceString jest znacząco inny.
                // Na razie aktualizujemy SourceString jeśli nowy jest dłuższy (bardziej kompletny)
                if (sourceMatch.Length > journalInfo.SourceString.Length)
                {
                    journalInfo.SourceString = sourceMatch;
                }
            }

            var positionNumbers = positionsStr.Split(new[] { ',', ' ', 'i' }, StringSplitOptions.RemoveEmptyEntries)
                                              .Select(s => int.TryParse(s.Trim(), out int num) ? num : (int?)null)
                                              .Where(n => n.HasValue)
                                              .Select(n => n.Value)
                                              .ToList();
            journalInfo.Positions.AddRange(positionNumbers);
            journalInfo.Positions = journalInfo.Positions.Distinct().OrderBy(p => p).ToList();

            // Nie zmieniamy tekstu, tylko zbieramy informacje
            return m.Value;
        });
    }

    private void ParseStandaloneArticleReference(ParagraphParseResult result)
    {
        // Wzorzec dla "w art. X" - powinien być ostrożny, aby nie kolidować z bardziej złożonymi regułami.
        // (?<!<art>)(?<!<\/art>) - próba uniknięcia tagowania wewnątrz już istniejących tagów <art> (uproszczone)
        // Lepszym podejściem byłoby parsowanie na surowym tekście i budowanie nowego z tagami.
        // Na razie, zakładamy, że ten regex działa na tekście, gdzie "art. X" nie jest jeszcze otagowane jako część większej struktury.
        string pattern = @"(?i)\b(w\s+(art\.\s*(?<article_id>[\w\d\.]+)))\b"; 
        // Ten regex łapie "w art. X". Grupa 2 to "art. X", grupa "article_id" to X.

        result.ProcessedText = Regex.Replace(result.ProcessedText, pattern, m => {
            string articleId = m.Groups["article_id"].Value;
            string matchedArticlePart = m.Groups[2].Value; // "art. X"
            string fullMatch = m.Value; // "w art. X"

            // Sprawdzenie, czy ten fragment nie jest już częścią innego, bardziej szczegółowego dopasowania (uproszczone)
            // To jest trudne do zrobienia tylko z Regex.Replace. Idealnie, stan parsowania by to śledził.
            // Na razie zakładamy, że jeśli dopasuje, to jest to samodzielne odniesienie.

            string taggedArticlePart = $"<art>{matchedArticlePart}</art>";
            
            result.AmendedUnits.Add(new AmendedUnitInfo
            {
                Article = articleId,
                Operation = AmendingOperation.REFERENCE, // Jest to odniesienie
                OriginalMatchedText = matchedArticlePart, // "art. X"
                TaggedMatchedText = taggedArticlePart 
            });
            // Zwracamy cały dopasowany fragment z otagowaną częścią "art. X"
            return fullMatch.Replace(matchedArticlePart, taggedArticlePart);
        });
    }

    private void ParseSimpleAmendment(ParagraphParseResult result, string unitType, string unitPattern, string tagName,
                                      string parentArticle = null, string parentSubsection = null, string parentPoint = null, string parentLetter = null)
    {
        // unitPattern to np. (ust\.\s*(?<id>[\w\d\.]+))
        // operacje: otrzymuje brzmienie, zmienia się, dodaje się, skreśla się, uchyla się
        string operationPattern = @"(?<operation_verb>otrzymuje\s+brzmienie|otrzymują\s+brzmienie|zmienia\s+się|dodaje\s+się|skreśla\s+się|uchyla\s+się)";
        string fullPattern = $@"(?i)\b{unitPattern}\s+{operationPattern}\b";
        // Dodano \b na końcu, aby uniknąć częściowych dopasowań np. "otrzymuje brzmienie cośtam"

        result.ProcessedText = Regex.Replace(result.ProcessedText, fullPattern, m => {
            string id = m.Groups["id"].Value;
            string originalUnitMatch = m.Groups[1].Value; // Grupa obejmująca np. "ust. 13b" lub "art. 2"
            string operationVerb = m.Groups["operation_verb"].Value.ToLower();
            string restOfMatch = m.Value.Substring(originalUnitMatch.Length); // np. " otrzymuje brzmienie:"

            AmendingOperation operation;
            if (operationVerb.Contains("otrzymuj") || operationVerb.Contains("zmienia"))
                operation = AmendingOperation.MODIFICATION;
            else if (operationVerb.Contains("dodaje"))
                operation = AmendingOperation.ADDITION;
            else if (operationVerb.Contains("skreśla") || operationVerb.Contains("uchyla"))
                operation = AmendingOperation.DELETION;
            else
                operation = AmendingOperation.MODIFICATION; // Domyślnie

            var amendedUnit = new AmendedUnitInfo
            {
                Operation = operation,
                OriginalMatchedText = originalUnitMatch,
                TaggedMatchedText = $"<{tagName}>{originalUnitMatch}</{tagName}>"
            };

            switch (unitType.ToLower())
            {
                case "art":
                    amendedUnit.Article = id;
                    break;
                case "ust":
                    amendedUnit.Article = parentArticle; // Kontekst nadrzędnego artykułu
                    amendedUnit.Subsection = id;
                    break;
                case "pkt":
                    amendedUnit.Article = parentArticle;
                    amendedUnit.Subsection = parentSubsection;
                    amendedUnit.Point = id;
                    break;
                // Dodaj litery, tirety itd. w miarę potrzeb
            }
            
            // Obsługa zakresów np. "1-3" dla punktów (jeśli 'id' zawiera myślnik)
            // To jest uproszczona obsługa zakresów w ramach "SimpleAmendment".
            // "ComplexAmendmentWithRange" jest bardziej dedykowany.
            var rangeParts = id.Split('-');
            if (rangeParts.Length == 2 && (unitType.ToLower() == "pkt" || unitType.ToLower() == "art" || unitType.ToLower() == "ust")) 
            {
                // Proste rozbicie dla jednostek numerycznych lub alfanumerycznych, gdzie początek i koniec są jasno określone.
                // Dla np. "pkt 1-3"
                if (int.TryParse(rangeParts[0], out int startNum) && int.TryParse(rangeParts[1], out int endNum) && startNum <= endNum)
                {
                    for (int i = startNum; i <= endNum; i++)
                    {
                        var unitCopy = new AmendedUnitInfo 
                        {
                           Article = amendedUnit.Article,
                           Subsection = amendedUnit.Subsection,
                           Point = amendedUnit.Point, // Skopiuj bazowe wartości
                           Letter = amendedUnit.Letter,
                           Tiret = amendedUnit.Tiret,
                           Operation = amendedUnit.Operation,
                           OriginalMatchedText = $"{unitType}. {i}", // np. "pkt 1"
                           TaggedMatchedText = $"<{tagName}>{unitType}. {i}</{tagName}>" // Indywidualne tagowanie
                        };
                        // Ustaw odpowiednie pole na podstawie unitType
                        if (unitType.ToLower() == "pkt") unitCopy.Point = i.ToString();
                        else if (unitType.ToLower() == "ust") unitCopy.Subsection = i.ToString();
                        else if (unitType.ToLower() == "art") unitCopy.Article = i.ToString();
                        // Jeśli OriginalMatchedText i TaggedMatchedText mają odzwierciedlać cały zakres, to:
                        // unitCopy.OriginalMatchedText = originalUnitMatch;
                        // unitCopy.TaggedMatchedText = amendedUnit.TaggedMatchedText;
                        result.AmendedUnits.Add(unitCopy);
                    }
                     // Zwracamy tekst z otagowanym całym zakresem i resztą dopasowania
                    return $"{amendedUnit.TaggedMatchedText}{restOfMatch}";
                }
                else // Zakres nie jest czysto numeryczny lub błędny, np. "1a-1c"
                {
                    // Dodajemy jako pojedynczy wpis z zakresem
                    result.AmendedUnits.Add(amendedUnit);
                    return $"{amendedUnit.TaggedMatchedText}{restOfMatch}";
                }
            }
            else // Pojedyncza jednostka
            {
                 result.AmendedUnits.Add(amendedUnit);
                 return $"{amendedUnit.TaggedMatchedText}{restOfMatch}";
            }
        });
    }

    private void ParseComplexAmendmentWithRange(ParagraphParseResult result,
        string parentUnitTypeIdentifier, string childUnitTypeIdentifier, // np. "ust", "pkt"
        string parentTag, string childTag, // np. "<sub>", "<point>"
        string articleContextForParent) // Kontekst artykułu dla jednostki nadrzędnej (jeśli sama nie jest artykułem)
    {
        // Wzorzec dla "w <parent_unit_type> <parent_id> <child_unit_type> <child_start>-<child_end> otrzymują brzmienie:"
        // lub "w <parent_unit_type> <parent_id> <child_unit_type> <child_id_single> otrzymuje brzmienie:"
        string pattern =
            $@"(?i)\b(?:w\s+)?({parentUnitTypeIdentifier}\.\s*(?<parent_id>[\w\d\.]+))\s+({childUnitTypeIdentifier}\.\s*(?<child_id_single>[\w\d\.]+)(?<!-)|{childUnitTypeIdentifier}\.\s*(?<child_id_start>[\w\d\.]+)\s*-\s*(?<child_id_end>[\w\d\.]+))\s+(?<operation_verb>otrzymuje\s+brzmienie|otrzymują\s+brzmienie|zmieniają\s+się|dodaje\s+się)\b";

        result.ProcessedText = Regex.Replace(result.ProcessedText, pattern, m =>
        {
            string parentId = m.Groups["parent_id"].Value;
            string originalParentMatch = m.Groups[1].Value; // np. "ust. 10" lub "art. 2"
            string taggedParentMatch = $"<{parentTag}>{originalParentMatch}</{parentTag}>";

            string originalChildFullMatch = m.Groups[2].Value; // np. "pkt 1-3" lub "pkt 1" lub "ust. 3"
            string taggedChildFullMatch = $"<{childTag}>{originalChildFullMatch}</{childTag}>";
            
            string operationVerb = m.Groups["operation_verb"].Value.ToLower();
            string restOfStatement = m.Value.Substring(m.Groups[0].Value.Length - operationVerb.Length -1); // " otrzymują brzmienie:" bez początkowej spacji

            List<string> childIdsToProcess = new List<string>();

            if (m.Groups["child_id_single"].Success)
            {
                childIdsToProcess.Add(m.Groups["child_id_single"].Value);
            }
            else // Zakres
            {
                string childStart = m.Groups["child_id_start"].Value;
                string childEnd = m.Groups["child_id_end"].Value;
                
                if (int.TryParse(childStart, out int startNum) && int.TryParse(childEnd, out int endNum) && startNum <= endNum)
                {
                    for (int i = startNum; i <= endNum; i++)
                    {
                        childIdsToProcess.Add(i.ToString());
                    }
                }
                else // Zakresy alfanumeryczne np. "1a-1c" lub "a-c"
                {
                    // Prosta obsługa dla liter a-c, a-z itp.
                    if (childStart.Length == 1 && childEnd.Length == 1 && char.IsLetter(childStart[0]) && char.IsLetter(childEnd[0]) && childStart[0] <= childEnd[0]) {
                        for (char c = childStart[0]; c <= childEnd[0]; c++) {
                            childIdsToProcess.Add(c.ToString());
                        }
                    } else {
                        // Bardziej złożone zakresy (np. "1a"-"1c") wymagałyby osobnej logiki
                        childIdsToProcess.Add($"{childStart}-{childEnd}"); // Dodajemy cały zakres jako jeden "id"
                    }
                }
            }

            AmendingOperation operation;
            if (operationVerb.Contains("otrzymuj") || operationVerb.Contains("zmieniaj"))
                operation = AmendingOperation.MODIFICATION;
            else if (operationVerb.Contains("dodaje"))
                operation = AmendingOperation.ADDITION;
            else
                operation = AmendingOperation.MODIFICATION; // Domyślnie


            foreach (var childId in childIdsToProcess)
            {
                var unit = new AmendedUnitInfo { Operation = operation };

                // Ustalanie adresu jednostki
                if (parentUnitTypeIdentifier.Equals("art", StringComparison.OrdinalIgnoreCase))
                {
                    unit.Article = parentId; // Rodzic to artykuł
                    if (childUnitTypeIdentifier.Equals("ust", StringComparison.OrdinalIgnoreCase)) unit.Subsection = childId;
                    else if (childUnitTypeIdentifier.Equals("pkt", StringComparison.OrdinalIgnoreCase)) unit.Point = childId; // Rzadkie: art. X pkt Y
                }
                else if (parentUnitTypeIdentifier.Equals("ust", StringComparison.OrdinalIgnoreCase))
                {
                    unit.Article = articleContextForParent; // Kontekst artykułu dla rodzica (ustępu)
                    unit.Subsection = parentId;
                    if (childUnitTypeIdentifier.Equals("pkt", StringComparison.OrdinalIgnoreCase)) unit.Point = childId;
                    else if (childUnitTypeIdentifier.Equals("lit", StringComparison.OrdinalIgnoreCase)) unit.Letter = childId;
                }
                // Dodaj inne kombinacje (pkt -> lit, lit -> tiret)

                // Dla celów logowania/identyfikacji.
                // OriginalMatchedText dla pojedynczej jednostki z zakresu powinien być bardziej szczegółowy.
                if (childIdsToProcess.Count > 1) { // Jeśli to był zakres
                     unit.OriginalMatchedText = $"{childUnitTypeIdentifier}. {childId}"; // np. "pkt 1"
                     unit.TaggedMatchedText = $"<{childTag}>{childUnitTypeIdentifier}. {childId}</{childTag}>"; // np. "<point>pkt 1</point>"
                } else { // Pojedyncza jednostka potomna
                     unit.OriginalMatchedText = originalChildFullMatch; // np. "pkt 1" lub "ust. 3"
                     unit.TaggedMatchedText = taggedChildFullMatch; // np. "<point>pkt 1</point>"
                }
                // Można też dodać informację o jednostce nadrzędnej w OriginalMatchedText, np.
                // unit.OriginalMatchedText = $"{originalParentMatch} {childUnitTypeIdentifier}. {childId}";

                result.AmendedUnits.Add(unit);
            }
            
            // Składanie zmodyfikowanego tekstu
            string prefix = "";
            if (m.Value.TrimStart().StartsWith("w ", StringComparison.OrdinalIgnoreCase)) {
                prefix = m.Value.Substring(0, m.Value.IndexOf(originalParentMatch[0])); // "w " lub spacje przed "w "
            } else {
                 prefix = m.Value.Substring(0, m.Value.IndexOf(originalParentMatch[0])); // Spacje przed np. "art. X"
            }
            
            return $"{prefix}{taggedParentMatch} {taggedChildFullMatch} {operationVerb}";
        });
    }
}