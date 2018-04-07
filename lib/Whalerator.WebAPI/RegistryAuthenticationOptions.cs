using Microsoft.AspNetCore.Authentication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Whalerator.Support;

namespace Whalerator.WebAPI
{
    public class RegistryAuthenticationOptions : AuthenticationSchemeOptions
    {
        public ICryptoAlgorithm Algorithm { get; set; }
    }
}
