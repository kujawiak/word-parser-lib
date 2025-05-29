using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Math;

namespace WordParserLibrary.Model
{

    public class TextSegment
    {
        public string Text { get; set; } = string.Empty;
    }

    public class Reference : TextSegment
    {
        required public string Article { get; set; }
        public string? Subsection { get; set; }
        public string? Point { get; set; }
        public string? Letter { get; set; }
        public string? Tiret { get; set; }
    }

    public class Content
    {
        BaseEntity Entity { get; set; }
        public Content(BaseEntity entity)
        {
            Entity = entity;
        }
        //w art. 1 w pkt 3 kropkę zastępuje się średnikiem i dodaje się pkt 4 w brzmieniu:
    }
}