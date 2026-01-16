using System;
using System.Collections.Generic;

namespace WordParserLibrary.Model
{
    public interface IAmendable
    {
        List<Amendment> Amendments { get; set; }
    }
}