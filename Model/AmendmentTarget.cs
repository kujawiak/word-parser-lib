using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace WordParserLibrary.Model
{
    public class AmendmentTarget
    {
        public string Target { get; set; } = string.Empty;
        public AmendmentObjectType ObjectType { get; set; }
        public AmendmentOperationType OperationType { get; set; }
    }
}