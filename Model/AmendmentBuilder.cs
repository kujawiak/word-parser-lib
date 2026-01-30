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

            if (baseEntity == null || string.IsNullOrEmpty(baseEntity.ContentText))
            {
                Console.WriteLine("BaseEntity or its Content cannot be null or empty.");
                return new List<AmendmentOperation>();
            }

            var amendmentOperations = new List<AmendmentOperation>();

            var ao = FindAmendmentOperation(baseEntity.ContentText);

            var targets = ParseTargets(ao);

            // Utwórz operacje nowelizujące dla każdego targetu
            foreach (var target in targets)
            {
                var amendmentOperation = new AmendmentOperation
                {
                    Type = ao.OperationType,
                    AmendmentTarget = baseEntity.LegalReference,
                    AmendmentObject = target,
                    AmendmentObjectType = ao.ObjectType
                };
                amendmentOperations.Add(amendmentOperation);
            }
            
            if (amendmentOperations.Count == 1)
            {
                foreach (var amendment in amendments)
                {
                    amendment.Operation = amendmentOperations.First();
                }
                amendmentOperations.First().Amendments.AddRange(amendments);
            }
            else if (amendmentOperations.Count > 1)
            {
                // W przypadku wielu operacji nowelizujących, przypisz odpowiednie poprawki do każdej operacji
                var allAmendmets = amendments.ToList();
                var firstAmendment = allAmendmets.FirstOrDefault();
                var style = firstAmendment?.Paragraph != null ? firstAmendment.Paragraph.StyleId() : null;
                foreach (var operation in amendmentOperations)
                {
                    if (operation.Type == AmendmentOperationType.Repeal)
                    {
                        break;
                    }
                    //TODO: Zastąpić to tymczasowe rozwiązanie bardziej kompleksowym podejściem
                    //TODO: Ewentualnie zapewnić weryfikację dokumentu przed jego przetworzeniem
                    if (allAmendmets.Count == 0)
                        break;
                    var first = allAmendmets.First();
                    var coZmieniane = operation.AmendmentObjectType.ToStyleValueString();
                    var czymZmieniane = first.Parent.Number?.Value ?? "unknown";
                    if (czymZmieniane == "ART" || czymZmieniane == "UST" || czymZmieniane == "PKT")
                    {
                        czymZmieniane = "";
                    }
                   
                    first.StyleValue = "Z"+czymZmieniane+coZmieniane;
                    //first;
                    first.Operation = operation;
                    operation.Amendments.Add(first);
                    allAmendmets.RemoveAt(0);
                    while (allAmendmets.Any())
                    {
                        var amendment = allAmendmets.First();
                        if (amendment.Paragraph?.StyleId() == style)
                            break;

                        amendment.Operation = operation;
                        operation.Amendments.Add(amendment);
                        allAmendmets.RemoveAt(0);
                    }
                }
            }
            return amendmentOperations;
        }
        private AmendmentTarget FindAmendmentOperation(string content)
        {
            var repealMatch = Regex.Match(content, @"uchyla się (?<newObject>.*?)(?=[;.,]$)");
            if (repealMatch.Success)
            //return (AmendmentOperationType.Repeal, repealMatch.Groups["newObject"].Value);
                return new AmendmentTarget() {
                    OperationType = AmendmentOperationType.Repeal,
                    ObjectType = DetermineAmendmentObjectType(repealMatch.Groups["newObject"].Value),
                    Target = repealMatch.Groups["newObject"].Value
                };

            var insertionMatch = Regex.Match(content, @"dodaje się (?<newObject>.*?) w brzmieniu:");
            if (insertionMatch.Success)
            //return (AmendmentOperationType.Insertion, insertionMatch.Groups["newObject"].Value);
                return new AmendmentTarget() {
                    OperationType = AmendmentOperationType.Insertion,
                    ObjectType = DetermineAmendmentObjectType(insertionMatch.Groups["newObject"].Value),
                    Target = insertionMatch.Groups["newObject"].Value
                };

            var modificationMatch = Regex.Match(content, @"\b(art\.|ust\.|pkt)\s*\d+[a-zA-Z]?\b(?=.*otrzymuj[e,ą] brzmienie:)");
            if (modificationMatch.Success)
            // return (AmendmentOperationType.Modification, modificationMatch.Value);
                return new AmendmentTarget()
                {
                    OperationType = AmendmentOperationType.Modification,
                    ObjectType = DetermineAmendmentObjectType(modificationMatch.Value),
                    Target = modificationMatch.Value
                };

            var letterModificationMatch = Regex.Match(content, @"lit\.\s*[a-zA-Z]+(?=.*otrzymuj[e,ą] brzmienie:)");
            if (letterModificationMatch.Success)
            // return (AmendmentOperationType.Modification, letterModificationMatch.Value);
                return new AmendmentTarget() {
                    OperationType = AmendmentOperationType.Modification,
                    ObjectType = DetermineAmendmentObjectType(letterModificationMatch.Value),
                    Target = letterModificationMatch.Value
                };

            return new AmendmentTarget() {
                OperationType = AmendmentOperationType.Error,
                ObjectType = AmendmentObjectType.None,
                Target = "Unable to determine AmendmentOperationType or extract NewObject from content."
            };
            //throw new InvalidOperationException("Unable to determine AmendmentOperationType or extract NewObject from content.");
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
        private List<string> ParseTargets(AmendmentTarget target)
        {
            var targets = new List<string>();

            // Parsuj <newObject> pod kątem zakresów, list i pojedynczych targetów
            // Obsługa zakresów, np. "24-26"
            var rangeMatch = Regex.Match(target.Target, @"(\d+[a-zA-Z]?)-(\d+[a-zA-Z]?)");
            if (rangeMatch.Success)
            {
                var start = rangeMatch.Groups[1].Value;
                var end = rangeMatch.Groups[2].Value;
                targets.AddRange(GenerateRange(start, end));
                return targets;
            }

            // Obsługa listy, np. "4a i 4b"
            var listMatch = Regex.Match(target.Target, @"((\d+[a-zA-Z]?)\s(i\s\d+[a-zA-Z]?)+)");
            if (listMatch.Success)
            {
                var items = listMatch.Groups[1].Value.Split(new[] { " i " }, StringSplitOptions.RemoveEmptyEntries);
                targets.AddRange(items.Select(item => item.Trim()));
                return targets;
            }

            // Obsługa paragrafu z wieloma elementami, np. "uchyla się art. 126 i art. 126a;"
            var multipleTargetsMatch = Regex.Matches(target.Target, @"\b((art\.|ust\.|pkt|lit\.)\s*\d+[a-zA-Z]?)\b");
            if (multipleTargetsMatch.Count > 0)
            {
                foreach (Match match in multipleTargetsMatch)
                {
                    var t = match.Value.Trim();
                    if (!targets.Contains(t))
                    {
                        var cleanedTarget = Regex.Replace(t, @"^(art\.|ust\.|pkt|lit\.)\s*", string.Empty);
                        targets.Add(cleanedTarget);
                    }
                }
                return targets;
            }

            // Obsługa pojedynczego targetu, np. "3b"
            var singleTargetMatch = Regex.Match(target.Target, @"\b(\d+[a-zA-Z]?)\b");
            if (singleTargetMatch.Success && !targets.Contains(singleTargetMatch.Groups[1].Value))
            {
                targets.Add(singleTargetMatch.Groups[1].Value);
            }

            return targets;
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