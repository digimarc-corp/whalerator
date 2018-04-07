using System;
using System.Collections.Generic;
using System.Text;

namespace Whalerator.Support
{
    public class Token
    {
        private ICryptoAlgorithm _Algorithm;

        public Token(ICryptoAlgorithm algorithm)
        {
            _Algorithm = algorithm;
        }

        public string Generate(object payload)
        {
            throw new NotImplementedException();
        }
    }
}
