using System;
using System.Collections.Generic;
using System.Xml.Linq;
using DocumentFormat.OpenXml.Wordprocessing;

namespace WordParserLibrary.Model
{
    public class Amendment : BaseEntity, IXmlConvertible
    {
        public AmendmentOperation Operation { get; set; }
        public string StyleValue { get; set; } = string.Empty;
        public Amendment(Paragraph paragraph, BaseEntity parent) : base(paragraph, parent)
        {
            Article = parent.Article ?? (parent as Article);
            Subsection = parent.Subsection ?? (parent as Subsection);
            Point = parent.Point ?? (parent as Point);
            Letter = parent.Letter ?? (parent as Letter);
            Tiret = parent.Tiret ?? (parent as Tiret);
            Parent = parent;
            Paragraph = paragraph;
            //TODO: For testing purposes only
            Parent?.Article?.AmendmentList.Add(Context);
            StyleValue = "Z";
        }

        public XElement ToXML(bool generateGuids)
        {
            var amendmentElement = new XElement("amendment",
                //new XAttribute("reference", LegalReference.ToString()),
                new XElement("content", ContentText)
            );
            return amendmentElement;
        }
    }
}