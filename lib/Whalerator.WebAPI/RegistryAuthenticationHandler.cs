using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Whalerator.WebAPI.Contracts;

namespace Whalerator.WebAPI
{
    public class RegistryAuthenticationHandler : AuthenticationHandler<RegistryAuthenticationOptions>
    {
        public RegistryAuthenticationHandler(IOptionsMonitor<RegistryAuthenticationOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock)
            : base(options, logger, encoder, clock) { }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (string.IsNullOrEmpty(Request.Headers["Authorization"]))
            {
                return Task.FromResult(AuthenticateResult.Fail("{ error: \"Authorization is required.\"}"));
            }
            else
            {
                try
                {
                    var header = AuthenticationHeaderValue.Parse(Request.Headers["Authorization"]);
                    var token = Jose.JWT.Decode<Token>(header.Parameter, Options.Algorithm.ToDotNetRSA());
                    var json = Options.Algorithm.Decrypt(token.Crd);
                    var credentials = JsonConvert.DeserializeObject<RegistryCredentials>(json);
                    var principal = new ClaimsPrincipal(credentials.ToClaimsIdentity());
                    var ticket = new AuthenticationTicket(principal, "token");

                    return Task.FromResult(AuthenticateResult.Success(ticket));
                }
                catch
                {
                    return Task.FromResult(AuthenticateResult.Fail("{ error: \"The supplied token is invalid.\" }"));
                }
            }
        }

        protected override Task HandleChallengeAsync(AuthenticationProperties properties)
        {
            /*
            var parts = new List<string>();
            if (!string.IsNullOrEmpty(Options.Realm)) { parts.Add($"realm=\"{Options.Realm}\""); }
            if (!string.IsNullOrEmpty(Options.Service)) { parts.Add($"service=\"{Options.Service}\""); }

            Response.Headers["WWW-Authenticate"] = $"Bearer {string.Join(',', parts)}";
            */
            return base.HandleChallengeAsync(properties);
        }
    }
}
