using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Whalerator.WebAPI
{
    public static class ExtensionMethods
    {
        public static RegistryCredentials ToRegistryCredentials(this ClaimsPrincipal principal)
        {
            return new RegistryCredentials
            {
                Registry = principal.Claims.FirstOrDefault(c => c.Type.Equals(nameof(RegistryCredentials.Registry)))?.Value,
                Username = principal.Claims.FirstOrDefault(c => c.Type.Equals(nameof(RegistryCredentials.Username)))?.Value,
                Password = principal.Claims.FirstOrDefault(c => c.Type.Equals(nameof(RegistryCredentials.Password)))?.Value
            };
        }

        public static ClaimsIdentity ToClaimsIdentity(this RegistryCredentials credentials)
        {
            var claims = new List<Claim>();
            claims.Add(new Claim(nameof(RegistryCredentials.Registry), credentials.Registry, ClaimValueTypes.String));
            if (!string.IsNullOrEmpty(credentials.Username)) { claims.Add(new Claim(nameof(RegistryCredentials.Username), credentials.Username, ClaimValueTypes.String)); }
            if (!string.IsNullOrEmpty(credentials.Password)) { claims.Add(new Claim(nameof(RegistryCredentials.Password), credentials.Password, ClaimValueTypes.String)); }

            var identity = new ClaimsIdentity(claims, "RegistryCredentials");
            return identity;
        }
    }
}
