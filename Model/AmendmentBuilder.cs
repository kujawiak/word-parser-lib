using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace WordParserLibrary.Model
{
    public class AmendmentBuilder
    {
        public List<AmendmentOperation> Build(List<Amendment> amendments, BaseEntity baseEntity)
        {
            if (amendments == null || amendments.Count == 0)
                throw new ArgumentException("Amendments list cannot be null or empty.");

            if (baseEntity == null || string.IsNullOrEmpty(baseEntity.Content))
            {
                Console.WriteLine("BaseEntity or its Content cannot be null or empty.");
                return new List<AmendmentOperation>();
            }

            var amendmentOperations = new List<AmendmentOperation>();

            // Ustal typ operacji na podstawie treści Content obiektu BaseEntity
            var operationType = DetermineOperationType(baseEntity.Content);

            // Dopasowanie obiektów na podstawie wzorców
            var targets = ParseTargets(baseEntity.Content);

            // Utwórz operacje nowelizujące dla każdego targetu
            foreach (var target in targets)
            {
                var amendmentOperation = new AmendmentOperation
                {
                    Type = operationType.OperationType,
                    AmendmentTarget = baseEntity.LegalReference,
                    AmendmentObject = target,
                    AmendmentObjectType = DetermineAmendmentObjectType(baseEntity.Content)
                };
                //amendmentOperation.Amendments.AddRange(amendments);
                amendmentOperations.Add(amendmentOperation);
            }
            
            if (amendmentOperations.Count == 1)
            {
                amendmentOperations.First().Amendments.AddRange(amendments);
            }
            else if (amendmentOperations.Count > 1)
            {
                var allAmendmets = amendments.ToList();
                var firstAmendment = allAmendmets.FirstOrDefault();
                var style = firstAmendment?.Paragraph != null ? firstAmendment.Paragraph.StyleId() : null;
                foreach (var operation in amendmentOperations)
                {
                    operation.Amendments.Add(allAmendmets.First());
                    allAmendmets.RemoveAt(0);
                    while (allAmendmets.Any())
                    {
                        var amendment = allAmendmets.First();
                        if (amendment.Paragraph?.StyleId() == style)
                            break;

                        operation.Amendments.Add(amendment);
                        allAmendmets.RemoveAt(0);
                    }
                }
            }
            return amendmentOperations;
        }
        private (AmendmentOperationType OperationType, string? NewObject) DetermineOperationType(string content)
        {
            var repealMatch = Regex.Match(content, @"uchyla się (?<newObject>.*?)(?=[;.,]$)");
            if (repealMatch.Success)
            return (AmendmentOperationType.Repeal, repealMatch.Groups["newObject"].Value);

            var insertionMatch = Regex.Match(content, @"dodaje się (?<newObject>.*?) w brzmieniu:");
            if (insertionMatch.Success)
            return (AmendmentOperationType.Insertion, insertionMatch.Groups["newObject"].Value);

            var modificationMatch = Regex.Match(content, @"\b(art\.|ust\.|pkt)\s*\d+[a-zA-Z]?\b(?=.*otrzymuj[e,ą] brzmienie:)");
            if (modificationMatch.Success)
            return (AmendmentOperationType.Modification, modificationMatch.Value);

            var letterModificationMatch = Regex.Match(content, @"lit\.\s*[a-zA-Z]+(?=.*otrzymuj[e,ą] brzmienie:)");
            if (letterModificationMatch.Success)
            return (AmendmentOperationType.Modification, letterModificationMatch.Value);

            throw new InvalidOperationException("Unable to determine AmendmentOperationType or extract NewObject from content.");
        }
        private AmendmentObjectType DetermineAmendmentObjectType(string target)
        {
            if (Regex.IsMatch(target, @"art\."))
                return AmendmentObjectType.Article;

            if (Regex.IsMatch(target, @"ust\."))
                return AmendmentObjectType.Subsection;

            if (Regex.IsMatch(target, @"pkt"))
                return AmendmentObjectType.Point;

            if (Regex.IsMatch(target, @"lit\."))
                return AmendmentObjectType.Letter;

            if (Regex.IsMatch(target, @"tiret"))
                return AmendmentObjectType.Tiret;

            return AmendmentObjectType.None;
        }
        private List<string> ParseTargets(string content)
        {
            var targets = new List<string>();

            // Wyodrębnij <newObject> za pomocą regexów z TryParseAmendingOperation
            string? newObject = ExtractNewObject(content);

            if (string.IsNullOrEmpty(newObject))
                return targets;

            // Parsuj <newObject> pod kątem zakresów, list i pojedynczych targetów
            // Obsługa zakresów, np. "24-26"
            var rangeMatch = Regex.Match(newObject, @"(\d+[a-zA-Z]?)-(\d+[a-zA-Z]?)");
            if (rangeMatch.Success)
            {
                var start = rangeMatch.Groups[1].Value;
                var end = rangeMatch.Groups[2].Value;
                targets.AddRange(GenerateRange(start, end));
            }

            // Obsługa listy, np. "4a i 4b"
            var listMatch = Regex.Match(newObject, @"((\d+[a-zA-Z]?)\s(i\s\d+[a-zA-Z]?)+)");
            if (listMatch.Success)
            {
                var items = listMatch.Groups[1].Value.Split(new[] { " i " }, StringSplitOptions.RemoveEmptyEntries);
                targets.AddRange(items.Select(item => item.Trim()));
            }

            // Obsługa pojedynczego targetu, np. "3b"
            var singleTargetMatch = Regex.Match(newObject, @"\b(\d+[a-zA-Z]?)\b");
            if (singleTargetMatch.Success && !targets.Contains(singleTargetMatch.Groups[1].Value))
            {
                targets.Add(singleTargetMatch.Groups[1].Value);
            }

            return targets;
        }
        private string? ExtractNewObject(string content)
        {
            // Sprawdź różne wzorce, aby wyodrębnić <newObject>
            var repealMatch = Regex.Match(content, @"uchyla się (?<newObject>.*?)(?=[;.,]$)");
            if (repealMatch.Success)
                return repealMatch.Groups["newObject"].Value;

            var insertionMatch = Regex.Match(content, @"dodaje się (?<newObject>.*?) w brzmieniu:");
            if (insertionMatch.Success)
                return insertionMatch.Groups["newObject"].Value;

            var modificationMatch = Regex.Match(content, @"\b(art\.|ust\.|pkt|lit\.)\s*(?<newObject>\d+[a-zA-Z]?)\b(?=.*otrzymuje brzmienie:)");
            if (modificationMatch.Success)
                return modificationMatch.Groups["newObject"].Value;

            return null;
        }
        private List<string> GenerateRange(string start, string end)
        {
            var range = new List<string>();

            // Obsługa zakresów alfanumerycznych, np. 2g-2l
            if (char.IsLetter(start.Last()) && char.IsLetter(end.Last()) && start[..^1] == end[..^1])
            {
                var prefix = start[..^1];
                var startChar = start.Last();
                var endChar = end.Last();

                for (char c = startChar; c <= endChar; c++)
                {
                    range.Add($"{prefix}{c}");
                }
            }
            else
            {
                // Obsługa zakresów numerycznych, np. 24-26
                if (int.TryParse(start, out var startNum) && int.TryParse(end, out var endNum))
                {
                    for (int i = startNum; i <= endNum; i++)
                    {
                        range.Add(i.ToString());
                    }
                }
            }

            return range;
        }
    }
}