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

        public ClaimsIdentity ToClaimsIdentity()
        {
            var claims = new List<Claim>();
            claims.Add(new Claim(nameof(Registry), Registry, ClaimValueTypes.String));
            claims.Add(new Claim(nameof(Username), Username, ClaimValueTypes.String));
            claims.Add(new Claim(nameof(Password), Password, ClaimValueTypes.String));

            var identity = new ClaimsIdentity(claims, "RegistryCredentials");
            return identity;
        }
    }
}
