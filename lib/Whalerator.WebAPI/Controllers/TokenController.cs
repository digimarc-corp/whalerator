using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Whalerator.Client;
using Whalerator.Support;

namespace Whalerator.WebAPI.Controllers
{
    [Produces("application/json")]
    [Route("api/Token")]
    public class TokenController : Controller
    {
        private ICryptoAlgorithm _Crypto;
        private ICache<Authorization> _Cache;

        public ILogger<TokenController> Logger { get; }
        public Config Config { get; }

        public TokenController(ICryptoAlgorithm crypto, ICache<Authorization> cache, ILogger<TokenController> logger, Config config)
        {
            _Crypto = crypto;
            _Cache = cache;
            Logger = logger;
            Config = config;
        }

        [HttpPost]
        public IActionResult Post([FromBody]RegistryCredentials credentials)
        {
            // must specify a registry
            if (string.IsNullOrEmpty(credentials.Registry)) { return Unauthorized(); }

            // deny requests for foreign instances, if configured
            if (!string.IsNullOrEmpty(Config.Catalog?.Registry) && credentials.Registry.ToLowerInvariant() != Config.Catalog.Registry.ToLowerInvariant())
            {
                return Unauthorized();
            }
            try
            {
                var handler = new AuthHandler(_Cache);
                handler.Login(credentials.Registry, credentials.Username, credentials.Password);
                var json = JsonConvert.SerializeObject(credentials);
                var cipherText = _Crypto.Encrypt(json);

                return Ok(new
                {
                    token = Jose.JWT.Encode(new Token
                    {
                        Crd = cipherText,
                        Usr = credentials.Username,
                        Reg = credentials.Registry,
                        Iat = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                        Exp = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + Config.Security.TokenLifetime
                    }, _Crypto.ToDotNetRSA(), Jose.JwsAlgorithm.RS256)
                });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error authenticating token request.");
                return Unauthorized();
            }
        }
    }
}
