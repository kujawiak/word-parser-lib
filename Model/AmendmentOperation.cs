using System.Xml.Linq;

namespace WordParserLibrary.Model
{
    public class AmendmentOperation : IXmlConvertible
    {
        public AmendmentOperationType Type { get; set; }
        public LegalReference AmendmentTarget { get; set; } = new LegalReference();
        public string AmendmentObject { get; set; } = string.Empty;
        public AmendmentObjectType AmendmentObjectType { get; set; }
        public List<Amendment> Amendments { get; set; } = new List<Amendment>();

        public override string ToString()
        {
            return $"{Type}, {AmendmentTarget}, {AmendmentObject}";
        }

        public XElement ToXML()
        {
            var newElement = new XElement("amendmentOperation",
                new XElement("type", Type.ToString()),
                new XElement("target", AmendmentTarget.ToString()),
                new XElement("object", AmendmentObject),
                new XElement("objectType", AmendmentObjectType.ToFriendlyString())
            );
            if (Amendments.Any())
            {
                var amendmentsElement = new XElement("amendments");
                newElement.Add(amendmentsElement);
                foreach (var amendment in Amendments)
                {
                    amendmentsElement.Add(amendment.ToXML());
                }
            }
            return newElement;
        }
    }
}