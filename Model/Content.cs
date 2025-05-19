using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace WordParserLibrary.Model
{
    public enum AmendmentType
    {
        INSERTION,
        DELETION,
        MODIFICATION
    }

    public class TextSegment
    {
        public string Text { get; set; }
    }

    public class Reference : TextSegment
    {
        public int Artykul { get; set; }
        public int? Punkt { get; set; } // Nullable, bo może nie być punktu
    }

    public class AmendmentSegment : TextSegment
    {
        public AmendmentType Amendment { get; set; }
        public string InsertedText { get; set; } // Nowe pole na dodany tekst
        public string AfterWord { get; set; } // Po jakim wyrazie dodano
    }

    public class Content
    {
        public List<TextSegment> Segments { get; set; } = new List<TextSegment>();
        public Content(BaseEntity entity)
        {
            var input = entity.ContentText;
            int pos = 0;
            while (pos < input.Length)
            {
                int artIndex = input.IndexOf("art.", pos, StringComparison.OrdinalIgnoreCase);
                if (artIndex == -1)
                {
                    // Brak kolejnego "art." - dodaj resztę jako TextSegment
                    if (pos < input.Length)
                    {
                        Segments.Add(new TextSegment { Text = input.Substring(pos) });
                    }
                    break;
                }

                // Dodaj tekst przed "art." jako TextSegment
                if (artIndex > pos)
                {
                    Segments.Add(new TextSegment { Text = input.Substring(pos, artIndex - pos) });
                }

                // Rozpoznaj "art. <numer> [w pkt <numer>]"
                var artMatch = Regex.Match(input.Substring(artIndex), @"^art\. (\d+)(?: w pkt (\d+))?", RegexOptions.IgnoreCase);
                if (artMatch.Success)
                {
                    Segments.Add(new Reference
                    {
                        Text = artMatch.Value,
                        Artykul = int.Parse(artMatch.Groups[1].Value),
                        Punkt = artMatch.Groups[2].Success ? int.Parse(artMatch.Groups[2].Value) : (int?)null
                    });
                    pos = artIndex + artMatch.Length;
                }
                else
                {
                    // Jeśli nie pasuje, potraktuj "art." jako zwykły tekst
                    Segments.Add(new TextSegment { Text = "art." });
                    pos = artIndex + 4;
                }
            }
            // Rozpoznanie "art. <numer> [w pkt <numer>]"
            // var artMatch = Regex.Match(input, @"art\. (\d+)(?: w pkt (\d+))?");
            // if (artMatch.Success)
            // {
            //     Segments.Add(new Reference
            //     {
            //         Text = artMatch.Value,
            //         Artykul = int.Parse(artMatch.Groups[1].Value),
            //         Punkt = artMatch.Groups[2].Success ? int.Parse(artMatch.Groups[2].Value) : (int?)null
            //     });
            // }

            // // Rozpoznanie "po wyrazie „...” dodaje się wyrazy „...”"
            // var insertMatch = Regex.Match(input, @"po wyrazie „([^”]+)” dodaje się wyrazy? „([^”]+)”");
            // if (insertMatch.Success)
            // {
            //     Segments.Add(new AmendmentSegment
            //     {
            //         Text = insertMatch.Value,
            //         Amendment = AmendmentType.INSERTION,
            //         AfterWord = insertMatch.Groups[1].Value,
            //         InsertedText = insertMatch.Groups[2].Value
            //     });
            // }
        }
    }
}