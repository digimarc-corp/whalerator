using System;
using System.Collections.Generic;
using System.Text;

namespace MobyInspector
{
    public static class ExtensionMethods
    {
        // Cannot use Equals(this string, etc) because of ambiguity with object.Equals(...)
        public static bool Matches(this string str, string value, bool ignoreCase)
        {            
            return ignoreCase ? str.Equals(value, StringComparison.InvariantCultureIgnoreCase) : str == value;
        }

    }
}
