using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Whalerator.Client
{
    public class RegistryException : Exception
    {
        public RegistryException() { }

        public RegistryException(string message) : base(message) { }

        public RegistryException(string message, Exception innerException) : base(message, innerException) { }

        protected RegistryException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
