using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Whalerator.WebAPI.Contracts
{
    public class RegistryCredentials
    {
        public string Registry { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }

        public static RegistryCredentials FromClaimsPrincipal(ClaimsPrincipal principal)
        {
            return new RegistryCredentials
            {
                Registry = principal.Claims.FirstOrDefault(c => c.Type.Equals(nameof(Registry)))?.Value,
                Username = principal.Claims.FirstOrDefault(c => c.Type.Equals(nameof(Username)))?.Value,
                Password = principal.Claims.FirstOrDefault(c => c.Type.Equals(nameof(Password)))?.Value
            };
        }

        public ClaimsIdentity ToClaimsIdentity()
        {
            var claims = new List<Claim>();
            claims.Add(new Claim(nameof(Registry), Registry, ClaimValueTypes.String));
            if (!string.IsNullOrEmpty(Username)) { claims.Add(new Claim(nameof(Username), Username, ClaimValueTypes.String)); }
            if (!string.IsNullOrEmpty(Password)) { claims.Add(new Claim(nameof(Password), Password, ClaimValueTypes.String)); }

            var identity = new ClaimsIdentity(claims, "RegistryCredentials");
            return identity;
        }
    }
}
